﻿using System.Globalization;
using Avalonia;
using PixiEditor.UI.Common.Converters;

namespace PixiEditor.AvaloniaUI.Helpers.Converters;

internal class IntToViewportRectConverter
    : SingleInstanceConverter<IntToViewportRectConverter>
{
    public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return parameter is string and "vertical"
            ? new Rect(0, 0, 1d / (int)value, 1d)
            : (object)new Rect(0, 0, 1d, 1d / (int)value);
    }
}
