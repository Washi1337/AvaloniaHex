using AvaloniaHex.Document;
using AvaloniaHex.Rendering;

namespace AvaloniaHex.Editing;

/// <summary>
/// Represents a selection within a hex editor.
/// </summary>
public class Selection
{
    /// <summary>
    /// Fires when the selection range has changed.
    /// </summary>
    public event EventHandler? RangeChanged;

    private BitRange _range;

    internal Selection(HexView hexView)
    {
        HexView = hexView;
    }

    /// <summary>
    /// Gets the hex view the selection is rendered on.
    /// </summary>
    public HexView HexView { get; }

    /// <summary>
    /// Gets or sets the range the selection spans.
    /// </summary>
    public BitRange Range
    {
        get => _range;
        set
        {
            value = HexView.Document is { } document
                ? value.Clamp(document.ValidRanges.EnclosingRange)
                : BitRange.Empty;

            if (_range != value)
            {
                _range = value;
                OnRangeChanged();
            }
        }
    }

    private void OnRangeChanged()
    {
        RangeChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Selects the entire document.
    /// </summary>
    public void SelectAll()
    {
        Range = HexView.Document is not null
            ? new BitRange(0, HexView.Document.Length)
            : default;
    }
}