﻿using PixiEditor.ChangeableDocument.Changeables.Animations;
using PixiEditor.ChangeableDocument.Changeables.Graph.Interfaces;
using PixiEditor.ChangeableDocument.Changeables.Interfaces;
using PixiEditor.ChangeableDocument.ChangeInfos.Properties;
using PixiEditor.ChangeableDocument.Helpers;
using PixiEditor.ChangeableDocument.Rendering;
using PixiEditor.DrawingApi.Core;
using PixiEditor.DrawingApi.Core.ColorsImpl;
using PixiEditor.DrawingApi.Core.Surfaces;
using PixiEditor.DrawingApi.Core.Surfaces.PaintImpl;
using PixiEditor.Numerics;
using BlendMode = PixiEditor.ChangeableDocument.Enums.BlendMode;

namespace PixiEditor.ChangeableDocument.Changeables.Graph.Nodes;

public abstract class StructureNode : Node, IReadOnlyStructureNode, IRenderInput
{
    public abstract VecD ScenePosition { get; }
    public abstract VecD SceneSize { get; }

    public const string DefaultMemberName = "DEFAULT_MEMBER_NAME";
    public RenderInputProperty RenderTarget { get; }
    public InputProperty<float> Opacity { get; }
    public InputProperty<bool> IsVisible { get; }
    public bool ClipToPreviousMember { get; set; }
    public InputProperty<BlendMode> BlendMode { get; }
    public InputProperty<Texture?> CustomMask { get; }
    public InputProperty<bool> MaskIsVisible { get; }
    public InputProperty<Filter> Filters { get; }
    public RenderOutputProperty Output { get; }

    public OutputProperty<DrawingSurface?> FilterlessOutput { get; }

    public ChunkyImage? EmbeddedMask { get; set; }

    protected Texture renderedMask;
    protected static readonly Paint replacePaint = new Paint() { BlendMode = DrawingApi.Core.Surfaces.BlendMode.Src };

    public virtual ShapeCorners GetTransformationCorners(KeyFrameTime frameTime)
    {
        return new ShapeCorners(GetTightBounds(frameTime).GetValueOrDefault());
    }

    public string MemberName
    {
        get => DisplayName;
        set => DisplayName = value;
    }

    private Paint maskPaint = new Paint() { BlendMode = DrawingApi.Core.Surfaces.BlendMode.DstIn };
    protected Paint blendPaint = new Paint() { BlendMode = DrawingApi.Core.Surfaces.BlendMode.SrcOver };

    private int maskCacheHash = 0;

    protected StructureNode()
    {
        RenderTarget = CreateRenderInput("Background", "BACKGROUND", (context => Output.GetFirstRenderTarget(context)));
        
        Opacity = CreateInput<float>("Opacity", "OPACITY", 1);
        IsVisible = CreateInput<bool>("IsVisible", "IS_VISIBLE", true);
        BlendMode = CreateInput("BlendMode", "BLEND_MODE", Enums.BlendMode.Normal);
        CustomMask = CreateInput<Texture?>("Mask", "MASK", null);
        MaskIsVisible = CreateInput<bool>("MaskIsVisible", "MASK_IS_VISIBLE", true);
        Filters = CreateInput<Filter>(nameof(Filters), "FILTERS", null);

        Output = CreateRenderOutput("Output", "OUTPUT");
        FilterlessOutput = CreateOutput<DrawingSurface?>(nameof(FilterlessOutput), "WITHOUT_FILTERS", null);

        MemberName = DefaultMemberName;
    }

    protected RenderOutputProperty? CreateRenderOutput(string internalName, string displayName)
    {
        RenderOutputProperty prop = new RenderOutputProperty(this, internalName, displayName, null);
        AddOutputProperty(prop);

        return prop;
    }

    protected RenderInputProperty CreateRenderInput(string internalName, string displayName,
        Func<RenderContext, DrawingSurface> renderTarget)
    {
        RenderInputProperty prop = new RenderInputProperty(this, internalName, displayName, null, renderTarget);
        AddInputProperty(prop);

        return prop;
    }

    protected override bool AffectedByChunkResolution => true;

    protected override void OnExecute(RenderContext context)
    {
        RectD localBounds = new RectD(0, 0, SceneSize.X, SceneSize.Y);

        DrawingSurface renderTarget = RenderTarget.Value ?? Output.GetFirstRenderTarget(context);

        int savedNum = renderTarget.Canvas.Save();

        renderTarget.Canvas.ClipRect(new RectD(ScenePosition - (SceneSize / 2f), SceneSize));

        SceneObjectRenderContext renderObjectContext = new SceneObjectRenderContext(renderTarget, localBounds,
            context.FrameTime, context.ChunkResolution, context.DocumentSize, renderTarget == context.RenderSurface);

        Render(renderObjectContext);

        renderTarget.Canvas.RestoreToCount(savedNum);

        Output.Value = renderTarget;
    }

    public abstract void Render(SceneObjectRenderContext sceneContext);

    protected void ApplyMaskIfPresent(DrawingSurface surface, RenderContext context)
    {
        if (MaskIsVisible.Value)
        {
            if (CustomMask.Value != null)
            {
                surface.Canvas.DrawSurface(CustomMask.Value.DrawingSurface, 0, 0, maskPaint);
            }
            else if (EmbeddedMask != null)
            {
                // apply resolution scaling
                surface.Canvas.DrawSurface(renderedMask.DrawingSurface, 0, 0, maskPaint);
            }
        }
    }

