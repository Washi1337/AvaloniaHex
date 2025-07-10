using Avalonia;
using Avalonia.Media;

namespace AvaloniaHex.Rendering;

/// <summary>
/// Represents the layer that renders the header in a hex view.
/// </summary>
public class HeaderLayer : Layer
{
    /// <summary>
    /// Dependency property for <see cref="HeaderBackground"/>
    /// </summary>
    public static readonly StyledProperty<IBrush?> HeaderBackgroundProperty =
        AvaloniaProperty.Register<HeaderLayer, IBrush?>(nameof(HeaderBackground));

    /// <summary>
    /// Gets or sets the base background brush that is used for rendering the header, or <c>null</c> if no background
    /// should be drawn.
    /// </summary>
    public IBrush? HeaderBackground
    {
        get => GetValue(HeaderBackgroundProperty);
        set => SetValue(HeaderBackgroundProperty, value);
    }

    /// <summary>
    /// Dependency property for <see cref="HeaderBorder"/>
    /// </summary>
    public static readonly StyledProperty<IPen?> HeaderBorderProperty =
        AvaloniaProperty.Register<HeaderLayer, IPen?>(nameof(HeaderBorder));

    /// <summary>
    /// Gets or sets the base border pen that is used for rendering the border of the header, or <c>null</c> if no
    /// border should be drawn.
    /// </summary>
    public IPen? HeaderBorder
    {
        get => GetValue(HeaderBorderProperty);
        set => SetValue(HeaderBorderProperty, value);
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (HexView is not {IsHeaderVisible: true})
            return;

        // Do we even have a header?
        double headerSize = HexView.EffectiveHeaderSize;
        if (headerSize <= 0)
            return;

        // Render base background + border when necessary.
        if (HeaderBackground is not null || HeaderBorder is not null)
            context.DrawRectangle(HeaderBackground, HeaderBorder, new Rect(0, 0, Bounds.Width, headerSize));

        var padding = HexView.HeaderPadding;
        for (int i = 0; i < HexView.Columns.Count; i++)
        {
            var column = HexView.Columns[i];

            // Only draw headers that are visible.
            if (column is not {IsVisible: true, IsHeaderVisible: true})
                continue;

            // Draw background + border when necessary.
            if (column.HeaderBackground is not null || column.HeaderBorder is not null)
            {
                context.DrawRectangle(
                    column.HeaderBackground,
                    column.HeaderBorder,
                    new Rect(column.Bounds.Left, 0, column.Bounds.Width, headerSize)
                );
            }

            // Draw header text.
            HexView.Headers[i]?.Draw(context, new Point(column.Bounds.Left, padding.Top));
        }
    }
}