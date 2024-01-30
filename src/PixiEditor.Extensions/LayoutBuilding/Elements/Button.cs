﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using PixiEditor.Extensions.CommonApi.LayoutBuilding;
using PixiEditor.Extensions.CommonApi.LayoutBuilding.Events;

namespace PixiEditor.Extensions.LayoutBuilding.Elements;

public class Button : SingleChildLayoutElement
{
    public event ElementEventHandler Click
    {
        add => AddEvent(nameof(Click), value);
        remove => RemoveEvent(nameof(Click), value);
    }

    public Button(ILayoutElement<Control>? child = null, ElementEventHandler? onClick = null)
    {
        Child = child;
        if (onClick != null)
        {
            Click += onClick;
        }
    }

    public override Control BuildNative()
    {
        Avalonia.Controls.Button btn = new Avalonia.Controls.Button();
        Binding binding = new Binding(nameof(Child)) { Source = this, Converter = LayoutElementToNativeControlConverter.Instance };
        btn.Bind(Avalonia.Controls.Button.ContentProperty, binding);

        btn.Click += (sender, args) => RaiseEvent(nameof(Click), new ElementEventArgs());

        return btn;
    }
}
