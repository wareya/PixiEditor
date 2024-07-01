﻿using PixiEditor.ChangeableDocument.Changeables.Interfaces;
using PixiEditor.ChangeableDocument.Rendering;
using PixiEditor.DrawingApi.Core.Numerics;
using PixiEditor.DrawingApi.Core.Surface;
using PixiEditor.DrawingApi.Core.Surface.PaintImpl;
using PixiEditor.Numerics;

namespace PixiEditor.ChangeableDocument.Changes.Drawing.FloodFill;

internal class FloodFillChunkCache : IDisposable
{
    private Paint ReplacingPaint { get; } = new Paint() { BlendMode = BlendMode.Src };

    private readonly HashSet<Guid>? membersToRender;
    private readonly IReadOnlyFolder? structureRoot;
    private readonly IReadOnlyChunkyImage? image;
    private readonly int frame;

    private readonly Dictionary<VecI, OneOf<Chunk, EmptyChunk>> acquiredChunks = new();

    public FloodFillChunkCache(IReadOnlyChunkyImage image)
    {
        this.image = image;
    }

    public FloodFillChunkCache(HashSet<Guid> membersToRender, IReadOnlyFolder structureRoot, int frame)
    {
        this.membersToRender = membersToRender;
        this.structureRoot = structureRoot;
        this.frame = frame;
    }

    public bool ChunkExistsInStorage(VecI pos)
    {
        if (image is not null)
            return acquiredChunks.ContainsKey(pos);
        return true;
    }

    public OneOf<Chunk, EmptyChunk> GetChunk(VecI pos)
    {
        // the chunk was already acquired before, return cached
        if (acquiredChunks.TryGetValue(pos, out var foundChunk))
            return foundChunk;

        // need to get the chunk by merging multiple members
        if (image is null)
        {
            if (structureRoot is null || membersToRender is null)
                throw new InvalidOperationException();
            var chunk = ChunkRenderer.MergeChosenMembers(pos, ChunkResolution.Full, structureRoot, frame, membersToRender);
            acquiredChunks[pos] = chunk;
            return chunk;
        }

        // there is only a single image, just get the chunk from it
        if (!image.LatestOrCommittedChunkExists(pos))
            return new EmptyChunk();
        Chunk chunkOnImage = Chunk.Create(ChunkResolution.Full);

        if (!image.DrawMostUpToDateChunkOn(pos, ChunkResolution.Full, chunkOnImage.Surface.DrawingSurface, VecI.Zero, ReplacingPaint))
        {
            chunkOnImage.Dispose();
            acquiredChunks[pos] = new EmptyChunk();
            return new EmptyChunk();
        }
        acquiredChunks[pos] = chunkOnImage;
        return chunkOnImage;
    }

    public void Dispose()
    {
        foreach (var chunk in acquiredChunks.Values.Where(static chunk => chunk.IsT0))
            chunk.AsT0.Dispose();
        ReplacingPaint.Dispose();
    }
}
