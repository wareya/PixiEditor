﻿using Drawie.Numerics;
using PixiEditor.ChangeableDocument;
using PixiEditor.ChangeableDocument.Changeables.Animations;
using PixiEditor.ChangeableDocument.Changeables.Graph.Interfaces.Shapes;
using PixiEditor.ChangeableDocument.Changeables.Graph.Nodes;
using PixiEditor.Extensions.Common.Localization;
using PixiEditor.Models.Handlers;
using PixiEditor.Models.Handlers.Tools;
using PixiEditor.ViewModels.Document;
using PixiEditor.ViewModels.Tools.ToolSettings.Toolbars;

namespace PixiEditor.ViewModels.Tools.Tools;

internal class VectorPathToolViewModel : ShapeTool, IVectorPathToolHandler
{
    public override string ToolNameLocalizationKey => "PATH_TOOL";
    public override Type[]? SupportedLayerTypes { get; } = [typeof(IVectorLayerHandler)];
    public override Type LayerTypeToCreateOnEmptyUse { get; } = typeof(VectorLayerNode);
    public override LocalizedString Tooltip => new LocalizedString("PATH_TOOL_TOOLTIP", Shortcut);

    public override bool StopsLinkedToolOnUse => false;

    private bool isActivated;

    public VectorPathToolViewModel()
    {
        var fillSetting = Toolbar.GetSetting(nameof(BasicShapeToolbar.Fill));
        if (fillSetting != null)
        {
            fillSetting.Value = false;
        }
    }

    public override void UseTool(VecD pos)
    {
        var doc =
            ViewModelMain.Current?.DocumentManagerSubViewModel.ActiveDocument;

        if (doc is null || isActivated) return;
        
        if (!doc.PathOverlayViewModel.IsActive)
        {
            doc?.Tools.UseVectorPathTool();
            isActivated = true;
        }
    }

    public override void OnSelected(bool restoring)
    {
        if (restoring) return;

        ViewModelMain.Current?.DocumentManagerSubViewModel.ActiveDocument?.Tools.UseVectorPathTool();
        isActivated = true;
    }

    public override void OnDeselecting(bool transient)
    {
        if (!transient)
        {
            ViewModelMain.Current.DocumentManagerSubViewModel.ActiveDocument?.Operations.TryStopToolLinkedExecutor();
            isActivated = false;
        }
    }

    protected override void OnSelectedLayersChanged(IStructureMemberHandler[] layers)
    {
        OnDeselecting(false);
        OnSelected(false);
    }
}
