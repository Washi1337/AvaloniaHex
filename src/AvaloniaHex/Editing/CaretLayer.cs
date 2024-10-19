using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using AvaloniaHex.Rendering;

namespace AvaloniaHex.Editing;

/// <summary>
/// Represents the layer that renders the caret in a hex view.
/// </summary>
public class CaretLayer : Layer
{
    private readonly DispatcherTimer _blinkTimer;
    private bool _caretVisible;

    static CaretLayer()
    {
        AffectsRender<CaretLayer>(
            InsertCaretWidthProperty,
            PrimaryColumnBorderProperty,
            PrimaryColumnBackgroundProperty,
            SecondaryColumnBorderProperty,
            SecondaryColumnBackgroundProperty
        );
    }

    /// <summary>
    /// Creates a new caret layer.
    /// </summary>
    /// <param name="caret">The caret to render.</param>
    public CaretLayer(Caret caret)
    {
        Caret = caret;
        Caret.LocationChanged += CaretOnChanged;
        Caret.ModeChanged += CaretOnChanged;
        Caret.PrimaryColumnChanged += CaretOnChanged;
        IsHitTestVisible = false;

        _blinkTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(0.5),
            IsEnabled = true
        };

        _blinkTimer.Tick += BlinkTimerOnTick;
    }
    
    /// <inheritdoc />
    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);

        _blinkTimer.IsEnabled = false;
        _blinkTimer.Tick -= BlinkTimerOnTick;
    }

    /// <inheritdoc />
    public override LayerRenderMoments UpdateMoments => LayerRenderMoments.NoResizeRearrange;

    /// <summary>
    /// Gets the caret to render.
    /// </summary>
    public Caret Caret { get; }

    /// <summary>
    /// Gets or sets a value indicating whether the caret is visible.
    /// </summary>
    public bool CaretVisible
    {
        get => _caretVisible;
        set
        {
            if (_caretVisible != value)
            {
                _caretVisible = value;
                InvalidateVisual();
            }
        }
    }

    /// <summary>
    /// Defines the <see cref="BlinkingInterval"/> property.
    /// </summary>
    public static readonly DirectProperty<CaretLayer, TimeSpan> BlinkingIntervalProperty =
        AvaloniaProperty.RegisterDirect<CaretLayer, TimeSpan>(nameof(BlinkingInterval),
            x => x.BlinkingInterval,
            (x, v) => x.BlinkingInterval = v,
            unsetValue: TimeSpan.FromMilliseconds(500));

    /// <summary>
    /// Gets or sets the animation interval of the cursor blinker.
    /// </summary>
    public TimeSpan BlinkingInterval
    {
        get => _blinkTimer.Interval;
        set => _blinkTimer.Interval = value;
    }

    /// <summary>
    /// Defines the <see cref="InsertCaretWidth"/> property.
    /// </summary>
    public static readonly StyledProperty<double> InsertCaretWidthProperty =
        AvaloniaProperty.Register<CaretLayer, double>(nameof(InsertCaretWidth), 1D);

    /// <summary>
    /// Gets or sets the width of the caret when it is in insertion mode.
    /// </summary>
    public double InsertCaretWidth
    {
        get => GetValue(InsertCaretWidthProperty);
        set => SetValue(InsertCaretWidthProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the cursor of the caret is blinking.
    /// </summary>
    public bool IsBlinking
    {
        get => _blinkTimer.IsEnabled;
        set
        {
            if (_blinkTimer.IsEnabled != value)
            {
                _blinkTimer.IsEnabled = value;
                CaretVisible = true;
            }
        }
    }

    /// <summary>
    /// Defines the <see cref="PrimaryColumnBorder"/> property.
    /// </summary>
    public static readonly StyledProperty<IPen?> PrimaryColumnBorderProperty =
        AvaloniaProperty.Register<CaretLayer, IPen?>(nameof(PrimaryColumnBorder), new Pen(Brushes.Magenta));

    /// <summary>
    /// Gets or sets the pen used to draw the border of the cursor in the primary column.
    /// </summary>
    public IPen? PrimaryColumnBorder
    {
        get => GetValue(PrimaryColumnBorderProperty);
        set => SetValue(PrimaryColumnBorderProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="PrimaryColumnBackground"/> property.
    /// </summary>
    public static readonly StyledProperty<IBrush?> PrimaryColumnBackgroundProperty =
        AvaloniaProperty.Register<CaretLayer, IBrush?>(
            nameof(PrimaryColumnBackground),
            new SolidColorBrush(Colors.Magenta, 0.3D)
        );

    /// <summary>
    /// Gets or sets the brush used to draw the background of the cursor in the primary column.
    /// </summary>
    public IBrush? PrimaryColumnBackground
    {
        get => GetValue(PrimaryColumnBackgroundProperty);
        set => SetValue(PrimaryColumnBackgroundProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="SecondaryColumnBorder"/> property.
    /// </summary>
    public static readonly StyledProperty<IPen?> SecondaryColumnBorderProperty =
        AvaloniaProperty.Register<CaretLayer, IPen?>(nameof(SecondaryColumnBorder), new Pen(Brushes.DarkMagenta));

    /// <summary>
    /// Gets or sets the pen used to draw the border of the cursor in the secondary columns.
    /// </summary>
    public IPen? SecondaryColumnBorder
    {
        get => GetValue(SecondaryColumnBorderProperty);
        set => SetValue(SecondaryColumnBorderProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="PrimaryColumnBackground"/> property.
    /// </summary>
    public static readonly StyledProperty<IBrush?> SecondaryColumnBackgroundProperty =
        AvaloniaProperty.Register<CaretLayer, IBrush?>(
            nameof(SecondaryColumnBackground),
            new SolidColorBrush(Colors.DarkMagenta, 0.5D)
        );

    /// <summary>
    /// Gets or sets the brush used to draw the background of the cursor in the secondary columns.
    /// </summary>
    public IBrush? SecondaryColumnBackground
    {
        get => GetValue(SecondaryColumnBackgroundProperty);
        set => SetValue(SecondaryColumnBackgroundProperty, value);
    }

    private void BlinkTimerOnTick(object? sender, EventArgs e)
    {
        CaretVisible = !CaretVisible;
        InvalidateVisual();
    }

    private void CaretOnChanged(object? sender, EventArgs e)
    {
        CaretVisible = true;
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

        for (int i = 0; i < HexView.Columns.Count; i++)
        {
            var column = HexView.Columns[i];
            if (column is not CellBasedColumn { IsVisible: true } cellBasedColumn)
                continue;

            var bounds = cellBasedColumn.GetCellBounds(line, Caret.Location);
            if (Caret.Mode == EditingMode.Insert)
                bounds = new Rect(bounds.Left, bounds.Top, InsertCaretWidth, bounds.Height);

            if (i == Caret.PrimaryColumnIndex)
            {
                if (CaretVisible)
                    context.DrawRectangle(PrimaryColumnBackground, PrimaryColumnBorder, bounds);
            }
            else
            {
                context.DrawRectangle(SecondaryColumnBackground, SecondaryColumnBorder, bounds);
            }
        }
    }
}