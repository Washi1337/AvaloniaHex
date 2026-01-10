using System.Collections;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using AvaloniaHex.Document;

namespace AvaloniaHex.Rendering;

/// <summary>
/// Provides a render target for binary data.
/// </summary>
public class HexView : Control, ILogicalScrollable
{
    // Virtual empty space at the end of the document (in number of lines).
    private const int VirtualSpace = 1;
    
    /// <inheritdoc />
    public event EventHandler? ScrollInvalidated;

    /// <summary>
    /// Fires when the document in the hex editor has changed.
    /// </summary>
    public event EventHandler<DocumentChangedEventArgs>? DocumentChanged;

    private readonly VisualBytesLinesBuffer _visualLines;
    private Vector _scrollOffset;
    private ulong? _anchorByteIndex;
    private int _actualBytesPerLine;

    static HexView()
    {
        FocusableProperty.OverrideDefaultValue<HexView>(true);

        TemplatedControl.FontFamilyProperty.Changed.AddClassHandler<HexView>(OnFontRelatedPropertyChanged);
        TemplatedControl.FontSizeProperty.Changed.AddClassHandler<HexView>(OnFontRelatedPropertyChanged);
        TemplatedControl.ForegroundProperty.Changed.AddClassHandler<HexView>(OnFontRelatedPropertyChanged);
        DocumentProperty.Changed.AddClassHandler<HexView>(OnDocumentChanged);
        IsHeaderVisibleProperty.Changed.AddClassHandler<HexView>(OnIsHeaderVisibleChanged);

        AffectsArrange<HexView>(
            DocumentProperty,
            BytesPerLineProperty,
            ColumnPaddingProperty
        );
    }

    /// <summary>
    /// Creates a new hex view control.
    /// </summary>
    public HexView()
    {
        Columns = new ColumnCollection(this);
        _visualLines = new VisualBytesLinesBuffer(this);

        EnsureTextProperties();

        Layers = new LayerCollection(this)
        {
            new ColumnBackgroundLayer(),
            new CellGroupsLayer(),
            new HeaderLayer(),
            new TextLayer(),
        };
    }

    /// <summary>
    /// Dependency property for <see cref="Document"/>.
    /// </summary>
    public static readonly StyledProperty<IBinaryDocument?> DocumentProperty =
        AvaloniaProperty.Register<HexView, IBinaryDocument?>(nameof(Document));

    /// <summary>
    /// Gets or sets the binary document that is currently being displayed.
    /// </summary>
    public IBinaryDocument? Document
    {
        get => GetValue(DocumentProperty);
        set => SetValue(DocumentProperty, value);
    }

    /// <summary>
    /// Dependency property for <see cref="BytesPerLine"/>.
    /// </summary>
    public static readonly StyledProperty<int?> BytesPerLineProperty =
        AvaloniaProperty.Register<HexView, int?>(nameof(BytesPerLine));

    /// <summary>
    /// Gets or sets the fixed amount of bytes per line that should be displayed, or <c>null</c> if the number of
    /// bytes is proportional to the width of the control.
    /// </summary>
    public int? BytesPerLine
    {
        get => GetValue(BytesPerLineProperty);
        set => SetValue(BytesPerLineProperty, value);
    }

    /// <summary>
    /// Dependency property for <see cref="ActualBytesPerLine"/>.
    /// </summary>
    public static readonly DirectProperty<HexView, int> ActualBytesPerLineProperty =
        AvaloniaProperty.RegisterDirect<HexView, int>(nameof(ActualBytesPerLine), o => o.ActualBytesPerLine);

    /// <summary>
    /// Gets the total amount of bytes per line that are displayed in the control.
    /// </summary>
    public int ActualBytesPerLine
    {
        get => _actualBytesPerLine;
        private set
        {
            if (SetAndRaise(ActualBytesPerLineProperty, ref _actualBytesPerLine, value))
            {
                InvalidateHeaders();
                InvalidateVisualLines();
            }
        }
    }

    /// <summary>
    /// Dependency property for <see cref="Columns"/>.
    /// </summary>
    public static readonly DirectProperty<HexView, ColumnCollection> ColumnsProperty =
        AvaloniaProperty.RegisterDirect<HexView, ColumnCollection>(nameof(Columns), o => o.Columns);

    /// <summary>
    /// Gets the columns displayed in the hex view.
    /// </summary>
    public ColumnCollection Columns
    {
        get;
    }

    /// <summary>
    /// Dependency property for <see cref="ColumnPadding"/>.
    /// </summary>
    public static readonly StyledProperty<double> ColumnPaddingProperty =
        AvaloniaProperty.Register<HexView, double>(nameof(ColumnPadding), 5D);

