﻿using System.Collections.Generic;
using System.Linq;
using Avalonia.Input;
using PixiEditor.Models.Commands;
using PixiEditor.Models.Commands.CommandContext;
using PixiEditor.Models.Commands.Commands;
using PixiEditor.Models.Input;
using PixiEditor.ViewModels.Tools;

namespace PixiEditor.Models.Controllers;

internal class ShortcutController
{
    public static bool ShortcutExecutionBlocked => _shortcutExecutionBlockers.Count > 0;

    private static readonly List<string> _shortcutExecutionBlockers = new List<string>();

    public IEnumerable<Command> LastCommands { get; private set; }

    public Dictionary<KeyCombination, ToolViewModel> TransientShortcuts { get; set; } = new();
    
    public Type? ActiveContext { get; private set; }

    public static void BlockShortcutExecution(string blocker)
    {
        if (_shortcutExecutionBlockers.Contains(blocker)) return;
        _shortcutExecutionBlockers.Add(blocker);
    }

    public static void UnblockShortcutExecution(string blocker)
    {
        if (!_shortcutExecutionBlockers.Contains(blocker)) return;
        _shortcutExecutionBlockers.Remove(blocker);
    }

    public static void UnblockShortcutExecutionAll()
    {
        _shortcutExecutionBlockers.Clear();
    }

    public KeyCombination GetToolShortcut<T>()
    {
        return GetToolShortcut(typeof(T));
    }

    public KeyCombination GetToolShortcut(Type type)
    {
        return CommandController.Current.Commands.First(x => x is Command.ToolCommand tool && tool.ToolType == type).Shortcut;
    }

    public void KeyPressed(bool isRepeat, Key key, KeyModifiers modifiers)
    {
        KeyCombination shortcut = new(key, modifiers);

        if (ShortcutExecutionBlocked)
        {
            return;
        }

        var commands = CommandController.Current.Commands[shortcut].Where(x => x.ShortcutContext is null || x.ShortcutContext == ActiveContext).ToList();

        if (!commands.Any())
        {
            return;
        }

        LastCommands = commands;

        var context = ShortcutSourceInfo.GetContext(shortcut, isRepeat);
        foreach (var command in commands)
        {
            command.Execute(context, false);
        }
    }

    public void OverwriteContext(Type getType)
    {
        ActiveContext = getType;
    }
    
    public void ClearContext(Type clearFrom)
    {
        if (ActiveContext == clearFrom)
        {
            ActiveContext = null;
        }
    }
}
