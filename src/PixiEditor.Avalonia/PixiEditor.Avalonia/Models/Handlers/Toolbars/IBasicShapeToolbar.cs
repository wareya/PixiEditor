﻿using Avalonia.Media;

namespace PixiEditor.Models.Containers.Toolbars;

public interface IBasicShapeToolbar : IBasicToolbar
{
    public bool Fill { get; }
    public Color FillColor { get; }
}
