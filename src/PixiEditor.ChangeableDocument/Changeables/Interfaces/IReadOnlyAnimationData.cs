﻿namespace PixiEditor.ChangeableDocument.Changeables.Interfaces;

public interface IReadOnlyAnimationData
{
    public IReadOnlyList<IReadOnlyKeyFrame> KeyFrames { get; }
    public bool TryFindKeyFrame<T>(Guid id, out T keyFrame) where T : IReadOnlyKeyFrame;
}