    /// <summary>
    /// Gets the amount of spacing in between columns.
    /// </summary>
    public double ColumnPadding
    {
        get => GetValue(ColumnPaddingProperty);
        set => SetValue(ColumnPaddingProperty, value);
    }

    /// <summary>
    /// Dependency property for <see cref="HeaderPadding"/>.
    /// </summary>
    public static readonly StyledProperty<Thickness> HeaderPaddingProperty =
        AvaloniaProperty.Register<HexView, Thickness>(nameof(HeaderPadding));

    /// <summary>
    /// Gets or sets the padding of the header.
    /// </summary>
    public Thickness HeaderPadding
    {
        get => GetValue(HeaderPaddingProperty);
        set => SetValue(HeaderPaddingProperty, value);
    }

    /// <summary>
    /// Dependency property for <see cref="IsHeaderVisible"/>.
    /// </summary>
    public static readonly StyledProperty<bool> IsHeaderVisibleProperty =
        AvaloniaProperty.Register<HexView, bool>(nameof(IsHeaderVisible), true);

    /// <summary>
    /// Gets or sets a value indicating whether the header (and padding) of the hex view should be rendered or not.
    /// </summary>
    public bool IsHeaderVisible
    {
        get => GetValue(IsHeaderVisibleProperty);
        set => SetValue(IsHeaderVisibleProperty, value);
    }

    internal TextLine?[] Headers
    {
        get;
        private set;
    } = [];

    /// <summary>
    /// Gets the total effective header size of the hex view.
    /// </summary>
    public double EffectiveHeaderSize
    {
        get;
        private set;
    }

    /// <summary>
    /// Gets the font family that is used for rendering the text in the hex view.
    /// </summary>
    public FontFamily FontFamily
    {
        get => GetValue(TemplatedControl.FontFamilyProperty);
        set => SetValue(TemplatedControl.FontFamilyProperty, value);
    }

    /// <summary>
    /// Gets the font size that is used for rendering the text in the hex view.
    /// </summary>
    public double FontSize
    {
        get => GetValue(TemplatedControl.FontSizeProperty);
        set => SetValue(TemplatedControl.FontSizeProperty, value);
    }

    /// <summary>
    /// Gets the typeface that is used for rendering the text in the hex view.
    /// </summary>
    public Typeface Typeface
    {
        get;
        private set;
    }

    /// <summary>
    /// Gets the base foreground brush that is used for rendering the text in the hex view.
    /// </summary>
    public IBrush? Foreground
    {
        get => GetValue(TemplatedControl.ForegroundProperty);
        set => SetValue(TemplatedControl.ForegroundProperty, value);
    }

    /// <summary>
    /// Gets the text run properties that are used for rendering the text in the hex view.
    /// </summary>
    public GenericTextRunProperties TextRunProperties
    {
        get;
        private set;
    }

    /// <summary>
    /// Gets the current lines that are visible.
    /// </summary>
    public IReadOnlyList<VisualBytesLine> VisualLines => _visualLines;

    /// <summary>
    /// Gets a collection of line transformers that are applied to each line in the hex view.
    /// </summary>
    public ObservableCollection<ILineTransformer> LineTransformers { get; } = new();

    /// <summary>
    /// Gets a collection of render layers in the hex view.
    /// </summary>
    public LayerCollection Layers { get; }

    /// <inheritdoc />
    public Size Extent
    {
        get; 
        private set;
    }

    /// <summary>
    /// Gets or sets the current scroll offset.
    /// </summary>
    public Vector ScrollOffset
    {
        get => _scrollOffset;
        set
        {
            if (_scrollOffset == value)
                return;

            _scrollOffset = value;
            _anchorByteIndex = null;
            InvalidateArrange();
            ((ILogicalScrollable) this).RaiseScrollInvalidated(EventArgs.Empty);
        }
    }

    /// <inheritdoc />
    Vector IScrollable.Offset
    {
        get => ScrollOffset;
        set => ScrollOffset = value;
    }

    /// <inheritdoc />
    public Size Viewport
    {
        get; 
        private set;
    }

    bool ILogicalScrollable.CanHorizontallyScroll { get; set; } = false;

    bool ILogicalScrollable.CanVerticallyScroll { get; set; } = true;

    bool ILogicalScrollable.IsLogicalScrollEnabled => true;

    /// <inheritdoc />
    public Size ScrollSize => new(0, 1);

    /// <inheritdoc />
    public Size PageScrollSize => new(0, VisualLines.Count);

    /// <summary>
    /// Gets the binary range that is currently visible in the view.
    /// </summary>
    public BitRange VisibleRange { get; private set; }

    /// <summary>
    /// Gets the binary range that is fully visible in the view, excluding lines that are only partially visible.
    /// </summary>
    public BitRange FullyVisibleRange { get; private set; }

