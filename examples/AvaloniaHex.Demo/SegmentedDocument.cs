using System;
using System.Collections.Generic;
using System.Linq;
using AvaloniaHex.Document;

namespace AvaloniaHex.Demo;

/// <summary>
/// Provides an example implementation of a custom binary document that consists of one or more disjoint segments.
/// </summary>
public class SegmentedDocument : IBinaryDocument
{
    public event EventHandler<BinaryDocumentChange>? Changed;

    private readonly Mapping[] _mappings;
    private readonly BitRangeUnion _ranges = new();

    public SegmentedDocument(IEnumerable<Mapping> mappings)
        : this(mappings.ToArray())
    {
    }

    public SegmentedDocument(params Mapping[] mappings)
    {
        _mappings = mappings.ToArray();

        foreach (var mapping in _mappings)
        {
            ulong oldLength = _ranges.EnclosingRange.ByteLength;
            _ranges.Add(mapping.Range);
            if (_ranges.EnclosingRange.ByteLength < oldLength + mapping.Range.ByteLength)
                throw new ArgumentException("Mappings are overlapping.");
        }

        Array.Sort(_mappings, (a, b) => a.Location.CompareTo(b.Location));

        ValidRanges = _ranges.AsReadOnly();
    }

    /// <inheritdoc />
    public ulong Length => _mappings[^1].Range.End.ByteIndex;

    /// <inheritdoc />
    public bool IsReadOnly => false;

    /// <inheritdoc />
    public bool CanInsert => false;

    /// <inheritdoc />
    public bool CanRemove => true;

    /// <inheritdoc />
    public IReadOnlyBitRangeUnion ValidRanges { get; }

    private bool TryGetMappingIndex(ulong offset, out int index)
    {
        // Linear (slow) lookup of mapping.

        for (var i = 0; i < _mappings.Length; i++)
        {
            if (_mappings[i].Range.Contains(new BitLocation(offset)))
            {
                index = i;
                return true;
            }

            if (_mappings[i].Range.Start.ByteIndex > offset)
            {
                index = i;
                return false;
            }
        }

        index = _mappings.Length;
        return false;
    }

    /// <inheritdoc />
    public void ReadBytes(ulong offset, Span<byte> buffer)
    {
        int bufferIndex = 0;
        while (bufferIndex < buffer.Length && offset < Length)
        {
            // Find mapped segment for this offset.
            if (!TryGetMappingIndex(offset, out int mappingIndex))
            {
                // It does not exist for this byte. Jump to next segment.
                if (mappingIndex >= _mappings.Length)
                    return;

                ulong nextStart = _mappings[mappingIndex].Location;
                ulong delta = nextStart - offset;
                offset = nextStart;
                bufferIndex += (int) delta;
                continue;
            }

            // Get the current segment and compute boundaries.
            var mapping = _mappings[mappingIndex];
            int remainingBytesToRead = buffer.Length - bufferIndex;
            int relativeOffset = (int) (offset - mapping.Location);
            int remainingAvailableBytes = mapping.Data.Length - relativeOffset;

            // Read the data.
            int actualLength = Math.Min(remainingBytesToRead, remainingAvailableBytes);
            mapping.Data.AsSpan(relativeOffset, actualLength).CopyTo(buffer[bufferIndex..]);

            // Increment pointers.
            bufferIndex += actualLength;
            offset += (ulong) actualLength;
        }
    }

    /// <inheritdoc />
    public void WriteBytes(ulong offset, ReadOnlySpan<byte> buffer)
    {
        // Get the segment to write to.
        if (!TryGetMappingIndex(offset, out int mappingIndex))
            return;

        // Get mapping and compute boundaries.
        var mapping = _mappings[mappingIndex];
        int relativeOffset = (int) (offset - mapping.Location);
        int availableBytes = mapping.Data.Length - relativeOffset;

        // Write
        int actualLength = Math.Min(availableBytes, buffer.Length);
        buffer[..actualLength].CopyTo(mapping.Data.AsSpan(relativeOffset, actualLength));

        // Notify for changes.
        OnChanged(new BinaryDocumentChange(BinaryDocumentChangeType.Modify, new BitRange(offset, offset + (ulong) actualLength)));
    }

    /// <inheritdoc />
    public void InsertBytes(ulong offset, ReadOnlySpan<byte> buffer) => throw new NotSupportedException();

    /// <inheritdoc />
    public void RemoveBytes(ulong offset, ulong length) => throw new NotSupportedException();

    protected virtual void OnChanged(BinaryDocumentChange e) => Changed?.Invoke(this, e);


    void IBinaryDocument.Flush()
    {
    }

    void IDisposable.Dispose()
    {
    }

    /// <summary>
    /// A single mapped segment.
    /// </summary>
    /// <param name="Location">The start address of the segment.</param>
    /// <param name="Data">The backing buffer.</param>
    public readonly record struct Mapping(ulong Location, byte[] Data)
    {
        /// <summary>
        /// Gets the memory range the segment spans.
        /// </summary>
        public BitRange Range => new(Location, Location + (ulong) Data.Length);
    }
}