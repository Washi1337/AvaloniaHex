using System.Diagnostics;
using System.Runtime.InteropServices;

namespace AvaloniaHex.Document;

/// <summary>
/// Represents a binary document that can be dynamically resized.
/// </summary>
public class DynamicBinaryDocument : IBinaryDocument
{
    // Implementation is based on a piece table with some small optimizations where we merge pieces during
    // contiguous insertions and removals of pieces (i.e., happens a lot when typing character by character).

    // TODO: support other backend storages (e.g., for files larger than 2GB)
    private byte[] _data;
    private readonly List<byte> _addBuffer = [];
    private readonly List<Piece> _pieces = [];
    private readonly BitRangeUnion _validRanges;

    /// <inheritdoc />
    public event EventHandler<BinaryDocumentChange>? Changed;

    /// <summary>
    /// Creates a new empty dynamic binary document.
    /// </summary>
    public DynamicBinaryDocument()
    {
        _data = [];
        Length = 0;
        _validRanges = new BitRangeUnion();
        ValidRanges = _validRanges.AsReadOnly();
    }

    /// <summary>
    /// Creates a new dynamic binary document with the provided initial data.
    /// </summary>
    /// <param name="initialData">The data to initialize the document with.</param>
    public DynamicBinaryDocument(byte[] initialData)
    {
        _data = [..initialData];
        Length = (ulong) initialData.Length;
        _validRanges = new BitRangeUnion([new BitRange(0ul, (ulong) initialData.Length)]);
        ValidRanges = _validRanges.AsReadOnly();

        InitializePieces();
    }

    /// <inheritdoc />
    public ulong Length { get; private set; }

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

    private void RefreshValidRange()
    {
        _validRanges.Clear();
        _validRanges.Add(new BitRange(0, Length));
    }

    private void InitializePieces()
    {
        _pieces.Clear();
        if (Length > 0)
            _pieces.Add(new Piece(PieceDataSource.Original, 0ul, (ulong) _data.Length));
    }

    private (int PieceIndex, ulong RelativeIndex) GetPieceIndex(ulong offset)
    {
        // TODO: binary search / piece tree for faster lookup?

        ulong pieceOffset = 0;
        var pieces = CollectionsMarshal.AsSpan(_pieces);
        for (int i = 0; i < pieces.Length; i++)
        {
            var piece = pieces[i];
            if (offset >= pieceOffset && offset <= pieceOffset + piece.Length)
                return (i, offset - pieceOffset);

            pieceOffset += piece.Length;
        }

        return (-1, 0);
    }

    private void ReadFromPiece(Piece piece, ulong index, Span<byte> buffer)
    {
        // TODO: support 64-bit addressing properly

        var source = piece.DataSource switch
        {
            PieceDataSource.Original => _data.AsSpan((int) (piece.StartIndex + index), buffer.Length),
            PieceDataSource.Add => CollectionsMarshal.AsSpan(_addBuffer).Slice((int) (piece.StartIndex + index), buffer.Length),
            _ => throw new ArgumentOutOfRangeException(nameof(piece))
        };

        source.CopyTo(buffer);
    }

    /// <inheritdoc />
    public void ReadBytes(ulong offset, Span<byte> buffer)
    {
        if (buffer.IsEmpty)
            return;
        if (Length - offset < (ulong) buffer.Length)
            throw new EndOfStreamException();

        var pieces = CollectionsMarshal.AsSpan(_pieces);
        (int pieceIndex, ulong relativeIndex) = GetPieceIndex(offset);
        while (!buffer.IsEmpty)
        {
            var piece = pieces[pieceIndex];
            Debug.Assert(piece.Length > 0);

            int count = (int) Math.Min((ulong) buffer.Length, piece.Length - relativeIndex);

            ReadFromPiece(_pieces[pieceIndex], relativeIndex, buffer[..count]);

            pieceIndex++;
            relativeIndex = 0;
            buffer = buffer[count..];
        }
    }

    /// <inheritdoc />
    public void WriteBytes(ulong offset, ReadOnlySpan<byte> buffer)
    {
        AssertIsWriteable();

        RemovePieces(offset, (ulong) buffer.Length);
        InsertPieces(offset, buffer);

        OnChanged(new BinaryDocumentChange(
            BinaryDocumentChangeType.Modify,
            new BitRange(offset, offset + (ulong) buffer.Length)
        ));
    }

    /// <inheritdoc />
    public void InsertBytes(ulong offset, ReadOnlySpan<byte> buffer)
    {
        AssertIsWriteable();
        ArgumentOutOfRangeException.ThrowIfGreaterThan(offset, Length);

        if (buffer.IsEmpty)
            return;

        InsertPieces(offset, buffer);

        Length += (ulong) buffer.Length;
        RefreshValidRange();

        OnChanged(new BinaryDocumentChange(
            BinaryDocumentChangeType.Insert,
            new BitRange(offset, offset + (ulong) buffer.Length)
        ));
    }

