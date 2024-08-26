﻿using System.Globalization;
using Avalonia;
using Avalonia.Media;

namespace PixiEditor.UI.Common.Rendering;

public class IconImage : IImage
{
    public string Icon { get; }
    public FontFamily FontFamily { get; }
    public double FontSize { get; }
    
    public SolidColorBrush Foreground { get; }
    public Size Size { get; }
    
    public double RotationAngle { get; }
    
    private Typeface _typeface;
    
    
    public IconImage(string icon, FontFamily fontFamily, double fontSize, Color foreground)
    {
        Icon = icon;
        FontFamily = fontFamily;
        FontSize = fontSize;
        Foreground = new SolidColorBrush(foreground);
        _typeface = new Typeface(FontFamily);
        Size = new Size(FontSize, FontSize);
    }

    public IconImage(string unicode, FontFamily fontFamily, double fontSize, Color foreground, double rotationAngle)
    {
        Icon = unicode;
        FontFamily = fontFamily;
        FontSize = fontSize;
        Foreground = new SolidColorBrush(foreground);
        _typeface = new Typeface(FontFamily);
        Size = new Size(FontSize, FontSize);
        RotationAngle = rotationAngle;
    }

    public void Draw(DrawingContext context, Rect sourceRect, Rect destRect)
    {
        if (RotationAngle != 0)
        {
            double radians = RotationAngle * Math.PI / 180;
            context.PushTransform(Matrix.CreateTranslation(destRect.Width, destRect.Height));
            context.PushTransform(Matrix.CreateRotation(radians));
            context.PushTransform(Matrix.CreateTranslation(destRect.Width / 4f, destRect.Height / 4f));
        }
        
        context.DrawText(
            new FormattedText(Icon, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, _typeface, FontSize, Foreground),
            destRect.TopLeft);
    }
}
