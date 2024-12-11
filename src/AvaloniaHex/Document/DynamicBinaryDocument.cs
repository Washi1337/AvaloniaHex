using System.Runtime.InteropServices;

namespace AvaloniaHex.Document;

/// <summary>
/// Represents a binary document that can be dynamically resized.
/// </summary>
public class DynamicBinaryDocument : IBinaryDocument
{
    // TODO: List<byte> should be replaced with something that is more efficient for insert/remove operations
    //       such as a Rope or gap-buffer.
    private readonly List<byte> _data;

    private readonly BitRangeUnion _validRanges;

    /// <inheritdoc />
    public event EventHandler<BinaryDocumentChange>? Changed;

    /// <summary>
    /// Creates a new empty dynamic binary document.
    /// </summary>
    public DynamicBinaryDocument()
    {
        _data = new List<byte>();
        _validRanges = new BitRangeUnion();
        ValidRanges = _validRanges.AsReadOnly();
    }

    /// <summary>
    /// Creates a new dynamic binary document with the provided initial data.
    /// </summary>
    /// <param name="initialData">The data to initialize the document with.</param>
    public DynamicBinaryDocument(byte[] initialData)
    {
        _data = new List<byte>(initialData);
        _validRanges = new BitRangeUnion([new BitRange(0ul, (ulong) initialData.Length)]);
        ValidRanges = _validRanges.AsReadOnly();
    }

    /// <inheritdoc />
    public ulong Length => (ulong) _data.Count;

    /// <inheritdoc />
    public bool IsReadOnly { get; set; }

    /// <inheritdoc />
    public bool CanInsert { get; set; } = true;

    /// <inheritdoc />
    public bool CanRemove { get; set; } = true;

    /// <inheritdoc />
    public IReadOnlyBitRangeUnion ValidRanges { get; }

    private void AssertIsWriteable()
    {
        if (IsReadOnly)
            throw new InvalidOperationException("Document is read-only.");
    }

    /// <inheritdoc />
    public void ReadBytes(ulong offset, Span<byte> buffer)
    {
        CollectionsMarshal.AsSpan(_data).Slice((int) offset, buffer.Length).CopyTo(buffer);
    }

    /// <inheritdoc />
    public void WriteBytes(ulong offset, ReadOnlySpan<byte> buffer)
    {
        AssertIsWriteable();

        buffer.CopyTo(CollectionsMarshal.AsSpan(_data).Slice((int) offset, buffer.Length));
        OnChanged(new BinaryDocumentChange(BinaryDocumentChangeType.Modify, new BitRange(offset, offset + (ulong) buffer.Length)));
    }

    /// <inheritdoc />
    public void InsertBytes(ulong offset, ReadOnlySpan<byte> buffer)
    {
        AssertIsWriteable();

        if (!CanInsert)
            throw new InvalidOperationException("Data cannot be inserted into the document.");

        _data.InsertRange((int) offset, buffer.ToArray());
        _validRanges.Add(new BitRange(_validRanges.EnclosingRange.End, _validRanges.EnclosingRange.End.AddBytes((ulong) buffer.Length)));

        OnChanged(new BinaryDocumentChange(BinaryDocumentChangeType.Insert, new BitRange(offset, offset + (ulong) buffer.Length)));
    }

    /// <inheritdoc />
    public void RemoveBytes(ulong offset, ulong length)
    {
        AssertIsWriteable();

        if (!CanRemove)
            throw new InvalidOperationException("Data cannot be removed from the document.");

        _data.RemoveRange((int) offset, (int) length);
        _validRanges.Remove(new BitRange(_validRanges.EnclosingRange.End.SubtractBytes(length), _validRanges.EnclosingRange.End));

        OnChanged(new BinaryDocumentChange(BinaryDocumentChangeType.Remove, new BitRange(offset, offset + length)));
    }

    /// <summary>
    /// Fires the <see cref="Changed"/> event.
    /// </summary>
    /// <param name="e">The event arguments describing the change.</param>
    protected virtual void OnChanged(BinaryDocumentChange e) => Changed?.Invoke(this, e);

    /// <inheritdoc />
    public void Flush()
    {
    }

    /// <inheritdoc />
    public void Dispose()
    {
    }

    /// <summary>
    /// Serializes the contents of the document into a byte array.
    /// </summary>
    /// <returns>The serialized contents.</returns>
    public byte[] ToArray() => _data.ToArray();
}