    /// <summary>
    /// Invalidates the line that includes the provided location.
    /// </summary>
    /// <param name="location">The location.</param>
    public void InvalidateVisualLine(BitLocation location)
    {
        var line = GetVisualLineByLocation(location);
        if (line is not null)
            InvalidateVisualLine(line);
    }

    /// <summary>
    /// Schedules a repaint of the provided visual line.
    /// </summary>
    /// <param name="line"></param>
    public void InvalidateVisualLine(VisualBytesLine line)
    {
        line.Invalidate();
        InvalidateArrange();

        for (int i = 0; i < Layers.Count; i++)
        {
            if ((Layers[i].UpdateMoments & LayerRenderMoments.LineInvalidate) != 0)
                Layers[i].InvalidateVisual();
        }
    }

    /// <summary>
    /// Clears out all visual lines and schedules a new layout pass.
    /// </summary>
    public void InvalidateVisualLines()
    {
        _visualLines.Clear();
        InvalidateArrange();
    }

    /// <summary>
    /// Invalidates the lines that contain the bits in the provided range.
    /// </summary>
    /// <param name="range">The range to invalidate.</param>
    public void InvalidateVisualLines(BitRange range)
    {
        if (!VisibleRange.OverlapsWith(range))
            return;

        foreach (var line in GetVisualLinesByRange(range))
            line.Invalidate();

        for (int i = 0; i < Layers.Count; i++)
        {
            if ((Layers[i].UpdateMoments & LayerRenderMoments.LineInvalidate) != 0)
                Layers[i].InvalidateVisual();
        }

        InvalidateArrange();
    }

    /// <summary>
    /// Invalidates the headers of the hex view.
    /// </summary>
    public void InvalidateHeaders()
    {
        Array.Clear(Headers);
        InvalidateArrange();
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        for (int i = 0; i < Columns.Count; i++)
            Columns[i].Measure();

        for (int i = 0; i < Layers.Count; i++)
            Layers[i].Measure(availableSize);

        return base.MeasureOverride(availableSize);
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        SetScrollAnchor();
        ComputeBytesPerLine(finalSize);
        UpdateColumnBounds();
        UpdateVisualLines(finalSize);
        UpdateScrollInfo(finalSize);

        bool hasResized = finalSize != Bounds.Size;

        for (int i = 0; i < Layers.Count; i++)
        {
            Layers[i].Arrange(new Rect(new Point(0, 0), finalSize));

            if (hasResized || (Layers[i].UpdateMoments & LayerRenderMoments.NoResizeRearrange) != 0)
                Layers[i].InvalidateVisual();
        }

        return base.ArrangeOverride(finalSize);
    }

    private void SetScrollAnchor()
    {
        // When resizing the hex view, keep the top-left byte ("anchor") on the first visible line.
        if (!_anchorByteIndex.HasValue && Document?.ValidRanges.EnclosingRange is { } enclosingRange)
        {
            // Remember the index of the top-left byte based on the current scroll offset.
            // (The anchor byte index is reset when the user changes scroll offset.)
            ulong lineIndex = (ulong) Math.Round(ScrollOffset.Y, MidpointRounding.AwayFromZero);
            _anchorByteIndex = enclosingRange.Start.ByteIndex + lineIndex * (ulong) ActualBytesPerLine;
        }
    }

    private void ComputeBytesPerLine(Size finalSize)
    {
        if (BytesPerLine is { } bytesPerLine)
        {
            ActualBytesPerLine = bytesPerLine;
            return;
        }

        // total                                            = minimum_width + n * word_width + (n - 1) * word_padding
        // 0                                                = total - (minimum_width + n * word_width + (n - 1) * word_padding)
        // n * word_width + (n - 1) * word_padding          = total - minimum_width
        // n * word_width + n * word_padding - word_padding = total - minimum_width
        // n * (word_width + word_padding) - word_padding   = total - minimum_width
        // n * (word_width + word_padding)                  = total - minimum_width + word_padding
        // n                                                = (total - minimum_width + word_padding) / (word_width + word_padding)

        double minimumWidth = 0;
        double wordWidth = 0;
        double wordPadding = 0;

        for (int i = 0; i < Columns.Count; i++)
        {
            var column = Columns[i];
            if (!column.IsVisible)
                continue;

            minimumWidth += column.MinimumSize.Width;
            if (i > 0)
                minimumWidth += ColumnPadding;

            if (column is CellBasedColumn x)
            {
                wordWidth += x.WordWidth;
                wordPadding += x.GroupPadding;
            }
        }

        int count = (int) ((finalSize.Width - minimumWidth + wordPadding) / (wordWidth + wordPadding));
        ActualBytesPerLine = wordWidth != 0
            ? Math.Max(1, count)
            : 16;
    }

