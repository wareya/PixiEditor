﻿using PixiEditor.Extensions.FlyUI.Elements;
using PixiEditor.Extensions.WasmRuntime.Utilities;
using PixiEditor.Extensions.Windowing;

namespace PixiEditor.Extensions.WasmRuntime.Api;

internal class WindowingApi : ApiGroupHandler
{
    [ApiFunction("create_popup_window")]
    public int CreatePopupWindow(string title, Span<byte> bodySpan)
    {
        var body = LayoutBuilder.Deserialize(bodySpan, DuplicateResolutionTactic.ThrowException);
        var popupWindow = Api.Windowing.CreatePopupWindow(title, body.BuildNative());

        int handle = NativeObjectManager.AddObject(popupWindow);
        return handle;
    }

    [ApiFunction("set_window_title")]
    public void SetWindowTitle(int handle, string title)
    {
        var window = NativeObjectManager.GetObject<PopupWindow>(handle);
        window.Title = title;
    }

    [ApiFunction("get_window_title")]
    public string GetWindowTitle(int handle)
    {
        var window = NativeObjectManager.GetObject<PopupWindow>(handle);
        return window.Title;
    }

    [ApiFunction("show_window")]
    public void ShowWindow(int handle)
    {
        var window = NativeObjectManager.GetObject<PopupWindow>(handle);
        window.Show();
    }
    
    [ApiFunction("show_window_async")]
    public int ShowWindowAsync(int handle)
    {
        var window = NativeObjectManager.GetObject<PopupWindow>(handle);
        Task<int> showDialogTask = AsyncUtility.ToResultFrom(window.ShowDialog());
        return AsyncHandleManager.AddAsyncCall(showDialogTask);
    }


    [ApiFunction("close_window")]
    public void CloseWindow(int handle)
    {
        var window = NativeObjectManager.GetObject<PopupWindow>(handle);
        window.Close();
    }
    
    [ApiFunction("get_window_width")]
    public double GetWindowWidth(int handle)
    {
        var window = NativeObjectManager.GetObject<PopupWindow>(handle);
        return window.Width;
    }
    
    [ApiFunction("set_window_width")]
    public void SetWindowWidth(int handle, double width)
    {
        var window = NativeObjectManager.GetObject<PopupWindow>(handle);
        window.Width = width;
    }
    
    [ApiFunction("get_window_height")]
    public double GetWindowHeight(int handle)
    {
        var window = NativeObjectManager.GetObject<PopupWindow>(handle);
        return window.Height;
    }
    
    [ApiFunction("set_window_height")]
    public void SetWindowHeight(int handle, double height)
    {
        var window = NativeObjectManager.GetObject<PopupWindow>(handle);
        window.Height = height;
    }
    
    [ApiFunction("get_window_resizable")]
    public bool GetWindowResizable(int handle)
    {
        var window = NativeObjectManager.GetObject<PopupWindow>(handle);
        return window.CanResize;
    }
    
    [ApiFunction("set_window_resizable")]
    public void SetWindowResizable(int handle, bool resizable)
    {
        var window = NativeObjectManager.GetObject<PopupWindow>(handle);
        window.CanResize = resizable;
    }
    
    [ApiFunction("get_window_minimizable")]
    public bool GetWindowMinimizable(int handle)
    {
        var window = NativeObjectManager.GetObject<PopupWindow>(handle);
        return window.CanMinimize;
    }
    
    [ApiFunction("set_window_minimizable")]
    public void SetWindowMinimizable(int handle, bool minimizable)
    {
        var window = NativeObjectManager.GetObject<PopupWindow>(handle);
        window.CanMinimize = minimizable;
    }
}
