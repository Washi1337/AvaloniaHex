using AvaloniaHex.Document;

namespace AvaloniaHex.Rendering;

/// <summary>
/// Provides an implementation of a highlighter that highlights all invalid ranges in a document.
/// </summary>
public class InvalidRangesHighlighter : ByteHighlighter
{
    /// <inheritdoc />
    protected override bool IsHighlighted(HexView hexView, VisualBytesLine line, BitLocation location)
    {
        return !hexView.Document?.ValidRanges.Contains(location) ?? false;
    }
}