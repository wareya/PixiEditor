﻿namespace PixiEditor.AvaloniaUI.Models.Handlers;

internal interface IAnimationHandler
{
    public IReadOnlyCollection<IKeyFrameHandler> KeyFrames { get; }
    public int ActiveFrameBindable { get; set; }
    public void CreateRasterKeyFrame(Guid targetLayerGuid, int frame, bool cloneFromExisting);
    public void SetActiveFrame(int newFrame);
    public void SetFrameLength(Guid keyFrameId, int newStartFrame, int newDuration);
    public bool FindKeyFrame<T>(Guid guid, out T keyFrameHandler) where T : IKeyFrameHandler;
    internal void AddKeyFrame(IKeyFrameHandler keyFrame);
    internal void RemoveKeyFrame(Guid keyFrameId);
}
