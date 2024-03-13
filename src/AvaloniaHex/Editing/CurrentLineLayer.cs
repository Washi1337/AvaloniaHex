using Avalonia;
using Avalonia.Media;
using AvaloniaHex.Rendering;

namespace AvaloniaHex.Editing;

/// <summary>
/// Renders a highlight on the current active visual line.
/// </summary>
public class CurrentLineLayer : Layer
{
    static CurrentLineLayer()
    {
        AffectsRender<CurrentLineLayer>(
            CurrentLineBackgroundProperty,
            CurrentLineBorderProperty
        );
    }

    /// <summary>
    /// Creates a new current line highlighting layer.
    /// </summary>
    /// <param name="caret">The cursor to follow.</param>
    /// <param name="selection">The selection to follow.</param>
    public CurrentLineLayer(Caret caret, Selection selection)
    {
        Caret = caret;
        Selection = selection;

        Caret.LocationChanged += OnCursorChanged;
        Selection.RangeChanged += OnCursorChanged;
    }

    /// <inheritdoc />
    public override LayerRenderMoments UpdateMoments => LayerRenderMoments.NoResizeRearrange;

    /// <summary>
    /// Gets the cursor the highlighter is following.
    /// </summary>
    public Caret Caret { get; }

    /// <summary>
    /// Gets the selection the highlighter is following.
    /// </summary>
    public Selection Selection { get; }

    /// <summary>
    /// Defines the <see cref="CurrentLineBorder"/> property.
    /// </summary>
    public static readonly StyledProperty<IPen?> CurrentLineBorderProperty =
        AvaloniaProperty.Register<CurrentLineLayer, IPen?>(
            nameof(CurrentLineBorder),
            new Pen(new SolidColorBrush(Colors.DimGray), 1.5)
        );

    /// <summary>
    /// Gets or sets the brush used to draw the background of the cursor in the secondary columns.
    /// </summary>
    public IPen? CurrentLineBorder
    {
        get => GetValue(CurrentLineBorderProperty);
        set => SetValue(CurrentLineBorderProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="CurrentLineBackground"/> property.
    /// </summary>
    public static readonly StyledProperty<IBrush?> CurrentLineBackgroundProperty =
        AvaloniaProperty.Register<CurrentLineLayer, IBrush?>(
            nameof(CurrentLineBackground),
            new SolidColorBrush(Colors.DimGray, 0.1)
        );

    /// <summary>
    /// Gets or sets the brush used to draw the background of the cursor in the secondary columns.
    /// </summary>
    public IBrush? CurrentLineBackground
    {
        get => GetValue(CurrentLineBackgroundProperty);
        set => SetValue(CurrentLineBackgroundProperty, value);
    }

    private void OnCursorChanged(object? sender, EventArgs e)
    {
        InvalidateVisual();
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (HexView is null || !HexView.IsFocused)
            return;

        var line = HexView.GetVisualLineByLocation(Caret.Location);
        if (line is null)
            return;

        if (Selection.Range.ByteLength == 1)
            context.DrawRectangle(CurrentLineBackground, CurrentLineBorder, line.Bounds);
    }
}