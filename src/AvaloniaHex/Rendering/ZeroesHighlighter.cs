using AvaloniaHex.Document;

namespace AvaloniaHex.Rendering;

/// <summary>
/// Provides an implementation of a highlighter that highlights all zero bytes in a visual line.
/// </summary>
public class ZeroesHighlighter : ByteHighlighter
{
    /// <inheritdoc />
    protected override bool IsHighlighted(HexView hexView, VisualBytesLine line, BitLocation location)
    {
        return hexView.Document!.ValidRanges.Contains(location) && line.GetByteAtAbsolute(location.ByteIndex) == 0;
    }
}