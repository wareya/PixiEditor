﻿namespace PixiEditor.Models.Handlers.Tools;

internal interface IPenToolHandler : IToolHandler
{
    public bool PixelPerfectEnabled { get; }
    public bool AntiAliasing { get; }
}
