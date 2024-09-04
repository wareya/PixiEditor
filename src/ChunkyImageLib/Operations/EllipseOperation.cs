﻿using ChunkyImageLib.DataHolders;
using PixiEditor.DrawingApi.Core.ColorsImpl;
using PixiEditor.DrawingApi.Core.Numerics;
using PixiEditor.DrawingApi.Core.Surfaces;
using PixiEditor.DrawingApi.Core.Surfaces.PaintImpl;
using PixiEditor.DrawingApi.Core.Surfaces.Vector;
using PixiEditor.Numerics;

namespace ChunkyImageLib.Operations;
internal class EllipseOperation : IMirroredDrawOperation
{
    public bool IgnoreEmptyChunks => false;

    private readonly RectI location;
    private readonly Color strokeColor;
    private readonly Color fillColor;
    private readonly int strokeWidth;
    private readonly double rotation;
    private readonly Paint paint;
    private bool init = false;
    private VectorPath? outerPath;
    private VectorPath? innerPath;
    private Point[]? ellipse;
    private Point[]? ellipseFill;
    private RectI? ellipseFillRect;

    public EllipseOperation(RectI location, Color strokeColor, Color fillColor, int strokeWidth, double rotationRad, Paint? paint = null)
    {
        this.location = location;
        this.strokeColor = strokeColor;
        this.fillColor = fillColor;
        this.strokeWidth = strokeWidth;
        this.rotation = rotationRad;
        this.paint = paint?.Clone() ?? new Paint();
    }

    private void Init()
    {
        init = true;
        if (strokeWidth == 1)
        {
            var ellipseList = EllipseHelper.GenerateEllipseFromRect(location, rotation);

            ellipse = ellipseList.Select(a => new Point(a)).Distinct().ToArray();
            if (fillColor.A > 0 || paint.BlendMode != BlendMode.SrcOver)
            {
                /*(var fill, ellipseFillRect) = EllipseHelper.SplitEllipseFillIntoRegions(ellipseList, location);
                ellipseFill = fill.Select(a => new Point(a)).ToArray();*/
            }
        }
        else
        {
            outerPath = new VectorPath();
            outerPath.ArcTo(location, 0, 359, true);
            innerPath = new VectorPath();
            innerPath.ArcTo(location.Inflate(-strokeWidth), 0, 359, true);
        }
    }

    public void DrawOnChunk(Chunk targetChunk, VecI chunkPos)
    {
        if (!init)
            Init();
        var surf = targetChunk.Surface.DrawingSurface;
        surf.Canvas.Save();
        surf.Canvas.Scale((float)targetChunk.Resolution.Multiplier());
        surf.Canvas.Translate(-chunkPos * ChunkyImage.FullChunkSize);

        paint.IsAntiAliased = targetChunk.Resolution != ChunkResolution.Full;

        if (strokeWidth == 1)
        {
            if (fillColor.A > 0 || paint.BlendMode != BlendMode.SrcOver)
            {
                /*paint.Color = fillColor;
                surf.Canvas.DrawPoints(PointMode.Lines, ellipseFill!, paint);
                surf.Canvas.DrawRect(ellipseFillRect!.Value, paint);*/
            }
            paint.Color = strokeColor;
            surf.Canvas.DrawPoints(PointMode.Points, ellipse!, paint);
        }
        else
        {
            if (fillColor.A > 0 || paint.BlendMode != BlendMode.SrcOver)
            {
                surf.Canvas.Save();
                surf.Canvas.ClipPath(innerPath!);
                surf.Canvas.DrawColor(fillColor, paint.BlendMode);
                surf.Canvas.Restore();
            }
            surf.Canvas.Save();
            surf.Canvas.ClipPath(outerPath!);
            surf.Canvas.ClipPath(innerPath!, ClipOperation.Difference);
            surf.Canvas.DrawColor(strokeColor, paint.BlendMode);
            surf.Canvas.Restore();
        }
        surf.Canvas.Restore();
    }

    public AffectedArea FindAffectedArea(VecI imageSize)
    {
        ShapeCorners corners = new((RectD)location);
        corners = corners.AsRotated(rotation, (VecD)location.Center);
        RectI bounds = (RectI)corners.AABBBounds;
        
        /*VecI shift = new VecI(Math.Max(0, -aabb.X), Math.Max(0, -aabb.Y));
        aabb = aabb.Offset(shift);*/
        
        var chunks = OperationHelper.FindChunksTouchingRectangle(bounds, ChunkyImage.FullChunkSize);
        if (fillColor.A == 0)
        {
            // TODO: Implement ellipse fill optimization for rotated ellipses
            /*
            chunks.ExceptWith(OperationHelper.FindChunksFullyInsideEllipse
                (location.Center, location.Width / 2.0 - strokeWidth * 2, location.Height / 2.0 - strokeWidth * 2, ChunkyImage.FullChunkSize));
        */
        }
        return new AffectedArea(chunks, bounds);
    }

    public IDrawOperation AsMirrored(double? verAxisX, double? horAxisY)
    {
        RectI newLocation = location;
        if (verAxisX is not null)
            newLocation = (RectI)newLocation.ReflectX((double)verAxisX).Round();
        if (horAxisY is not null)
            newLocation = (RectI)newLocation.ReflectY((double)horAxisY).Round();
        return new EllipseOperation(newLocation, strokeColor, fillColor, strokeWidth, rotation, paint);
    }

    public void Dispose()
    {
        paint.Dispose();
        outerPath?.Dispose();
        innerPath?.Dispose();
    }
}
