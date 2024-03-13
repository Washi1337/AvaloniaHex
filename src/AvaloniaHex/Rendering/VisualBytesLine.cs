using System.Diagnostics;
using Avalonia;
using Avalonia.Media.TextFormatting;
using AvaloniaHex.Document;

namespace AvaloniaHex.Rendering;

/// <summary>
/// Represents a single visual line in a hex view.
/// </summary>
[DebuggerDisplay("{Range}")]
public class VisualBytesLine
{
    internal VisualBytesLine(BitRange range, int columnCount)
    {
        Range = range;
        Data = new byte[range.ByteLength];
        ColumnTextLines = new TextLine?[columnCount];
        Segments = new List<VisualBytesLineSegment>();
    }

    /// <summary>
    /// Gets the bit range the visual line spans.
    /// </summary>
    public BitRange Range { get; }

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
}