    private void InsertPieces(ulong offset, ReadOnlySpan<byte> buffer)
    {
        var newPiece = new Piece(PieceDataSource.Add, (ulong) _addBuffer.Count, (ulong) buffer.Length);
        _addBuffer.AddRange(buffer);

        // Base case: If the document is empty, then there are no pieces yet to split or merge.
        if (_pieces.Count == 0)
        {
            _pieces.Add(newPiece);
            return;
        }

        // Otherwise, figure out if we need to split any pieces and insert the new piece.
        (int pieceIndex, ulong relativeIndex) = GetPieceIndex(offset);
        var currentPiece = _pieces[pieceIndex];

        if (relativeIndex > 0)
        {
            if (relativeIndex == currentPiece.Length)
            {
                // We're inserting at the end of a piece: No need for splitting.

                // Check if we can merge with the current piece to save elements in the pieces array.
                // This is only possible if the current piece is situated at the end of the add-buffer and we're
                // appending to the same chunk.
                if (currentPiece.DataSource == PieceDataSource.Add
                    && currentPiece.StartIndex == (ulong) _addBuffer.Count - currentPiece.Length - (ulong) buffer.Length)
                {
                    _pieces[pieceIndex] = currentPiece with
                    {
                        Length = currentPiece.Length + newPiece.Length
                    };
                    return;
                }

                // Otherwise, just insert after the current piece.
                pieceIndex++;
            }
            else
            {
                // We're inserting in the middle of a piece: Split it and move to the beginning of the right piece.
                var (left, right) = currentPiece.Split(relativeIndex);
                _pieces[pieceIndex] = left;
                pieceIndex++;
                _pieces.Insert(pieceIndex, right);

                Debug.Assert(left.Length > 0);
                Debug.Assert(right.Length > 0);
            }
        }

        _pieces.Insert(pieceIndex, newPiece);
    }

    /// <inheritdoc />
    public void RemoveBytes(ulong offset, ulong length)
    {
        AssertIsWriteable();
        ArgumentOutOfRangeException.ThrowIfGreaterThan(length, Length - offset);

        if (length == 0)
            return;

        RemovePieces(offset, length);

        Length -= length;
        RefreshValidRange();

        OnChanged(new BinaryDocumentChange(
            BinaryDocumentChangeType.Remove,
            new BitRange(offset, offset + length)
        ));
    }

    private void RemovePieces(ulong offset, ulong length)
    {
        (int pieceIndex, ulong relativeIndex) = GetPieceIndex(offset);
        Debug.Assert(pieceIndex >= 0 && pieceIndex < _pieces.Count);

        while (length > 0)
        {
            var currentPiece = _pieces[pieceIndex];

            if (relativeIndex > 0)
            {
                // We're removing starting from the middle of the piece, split piece before the starting index.
                var (left, right) = _pieces[pieceIndex].Split(relativeIndex);
                _pieces[pieceIndex] = left;
                pieceIndex++;
                _pieces.Insert(pieceIndex, right);

                currentPiece = right;
                relativeIndex = 0;
            }

            if (currentPiece.Length > length)
            {
                // We're removing only a part of the piece starting at the beginning. Split it up and remove the left part.
                var (left, right) = _pieces[pieceIndex].Split(relativeIndex + length);
                _pieces[pieceIndex] = right;
                length -= left.Length;
            }
            else
            {
                // The entire piece is spanned by the length, remove it completely.
                _pieces.RemoveAt(pieceIndex);
                length -= currentPiece.Length;

                // Optimization: Remove also from add-buffer when possible to allow for merges later when inserting new pieces.
                if (currentPiece.DataSource == PieceDataSource.Add
                    && currentPiece.StartIndex == (ulong) _addBuffer.Count - currentPiece.Length)
                {
                    _addBuffer.RemoveRange(
                        _addBuffer.Count - (int) currentPiece.Length,
                        (int) currentPiece.Length
                    );
                }
            }
        }
    }

    /// <summary>
    /// Fires the <see cref="Changed"/> event.
    /// </summary>
    /// <param name="e">The event arguments describing the change.</param>
    protected virtual void OnChanged(BinaryDocumentChange e) => Changed?.Invoke(this, e);

    /// <inheritdoc />
    public void Flush()
    {
        _data = ToArray();
        _addBuffer.Clear();
        InitializePieces();
    }

    /// <inheritdoc />
    public void Dispose()
    {
    }

    /// <summary>
    /// Serializes the contents of the document into a byte array.
    /// </summary>
    /// <returns>The serialized contents.</returns>
    public byte[] ToArray()
    {
        if (Length == 0)
            return [];
        if (_pieces.Count == 0)
            return [.._data];

        byte[] result = new byte[Length];

        int offset = 0;
        foreach (var piece in _pieces)
        {
            ReadFromPiece(piece, 0, result.AsSpan(offset, (int) piece.Length));
            Debug.Assert(piece.Length > 0);
            offset += (int) piece.Length;
        }

        return result;
    }

    private enum PieceDataSource
    {
        Original,
        Add
    }

    private readonly record struct Piece(PieceDataSource DataSource, ulong StartIndex, ulong Length)
    {
        public (Piece, Piece) Split(ulong relativeIndex)
        {
            return (
                new Piece(DataSource, StartIndex, relativeIndex),
                new Piece(DataSource, StartIndex + relativeIndex, Length - relativeIndex)
            );
        }
    }
}