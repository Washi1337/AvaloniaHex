using System.Diagnostics;
using Avalonia;
using Avalonia.Media.TextFormatting;
using AvaloniaHex.Document;

namespace AvaloniaHex.Rendering;

/// <summary>
/// Represents a single visual line in a hex view.
/// </summary>
[DebuggerDisplay("{Range}")]
public sealed class VisualBytesLine
{
    internal VisualBytesLine(HexView hexView)
    {
        HexView = hexView;

        Data = new byte[hexView.ActualBytesPerLine];
        ColumnTextLines = new TextLine?[hexView.Columns.Count];
        Segments = new List<VisualBytesLineSegment>();
    }

    /// <summary>
    /// Gets the parent ehx view the line is visible in.
    /// </summary>
    public HexView HexView { get; }

    /// <summary>
    /// Gets the bit range the visual line spans. If this line is the last visible line in the document, this may include
    /// the "virtual" cell to insert into.
    /// </summary>
    public BitRange VirtualRange { get; internal set; }

    /// <summary>
    /// Gets the bit range the visual line spans.
    /// </summary>
    public BitRange Range { get; internal set; }

    /// <summary>
    /// Gets the data that is displayed in the line.
    /// </summary>
    public byte[] Data { get; }

    /// <summary>
    /// Gets the bounding box in the hex view the line is rendered at.
    /// </summary>
    public Rect Bounds { get; internal set; }

    /// <summary>
    /// Gets the individual segments the line comprises.
    /// </summary>
    public List<VisualBytesLineSegment> Segments { get; }

    /// <summary>
    /// Gets the individual text lines for every column.
    /// </summary>
    public TextLine?[] ColumnTextLines { get; }

    /// <summary>
    /// Gets a value indicating whether the data and line segments present in the visual line are up to date.
    /// </summary>
    public bool IsValid { get; private set; }

    /// <summary>
    /// Gets the byte in the visual line at the provided absolute byte offset.
    /// </summary>
    /// <param name="byteIndex">The byte offset.</param>
    /// <returns>The byte.</returns>
    public byte GetByteAtAbsolute(ulong byteIndex)
    {
        return Data[byteIndex - Range.Start.ByteIndex];
    }

    /// <summary>
    /// Obtains the span that includes the provided range.
    /// </summary>
    /// <param name="range">The range.</param>
    /// <returns>The span.</returns>
    public Span<byte> AsAbsoluteSpan(BitRange range)
    {
        if (!Range.Contains(range))
            throw new ArgumentException("Provided range is not within the current line");

        return Data.AsSpan(
            (int)(range.Start.ByteIndex - Range.Start.ByteIndex),
            (int)range.ByteLength
        );
    }

    /// <summary>
    /// Finds the segment that contains the provided location.
    /// </summary>
    /// <param name="location">The location.</param>
    /// <returns>The segment, or <c>null</c> if no segment contains the provided location.</returns>
    public VisualBytesLineSegment? FindSegmentContaining(BitLocation location)
    {
        foreach (var segment in Segments)
        {
            if (segment.Range.Contains(location))
                return segment;
        }

        return null;
    }

    /// <summary>
    /// Ensures the visual line is populated with the latest binary data and line segments.
    /// </summary>
    public void EnsureIsValid()
    {
        if (!IsValid)
            Refresh();
    }

    /// <summary>
    /// Marks the visual line, its binary data and line segments as out of date.
    /// </summary>
    public void Invalidate() => IsValid = false;

    /// <summary>
    /// Updates the visual line with the latest data of the document and reconstructs all line segments.
    /// </summary>
    public void Refresh()
    {
        if (HexView.Document is null)
            return;

        // Read data
        HexView.Document!.ReadBytes(Range.Start.ByteIndex, Data.AsSpan(0, (int) Range.ByteLength));

        // Apply transformers
        Segments.Clear();
        Segments.Add(new VisualBytesLineSegment(Range));

        var transformers = HexView.LineTransformers;
        for (int i = 0; i < transformers.Count; i++)
            transformers[i].Transform(HexView, this);

        // Create columns
        for (int i = 0; i < HexView.Columns.Count; i++)
        {
            var column = HexView.Columns[i];
            if (column.IsVisible)
            {
                ColumnTextLines[i]?.Dispose();
                ColumnTextLines[i] = column.CreateTextLine(this);
            }
        }

        IsValid = true;
    }

    /// <summary>
    /// Computes the required height required to the visual line occupies.
    /// </summary>
    /// <returns>The height.</returns>
    public double GetRequiredHeight()
    {
        double height = 0;
        foreach (var columns in ColumnTextLines)
            height = Math.Max(height, columns?.Height ?? 0);
        return height;
    }
}