    private void UpdateColumnBounds()
    {
        double currentX = 0;
        foreach (var column in Columns)
        {
            if (!column.IsVisible)
            {
                column.SetBounds(default);
            }
            else
            {
                double width = column.Width;
                column.SetBounds(new Rect(currentX, 0, width, Bounds.Height));
                currentX += width + ColumnPadding;
            }
        }
    }

    private void EnsureHeaders()
    {
        if (Headers.Length != Columns.Count)
            Headers = new TextLine?[Columns.Count];

        EffectiveHeaderSize = 0;

        if (!IsHeaderVisible)
            return;

        for (int i = 0; i < Columns.Count; i++)
        {
            var column = Columns[i];
            if (column is not { IsVisible: true, IsHeaderVisible: true })
                continue;

            var headerLine = Headers[i] ??= column.CreateHeaderLine();
            if (headerLine is not null)
                EffectiveHeaderSize = Math.Max(EffectiveHeaderSize, headerLine.Height);
        }

        EffectiveHeaderSize += HeaderPadding.Top + HeaderPadding.Bottom;
    }

    private void UpdateVisualLines(Size finalSize)
    {
        EnsureHeaders();

        // No columns or no document means we need a completely empty control.
        if (Columns.Count == 0 || Document is null)
        {
            _visualLines.Clear();
            _anchorByteIndex = null;

            VisibleRange = default;
            FullyVisibleRange = default;
            return;
        }

        // In case of an empty document, always ensure that there's at least one (empty) line rendered.
        if (Document.Length == 0)
        {
            _visualLines.Clear();
            _anchorByteIndex = null;

            var line = _visualLines.GetOrCreateVisualLine(new BitRange(0, 1));

            line.EnsureIsValid();
            line.Bounds = new Rect(0, EffectiveHeaderSize, finalSize.Width, line.GetRequiredHeight());

            VisibleRange = line.VirtualRange;
            FullyVisibleRange = VisibleRange;
            return;
        }

        Debug.Assert(_anchorByteIndex.HasValue, "Scroll anchor expected to be set.");

        // Otherwise, ensure all visible lines are created.
        var enclosingRange = Document.ValidRanges.EnclosingRange;
        _anchorByteIndex = ulong.Clamp(
            _anchorByteIndex.Value,
            enclosingRange.Start.ByteIndex,
            enclosingRange.End.ByteIndex
        );
        ulong lineIndex = (_anchorByteIndex.Value - enclosingRange.Start.ByteIndex) / (ulong) ActualBytesPerLine;
        var startLocation = new BitLocation(enclosingRange.Start.ByteIndex + lineIndex * (ulong) ActualBytesPerLine);
        var currentRange = new BitRange(startLocation, startLocation);

        double currentY = EffectiveHeaderSize;
        while (currentY < finalSize.Height && currentRange.End <= enclosingRange.End)
        {
            // Get/create next visual line.
            var line = _visualLines.GetOrCreateVisualLine(new BitRange(
                currentRange.End.ByteIndex,
                Math.Min(enclosingRange.End.ByteIndex + 1, currentRange.End.ByteIndex + (ulong) ActualBytesPerLine)
            ));

            line.EnsureIsValid();
            line.Bounds = new Rect(0, currentY, finalSize.Width, line.GetRequiredHeight());

            // Move to next line / range.
            currentY += line.Bounds.Height;
            currentRange = line.VirtualRange;
        }

        // Compute full visible range (including lines that are only slightly visible).
        VisibleRange = _visualLines.Count == 0
            ? new BitRange(enclosingRange.End, enclosingRange.End)
            : new BitRange(startLocation, currentRange.End);

        // Cut off excess visual lines.
        _visualLines.RemoveOutsideOfRange(VisibleRange);

        // Get fully visible byte range.
        if (_visualLines.Count == 0 || !(_visualLines[^1].Bounds.Bottom > finalSize.Height))
        {
            FullyVisibleRange = VisibleRange;
        }
        else
        {
            var start = VisibleRange.Start;
            var end = new BitLocation(VisibleRange.End.ByteIndex - (ulong) ActualBytesPerLine);
            if (end < start)
            {
                // Viewport too small, no fully visible lines.
                // Treat last line as fully visible, which is needed for BringIntoView().
                FullyVisibleRange = VisibleRange;
            }
            else
            {
                FullyVisibleRange = new BitRange(start, end);
            }
        }
    }

