using System.Collections;
using System.Collections.ObjectModel;
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
    /// <inheritdoc />
    public event EventHandler? ScrollInvalidated;

    /// <summary>
    /// Fires when the document in the hex editor has changed.
    /// </summary>
    public event EventHandler<DocumentChangedEventArgs>? DocumentChanged;

    private readonly List<VisualBytesLine> _visualLines = new();
    private Vector _scrollOffset;
    private Size _extent;
    private int _actualBytesPerLine;

    static HexView()
    {
        FocusableProperty.OverrideDefaultValue<HexView>(true);

        TemplatedControl.FontFamilyProperty.Changed.AddClassHandler<HexView>(OnFontRelatedPropertyChanged);
        TemplatedControl.FontSizeProperty.Changed.AddClassHandler<HexView>(OnFontRelatedPropertyChanged);
        TemplatedControl.ForegroundProperty.Changed.AddClassHandler<HexView>(OnFontRelatedPropertyChanged);
        DocumentProperty.Changed.AddClassHandler<HexView>(OnDocumentChanged);

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

        EnsureTextProperties();

        Layers = new LayerCollection(this)
        {
            new ColumnBackgroundLayer(),
            new CellGroupsLayer(),
            new TextLayer()
        };;
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
                InvalidateVisualLines();
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
        get => _extent;
        private set
        {
            if (_extent != value)
            {
                _extent = value;
                ((ILogicalScrollable) this).RaiseScrollInvalidated(EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// Gets or sets the current scroll offset.
    /// </summary>
    public Vector ScrollOffset
    {
        get => _scrollOffset;
        set
        {
            _scrollOffset = value;
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

    Size IScrollable.Viewport => new(0, 1);

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
        ComputeBytesPerLine(finalSize);
        UpdateColumnBounds();
        UpdateVisualLines(finalSize);

        Extent = Document is not null
            ? new Size(0, Math.Ceiling((double) Document.Length / ActualBytesPerLine))
            : default;

        bool hasResized = finalSize != Bounds.Size;

        for (int i = 0; i < Layers.Count; i++)
        {
            Layers[i].Arrange(new Rect(new Point(0, 0), finalSize));

            if (hasResized || (Layers[i].UpdateMoments & LayerRenderMoments.NoResizeRearrange) != 0)
                Layers[i].InvalidateVisual();
        }

        return base.ArrangeOverride(finalSize);
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

    private void UpdateVisualLines(Size finalSize)
    {
        if (Columns.Count == 0 || Document is null)
        {
            _visualLines.Clear();

            VisibleRange = default;
            FullyVisibleRange = default;
            return;
        }

        if (Document.Length == 0)
        {
            // In case of an empty document, always ensure that there's at least one (empty) line rendered.
            _visualLines.Clear();

            var line = new VisualBytesLine(this, new BitRange(0, 1), Columns.Count);
            _visualLines.Add(line);

            line.EnsureIsValid();
            line.Bounds = new Rect(0, 0, finalSize.Width, line.GetRequiredHeight());

            VisibleRange = line.VirtualRange;
            FullyVisibleRange = VisibleRange;
            return;
        }

        var startLocation = new BitLocation((ulong) ScrollOffset.Y * (ulong) ActualBytesPerLine);

        var currentRange = new BitRange(startLocation, startLocation);

        double currentY = 0;
        while (currentY < finalSize.Height && currentRange.End.ByteIndex <= Document.Length)
        {
            // Get/create next visual line.
            var line = GetOrCreateVisualLine(new BitRange(
                currentRange.End.ByteIndex,
                Math.Min(Document.Length + 1, currentRange.End.ByteIndex + (ulong) ActualBytesPerLine)
            ));

            line.EnsureIsValid();
            line.Bounds = new Rect(0, currentY, finalSize.Width, line.GetRequiredHeight());

            // Move to next line / range.
            currentY += line.Bounds.Height;
            currentRange = line.VirtualRange;
        }

        // Compute full visible range (including lines that are only slightly visible).
        VisibleRange = _visualLines.Count == 0
            ? new BitRange(Document.Length, Document.Length)
            : new BitRange(startLocation, currentRange.End);

        // Get fully visible byte range.
        if (_visualLines.Count == 0 || !(_visualLines[^1].Bounds.Bottom > finalSize.Height))
        {
            FullyVisibleRange = VisibleRange;
        }
        else
        {
            FullyVisibleRange = new BitRange(
                VisibleRange.Start,
                new BitLocation(VisibleRange.End.ByteIndex - (ulong) ActualBytesPerLine, 0)
            );
        }

        // Cut off excess visual lines.
        for (int i = 0; i < _visualLines.Count; i++)
        {
            if (!VisibleRange.Contains(_visualLines[i].VirtualRange.Start))
                _visualLines.RemoveAt(i--);
        }
    }

    private VisualBytesLine GetOrCreateVisualLine(BitRange range)
    {
        VisualBytesLine? newLine = null;
        // Find existing line or create a new one, while keeping the list of visual lines ordered by range.
        for (int i = 0; i < _visualLines.Count; i++)
        {
            // Exact match?
            var currentLine = _visualLines[i];
            if (currentLine.VirtualRange.Start == range.Start)
            {
                // Edge-case: if our range is not exactly right, the line's range is outdated (e.g., as a result of
                // inserting or removing a character at the end of the document).
                if (currentLine.VirtualRange.End != range.End)
                    _visualLines[i] = currentLine = new VisualBytesLine(this, range, Columns.Count);

                return currentLine;
            }

            // If the next line is further than the requested start, the line does not exist.
            if (currentLine.VirtualRange.Start > range.Start)
            {
                newLine = new VisualBytesLine(this, range, Columns.Count);
                _visualLines.Insert(i, newLine);
                break;
            }
        }

        // We didn't find any line for the location, add it to the end.
        if (newLine is null)
        {
            newLine = new VisualBytesLine(this, range, Columns.Count);
            _visualLines.Add(newLine);
        }

        return newLine;
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

        for (int i = 0; i < VisualLines.Count; i++)
        {
            var line = VisualLines[i];
            if (line.VirtualRange.Contains(location))
                return line;
        }

        return null;
    }

    /// <summary>
    /// Enumerates all lines that overlap with the provided range.
    /// </summary>
    /// <param name="range">The range.</param>
    /// <returns>The lines.</returns>
    public IEnumerable<VisualBytesLine> GetVisualLinesByRange(BitRange range)
    {
        if (!VisibleRange.OverlapsWith(range))
            yield break;

        for (int i = 0; i < VisualLines.Count; i++)
        {
            var line = VisualLines[i];
            if (line.VirtualRange.OverlapsWith(range))
                yield return line;
        }
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
    /// <returns>The location of the cell, or <c>null</c> if no cell is under the provided point.</returns>
    public BitLocation? GetLocationByPoint(Point point)
    {
        if (GetColumnByPoint(point) is not CellBasedColumn column)
            return null;

        return GetLocationByPoint(point, column);
    }

    /// <summary>
    /// Gets the location of the cell within a column under the provided point.
    /// </summary>
    /// <param name="point">The point.</param>
    /// <param name="column">The column</param>
    /// <returns>The location of the cell, or <c>null</c> if no cell is under the provided point.</returns>
    public BitLocation? GetLocationByPoint(Point point, CellBasedColumn column)
    {
        if (GetVisualLineByPoint(point) is not { } line)
            return null;

        return column.GetLocationByPoint(line, point);
    }

    /// <summary>
    /// Ensures the provided bit location is put into view.
    /// </summary>
    /// <param name="location">The location to scroll to.</param>
    /// <returns><c>true</c> if the scroll offset has changed, <c>false</c> otherwise.</returns>
    public bool BringIntoView(BitLocation location)
    {
        if (location.ByteIndex >= Document?.Length + 1 || FullyVisibleRange.Contains(location))
            return false;

        ulong firstLineIndex = FullyVisibleRange.Start.ByteIndex / (ulong) ActualBytesPerLine;
        ulong lastLineIndex = (FullyVisibleRange.End.ByteIndex - 1) / (ulong) ActualBytesPerLine;
        ulong targetLineIndex = location.ByteIndex / (ulong) ActualBytesPerLine;

        ulong newIndex;

        if (location > FullyVisibleRange.End)
        {
            ulong difference = targetLineIndex - lastLineIndex;
            newIndex = firstLineIndex + difference;
        }
        else  if (location < FullyVisibleRange.Start)
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