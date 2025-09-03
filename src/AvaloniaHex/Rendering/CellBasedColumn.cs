using Avalonia;
using Avalonia.Media.TextFormatting;
using AvaloniaHex.Document;
using AvaloniaHex.Editing;

namespace AvaloniaHex.Rendering;

/// <summary>
/// Represents a column that contains individual selectable cells.
/// </summary>
public abstract class CellBasedColumn : Column
{
    /// <summary>
    /// Gets the size of an individual selectable cell.
    /// </summary>
    public Size CellSize { get; private set; }

    /// <summary>
    /// Gets the amount of padding in between groups of cells.
    /// </summary>
    public abstract double GroupPadding { get; }

    /// <summary>
    /// Gets the number of bits represented by each cell.
    /// </summary>
    public abstract int BitsPerCell { get;  }

    /// <summary>
    /// Gets the number of cells that are within a single word.
    /// </summary>
    public abstract int CellsPerWord { get;  }

    /// <summary>
    /// Gets the bit index of the first, left-most, selectable cell.
    /// </summary>
    public int FirstBitIndex => 8 - BitsPerCell;

    /// <summary>
    /// Gets the total amount of cells in a single line.
    /// </summary>
    public int CellCount => (HexView?.ActualBytesPerLine * 8 ?? 0) / BitsPerCell;

    /// <summary>
    /// Gets the total width of a single word of cells.
    /// </summary>
    public double WordWidth => CellSize.Width * CellsPerWord;

    /// <summary>
    /// Gets the total amount of words in a single line.
    /// </summary>
    public int WordCount => CellCount / CellsPerWord;

    /// <summary>
    /// Gets the total width of the column.
    /// </summary>
    public override double Width => base.Width + WordCount * WordWidth + (WordCount - 1) * GroupPadding;

    /// <summary>
    /// Defines the <see cref="InvalidCellChar"/> property.
    /// </summary>
    public static readonly StyledProperty<char> InvalidCellCharProperty =
        AvaloniaProperty.Register<CellBasedColumn, char>(nameof(InvalidCellChar), '?');

    /// <summary>
    /// Gets or sets the character that is used for displaying invalid or inaccessible cells.
    /// </summary>
    public char InvalidCellChar
    {
        get => GetValue(InvalidCellCharProperty);
        set => SetValue(InvalidCellCharProperty, value);
    }

    /// <summary>
    /// Preprocesses the provided text input for insertion into the column.
    /// </summary>
    /// <param name="input">The input text to process.</param>
    /// <returns>The processed input.</returns>
    protected virtual string PrepareTextInput(string input) => input;

    /// <summary>
    /// Interprets a single character and writes it into the provided buffer.
    /// </summary>
    /// <param name="buffer">The buffer to modify.</param>
    /// <param name="bufferStart">The start address of the buffer.</param>
    /// <param name="writeLocation">The address to write to.</param>
    /// <param name="input">The textual input to write.</param>
    /// <returns><c>true</c> if the input was interpreted and written to the buffer, <c>false</c> otherwise.</returns>
    protected abstract bool TryWriteCell(Span<byte> buffer, BitLocation bufferStart, BitLocation writeLocation, char input);

