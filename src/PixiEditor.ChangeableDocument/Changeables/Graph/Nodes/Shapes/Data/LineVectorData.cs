﻿using PixiEditor.ChangeableDocument.Changeables.Graph.Interfaces.Shapes;
using PixiEditor.DrawingApi.Core.ColorsImpl;
using PixiEditor.DrawingApi.Core.Surfaces;
using PixiEditor.DrawingApi.Core.Surfaces.PaintImpl;
using PixiEditor.Numerics;

namespace PixiEditor.ChangeableDocument.Changeables.Graph.Nodes.Shapes.Data;

public class LineVectorData(VecD startPos, VecD pos) : ShapeVectorData, IReadOnlyLineData
{
    public VecD Start { get; set; } = startPos;
    public VecD End { get; set; } = pos;
    
    public override RectD GeometryAABB
    {
        get
        {
            if (StrokeWidth == 1)
            {
                return RectD.FromTwoPoints(Start, End);
            }
            
            VecD halfStroke = new(StrokeWidth / 2f);
            VecD min = new VecD(Math.Min(Start.X, End.X), Math.Min(Start.Y, End.Y)) - halfStroke;
            VecD max = new VecD(Math.Max(Start.X, End.X), Math.Max(Start.Y, End.Y)) + halfStroke;
            
            return new RectD(min, max - min);
        }
    }

    public override ShapeCorners TransformationCorners => new ShapeCorners(GeometryAABB)
        .WithMatrix(TransformationMatrix);

    public override void Rasterize(DrawingSurface drawingSurface, ChunkResolution resolution, Paint paint)
    {
        RectD adjustedAABB = GeometryAABB.RoundOutwards().Inflate(1);
        var imageSize = (VecI)adjustedAABB.Size;
        
        using ChunkyImage img = new ChunkyImage(imageSize);

        if (StrokeWidth == 1)
        {
            img.EnqueueDrawBresenhamLine(
                (VecI)Start - (VecI)adjustedAABB.TopLeft,
                (VecI)End - (VecI)adjustedAABB.TopLeft, StrokeColor, BlendMode.SrcOver);
        }
        else
        {
            img.EnqueueDrawSkiaLine(
                (VecI)Start - (VecI)adjustedAABB.TopLeft,
                (VecI)End - (VecI)adjustedAABB.TopLeft, StrokeCap.Butt, StrokeWidth, StrokeColor, BlendMode.SrcOver);
        }

        img.CommitChanges();
        
        VecI topLeft = (VecI)adjustedAABB.TopLeft; 
        
        RectI region = new(VecI.Zero, imageSize);
        
        int num = drawingSurface.Canvas.Save();
        drawingSurface.Canvas.SetMatrix(TransformationMatrix);
        
        img.DrawMostUpToDateRegionOn(region, resolution, drawingSurface, topLeft, paint);
        
        drawingSurface.Canvas.RestoreToCount(num);
    }

    public override bool IsValid()
    {
        return Start != End;
    }

    public override int GetCacheHash()
    {
        return HashCode.Combine(Start, End, StrokeColor, StrokeWidth, TransformationMatrix);
    }

    public override int CalculateHash()
    {
        return GetCacheHash();
    }

    public override object Clone()
    {
        return new LineVectorData(Start, End)
        {
            StrokeColor = StrokeColor,
            StrokeWidth = StrokeWidth,
            TransformationMatrix = TransformationMatrix
        };
    }
}
