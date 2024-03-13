using Avalonia;
using Avalonia.Media;
using AvaloniaHex.Document;
using AvaloniaHex.Rendering;

namespace AvaloniaHex.Editing;

/// <summary>
/// Represents the layer that renders the selection in a hex view.
/// </summary>
public class SelectionLayer : Layer
{
    private readonly Selection _selection;
    private readonly Caret _caret;

    static SelectionLayer()
    {
        AffectsRender<SelectionLayer>(
            PrimarySelectionBorderProperty,
            PrimarySelectionBackgroundProperty,
            SecondarySelectionBorderProperty,
            SecondarySelectionBackgroundProperty
        );
    }

    /// <inheritdoc />
    public override LayerRenderMoments UpdateMoments => LayerRenderMoments.NoResizeRearrange;

    /// <summary>
    /// Creates a new selection layer.
    /// </summary>
    /// <param name="caret">The caret the selection is following.</param>
    /// <param name="selection">The selection to render.</param>
    public SelectionLayer(Caret caret, Selection selection)
    {
        _selection = selection;
        _caret = caret;
        _selection.RangeChanged += SelectionOnRangeChanged;
        _caret.PrimaryColumnChanged += CaretOnPrimaryColumnChanged;
    }

    /// <summary>
    /// Defines the <see cref="PrimarySelectionBorder"/> property.
    /// </summary>
    public static readonly StyledProperty<IPen?> PrimarySelectionBorderProperty =
        AvaloniaProperty.Register<SelectionLayer, IPen?>(
            nameof(PrimarySelectionBorder),
            new Pen(Brushes.Blue)
        );

    /// <summary>
    /// Gets or sets the pen used for drawing the border of the selection in the active column.
    /// </summary>
    public IPen? PrimarySelectionBorder
    {
        get => GetValue(PrimarySelectionBorderProperty);
        set => SetValue(PrimarySelectionBorderProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="PrimarySelectionBorder"/> property.
    /// </summary>
    public static readonly StyledProperty<IBrush?> PrimarySelectionBackgroundProperty =
        AvaloniaProperty.Register<SelectionLayer, IBrush?>(
            nameof(PrimarySelectionBackground),
            new SolidColorBrush(Colors.Blue, 0.5D)
        );

    /// <summary>
    /// Gets or sets the brush used for drawing the background of the selection in the active column.
    /// </summary>
    public IBrush? PrimarySelectionBackground
    {
        get => GetValue(PrimarySelectionBackgroundProperty);
        set => SetValue(PrimarySelectionBackgroundProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="PrimarySelectionBorder"/> property.
    /// </summary>
    public static readonly StyledProperty<IPen?> SecondarySelectionBorderProperty =
        AvaloniaProperty.Register<SelectionLayer, IPen?>(
            nameof(PrimarySelectionBorder),
            new Pen(Brushes.Blue)
        );

    /// <summary>
    /// Gets or sets the pen used for drawing the border of the selection in non-active columns.
    /// </summary>
    public IPen? SecondarySelectionBorder
    {
        get => GetValue(SecondarySelectionBorderProperty);
        set => SetValue(SecondarySelectionBorderProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="PrimarySelectionBorder"/> property.
    /// </summary>
    public static readonly StyledProperty<IBrush?> SecondarySelectionBackgroundProperty =
        AvaloniaProperty.Register<SelectionLayer, IBrush?>(
            nameof(SecondarySelectionBackgroundProperty),
            new SolidColorBrush(Colors.Blue, 0.25D)
        );

    /// <summary>
    /// Gets or sets the brush used for drawing the background of the selection in non-active columns.
    /// </summary>
    public IBrush? SecondarySelectionBackground
    {
        get => GetValue(SecondarySelectionBackgroundProperty);
        set => SetValue(SecondarySelectionBackgroundProperty, value);
    }

    private void SelectionOnRangeChanged(object? sender, EventArgs e)
    {
        InvalidateVisual();
    }

    private void CaretOnPrimaryColumnChanged(object? sender, EventArgs e)
    {
        InvalidateVisual();
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (HexView is null || GetVisibleSelectionRange() is not { } range)
            return;

        for (int i = 0; i < HexView.Columns.Count; i++)
        {
            if (HexView.Columns[i] is CellBasedColumn { IsVisible: true } column)
                DrawSelection(context, column, range);
        }
    }

    private BitRange? GetVisibleSelectionRange()
    {
        if (HexView is null || !_selection.Range.OverlapsWith(HexView.VisibleRange))
            return null;

        return new BitRange(
            _selection.Range.Start.Max(HexView.VisibleRange.Start),
            _selection.Range.End.Min(HexView.VisibleRange.End)
        );
    }

    private void DrawSelection(DrawingContext context, CellBasedColumn column, BitRange range)
    {
        var geometry = CellGeometryBuilder.CreateBoundingGeometry(column, range);
        if (geometry is null)
            return;

        if (_caret.PrimaryColumnIndex == column.Index)
            context.DrawGeometry(PrimarySelectionBackground, PrimarySelectionBorder, geometry);
        else
            context.DrawGeometry(SecondarySelectionBackground, SecondarySelectionBorder, geometry);
    }

}