    protected override bool CacheChanged(RenderContext context)
    {
        int cacheHash = EmbeddedMask?.GetCacheHash() ?? 0;
        return base.CacheChanged(context) || maskCacheHash != cacheHash;
    }

    protected override void UpdateCache(RenderContext context)
    {
        base.UpdateCache(context);
        maskCacheHash = EmbeddedMask?.GetCacheHash() ?? 0;
    }

    public virtual void RenderChunk(VecI chunkPos, ChunkResolution resolution, KeyFrameTime frameTime)
    {
        RenderChunkyImageChunk(chunkPos, resolution, EmbeddedMask, ref renderedMask);
    }

    protected void RenderChunkyImageChunk(VecI chunkPos, ChunkResolution resolution, ChunkyImage img,
        ref Texture? renderSurface)
    {
        if (img is null)
        {
            return;
        }

        VecI targetSize = img.LatestSize;

        if (renderSurface == null || renderSurface.Size != targetSize)
        {
            renderSurface?.Dispose();
            renderSurface = new Texture(targetSize);
        }

        img.DrawMostUpToDateChunkOn(
            chunkPos,
            ChunkResolution.Full,
            renderSurface.DrawingSurface,
            chunkPos * ChunkResolution.Full.PixelSize(),
            replacePaint);

        renderSurface.DrawingSurface.Flush();
    }

    protected void ApplyRasterClip(DrawingSurface toClip, DrawingSurface clipSource)
    {
        if (ClipToPreviousMember && RenderTarget.Value != null)
        {
            toClip.Canvas.DrawSurface(clipSource, 0, 0, maskPaint);
        }
    }

    protected bool IsEmptyMask()
    {
        return EmbeddedMask != null && MaskIsVisible.Value && !EmbeddedMask.LatestOrCommittedChunkExists();
    }

    protected bool HasOperations()
    {
        return (MaskIsVisible.Value && (EmbeddedMask != null || CustomMask.Value != null)) || ClipToPreviousMember;
    }

    protected void DrawClipSource(DrawingSurface drawOnto, IClipSource clipSource, SceneObjectRenderContext context)
    {
        blendPaint.Color = Colors.White;
        clipSource.DrawOnTexture(context, drawOnto);
    }

    protected void DrawSurface(DrawingSurface workingSurface, DrawingSurface source, RenderContext context,
        Filter? filter)
    {
        // Maybe clip rect will allow to avoid snapshotting? Idk if it will be faster
        /*
        RectI sourceRect = CalculateSourceRect(workingSurface.Size, source.Size, context);
        RectI targetRect = CalculateDestinationRect(context);
        using var snapshot = source.DrawingSurface.Snapshot(sourceRect);
        */

        blendPaint.SetFilters(filter);

        workingSurface.Canvas.DrawSurface(source, source.DeviceClipBounds.X, source.DeviceClipBounds.Y, blendPaint);
    }

    protected RectI CalculateSourceRect(VecI targetSize, VecI sourceSize, RenderContext context)
    {
        /*float divider = 1;

        if (sourceSize.X < targetSize.X || sourceSize.Y < targetSize.Y)
        {
            divider = Math.Min((float)targetSize.X / sourceSize.X, (float)targetSize.Y / sourceSize.Y);
        }

        int chunkSize = (int)Math.Round(context.ChunkResolution.PixelSize() / divider);
        VecI chunkPos = context.ChunkToUpdate.Value;

        int x = (int)(chunkPos.X * chunkSize);
        int y = (int)(chunkPos.Y * chunkSize);
        int width = (int)(chunkSize);
        int height = (int)(chunkSize);

        x = Math.Clamp(x, 0, Math.Max(sourceSize.X - width, 0));
        y = Math.Clamp(y, 0, Math.Max(sourceSize.Y - height, 0));

        return new RectI(x, y, width, height);*/

        return new RectI(0, 0, sourceSize.X, sourceSize.Y);
    }

    protected RectI CalculateDestinationRect(RenderContext context)
    {
        /*int chunkSize = context.ChunkResolution.PixelSize();
        VecI chunkPos = context.ChunkToUpdate.Value;

        int x = chunkPos.X * chunkSize;
        int y = chunkPos.Y * chunkSize;
        int width = chunkSize;
        int height = chunkSize;

        return new RectI(x, y, width, height);*/

        return new RectI(0, 0, context.DocumentSize.X, context.DocumentSize.Y);
    }

    public abstract RectD? GetTightBounds(KeyFrameTime frameTime);

    public override void SerializeAdditionalData(Dictionary<string, object> additionalData)
    {
        base.SerializeAdditionalData(additionalData);
        if (EmbeddedMask != null)
        {
            additionalData["embeddedMask"] = EmbeddedMask;
        }
    }

    internal override OneOf<None, IChangeInfo, List<IChangeInfo>> DeserializeAdditionalData(IReadOnlyDocument target,
        IReadOnlyDictionary<string, object> data)
    {
        base.DeserializeAdditionalData(target, data);
        bool hasMask = data.ContainsKey("embeddedMask");
        if (hasMask)
        {
            ChunkyImage? mask = (ChunkyImage?)data["embeddedMask"];

            EmbeddedMask?.Dispose();
            EmbeddedMask = mask;

            return new List<IChangeInfo> { new StructureMemberMask_ChangeInfo(Id, mask != null) };
        }

        return new None();
    }

    public override void Dispose()
    {
        Output.Value = null;
        base.Dispose();
        maskPaint.Dispose();
        blendPaint.Dispose();
    }
}
