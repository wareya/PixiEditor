﻿using PixiEditor.DrawingApi.Core.Surfaces;
using PixiEditor.DrawingApi.Core.Surfaces.Vector;
using PixiEditor.Numerics;

namespace PixiEditor.ChangeableDocument.Changeables.Graph.Nodes.Shapes.Data;

public class EllipseVectorData : ShapeVectorData
{
    public VecD Radius { get; set; }
    public VecD Center { get; set; }
    
    public double RotationRadians { get; set; } = 0;

    public override RectD GeometryAABB =>
        new ShapeCorners(Center, Radius * 2).AsRotated(RotationRadians, Center).AABBBounds;

    public override ShapeCorners TransformationCorners =>
        new ShapeCorners(Center, Radius * 2).AsRotated(RotationRadians, Center).WithMatrix(TransformationMatrix);


    public EllipseVectorData(VecD center, VecD radius)
    {
        Center = center;
        Radius = radius;
    }

    public override void Rasterize(DrawingSurface drawingSurface, ChunkResolution resolution)
    {
        var imageSize = (VecI)(Radius * 2);
        
        using ChunkyImage img = new ChunkyImage((VecI)GeometryAABB.Size);

        RectD rotated = new ShapeCorners(RectD.FromTwoPoints(VecD.Zero, imageSize))
            .AsRotated(RotationRadians, imageSize / 2f).AABBBounds;

        VecI shift = new VecI((int)Math.Floor(-rotated.Left), (int)Math.Floor(-rotated.Top));
        RectI drawRect = new(shift, imageSize);
        
        img.EnqueueDrawEllipse(drawRect, StrokeColor, FillColor, StrokeWidth, RotationRadians);
        img.CommitChanges();

        VecI topLeft = new VecI((int)Math.Round(Center.X - Radius.X), (int)Math.Round(Center.Y - Radius.Y)) - shift;
        
        RectI region = new(VecI.Zero, (VecI)GeometryAABB.Size);

        int num = drawingSurface.Canvas.Save();
        drawingSurface.Canvas.SetMatrix(TransformationMatrix);

        img.DrawMostUpToDateRegionOn(region, resolution, drawingSurface, topLeft);
        
        drawingSurface.Canvas.RestoreToCount(num);
    }

    public override bool IsValid()
    {
        return Radius is { X: > 0, Y: > 0 };
    }

    public override int CalculateHash()
    {
        return HashCode.Combine(Center, Radius, StrokeColor, FillColor, StrokeWidth, RotationRadians, TransformationMatrix);
    }

    public override int GetCacheHash()
    {
        return CalculateHash();
    }

    public override object Clone()
    {
        return new EllipseVectorData(Center, Radius)
        {
            StrokeColor = StrokeColor,
            FillColor = FillColor,
            StrokeWidth = StrokeWidth,
            RotationRadians = RotationRadians,
            TransformationMatrix = TransformationMatrix
        };
    }
}
