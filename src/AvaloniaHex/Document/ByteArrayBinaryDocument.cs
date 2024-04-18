namespace AvaloniaHex.Document;

/// <summary>
/// Wraps a byte array into a binary document.
/// </summary>
public class ByteArrayBinaryDocument : IBinaryDocument
{
    /// <inheritdoc />
    public event EventHandler<BinaryDocumentChange>? Changed;

    private readonly byte[] _data;

    /// <summary>
    /// Creates a new byte array document.
    /// </summary>
    /// <param name="data">The initial data.</param>
    public ByteArrayBinaryDocument(byte[] data)
        : this(data, false)
    {
    }

    /// <summary>
    /// Creates a new byte array document.
    /// </summary>
    /// <param name="data">The initial data.</param>
    /// <param name="isReadOnly">Determines whether the document is read-only or not.</param>
    public ByteArrayBinaryDocument(byte[] data, bool isReadOnly)
    {
        IsReadOnly = isReadOnly;
        _data = data;
        ValidRanges = new ReadOnlyBitRangeUnion(new BitRangeUnion { new(0, Length) });
    }

    /// <summary>
    /// Gets the data stored in the document.
    /// </summary>
    public byte[] Data => _data;

    /// <inheritdoc />
    public ulong Length => (ulong) _data.Length;

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
        _data.AsSpan((int) offset, buffer.Length).CopyTo(buffer);
    }

    /// <inheritdoc />
    public void WriteBytes(ulong offset, ReadOnlySpan<byte> buffer)
    {
        if (IsReadOnly)
            throw new InvalidOperationException("Document is read-only.");

        buffer.CopyTo(_data.AsSpan((int) offset, buffer.Length));
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

    /// <summary>
    /// Fires the <see cref="Changed"/> event.
    /// </summary>
    /// <param name="e">The event arguments describing the change.</param>
    protected virtual void OnChanged(BinaryDocumentChange e) => Changed?.Invoke(this, e);
}