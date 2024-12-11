using Avalonia;
using Avalonia.Media;
using AvaloniaHex.Document;

namespace AvaloniaHex.Rendering;

/// <summary>
/// Provides utilities for computing the geometry of ranges within a hex view.
/// </summary>
public static class CellGeometryBuilder
{
    /// <summary>
    /// Computes the geometry that bounds the provided cells in a range of a column.
    /// </summary>
    /// <param name="column">The column.</param>
    /// <param name="range">The range of the cells to bound.</param>
    /// <returns>The geometry, or <c>null</c> if the range is not visible.</returns>
    public static Geometry? CreateBoundingGeometry(CellBasedColumn column, BitRange range)
    {
        if (column.HexView is null || range.IsEmpty)
            return null;

        var startLine = column.HexView.GetVisualLineByLocation(range.Start);
        var endLine = column.HexView.GetVisualLineByLocation(range.End.PreviousOrZero());
        if (startLine is null || endLine is null)
            return null;

        var startBounds = column.GetGroupBounds(startLine, range.Start);
        var endBounds = column.GetGroupBounds(endLine, range.End.PreviousOrZero());

        var geometry = new PolylineGeometry
        {
            IsFilled = true
        };

        geometry.Points.Add(startBounds.TopLeft);

        if (startLine == endLine)
        {
            geometry.Points.Add(endBounds.TopRight);
            geometry.Points.Add(endBounds.BottomRight);
        }
        else
        {
            geometry.Points.Add(new Point(column.Bounds.Right, startBounds.Top));
            geometry.Points.Add(new Point(column.Bounds.Right, endBounds.Top));
            geometry.Points.Add(endBounds.TopRight);
            geometry.Points.Add(endBounds.BottomRight);
            geometry.Points.Add(new Point(column.Bounds.Left, endBounds.Bottom));
            geometry.Points.Add(new Point(column.Bounds.Left, startBounds.Bottom));
        }

        geometry.Points.Add(startBounds.BottomLeft);

        return geometry;
    }
}