    private void UpdateScrollInfo(Size finalSize)
    {
        Size newExtent;
        Size newViewport;
        Vector newScrollOffset;

        if (Document is { } document)
        {
            newExtent = new Size(0, Math.Ceiling((double)document.Length / ActualBytesPerLine) + VirtualSpace);

            if (VisualLines.Count > 0)
            {
                double height = finalSize.Height - EffectiveHeaderSize;
                var line = VisualLines[0];
                double lineHeight = line.GetRequiredHeight();
                newViewport = new Size(0, lineHeight > 0 ? height / lineHeight : 1);
            }
            else
            {
                newViewport = new Size(0, 1);
            }

            if (_anchorByteIndex.HasValue)
            {
                var enclosingRange = document.ValidRanges.EnclosingRange;
                ulong lineIndex = (_anchorByteIndex.Value - enclosingRange.Start.ByteIndex) / (ulong) ActualBytesPerLine;

                // The scroll offset can be a fractional value. Update the scroll offset only when
                // it is necessary to scroll to a different line.
                newScrollOffset = lineIndex != (ulong) Math.Round(_scrollOffset.Y, MidpointRounding.AwayFromZero)
                    ? new Vector(0, lineIndex)
                    : _scrollOffset;
            }
            else
            {
                newScrollOffset = default;
            }
        }
        else
        {
            newExtent = default;
            newViewport = new Size(0, 1);
            newScrollOffset = default;
        }

        if (newExtent == Extent && newViewport == Viewport && newScrollOffset == ScrollOffset)
            return;

        // When the extent grows or shrinks, the previous scroll offset may fall outside the new
        // extent. The properties must be updated in the correct order to prevent the ScrollViewer's
        // coercion logic from overriding the scroll offset.
        if (newExtent.Height > Extent.Height)
        {
            // Grow extent.
            Extent = newExtent;
            Viewport = newViewport;
            ((ILogicalScrollable) this).RaiseScrollInvalidated(EventArgs.Empty);

            _scrollOffset = newScrollOffset;
            ((ILogicalScrollable) this).RaiseScrollInvalidated(EventArgs.Empty);
        }
        else
        {
            // Shrink extent.
            _scrollOffset = newScrollOffset;
            ((ILogicalScrollable) this).RaiseScrollInvalidated(EventArgs.Empty);

            Extent = newExtent;
            Viewport = newViewport;
            ((ILogicalScrollable) this).RaiseScrollInvalidated(EventArgs.Empty);
        }
    }

    /// <summary>
    /// Gets the visual line containing the provided location.
    /// </summary>
    /// <param name="location">The location</param>
    /// <returns>The line, or <c>null</c> if the location is currently not visible.</returns>
    public VisualBytesLine? GetVisualLineByLocation(BitLocation location)
    {
        if (!VisibleRange.Contains(location))
            return null;

        return _visualLines.GetVisualLineByLocation(location);
    }

    /// <summary>
    /// Enumerates all lines that overlap with the provided range.
    /// </summary>
    /// <param name="range">The range.</param>
    /// <returns>The lines.</returns>
    public IEnumerable<VisualBytesLine> GetVisualLinesByRange(BitRange range)
    {
        if (!VisibleRange.OverlapsWith(range))
            return [];

        return _visualLines.GetVisualLinesByRange(range);
    }

    /// <summary>
    /// Gets the visual line containing the provided point.
    /// </summary>
    /// <param name="point">The point</param>
    /// <returns>The line, or <c>null</c> if the location is currently not visible.</returns>
    public VisualBytesLine? GetVisualLineByPoint(Point point)
    {
        for (int i = 0; i < VisualLines.Count; i++)
        {
            var line = VisualLines[i];
            if (line.Bounds.Contains(point))
                return line;
        }

        return null;
    }

    /// <summary>
    /// Gets the column containing the provided point.
    /// </summary>
    /// <param name="point">The point.</param>
    /// <returns>The point, or <c>null</c> if the location does not fall inside of a column.</returns>
    public Column? GetColumnByPoint(Point point)
    {
        foreach (var column in Columns)
        {
            if (column.IsVisible && column.Bounds.Contains(point))
                return column;
        }

        return null;
    }

    /// <summary>
    /// Gets the location of the cell under the provided point.
    /// </summary>
    /// <param name="point">The point.</param>
    /// <param name="restrictToVisibleRange"><c>true</c> if the locations should be restricted to the valid, visible ranges only, <c>false</c> otherwise.</param>
    /// <returns>The location of the cell, or <c>null</c> if no cell is under the provided point.</returns>
    public BitLocation? GetLocationByPoint(Point point, bool restrictToVisibleRange = true)
    {
        if (GetColumnByPoint(point) is not CellBasedColumn column)
            return null;

        return GetLocationByPoint(point, column, restrictToVisibleRange);
    }

