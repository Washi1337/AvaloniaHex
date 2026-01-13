namespace AvaloniaHex.Document;

/// <summary>
/// Represents a binary document that can be displayed in a hex editor.
/// </summary>
public interface IBinaryDocument : IDisposable
{
    /// <summary>
    /// Fires when the contents of the document has changed.
    /// </summary>
    event EventHandler<BinaryDocumentChange> Changed;

    /// <summary>
    /// Gets the total length of the document.
    /// </summary>
    ulong Length { get; }

    /// <summary>
    /// Gets a value indicating whether the document can be changed or not.
    /// </summary>
    bool IsReadOnly { get; }

    /// <summary>
    /// Gets a value indicating whether additional bytes can be inserted into the document.
    /// </summary>
    bool CanInsert { get; }

    /// <summary>
    /// Gets a value indicating whether bytes can be removed from the document.
    /// </summary>
    bool CanRemove { get; }

    /// <summary>
    /// Gets a collection of binary ranges in the document that are accessible/valid.
    /// </summary>
    IReadOnlyBitRangeUnion ValidRanges { get; }

    /// <summary>
    /// Reads bytes from the document at the provided offset.
    /// </summary>
    /// <param name="offset">The offset to start reading at.</param>
    /// <param name="buffer">The buffer to write the read data to.</param>
    void ReadBytes(ulong offset, Span<byte> buffer);

    /// <summary>
    /// Writes bytes to the document at the provided offset.
    /// </summary>
    /// <param name="offset">The offset to start writing at.</param>
    /// <param name="buffer">The data to write.</param>
    /// <exception cref="InvalidOperationException">Occurs when the document is read-only.</exception>
    void WriteBytes(ulong offset, ReadOnlySpan<byte> buffer);

    /// <summary>
    /// Inserts bytes to the document at the provided offset, extending the document.
    /// </summary>
    /// <param name="offset">The offset to start inserting at.</param>
    /// <param name="buffer">The data to insert.</param>
    /// <exception cref="InvalidOperationException">
    /// Occurs when the document is read-only or cannot insert bytes.
    /// </exception>
    void InsertBytes(ulong offset, ReadOnlySpan<byte> buffer);

    /// <summary>
    /// Writes bytes to the document at the provided offset.
    /// </summary>
    /// <param name="offset">The offset to start writing at.</param>
    /// <param name="length">The number of bytes to remove.</param>
    /// <exception cref="InvalidOperationException">
    /// Occurs when the document is read-only or cannot remove bytes.
    /// </exception>
    void RemoveBytes(ulong offset, ulong length);

    /// <summary>
    /// Flushes all buffered changes to the underlying persistent backing storage of the document.
    /// </summary>
    void Flush();
}

/// <summary>
/// Provides extensions to <see cref="IBinaryDocument"/>.
/// </summary>
public static class BinaryDocumentExtensions
{
    extension(IBinaryDocument document)
    {
        /// <summary>
        /// Reads bytes from the document at the provided offset.
        /// </summary>
        /// <param name="offset">The offset to start reading at.</param>
        /// <param name="count">The number of bytes to read.</param>
        /// <returns>The read bytes.</returns>
        public byte[] ReadBytes(ulong offset, int count)
        {
            byte[] result = new byte[count];
            document.ReadBytes(offset, result);
            return result;
        }
    }
}