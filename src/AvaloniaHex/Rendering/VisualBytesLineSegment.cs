using Avalonia.Media;
using AvaloniaHex.Document;

namespace AvaloniaHex.Rendering;

/// <summary>
/// Represents a single segment in a visual line.
/// </summary>
public class VisualBytesLineSegment
{
    /// <summary>
    /// Creates a new segment range.
    /// </summary>
    /// <param name="range">The bit range the segment spans.</param>
    public VisualBytesLineSegment(BitRange range)
    {
        Range = range;
    }

    /// <summary>
    /// Gets the bit range the segment spans in the visual.
    /// </summary>
    public BitRange Range { get; }

    /// <summary>
    /// Gets the foreground brush used for rendering the text in the segment, or <c>null</c> if the default foreground
    /// brush should be used instead. 
    /// </summary>
    public IBrush? ForegroundBrush { get; set; }

    /// <summary>
    /// Gets the background brush used for rendering the text in the segment, or <c>null</c> if the default background
    /// brush should be used instead. 
    /// </summary>
    public IBrush? BackgroundBrush { get; set; }

    /// <summary>
    /// Splits the segment in two parts at the provided bit location.
    /// </summary>
    /// <param name="location">The location to split at.</param>
    /// <returns>The two resulting segments.</returns>
    public (VisualBytesLineSegment, VisualBytesLineSegment) Split(BitLocation location)
    {
        var (left, right) = Range.Split(location);

        return (
            new VisualBytesLineSegment(left)
            {
                ForegroundBrush = ForegroundBrush,
                BackgroundBrush = BackgroundBrush
            },
            new VisualBytesLineSegment(right)
            {
                ForegroundBrush = ForegroundBrush,
                BackgroundBrush = BackgroundBrush
            }
        );
    }
}
