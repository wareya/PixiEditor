﻿using ChunkyImageLib.Operations;
using PixiEditor.ChangeableDocument.Changeables.Animations;
using PixiEditor.ChangeableDocument.Changeables.Graph.Interfaces;
using PixiEditor.ChangeableDocument.Changeables.Graph.Nodes.Shapes.Data;
using PixiEditor.ChangeableDocument.Changeables.Interfaces;
using PixiEditor.ChangeableDocument.ChangeInfos.Vectors;
using PixiEditor.ChangeableDocument.Rendering;
using PixiEditor.DrawingApi.Core;
using PixiEditor.DrawingApi.Core.Numerics;
using PixiEditor.DrawingApi.Core.Surfaces;
using PixiEditor.DrawingApi.Core.Surfaces.PaintImpl;
using PixiEditor.Numerics;

namespace PixiEditor.ChangeableDocument.Changeables.Graph.Nodes;

[NodeInfo("VectorLayer")]
public class VectorLayerNode : LayerNode, ITransformableObject, IReadOnlyVectorNode, IRasterizable
{
    public Matrix3X3 TransformationMatrix
    {
        get => ShapeData?.TransformationMatrix ?? Matrix3X3.Identity;
        set
        {
            if (ShapeData == null)
            {
                return;
            }

            ShapeData.TransformationMatrix = value;
        }
    }

    public ShapeVectorData? ShapeData { get; set; }
    IReadOnlyShapeVectorData IReadOnlyVectorNode.ShapeData => ShapeData;

    protected override bool AffectedByChunkResolution => true;

    private int lastCacheHash;

    protected override Texture? OnExecute(RenderingContext context)
    {
        var rendered = base.OnExecute(context);

        Output.Value = rendered;

        return rendered;
    }

    protected override VecI GetTargetSize(RenderingContext ctx)
    {
        return ctx.DocumentSize;
    }

    protected override void DrawWithoutFilters(RenderingContext ctx, Texture workingSurface, bool shouldClear,
        Paint paint)
    {
        if (ShapeData == null)
        {
            return;
        }

        if (shouldClear)
        {
            workingSurface.DrawingSurface.Canvas.Clear();
        }

        Rasterize(workingSurface.DrawingSurface, ctx.ChunkResolution, paint);
    }

    protected override void DrawWithFilters(RenderingContext ctx, Texture workingSurface, bool shouldClear, Paint paint)
    {
        if (ShapeData == null)
        {
            return;
        }

        if (shouldClear)
        {
            workingSurface.DrawingSurface.Canvas.Clear();
        }

        Rasterize(workingSurface.DrawingSurface, ctx.ChunkResolution, paint);
    }

    public override void SerializeAdditionalData(Dictionary<string, object> additionalData)
    {
        base.SerializeAdditionalData(additionalData);
        additionalData["ShapeData"] = ShapeData;
    }

    internal override OneOf<None, IChangeInfo, List<IChangeInfo>> DeserializeAdditionalData(IReadOnlyDocument target,
        IReadOnlyDictionary<string, object> data)
    {
        base.DeserializeAdditionalData(target, data);
        ShapeData = (ShapeVectorData)data["ShapeData"];
        return new VectorShape_ChangeInfo(Id);
    }

    protected override bool CacheChanged(RenderingContext context)
    {
        return base.CacheChanged(context) || (ShapeData?.GetCacheHash() ?? -1) != lastCacheHash;
    }

    protected override void UpdateCache(RenderingContext context)
    {
        base.UpdateCache(context);
        lastCacheHash = ShapeData?.GetCacheHash() ?? -1;
    }

    public override RectD? GetTightBounds(KeyFrameTime frameTime)
    {
        return ShapeData?.TransformedAABB ?? null;
    }

    public override ShapeCorners GetTransformationCorners(KeyFrameTime frameTime)
    {
        return ShapeData?.TransformationCorners ?? new ShapeCorners();
    }

    public void Rasterize(DrawingSurface surface, ChunkResolution resolution, Paint paint)
    {
        ShapeData?.Rasterize(surface, resolution, paint);
    }

    public override Node CreateCopy()
    {
        return new VectorLayerNode() { ShapeData = (ShapeVectorData?)ShapeData?.Clone(), };
    }
}
