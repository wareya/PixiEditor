﻿using PixiEditor.Extensions.Wasm;

namespace Preferences;

public class PreferencesSampleExtension : WasmExtension
{
    /// <summary>
    ///     This method is called when extension is loaded.
    ///  All extensions are first loaded and then initialized. This method is called before <see cref="OnInitialized"/>.
    /// </summary>
    public override void OnLoaded()
    {
    }

    /// <summary>
    ///     This method is called when extension is initialized. After this method is called, you can use Api property to access PixiEditor API.
    /// </summary>
    public override void OnInitialized()
    {
        int helloCount = Api.Preferences.GetPreference<int>("HelloCount");

        Api.Logger.Log($"Hello count: {helloCount}");

        Api.Preferences.UpdatePreference("HelloCount", helloCount + 1);
        Api.Preferences.Save();
    }
}