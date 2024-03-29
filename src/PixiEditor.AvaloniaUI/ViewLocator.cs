using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using CommunityToolkit.Mvvm.ComponentModel;
using PixiDocks.Core.Docking;
using PixiEditor.AvaloniaUI.ViewModels.Dock;
using PixiEditor.AvaloniaUI.ViewModels.SubViewModels;
using PixiEditor.AvaloniaUI.Views.Dock;
using PixiEditor.AvaloniaUI.Views.Layers;

namespace PixiEditor.AvaloniaUI;

public class ViewLocator : IDataTemplate
{
    public static Dictionary<Type, Type> ViewBindingsMap = new Dictionary<Type, Type>()
    {
        [typeof(ViewportWindowViewModel)] = typeof(DocumentTemplate),
        [typeof(LayersDockViewModel)] = typeof(LayersManager),
    };

    public Control Build(object? data)
    {
        var name = data.GetType().FullName.Replace("ViewModel", "View");
        var type = Type.GetType(name);

        if (type != null)
        {
            return (Control)Activator.CreateInstance(type);
        }

        type = data?.GetType() ?? typeof(object);
        if (ViewBindingsMap.TryGetValue(type, out Type viewType))
        {
            var instance = Activator.CreateInstance(viewType);
            if (instance is not null)
            {
                return (Control)instance;
            }
            else
            {
                return new TextBlock { Text = "Create Instance Failed: " + viewType.FullName };
            }
        }

        throw new KeyNotFoundException($"View for {type.FullName} not found");
    }

    public bool Match(object? data)
    {
        return data is ObservableObject or IDockable;
    }
}