    /// <summary>
    /// Gets the location of the cell within a column under the provided point.
    /// </summary>
    /// <param name="point">The point.</param>
    /// <param name="column">The column</param>
    /// /// <param name="restrictToVisibleRange"><c>true</c> if the locations should be restricted to the valid, visible ranges only, <c>false</c> otherwise.</param>
    /// <returns>The location of the cell, or <c>null</c> if no cell is under the provided point.</returns>
    public BitLocation? GetLocationByPoint(Point point, CellBasedColumn column, bool restrictToVisibleRange = true)
    {
        if (restrictToVisibleRange)
            return GetVisualLineByPoint(point) is { } line ? column.GetLocationByPoint(line, point) : null;

        var range = GetLineRangeByYCoord(point.Y);
        if (range is null)
            return null;

        return column.GetLocationByPoint(range.Value, point);
    }

    private BitRange? GetLineRangeByYCoord(double yCoord)
    {
        if (VisualLines.Count == 0 || Document is not { } document)
            return null;

        ulong bytesPerLine = (ulong) ActualBytesPerLine;
        double lineHeight = VisualLines[0].GetRequiredHeight();

        // Apply correction for when header is visible.
        if (IsHeaderVisible)
            yCoord -= EffectiveHeaderSize;

        // Address off-by-one line correction for when cursor is just above the control.
        // (Otherwise, the division below gets rounded the wrong way around).
        if (yCoord < 0)
            yCoord -= lineHeight;

        int lineDelta = (int) (yCoord / lineHeight);
        var enclosingRange = document.ValidRanges.EnclosingRange;
        var visibleRangeStart = VisibleRange.Start;

        // Determine start of line.
        BitLocation start;
        if (lineDelta < 0)
        {
            lineDelta = -lineDelta;

            // Clamp to beginning of document.
            int linesAvailable = (int) ((visibleRangeStart.ByteIndex - enclosingRange.Start.ByteIndex) / bytesPerLine);
            start = linesAvailable >= lineDelta
                ? visibleRangeStart.SubtractBytes((ulong) lineDelta * bytesPerLine)
                : enclosingRange.Start;
        }
        else
        {
            // Clamp to ending of document.
            int linesAvailable = (int) ((enclosingRange.End.ByteIndex - visibleRangeStart.ByteIndex) / bytesPerLine);
            start = linesAvailable >= lineDelta
                ? visibleRangeStart.AddBytes((ulong) lineDelta * bytesPerLine)
                : new BitLocation(enclosingRange.End.ByteIndex / bytesPerLine * bytesPerLine);
        }

        // Span entire line.
        return new BitRange(start, start.AddBytes(bytesPerLine));
    }

    /// <summary>
    /// Ensures the provided bit location is put into view.
    /// </summary>
    /// <param name="location">The location to scroll to.</param>
    /// <returns><c>true</c> if the scroll offset has changed, <c>false</c> otherwise.</returns>
    public bool BringIntoView(BitLocation location)
    {
        if (Document is not { ValidRanges.EnclosingRange: var enclosingRange }
            || location.ByteIndex >= enclosingRange.End.ByteIndex + 1
            || FullyVisibleRange.Contains(location)
            || ActualBytesPerLine == 0)
        {
            return false;
        }

        UpdateLayout();

        ulong firstLineIndex = FullyVisibleRange.Start.ByteIndex / (ulong) ActualBytesPerLine;
        ulong lastLineIndex = (FullyVisibleRange.End.ByteIndex - 1) / (ulong) ActualBytesPerLine;
        ulong targetLineIndex = (location.ByteIndex - enclosingRange.Start.ByteIndex) / (ulong) ActualBytesPerLine;

        ulong newIndex;

        if (location > FullyVisibleRange.End)
        {
            ulong difference = targetLineIndex - lastLineIndex;
            newIndex = firstLineIndex + difference;
        }
        else if (location < FullyVisibleRange.Start)
        {
            ulong difference = firstLineIndex - targetLineIndex;
            newIndex = firstLineIndex - difference;
        }
        else
        {
            return false;
        }

        ScrollOffset = new Vector(0, newIndex);

        return true;
    }

    bool ILogicalScrollable.BringIntoView(Control target, Rect targetRect) => false;

    Control? ILogicalScrollable.GetControlInDirection(NavigationDirection direction, Control? from) => null;

    void ILogicalScrollable.RaiseScrollInvalidated(EventArgs e) => ScrollInvalidated?.Invoke(this, e);

    private static void OnDocumentChanged(HexView view, AvaloniaPropertyChangedEventArgs arg2)
    {
        view._scrollOffset = default;
        view._anchorByteIndex = null;
        view.InvalidateVisualLines();

        var oldDocument = (IBinaryDocument?) arg2.OldValue;
        if (oldDocument is not null)
            oldDocument.Changed -= view.DocumentOnChanged;

        var newDocument = (IBinaryDocument?) arg2.NewValue;
        if (newDocument is not null)
            newDocument.Changed += view.DocumentOnChanged;

        view.OnDocumentChanged(new DocumentChangedEventArgs(
            oldDocument,
            newDocument
        ));
    }

