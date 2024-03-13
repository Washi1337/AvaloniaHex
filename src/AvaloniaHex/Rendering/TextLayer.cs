using Avalonia;
using Avalonia.Media;

namespace AvaloniaHex.Rendering;

/// <summary>
/// Represents the layer that renders the text in a hex view.
/// </summary>
public class TextLayer : Layer
{
    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (HexView is null)
            return;

        double currentY = 0;
        foreach (var line in HexView.VisualLines)
        {
            foreach (var column in HexView.Columns)
            {
                if (column.IsVisible)
                    line.ColumnTextLines[column.Index]?.Draw(context, new Point(column.Bounds.Left, currentY));
            }

            currentY += line.Bounds.Height;
        }
    }
}