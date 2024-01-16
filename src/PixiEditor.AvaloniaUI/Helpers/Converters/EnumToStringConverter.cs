﻿using PixiEditor.AvaloniaUI.Models.Dialogs;
using PixiEditor.UI.Common.Converters;

namespace PixiEditor.AvaloniaUI.Helpers.Converters;

internal class EnumToStringConverter : SingleInstanceConverter<EnumToStringConverter>
{
    public override object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        try
        {
            var type = value.GetType();
            if (type == typeof(SizeUnit))
            {
                var valueCasted = (SizeUnit)value;
                if (valueCasted == SizeUnit.Percentage)
                    return "%";

                return "PIXEL_UNIT";
            }
            return Enum.GetName((value.GetType()), value);
        }
        catch
        {
            return string.Empty;
        }
    }
}
