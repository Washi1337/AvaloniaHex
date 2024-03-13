using Avalonia.Media;

namespace AvaloniaHex.Rendering;

/// <summary>
/// Represents the column background rendering layer in a hex view.
/// </summary>
public class ColumnBackgroundLayer : Layer
{
    /// <inheritdoc />
    public override LayerRenderMoments UpdateMoments => LayerRenderMoments.Minimal;

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (HexView is null)
            return;

        foreach (var column in HexView.Columns)
        {
            if (column.Background is not null || column.Border is not null)
                context.DrawRectangle(column.Background, column.Border, column.Bounds);
        }
    }
}