    /// <summary>
    /// Processes textual input in the column.
    /// </summary>
    /// <param name="location">The location to insert at.</param>
    /// <param name="input">The textual input to process.</param>
    /// <param name="mode"></param>
    /// <returns><c>true</c> if the document was changed, <c>false</c> otherwise.</returns>
    public bool HandleTextInput(ref BitLocation location, string input, EditingMode mode)
    {
        var document = HexView?.Document;
        if (document is null)
            return false;

        // Pre-process text (e.g., remove spaces etc.)
        input = PrepareTextInput(input);

        // We have special behavior if we are not at the beginning of a byte.
        bool isAtFirstCell = location.BitIndex == FirstBitIndex;

        // Compute affected bytes.
        uint byteCount = (uint)((input.Length - 1) / CellsPerWord + 1);
        var alignedStart = new BitLocation(location.ByteIndex, 0);
        var alignedEnd = new BitLocation(alignedStart.ByteIndex + byteCount, 0);
        if (!isAtFirstCell && input.Length > 1)
            alignedEnd = alignedEnd.AddBytes(1);

        var affectedRange = new BitRange(alignedStart, alignedEnd);

        // Determine the number of bytes to read from the original document.
        int originalDataReadCount = 0;
        switch (mode)
        {
            case EditingMode.Overwrite:
                if (!document.ValidRanges.IsSuperSetOf(affectedRange))
                    return false;

                // We need to read the original bytes if we are overwriting, as cells do not necessarily encompass entire bytes.
                originalDataReadCount = (int) affectedRange.ByteLength;
                break;

            case EditingMode.Insert:
                // Edge-case, if we are not at the beginning of a byte, we are actually replacing that byte.
                if (!isAtFirstCell)
                {
                    if (document.ValidRanges.IsSuperSetOf(affectedRange))
                        originalDataReadCount = 1;
                }

                break;
        }

        // Allocate temporary buffer to write the data into.
        byte[] data = new byte[affectedRange.ByteLength];

        if (originalDataReadCount > 0)
            document.ReadBytes(location.ByteIndex, data.AsSpan(0, originalDataReadCount));

        // Write all the cells in the temporary buffer.
        var newLocation = location;
        for (int i = 0; i < input.Length; i++)
        {
            // Are we overwriting in a valid cell in the document?
            if (mode == EditingMode.Overwrite
                && !document.ValidRanges.IsSuperSetOf(new BitRange(newLocation, newLocation.AddBits((ulong) BitsPerCell))))
            {
                return false;
            }

            // Try handling the textual input according to the column's string format.
            if (!TryWriteCell(data, location, newLocation, input[i]))
                return false;

            newLocation = GetNextLocation(newLocation, mode == EditingMode.Insert, false);
        }

        // Apply changes to document.
        switch (mode)
        {
            case EditingMode.Overwrite:
                document.WriteBytes(location.ByteIndex, data);
                break;

            case EditingMode.Insert:
                if (isAtFirstCell)
                {
                    document.InsertBytes(location.ByteIndex, data);
                }
                else
                {
                    // Edge-case, if we are not at the beginning of a byte, we are actually replacing that byte.
                    document.WriteBytes(location.ByteIndex, data.AsSpan(0, 1));
                    document.InsertBytes(location.ByteIndex, data.AsSpan(1));
                }

                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
        }

        // Move to final location.
        location = newLocation;
        return true;
    }

    /// <summary>
    /// Gets the textual representation of the provided bit range.
    /// </summary>
    /// <param name="range">The range.</param>
    /// <returns>The text.</returns>
    public abstract string? GetText(BitRange range);

    /// <inheritdoc />
    public override void Measure()
    {
        if (HexView is null)
        {
            CellSize = default;
        }
        else
        {
            var properties = GetTextRunProperties();
            var dummyTemplate = TextFormatter.Current.FormatLine(
                new SimpleTextSource(".", properties),
                0,
                double.MaxValue,
                new GenericTextParagraphProperties(properties)
            )!;

            CellSize = new Size(dummyTemplate.Width, dummyTemplate.Height);
        }
    }

    /// <summary>
    /// Gets the bounding box of the cell group containing the cell of the provided location.
    /// </summary>
    /// <param name="line">The line.</param>
    /// <param name="location">The location.</param>
    /// <returns>The bounding box.</returns>
    public Rect GetGroupBounds(VisualBytesLine line, BitLocation location)
    {
        var rect = GetRelativeGroupBounds(line, location);
        return new Rect(new Point(Bounds.Left, line.Bounds.Top) + rect.TopLeft, rect.Size);
    }

    /// <summary>
    /// Gets the bounding box of the cell containing the provided location.
    /// </summary>
    /// <param name="line">The line.</param>
    /// <param name="location">The location.</param>
    /// <returns>The bounding box.</returns>
    public Rect GetCellBounds(VisualBytesLine line, BitLocation location)
    {
        var rect = GetRelativeCellBounds(line, location);
        return new Rect(new Point(Bounds.Left, line.Bounds.Top) + rect.TopLeft, rect.Size);
    }

    /// <summary>
    /// Gets the bounding box of the cell group containing the cell of the provided location, relative to the current
    /// line.
    /// </summary>
    /// <param name="line">The line.</param>
    /// <param name="location">The location.</param>
    /// <returns>The bounding box.</returns>
    public  Rect GetRelativeCellBounds(VisualBytesLine line, BitLocation location)
    {
        ulong relativeByteIndex = location.ByteIndex - line.VirtualRange.Start.ByteIndex;
        int nibbleIndex = (CellsPerWord - 1) - location.BitIndex / BitsPerCell;

        return new Rect(
            new Point((WordWidth + GroupPadding) * relativeByteIndex + CellSize.Width * nibbleIndex, 0),
            CellSize
        );
    }

