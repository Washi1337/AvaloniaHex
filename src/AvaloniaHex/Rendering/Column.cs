using Avalonia;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;

namespace AvaloniaHex.Rendering;

/// <summary>
/// Represents a single column in a hex view.
/// </summary>
public abstract class Column : Visual
{
    internal static readonly Cursor IBeamCursor = new(StandardCursorType.Ibeam);

    private GenericTextRunProperties? _textRunProperties;

    static Column()
    {
        ForegroundProperty.Changed.AddClassHandler<Column>(OnVisualPropertyChanged);
        BackgroundProperty.Changed.AddClassHandler<Column>(OnVisualPropertyChanged);
        BorderProperty.Changed.AddClassHandler<Column>(OnVisualPropertyChanged);
        IsVisibleProperty.Changed.AddClassHandler<Column>(OnVisibleChanged);
    }

    /// <summary>
    /// Gets the parent hex view the column was added to.
    /// </summary>
    public HexView? HexView
    {
        get;
        internal set;
    }

    /// <summary>
    /// Gets the index of the column in the hex view.
    /// </summary>
    public int Index => HexView?.Columns.IndexOf(this) ?? -1;

    /// <summary>
    /// Gets the minimum size of the column.
    /// </summary>
    public abstract Size MinimumSize { get; }

    /// <summary>
    /// Dependency property for <see cref="Border"/>/
    /// </summary>
    public static readonly StyledProperty<IPen?> BorderProperty =
        AvaloniaProperty.Register<Column, IPen?>(nameof(Border));

    /// <summary>
    /// Gets or sets the pen to draw border of the column with, or <c>null</c> if no border should be drawn.
    /// </summary>
    public IPen? Border
    {
        get => GetValue(BorderProperty);
        set => SetValue(BorderProperty, value);
    }

    /// <summary>
    /// Dependency property for <see cref="Background"/>/
    /// </summary>
    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        AvaloniaProperty.Register<Column, IBrush?>(nameof(Background));

    /// <summary>
    /// Gets or sets the base background brush of the column, or <c>null</c> if no background should be drawn.
    /// </summary>
    public IBrush? Background
    {
        get => GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    /// <summary>
    /// Dependency property for <see cref="Foreground"/>/
    /// </summary>
    public static readonly StyledProperty<IBrush?> ForegroundProperty =
        AvaloniaProperty.Register<Column, IBrush?>(nameof(Foreground));

    /// <summary>
    /// Gets or sets the base foreground brush of the column, or <c>null</c> if the default foreground brush of the
    /// parent hex view should be used.
    /// </summary>
    public IBrush? Foreground
    {
        get => GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    /// <summary>
    /// Dependency property for <see cref="Cursor"/>/
    /// </summary>
    public static readonly StyledProperty<Cursor?> CursorProperty =
        AvaloniaProperty.Register<Column, Cursor?>(nameof(Cursor));

    /// <summary>
    /// Gets or sets the cursor to use in the column.
    /// </summary>
    public Cursor? Cursor
    {
        get => GetValue(CursorProperty);
        set => SetValue(CursorProperty, value);
    }

    /// <summary>
    /// Gets the column width.
    /// </summary>
    public virtual double Width => MinimumSize.Width;

    internal void SetBounds(Rect bounds) => Bounds = bounds;

    /// <summary>
    /// Gets the text run properties to use for rendering text in this column.
    /// </summary>
    /// <returns>The properties.</returns>
    /// <exception cref="InvalidOperationException">Occurs when the column is not added to a hex view.</exception>
    protected GenericTextRunProperties GetTextRunProperties()
    {
        if (HexView is null)
            throw new InvalidOperationException("Cannot query text run properties on a column that is not attached to a hex view.");

        if (!HexView.TextRunProperties.Equals(_textRunProperties))
            _textRunProperties = HexView.TextRunProperties.WithBrushes(Foreground ?? HexView.Foreground, Background);

        return _textRunProperties;
    }

    /// <summary>
    /// Refreshes the measurements required to calculate the dimensions of the column.
    /// </summary>
    public abstract void Measure();

    /// <summary>
    /// Constructs the text line of the provided hex view line for this column.
    /// </summary>
    /// <param name="line">The line to render.</param>
    /// <returns>The rendered text.</returns>
    public abstract TextLine? CreateTextLine(VisualBytesLine line);

    private static void OnVisualPropertyChanged(Column arg1, AvaloniaPropertyChangedEventArgs arg2)
    {
        arg1.HexView?.InvalidateVisualLines();
    }

    private static void OnVisibleChanged(Column arg1, AvaloniaPropertyChangedEventArgs arg2)
    {
        if (arg1.HexView is null)
            return;

        arg1.HexView.InvalidateVisualLines();
        foreach (var layer in arg1.HexView.Layers)
            layer.InvalidateVisual();
    }
}