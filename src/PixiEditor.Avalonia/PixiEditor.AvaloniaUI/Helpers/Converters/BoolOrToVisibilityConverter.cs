﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using PixiEditor.AvaloniaUI.Helpers.Converters;

namespace PixiEditor.Helpers.Converters;
internal class BoolOrToVisibilityConverter : SingleInstanceMultiValueConverter<BoolOrToVisibilityConverter>
{
    public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (bool)value;
    }

    public override object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        bool boolean = values.Aggregate(false, (acc, cur) => acc |= (cur as bool?) ?? false);
        return boolean ? true : false;
    }
}
