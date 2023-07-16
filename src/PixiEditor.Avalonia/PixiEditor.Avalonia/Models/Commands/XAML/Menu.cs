﻿using Avalonia;
using Avalonia.Controls;
using PixiEditor.Helpers;
using PixiEditor.Models.DataHolders;
using ReactiveUI;

namespace PixiEditor.Models.Commands.XAML;

internal class Menu : global::Avalonia.Controls.Menu
{
    public static readonly DirectProperty<Menu, string> CommandNameProperty;

    static Menu()
    {
        CommandNameProperty = AvaloniaProperty.RegisterDirect<Menu, string>(
            nameof(Command),
            GetCommand,
            SetCommand);
        CommandNameProperty.Changed.Subscribe(CommandChanged);
    }

    public const double IconDimensions = 21;
    
    public static string GetCommand(Menu menu) => (string)menu.GetValue(CommandNameProperty);

    public static void SetCommand(Menu menu, string value) => menu.SetValue(CommandNameProperty, value);

    public static void CommandChanged(AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is not string value || e.Sender is not MenuItem item)
        {
            throw new InvalidOperationException($"{nameof(Menu)}.Command only works for MenuItem's");
        }

        if (Design.IsDesignMode)
        {
            HandleDesignMode(item, value);
            return;
        }

        var command = CommandController.Current.Commands[value];

        var icon = new Image
        {
            Source = command.GetIcon(), 
            Width = IconDimensions, Height = IconDimensions,
            Opacity = command.CanExecute() ? 1 : 0.75
        };

        icon.IsVisible.WhenAnyValue(v => v).Subscribe(newValue =>
        {
            icon.Opacity = command.CanExecute() ? 1 : 0.75;

        });

        //TODO: This, some ReactiveUI shit should be here, https://docs.avaloniaui.net/docs/next/concepts/reactiveui/reactive-command
        //item.Command = Command.GetICommand(command, false);
        item.Icon = icon;
        item.Bind(MenuItem.InputGestureProperty, ShortcutBinding.GetBinding(command, null));
    }

    private static void HandleDesignMode(MenuItem item, string name)
    {
        var command = DesignCommandHelpers.GetCommandAttribute(name);
        item.InputGesture = new KeyCombination(command.Key, command.Modifiers).ToKeyGesture();
    }
}
