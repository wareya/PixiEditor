﻿using Drawie.Backend.Core.Text;
using PixiEditor.Extensions.Common.Localization;
using PixiEditor.Models.Handlers.Toolbars;
using PixiEditor.ViewModels.Tools.ToolSettings.Settings;

namespace PixiEditor.ViewModels.Tools.ToolSettings.Toolbars;

internal class TextToolbar : FillableShapeToolbar, ITextToolbar
{
    public FontFamilyName FontFamily
    {
        get
        {
            return GetSetting<FontFamilySettingViewModel>(nameof(FontFamily)).Value;
        }
        set
        {
            GetSetting<FontFamilySettingViewModel>(nameof(FontFamily)).Value = value;
        }
    }

    public double FontSize
    {
        get
        {
            return GetSetting<SizeSettingViewModel>(nameof(FontSize)).Value;
        }
        set
        {
            GetSetting<SizeSettingViewModel>(nameof(FontSize)).Value = value;
        }
    }

    public double Spacing
    {
        get
        {
            return GetSetting<SizeSettingViewModel>(nameof(Spacing)).Value;
        }
        set
        {
            GetSetting<SizeSettingViewModel>(nameof(Spacing)).Value = value;
        }
    }

    public TextToolbar()
    {
        AddSetting(new FontFamilySettingViewModel(nameof(FontFamily), "FONT_LABEL"));
        var sizeSetting =
            new SizeSettingViewModel(nameof(FontSize), "FONT_SIZE_LABEL", unit: new LocalizedString("UNIT_PT"))
            {
                Value = 12
            };
        AddSetting(sizeSetting);
        var spacingSetting =
            new SizeSettingViewModel(nameof(Spacing), "SPACING_LABEL", unit: new LocalizedString("UNIT_PT"));
        spacingSetting.Value = 12;

        sizeSetting.ValueChanged += (sender, args) =>
        {
            double delta = args.NewValue - args.OldValue;
            spacingSetting.Value += delta;
        };

        AddSetting(spacingSetting);
    }

    public Font ConstructFont()
    {
        Font font = null;
        if (!string.IsNullOrEmpty(FontFamily.Name))
        {
            font = Font.FromFontFamily(FontFamily);
        }

        if (font is null)
        {
            font = Font.CreateDefault();
        }

        font.Size = (float)FontSize;

        return font;
    }
}
