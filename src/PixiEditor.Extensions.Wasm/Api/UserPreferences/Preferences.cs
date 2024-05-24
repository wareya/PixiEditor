﻿using PixiEditor.Extensions.CommonApi.UserPreferences;
using PixiEditor.Extensions.Wasm.Bridge;

namespace PixiEditor.Extensions.Wasm.Api.UserPreferences;

/// <summary>
///     Add, remove and update user preferences.
/// </summary>
public class Preferences : IPreferences
{
    /// <summary>
    ///     Save preferences to disk. This usually happens automatically during updating preferences, but you can call this method to force saving.
    /// </summary>
    public void Save()
    {
        Native.save_preferences();
    }
    
    /// <summary>
    ///     Update user preference by name.
    /// </summary>
    /// <param name="name">Name of the preference. You can use "ExtensionUniqueName:PreferenceName" or "PreferenceName" schema. To access PixiEditor's built-in preferences, use "PixiEditor:PreferenceName"</param>
    /// <param name="value">Value of the preference.</param>
    /// <typeparam name="T">Type of the preference.</typeparam>
    public void UpdatePreference<T>(string name, T value)
    {
        Interop.UpdateUserPreference(name, value);
    }

    /// <summary>
    ///    Update local preference by name. Local preferences are editor data.
    /// 
    /// </summary>
    /// <param name="name">Name of the preference. You can use "ExtensionUniqueName:PreferenceName" or "PreferenceName" schema. To access PixiEditor's built-in preferences, use "PixiEditor:PreferenceName"</param>
    /// <param name="value">Value of the preference.</param>
    /// <typeparam name="T">Type of the preference.</typeparam>
    public void UpdateLocalPreference<T>(string name, T value)
    {
        Interop.UpdateLocalUserPreference(name, value);
    }

    /// <summary>
    ///     Gets user preference by name.
    /// </summary>
    /// <param name="name">Name of the preference. You can use "ExtensionUniqueName:PreferenceName" or "PreferenceName" schema. To access PixiEditor's built-in preferences, use "PixiEditor:PreferenceName"</param>
    /// <typeparam name="T">Type of the preference.</typeparam>
    /// <returns>Preference value.</returns>
    public T GetPreference<T>(string name)
    {
        return Interop.GetPreference<T>(name, default);
    }

    /// <summary>
    ///     Gets user preference by name.
    /// </summary>
    /// <param name="name">Name of the preference. You can use "ExtensionUniqueName:PreferenceName" or "PreferenceName" schema. To access PixiEditor's built-in preferences, use "PixiEditor:PreferenceName"</param>
    /// <param name="fallbackValue">Value to return if preference doesn't exist.</param>
    /// <typeparam name="T">Type of the preference.</typeparam>
    /// <returns>Preference value.</returns>
    public T GetPreference<T>(string name, T fallbackValue)
    {
        return Interop.GetPreference<T>(name, fallbackValue);
    }

    /// <summary>
    ///     Gets local preference by name. Local preferences are editor data.
    /// </summary>
    /// <param name="name">Name of the preference. You can use "ExtensionUniqueName:PreferenceName" or "PreferenceName" schema. To access PixiEditor's built-in preferences, use "PixiEditor:PreferenceName"</param>
    /// <typeparam name="T">Type of the preference.</typeparam>
    /// <returns>Preference value.</returns>
    public T GetLocalPreference<T>(string name)
    {
        return Interop.GetLocalPreference<T>(name, default);
    }

    /// <summary>
    ///     Gets local preference by name. Local preferences are editor data.
    /// </summary>
    /// <param name="name">Name of the preference. You can use "ExtensionUniqueName:PreferenceName" or "PreferenceName" schema. To access PixiEditor's built-in preferences, use "PixiEditor:PreferenceName"</param>
    /// <param name="fallbackValue">Value to return if preference doesn't exist.</param>
    /// <typeparam name="T">Type of the preference.</typeparam>
    /// <returns>Preference value.</returns>
    public T GetLocalPreference<T>(string name, T fallbackValue)
    {
        return Interop.GetLocalPreference(name, fallbackValue);
    }
    
    public void AddCallback(string name, Action<object> action)
    {
        
    }

    public void AddCallback<T>(string name, Action<T> action)
    {
        throw new NotImplementedException();
    }

    public void RemoveCallback(string name, Action<object> action)
    {
        throw new NotImplementedException();
    }

    public void RemoveCallback<T>(string name, Action<T> action)
    {
        throw new NotImplementedException();
    }

    void IPreferences.Init() { }

    void IPreferences.Init(string path, string localPath) { }
}
