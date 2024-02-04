﻿namespace PixiEditor.Extensions.Wasm.Api.LayoutBuilding;

public class Row : MultiChildLayoutElement
{
    public override CompiledControl BuildNative()
    {
        CompiledControl control = new CompiledControl(UniqueId, "Row");
        control.Children.AddRange(Children.Select(x => x.BuildNative()));

        return control;
    }
}
