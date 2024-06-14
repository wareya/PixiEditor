﻿namespace PixiEditor.ChangeableDocument.Changeables.Animations;

internal class RasterKeyFrame : KeyFrame
{
    public ChunkyImage Image { get; set; }
    public Document Document { get; set; }

    private ChunkyImage originalLayerImage;

    public RasterKeyFrame(Guid targetLayerGuid, int startFrame, Document document, ChunkyImage? cloneFrom = null)
        : base(targetLayerGuid, startFrame)
    {
        Image = cloneFrom?.CloneFromCommitted() ?? new ChunkyImage(document.Size);

        Document = document;
    }
    
    public override void ActiveFrameChanged(int atFrame)
    {
        if (Document.TryFindMember<RasterLayer>(LayerGuid, out var layer))
        {
            layer.LayerImage = Image;
        }
    }

    public override void Deactivated(int atFrame)
    {
        if (Document.TryFindMember<RasterLayer>(LayerGuid, out var layer))
        {
            //layer.LayerImage = originalLayerImage;
        }
    }
}
