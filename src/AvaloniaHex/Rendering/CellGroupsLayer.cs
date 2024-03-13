using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Media;
using AvaloniaHex.Document;

namespace AvaloniaHex.Rendering;

/// <summary>
/// Provides a render layer for a hex view that visually separates groups of cells.
/// </summary>
public class CellGroupsLayer : Layer
{
    static CellGroupsLayer()
    {
        AffectsRender<CellGroupsLayer>(
            BytesPerGroupProperty,
            BorderProperty,
            BackgroundsProperty
        );
    }

    /// <summary>
    /// Defines the <see cref="BytesPerGroupProperty"/> property.
    /// </summary>
    public static readonly StyledProperty<int> BytesPerGroupProperty =
        AvaloniaProperty.Register<CellGroupsLayer, int>(nameof(BytesPerGroup), 8);

    /// <summary>
    /// Gets or sets a value indicating the number of cells each group consists of.
    /// </summary>
    public int BytesPerGroup
    {
        get => GetValue(BytesPerGroupProperty);
        set => SetValue(BytesPerGroupProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="Border"/> property.
    /// </summary>
    public static readonly StyledProperty<IPen?> BorderProperty =
        AvaloniaProperty.Register<CellGroupsLayer, IPen?>(
            nameof(Border));

    /// <summary>
    /// Gets or sets the pen used for rendering the separation lines between each group.
    /// </summary>
    public IPen? Border
    {
        get => GetValue(BorderProperty);
        set => SetValue(BorderProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="Backgrounds"/> property.
    /// </summary>
    public static readonly DirectProperty<CellGroupsLayer, ObservableCollection<IBrush?>> BackgroundsProperty =
        AvaloniaProperty.RegisterDirect<CellGroupsLayer, ObservableCollection<IBrush?>>(
            nameof(Backgrounds),
            x => x.Backgrounds
        );

    /// <summary>
    /// Gets a collection of background brushes that each vertical cell group is rendered with.
    /// </summary>
    public ObservableCollection<IBrush?> Backgrounds { get; } = new();

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (HexView is null || Border is null || HexView.VisualLines.Count == 0)
            return;

        foreach (var c in HexView.Columns)
        {
            if (c is not CellBasedColumn { IsVisible: true } column)
                continue;

            DivideColumn(context, column);
        }
    }

    private void DivideColumn(DrawingContext context, CellBasedColumn column)
    {
        int groupIndex = 0;

        double left = column.Bounds.Left;

        var line = HexView!.VisualLines[0];
        for (uint offset = 0; offset < HexView.ActualBytesPerLine; offset += (uint)BytesPerGroup, groupIndex++)
        {
            var right1 = new BitLocation(line.Range.Start.ByteIndex + (uint)BytesPerGroup + offset - 1, 0).Clamp(line.Range);
            var right2 = new BitLocation(line.Range.Start.ByteIndex + (uint)BytesPerGroup + offset, 7).Clamp(line.Range);
            var rightCell1 = column.GetCellBounds(line, right1);
            var rightCell2 = column.GetCellBounds(line, right2);

            double right = Math.Min(column.Bounds.Right, 0.5 * (rightCell1.Right + rightCell2.Left));

            if (Backgrounds.Count > 0)
            {
                var background = Backgrounds[groupIndex % Backgrounds.Count];
                if (background is not null)
                    context.FillRectangle(background, new Rect(left, 0, right - left, column.Bounds.Height));
            }

            if (groupIndex > 0)
            {
                context.DrawLine(
                    Border!,
                    new Point(left, 0),
                    new Point(left, HexView.Bounds.Height)
                );
            }

            left = right;
        }
    }
}