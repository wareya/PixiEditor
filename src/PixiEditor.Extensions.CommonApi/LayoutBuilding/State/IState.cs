﻿namespace PixiEditor.Extensions.CommonApi.LayoutBuilding.State;

public interface IState<out TBuild>
{
    public ILayoutElement<TBuild> Build();
    public void SetState(Action setAction);
    public event Action StateChanged;
}
