﻿using Avalonia.Controls;
using Avalonia.Media;
using PixiEditor.AvaloniaUI.Views.Overlays.TransformOverlay;

namespace PixiEditor.AvaloniaUI.Views.Overlays.Handles;

public class RectangleHandle : Handle
{
    public double AnchorRadius { get; set; } = GetResource<double>("AnchorRadius");
    public RectangleHandle(Control owner) : base(owner)
    {
    }

    public override void Draw(DrawingContext context)
    {
        double scaleMultiplier = (1.0 / ZoomboxScale);
        double radius = AnchorRadius * scaleMultiplier;
        context.DrawRectangle(HandleBrush, HandlePen, TransformHelper.ToHandleRect(Position, Size, ZoomboxScale), radius, radius);
    }
}
