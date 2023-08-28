using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.DependencyInjection;
using PixiEditor.AvaloniaUI.Helpers;
using PixiEditor.AvaloniaUI.Models.AppExtensions;
using PixiEditor.AvaloniaUI.Models.Services;
using PixiEditor.DrawingApi.Core.Bridge;
using PixiEditor.DrawingApi.Skia;
using PixiEditor.Extensions.Common.UserPreferences;
using PixiEditor.Platform;
using ViewModelMain = PixiEditor.AvaloniaUI.ViewModels.ViewModelMain;

namespace PixiEditor.AvaloniaUI.Views;

internal partial class MainWindow : Window
{
    private readonly IPreferences preferences;
    private readonly IPlatform platform;
    private readonly IServiceProvider services;
    private static ExtensionLoader extLoader;

    public new ViewModelMain DataContext { get => (ViewModelMain)base.DataContext; set => base.DataContext = value; }
    
    public static MainWindow? Current {
        get 
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                return desktop.MainWindow as MainWindow;
            if (Application.Current is null)
                return null;
            throw new NotSupportedException("ApplicationLifetime is not supported");
        }
    }

    public MainWindow(ExtensionLoader extensionLoader)
    {
        (Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow = this;
        extLoader = extensionLoader;

        services = new ServiceCollection()
            .AddPlatform()
            .AddPixiEditor(extensionLoader)
            .AddExtensionServices()
            .BuildServiceProvider();

        SkiaDrawingBackend skiaDrawingBackend = new SkiaDrawingBackend();
        DrawingBackendApi.SetupBackend(skiaDrawingBackend);

        preferences = services.GetRequiredService<IPreferences>();
        platform = services.GetRequiredService<IPlatform>();
        DataContext = services.GetRequiredService<ViewModelMain>();
        DataContext.Setup(services);

        InitializeComponent();
    }

    public static MainWindow CreateWithDocuments(IEnumerable<(string? originalPath, byte[] dotPixiBytes)> documents)
    {
        //TODO: Implement this
        /*MainWindow window = new(extLoader);
        FileViewModel fileVM = window.services.GetRequiredService<FileViewModel>();

        foreach (var (path, bytes) in documents)
        {
            fileVM.OpenRecoveredDotPixi(path, bytes);
        }

        return window;*/

        return new MainWindow(null);
    }
}
