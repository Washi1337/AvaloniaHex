using AvaloniaHex.Document;
using AvaloniaHex.Rendering;

namespace AvaloniaHex.Editing;

/// <summary>
/// Represents a caret in a hex editor.
/// </summary>
public sealed class Caret
{
    /// <summary>
    /// Fires when the location of the caret has changed.
    /// </summary>
    public event EventHandler? LocationChanged;

    /// <summary>
    /// Fires when the caret's editing mode has changed.
    /// </summary>
    public event EventHandler? ModeChanged;

    /// <summary>
    /// Fires when the primary column of the caret has changed.
    /// </summary>
    public event EventHandler? PrimaryColumnChanged;

    private BitLocation _location;
    private int _primaryColumnIndex = 1;
    private EditingMode _mode;

    internal Caret(HexView view)
    {
        HexView = view;
    }

    /// <summary>
    /// Gets the hex view the caret is rendered on.
    /// </summary>
    public HexView HexView { get; }

    /// <summary>
    /// Gets or sets the editing.
    /// </summary>
    public EditingMode Mode
    {
        get => _mode;
        set
        {
            if (_mode != value)
            {
                _mode = value;
                OnModeChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the current location of the caret.
    /// </summary>
    public BitLocation Location
    {
        get => _location;
        set
        {
            if (_location != value)
            {
                _location = value;
                OnLocationChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the index of the primary column the caret is active in.
    /// </summary>
    public int PrimaryColumnIndex
    {
        get => _primaryColumnIndex;
        set
        {
            if (_primaryColumnIndex != value)
            {
                _primaryColumnIndex = value;
                OnPrimaryColumnChanged();
            }
        }
    }

    /// <summary>
    /// Gets the primary column the caret is active in..
    /// </summary>
    public CellBasedColumn? PrimaryColumn => HexView.Columns[PrimaryColumnIndex] as CellBasedColumn;

    private void OnLocationChanged()
    {
        HexView.BringIntoView(Location);
        LocationChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnModeChanged()
    {
        ModeChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnPrimaryColumnChanged()
    {
        PrimaryColumnChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Moves the caret to the beginning of the document.
    /// </summary>
    public void GoToStartOfDocument()
    {
        int bitIndex = 0;
        if (HexView.Columns[PrimaryColumnIndex] is CellBasedColumn column)
            bitIndex = column.FirstBitIndex;

        Location = new BitLocation(0, bitIndex);
    }

    /// <summary>
    /// Moves the caret to the end of the document.
    /// </summary>
    public void GoToEndOfDocument()
    {
        Location = HexView.Document?.Length > 0
            ? new BitLocation(HexView.Document.Length - 1, 0)
            : new BitLocation(0, 0);
    }

    /// <summary>
    /// Moves the caret to the beginning of the current line in the hex editor.
    /// </summary>
    public void GoToStartOfLine()
    {
        ulong bytesPerLine = (ulong) HexView.ActualBytesPerLine;
        ulong byteIndex = (Location.ByteIndex / bytesPerLine) * bytesPerLine;

        int bitIndex = 0;
        if (HexView.Columns[PrimaryColumnIndex] is CellBasedColumn column)
            bitIndex = column.FirstBitIndex;

        Location = new BitLocation(byteIndex, bitIndex);
    }

    /// <summary>
    /// Moves the caret to the end of the current line in the hex editor.
    /// </summary>
    public void GoToEndOfLine()
    {
        if (HexView.Document is null)
            return;

        ulong bytesPerLine = (ulong) HexView.ActualBytesPerLine;
        ulong byteIndex = Math.Min(((Location.ByteIndex / bytesPerLine) + 1) * bytesPerLine, HexView.Document.Length) - 1;

        Location = new BitLocation(byteIndex, 0);
    }

    /// <summary>
    /// Moves the caret one cell to the left in the hex editor.
    /// </summary>
    public void GoLeft()
    {
        if (HexView.Columns[PrimaryColumnIndex] is CellBasedColumn column)
            Location = column.GetPreviousLocation(Location);
    }

    /// <summary>
    /// Moves the caret one cell up in the hex editor.
    /// </summary>
    public void GoUp() => GoBackward((ulong)HexView.ActualBytesPerLine);

    /// <summary>
    /// Moves the caret one page up in the hex editor.
    /// </summary>
    public void GoPageUp() => GoBackward((ulong)(HexView.ActualBytesPerLine * HexView.VisualLines.Count));

    /// <summary>
    /// Moves the caret the provided number of bytes backward in the hex editor.
    /// </summary>
    /// <param name="byteCount">The number of bytes to move.</param>
    public void GoBackward(ulong byteCount)
    {
        if (HexView.Document is null || PrimaryColumn is null)
            return;

        // Note: We cannot use BitLocation.Clamp due to unsigned overflow that may happen.

        Location = Location.ByteIndex >= byteCount
            ? new BitLocation(Location.ByteIndex - byteCount, Location.BitIndex)
            : new BitLocation(0, PrimaryColumn.FirstBitIndex);
    }

    /// <summary>
    /// Moves the caret one cell to the right in the hex editor.
    /// </summary>
    public void GoRight()
    {
        if (HexView.Columns[PrimaryColumnIndex] is CellBasedColumn column)
            Location = column.GetNextLocation(Location);
    }

    /// <summary>
    /// Moves the caret one cell down in the hex editor.
    /// </summary>
    public void GoDown() => GoForward((ulong)HexView.ActualBytesPerLine);

    /// <summary>
    /// Moves the caret one page down in the hex editor.
    /// </summary>
    public void GoPageDown() => GoForward((ulong)(HexView.ActualBytesPerLine * HexView.VisualLines.Count));

    /// <summary>
    /// Moves the caret the provided number of bytes forward in the hex editor.
    /// </summary>
    /// <param name="byteCount">The number of bytes to move.</param>
    public void GoForward(ulong byteCount)
    {
        if (HexView.Document is null)
            return;

        // Note: We cannot use BitLocation.Clamp due to unsigned overflow that may happen.

        if (HexView.Document.Length < byteCount
            || Location.ByteIndex >= HexView.Document.Length - byteCount)
        {
            Location = new BitLocation(HexView.Document.Length - 1, 0);
            return;
        }

        Location = new BitLocation(Location.ByteIndex + byteCount, Location.BitIndex);
    }

}