using AvaloniaHex.Document;

namespace AvaloniaHex.Rendering;

/// <summary>
/// Highlights ranges of bytes within a document of a hex view.
/// </summary>
public class RangesHighlighter : ByteHighlighter
{
    /// <summary>
    /// Gets the bit ranges that should be highlighted in the document.
    /// </summary>
    public BitRangeUnion Ranges { get; } = new();

    /// <inheritdoc />
    protected override bool IsHighlighted(HexView hexView, VisualBytesLine line, BitLocation location)
    {
        return Ranges.Contains(location);
    }
}