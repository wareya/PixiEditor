﻿namespace PixiEditor.Models.Handlers.Tools;

internal interface IFloodFillToolHandler : IToolHandler
{
    public bool ConsiderAllLayers { get; }
}
