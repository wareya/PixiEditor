﻿using System.Collections.ObjectModel;
using PixiEditor.ViewModels.SubViewModels.Tools.ToolSettings.Settings;

namespace PixiEditor.Models.Containers.Toolbars;

internal interface IToolbar : IHandler
{
    public Setting GetSetting(string name);
    public ObservableCollection<Setting> Settings { get; set; }
    public bool SettingsGenerated { get; }
    public void GenerateSettings();
    public void SaveToolbarSettings();
    public void LoadSharedSettings();
}
