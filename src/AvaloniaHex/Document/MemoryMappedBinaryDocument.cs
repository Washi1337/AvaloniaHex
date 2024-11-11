using System.IO.MemoryMappedFiles;

namespace AvaloniaHex.Document;

/// <summary>
/// Represents a binary document that is backed by a file that is mapped into memory.
/// </summary>
public class MemoryMappedBinaryDocument : IBinaryDocument
{
    /// <inheritdoc />
    public event EventHandler<BinaryDocumentChange>? Changed;

    private readonly MemoryMappedViewAccessor _accessor;
    private readonly bool _leaveOpen;

    /// <summary>
    /// Opens a file as a memory mapped document.
    /// </summary>
    /// <param name="filePath">The file to memory map.</param>
    public MemoryMappedBinaryDocument(string filePath)
        : this(MemoryMappedFile.CreateFromFile(filePath, FileMode.OpenOrCreate), false, false)
    {
    }

    /// <summary>
    /// Wraps a memory mapped file in a document.
    /// </summary>
    /// <param name="file">The file to use as a backing storage.</param>
    /// <param name="leaveOpen"><c>true</c> if <paramref name="file"/> should be kept open on disposing, <c>false</c> otherwise.</param>
    public MemoryMappedBinaryDocument(MemoryMappedFile file, bool leaveOpen)
        : this(file, leaveOpen, false)
    {
    }

    /// <summary>
    /// Wraps a memory mapped file in a document.
    /// </summary>
    /// <param name="file">The file to use as a backing storage.</param>
    /// <param name="leaveOpen"><c>true</c> if <paramref name="file"/> should be kept open on disposing, <c>false</c> otherwise.</param>
    /// <param name="isReadOnly"><c>true</c> if the document can be edited, <c>false</c> otherwise.</param>
    public MemoryMappedBinaryDocument(MemoryMappedFile file, bool leaveOpen, bool isReadOnly)
    {
        File = file;
        _leaveOpen = leaveOpen;
        _accessor = file.CreateViewAccessor();

        // Yuck! But this seems to be the only way to get the length from a MemoryMappedFile.
        using var stream = file.CreateViewStream();
        Length = (ulong) stream.Length;

        ValidRanges = new BitRangeUnion([new BitRange(0, Length)]).AsReadOnly();
        IsReadOnly = isReadOnly;
    }

    /// <summary>
    /// Gets the underlying memory mapped file that is used as a backing storage for this document.
    /// </summary>
    public MemoryMappedFile File { get; }

    /// <inheritdoc />
    public ulong Length { get; }

    /// <inheritdoc />
    public bool IsReadOnly { get; }

    /// <inheritdoc />
    public bool CanInsert => false;

    /// <inheritdoc />
    public bool CanRemove => false;

    /// <inheritdoc />
    public IReadOnlyBitRangeUnion ValidRanges { get; }

    /// <inheritdoc />
    public void ReadBytes(ulong offset, Span<byte> buffer)
    {
        _accessor.SafeMemoryMappedViewHandle.ReadSpan(offset, buffer);
    }

    /// <inheritdoc />
    public void WriteBytes(ulong offset, ReadOnlySpan<byte> buffer)
    {
        if (IsReadOnly)
            throw new InvalidOperationException("Document is read-only.");

        _accessor.SafeMemoryMappedViewHandle.WriteSpan(offset, buffer);
        OnChanged(new BinaryDocumentChange(BinaryDocumentChangeType.Modify, new BitRange(offset, offset + (ulong) buffer.Length)));
    }

    /// <inheritdoc />
    public void InsertBytes(ulong offset, ReadOnlySpan<byte> buffer)
    {
        if (IsReadOnly)
            throw new InvalidOperationException("Document is read-only.");

        throw new InvalidOperationException("Document cannot be resized.");
    }

    /// <inheritdoc />
    public void RemoveBytes(ulong offset, ulong length)
    {
        if (IsReadOnly)
            throw new InvalidOperationException("Document is read-only.");

        throw new InvalidOperationException("Document cannot be resized.");
    }

    /// <inheritdoc />
    public void Flush() => _accessor.Flush();

    /// <summary>
    /// Fires the <see cref="Changed"/> event.
    /// </summary>
    /// <param name="e">The event arguments describing the change.</param>
    protected virtual void OnChanged(BinaryDocumentChange e) => Changed?.Invoke(this, e);

    /// <inheritdoc />
    public void Dispose()
    {
        _accessor.Dispose();

        if (!_leaveOpen)
            File.Dispose();
    }
}