﻿using Drawie.Backend.Core.Numerics;
using Drawie.Backend.Core.Surfaces;
using Drawie.Backend.Core.Surfaces.PaintImpl;
using Drawie.Backend.Core.Vector;
using Drawie.Numerics;

namespace PixiEditor.ChangeableDocument.Changeables.Graph.Nodes.Shapes.Data;

public class PointsVectorData : ShapeVectorData
{
    public List<VecD> Points { get; set; } = new();

    public PointsVectorData(IEnumerable<VecD> points)
    {
        Points = new List<VecD>(points);
    }

    public override RectD GeometryAABB => new RectD(Points.Min(p => p.X), Points.Min(p => p.Y), Points.Max(p => p.X),
        Points.Max(p => p.Y));

    public override RectD VisualAABB => GeometryAABB;

    public override ShapeCorners TransformationCorners => new ShapeCorners(
        GeometryAABB).WithMatrix(TransformationMatrix);

    public override void RasterizeGeometry(Canvas drawingSurface)
    {
        Rasterize(drawingSurface, false);
    }

    public override void RasterizeTransformed(Canvas drawingSurface)
    {
        Rasterize(drawingSurface, true);
    }

    private void Rasterize(Canvas canvas, bool applyTransform)
    {
        using Paint paint = new Paint();
        paint.Color = FillColor;
        paint.StrokeWidth = StrokeWidth;

        int num = 0;
        if (applyTransform)
        {
            num = canvas.Save();
            Matrix3X3 final = TransformationMatrix;
            canvas.SetMatrix(final);
        }

        canvas.DrawPoints(PointMode.Points, Points.Select(p => new VecF((int)p.X, (int)p.Y)).ToArray(),
            paint);

        if (applyTransform)
        {
            canvas.RestoreToCount(num);
        }
    }

    public override bool IsValid()
    {
        return Points.Count > 0;
    }

    public override int GetCacheHash()
    {
        return CalculateHash();
    }

    public override int CalculateHash()
    {
        return Points.GetHashCode();
    }

    public override object Clone()
    {
        return new PointsVectorData(Points)
        {
            StrokeColor = StrokeColor, FillColor = FillColor, StrokeWidth = StrokeWidth
        };
    }

    public override VectorPath ToPath()
    {
        VectorPath path = new VectorPath();
        
        foreach (VecD point in Points)
        {
            path.LineTo((VecF)point);
        }
        
        return path;
    }
}