    private static void OnIsHeaderVisibleChanged(HexView arg1, AvaloniaPropertyChangedEventArgs arg2)
    {
        arg1.InvalidateHeaders();
        arg1.InvalidateVisualLines();
        arg1.InvalidateArrange();
    }

    private void DocumentOnChanged(object? sender, BinaryDocumentChange e)
    {
        switch (e.Type)
        {
            case BinaryDocumentChangeType.Modify:
                InvalidateVisualLines(e.AffectedRange);
                break;

            case BinaryDocumentChangeType.Insert:
            case BinaryDocumentChangeType.Remove:
                InvalidateVisualLines(e.AffectedRange.ExtendTo(Document!.ValidRanges.EnclosingRange.End));
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    /// <summary>
    /// Fires the <see cref="DocumentChanged"/> event.
    /// </summary>
    /// <param name="e">The arguments describing the event.</param>
    protected virtual void OnDocumentChanged(DocumentChangedEventArgs e)
    {
        DocumentChanged?.Invoke(this, e);
    }

    private static void OnFontRelatedPropertyChanged(HexView arg1, AvaloniaPropertyChangedEventArgs arg2)
    {
        arg1.EnsureTextProperties();
        arg1.InvalidateMeasure();
        arg1.InvalidateVisualLines();
        arg1.InvalidateHeaders();
    }

    [MemberNotNull(nameof(TextRunProperties))]
    private void EnsureTextProperties()
    {
        if (Typeface.FontFamily != FontFamily)
            Typeface = new Typeface(FontFamily);

        TextRunProperties = new GenericTextRunProperties(
            Typeface,
            fontRenderingEmSize: FontSize,
            foregroundBrush: Foreground
        );
    }

    /// <summary>
    /// Represents a collection of layers in a hex view.
    /// </summary>
    public sealed class LayerCollection : ObservableCollection<Layer>
    {
        private readonly HexView _owner;

        internal LayerCollection(HexView owner)
        {
            _owner = owner;
        }

        /// <summary>
        /// Gets a single layer by its type.
        /// </summary>
        /// <typeparam name="TLayer">The layer type.</typeparam>
        /// <returns>The layer.</returns>
        public TLayer Get<TLayer>()
            where TLayer : Layer
        {
            return Items.OfType<TLayer>().First();
        }

        /// <summary>
        /// Attempts to find a single layer by its type.
        /// </summary>
        /// <typeparam name="TLayer">The layer type.</typeparam>
        /// <returns>The layer, or <c>null</c> if no layer of the provided type exists in the collection.</returns>
        public TLayer? GetOrDefault<TLayer>()
            where TLayer : Layer
        {
            return Items.OfType<TLayer>().FirstOrDefault();
        }

        /// <summary>
        /// Gets the index of a specific layer.
        /// </summary>
        /// <typeparam name="TLayer">The type of the layer.</typeparam>
        /// <returns>The index, or <c>-1</c> if the layer is not present in the collection.</returns>
        public int IndexOf<TLayer>()
            where TLayer : Layer
        {
            for (int i = 0; i < Count; i++)
            {
                if (Items[i] is TLayer)
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// Inserts a layer before another.
        /// </summary>
        /// <param name="layer">The layer to insert.</param>
        /// <typeparam name="TLayer">The type of layer to insert before.</typeparam>
        public void InsertBefore<TLayer>(Layer layer)
            where TLayer : Layer
        {
            int index = IndexOf<TLayer>();
            if (index == -1)
                Insert(0, layer);
            else
                Insert(index, layer);
        }

        /// <summary>
        /// Inserts a layer after another.
        /// </summary>
        /// <param name="layer">The layer to insert.</param>
        /// <typeparam name="TLayer">The type of layer to insert after.</typeparam>
        public void InsertAfter<TLayer>(Layer layer)
            where TLayer : Layer
        {
            int index = IndexOf<TLayer>();
            if (index == -1)
                Add(layer);
            else
                Insert(index + 1, layer);
        }

        private static void AssertNoOwner(Layer item)
        {
            if (item.HexView is not null)
                throw new InvalidOperationException("Layer is already added to another hex view.");
        }

        /// <inheritdoc />
        protected override void InsertItem(int index, Layer item)
        {
            AssertNoOwner(item);
            item.HexView = _owner;
            _owner.LogicalChildren.Insert(index + _owner.Columns.Count, item);
            _owner.VisualChildren.Insert(index, item);
            base.InsertItem(index, item);
        }

        /// <inheritdoc />
        protected override void RemoveItem(int index)
        {
            var item = Items[index];

            item.HexView = null;
            _owner.LogicalChildren.Remove(item);
            _owner.VisualChildren.Remove(item);

            base.RemoveItem(index);
        }

        /// <inheritdoc />
        protected override void SetItem(int index, Layer item)
        {
            Items[index].HexView = null;
            item.HexView = _owner;
            base.SetItem(index, item);

            _owner.LogicalChildren[index + _owner.Columns.Count] = item;
            _owner.VisualChildren[index] = item;
        }

        /// <inheritdoc />
        protected override void ClearItems()
        {
            foreach (var item in Items)
            {
                item.HexView = null;
                _owner.LogicalChildren.Remove(item);
                _owner.VisualChildren.Remove(item);
            }

            base.ClearItems();
        }
    }

    /// <summary>
    /// Represents a collection of columns that are added to a hex view.
    /// </summary>
    public class ColumnCollection : ObservableCollection<Column>
    {
        private readonly HexView _owner;

        internal ColumnCollection(HexView owner)
        {
            _owner = owner;
        }

        /// <summary>
        /// Gets a single column by its type.
        /// </summary>
        /// <typeparam name="TColumn">The column type.</typeparam>
        /// <returns>The column.</returns>
        public TColumn Get<TColumn>()
            where TColumn : Column
        {
            return Items.OfType<TColumn>().First();
        }

        /// <summary>
        /// Attempts to find a single column by its type.
        /// </summary>
        /// <typeparam name="TColumn">The column type.</typeparam>
        /// <returns>The column, or <c>null</c> if no column of the provided type exists in the collection.</returns>
        public TColumn? GetOrDefault<TColumn>()
            where TColumn : Column
        {
            return Items.OfType<TColumn>().FirstOrDefault();
        }

        /// <summary>
        /// Gets the index of a specific column.
        /// </summary>
        /// <typeparam name="TColumn">The type of the column.</typeparam>
        /// <returns>The index, or <c>-1</c> if the column is not present in the collection.</returns>
        public int IndexOf<TColumn>()
            where TColumn : Column
        {
            for (int i = 0; i < Count; i++)
            {
                if (Items[i] is TColumn)
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// Inserts a column before another.
        /// </summary>
        /// <param name="column">The column to insert.</param>
        /// <typeparam name="TColumn">The type of column to insert before.</typeparam>
        public void InsertBefore<TColumn>(Column column)
            where TColumn : Column
        {
            int index = IndexOf<TColumn>();
            if (index == -1)
                Insert(0, column);
            else
                Insert(index, column);
        }

        /// <summary>
        /// Inserts a column after another.
        /// </summary>
        /// <param name="column">The column to insert.</param>
        /// <typeparam name="TColumn">The type of column to insert after.</typeparam>
        public void InsertAfter<TColumn>(Column column)
            where TColumn : Column
        {
            int index = IndexOf<TColumn>();
            if (index == -1)
                Add(column);
            else
                Insert(index + 1, column);
        }

        private static void AssertNoOwner(Column column)
        {
            if (column.HexView is not null)
                throw new ArgumentException("Column is already added to another hex view.");
        }

        /// <inheritdoc />
        protected override void InsertItem(int index, Column item)
        {
            AssertNoOwner(item);
            base.InsertItem(index, item);
            item.HexView = _owner;
            _owner.LogicalChildren.Insert(index, item);
        }

        /// <inheritdoc />
        protected override void SetItem(int index, Column item)
        {
            AssertNoOwner(item);

            Items[index].HexView = null;
            base.SetItem(index, item);
            item.HexView = _owner;
            _owner.LogicalChildren[index] = item;
        }

        /// <inheritdoc />
        protected override void RemoveItem(int index)
        {
            Items[index].HexView = null;
            base.RemoveItem(index);
            _owner.LogicalChildren.RemoveAt(index);
        }

        /// <inheritdoc />
        protected override void ClearItems()
        {
            foreach (var item in Items)
            {
                item.HexView = null;
                _owner.LogicalChildren.Remove(item);
            }

            base.ClearItems();
        }

        /// <summary>
        /// Creates a new enumerator for the collection.
        /// </summary>
        /// <returns>The enumerator.</returns>
        public new Enumerator GetEnumerator() => new(this);

        /// <summary>
        /// Represents a column enumerator that enumerates all columns in a hex view from a left-to-right order.
        /// </summary>
        public struct Enumerator : IEnumerator<Column>
        {
            private readonly ColumnCollection _collection;
            private int _index = -1;

            internal Enumerator(ColumnCollection collection)
            {
                _collection = collection;
            }

            /// <inheritdoc />
            public Column Current => _collection[_index];

            object IEnumerator.Current => Current;

            /// <inheritdoc />
            public bool MoveNext()
            {
                _index++;
                return _index < _collection.Count;
            }

            /// <inheritdoc />
            public void Reset()
            {
                _index = 0;
            }

            /// <inheritdoc />
            public void Dispose()
            {
            }
        }
    }
}