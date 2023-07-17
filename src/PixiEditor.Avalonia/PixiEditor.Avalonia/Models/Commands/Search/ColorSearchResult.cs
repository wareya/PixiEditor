﻿using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using PixiEditor.Extensions.Palettes;
using PixiEditor.Helpers.Extensions;
using PixiEditor.Models.Containers;

namespace PixiEditor.Models.Commands.Search;

internal class ColorSearchResult : SearchResult
{
    private readonly DrawingImage icon;
    private readonly DrawingApi.Core.ColorsImpl.Color color;
    private string text;
    private bool requiresDocument;
    private bool isPalettePaste;
    private readonly Action<DrawingApi.Core.ColorsImpl.Color> target;

    public override string Text => text;

    public override AvaloniaObject Description => new TextBlock()
    {
        Text = $"{color} rgba({color.R}, {color.G}, {color.B}, {color.A})",
        FontSize = 16,
        TextDecorations = GetDecoration(1, new SolidColorBrush(color.ToOpaqueMediaColor()))
    };

    //public override bool CanExecute => !requiresDocument || (requiresDocument && ViewModelMain.Current.BitmapManager.ActiveDocument != null);
    public override bool CanExecute => !isPalettePaste || (IDocumentHandler.Instance != null && IDocumentHandler.Instance.HasActiveDocument);

    public override IImage Icon => icon;

    public override Task Execute()
    {
        target(color);
        return Task.CompletedTask;
    }

    private ColorSearchResult(DrawingApi.Core.ColorsImpl.Color color, Action<DrawingApi.Core.ColorsImpl.Color> target)
    {
        this.color = color;
        icon = GetIcon(color);
        this.target = target;
    }

    public ColorSearchResult(DrawingApi.Core.ColorsImpl.Color color) : this(color, x => IColorsHandler.Instance.PrimaryColor = x)
    {
        text = $"Set color to {color}";
    }

    public static ColorSearchResult PastePalette(DrawingApi.Core.ColorsImpl.Color color, string searchTerm = null)
    {
        var result = new ColorSearchResult(color, x =>
            IDocumentHandler.Instance.ActiveDocument.Palette.Add(new PaletteColor(x.R, x.G, x.B)))
        {
            SearchTerm = searchTerm,
            isPalettePaste = true
        };
        result.text = $"Add color {color} to palette";
        result.requiresDocument = true;

        return result;
    }

    public static DrawingImage GetIcon(DrawingApi.Core.ColorsImpl.Color color)
    {
        var drawing = new GeometryDrawing() { Brush = new SolidColorBrush(color.ToOpaqueMediaColor()), Pen = new Pen(Brushes.White, 1) };
        var geometry = new EllipseGeometry(new Rect(5, 5,5, 5));
        drawing.Geometry = geometry;
        return new DrawingImage(drawing);
    }

    private static TextDecorationCollection GetDecoration(double strokeThickness, SolidColorBrush solidColorBrush) => new()
    {
        new TextDecoration()
        {
            Location = TextDecorationLocation.Underline,
            StrokeThicknessUnit = TextDecorationUnit.Pixel,
            StrokeOffsetUnit = TextDecorationUnit.Pixel,
            StrokeThickness = strokeThickness,
            Stroke = solidColorBrush,
            StrokeOffset = 0,
        },
    };
}