    /// <summary>
    /// Gets the bounding box of the cell containing the provided location, relative to the current line.
    /// </summary>
    /// <param name="line">The line.</param>
    /// <param name="location">The location.</param>
    /// <returns>The bounding box.</returns>
    public Rect GetRelativeGroupBounds(VisualBytesLine line, BitLocation location)
    {
        ulong relativeByteIndex = location.ByteIndex - line.VirtualRange.Start.ByteIndex;

        return new Rect(
            new Point((WordWidth + GroupPadding) * relativeByteIndex, 0),
            new Size(CellSize.Width * CellsPerWord, CellSize.Height)
        );
    }

    /// <summary>
    /// Aligns the provided location to the beginning of the cell that contains the location.
    /// </summary>
    /// <param name="location">The location to align.</param>
    /// <returns>The aligned location.</returns>
    public BitLocation AlignToCell(BitLocation location)
    {
        return new BitLocation(location.ByteIndex, location.BitIndex / BitsPerCell * BitsPerCell);
    }

    /// <summary>
    /// Gets the location of the first selectable cell.
    /// </summary>
    /// <returns>The location.</returns>
    public BitLocation GetFirstLocation()
    {
        return HexView is { Document.ValidRanges.EnclosingRange: var enclosingRange }
            ? new BitLocation(enclosingRange.Start.ByteIndex, FirstBitIndex)
            : default;
    }

    /// <summary>
    /// Gets the location of the last selectable cell.
    /// </summary>
    /// <returns>The location.</returns>
    public BitLocation GetLastLocation(bool includeVirtualCell)
    {
        return HexView is { Document.ValidRanges.EnclosingRange: var enclosingRange }
            ? new BitLocation(enclosingRange.End.ByteIndex - (!includeVirtualCell ? 1u : 0u), 0)
            : default;
    }

    /// <summary>
    /// Given a bit location, gets the location of the cell before it.
    /// </summary>
    /// <param name="location">The location.</param>
    /// <returns>The previous cell's location.</returns>
    public BitLocation GetPreviousLocation(BitLocation location)
    {
        if (location.BitIndex < 8 - BitsPerCell)
            return AlignToCell(new BitLocation(location.ByteIndex, location.BitIndex + BitsPerCell));

        if (HexView is not { Document.ValidRanges.EnclosingRange: var enclosingRange }
            || location.ByteIndex == enclosingRange.Start.ByteIndex)
        {
            return GetFirstLocation();
        }

        return new BitLocation(location.ByteIndex - 1, 0);
    }

    /// <summary>
    /// Given a bit location, gets the location of the cell after it.
    /// </summary>
    /// <param name="location">The location.</param>
    /// <param name="includeVirtualCell"><c>true</c> if the virtual cell at the end of the document should be included.</param>
    /// <param name="clamp"><c>true</c> if the location should be restricted to the current document length.</param>
    /// <returns>The next cell's location.</returns>
    public BitLocation GetNextLocation(BitLocation location, bool includeVirtualCell, bool clamp)
    {
        if (HexView is not { Document.ValidRanges.EnclosingRange: var enclosingRange })
            return default;

        if (location.BitIndex != 0)
            return AlignToCell(new BitLocation(location.ByteIndex, location.BitIndex - BitsPerCell));

        if (clamp && location.ByteIndex >= enclosingRange.End.ByteIndex - (!includeVirtualCell ? 1u : 0u))
            return GetLastLocation(includeVirtualCell);

        return new BitLocation(location.ByteIndex + 1, 8 - BitsPerCell);
    }

    /// <summary>
    /// Gets the bit location of the cell under the provided point.
    /// </summary>
    /// <param name="line">The line.</param>
    /// <param name="point">The point.</param>
    /// <returns>The cell's location.</returns>
    public BitLocation? GetLocationByPoint(VisualBytesLine line, Point point)
    {
        return GetLocationByPoint(line.Range, point);
    }

    /// <summary>
    /// Gets the bit location of the cell under the provided point.
    /// </summary>
    /// <param name="lineRange">The range of the line.</param>
    /// <param name="point">The point.</param>
    /// <returns>The cell's location.</returns>
    public BitLocation? GetLocationByPoint(BitRange lineRange, Point point)
    {
        var relativePoint = point - Bounds.TopLeft;
        double totalGroupWidth = WordWidth + GroupPadding;

        ulong byteIndex = (ulong) (relativePoint.X / totalGroupWidth);
        int nibbleIndex = Math.Clamp(
            (int) (CellsPerWord * (relativePoint.X - byteIndex * totalGroupWidth) / WordWidth),
            0,
            CellsPerWord - 1
        );

        var location = new BitLocation(
            lineRange.Start.ByteIndex + byteIndex,
            (CellsPerWord - 1 - nibbleIndex) * BitsPerCell
        );

        return location.Clamp(lineRange);
    }
}