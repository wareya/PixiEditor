﻿using System;
using Avalonia;

namespace PixiEditor.Desktop;

public class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .With(new Win32PlatformOptions()
            {
                RenderingMode = new Win32RenderingMode[] { Win32RenderingMode.Wgl, Win32RenderingMode.AngleEgl },
                OverlayPopups = true
            })
            .With(new SkiaOptions()
            {
                MaxGpuResourceSizeBytes = 1024 * 1024 * 1024,
            })
            .LogToTrace();
}
