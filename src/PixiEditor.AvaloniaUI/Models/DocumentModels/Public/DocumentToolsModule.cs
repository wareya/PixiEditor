﻿using PixiEditor.AvaloniaUI.Models.DocumentModels.UpdateableChangeExecutors;
using PixiEditor.AvaloniaUI.Models.Handlers;
using PixiEditor.AvaloniaUI.Models.Tools;
using PixiEditor.ChangeableDocument.Enums;

namespace PixiEditor.AvaloniaUI.Models.DocumentModels.Public;
internal class DocumentToolsModule
{
    private IDocument Document { get; set; }
    private DocumentInternalParts Internals { get; set; }

    public DocumentToolsModule(IDocument doc, DocumentInternalParts internals)
    {
        this.Document = doc;
        this.Internals = internals;
    }

    public void UseSymmetry(SymmetryAxisDirection dir) => Internals.ChangeController.TryStartExecutor(new SymmetryExecutor(dir));
    public void UseActiveFrame(int activeFrame) => Internals.ChangeController.TryStartExecutor(new AnimationFrameExecutor(activeFrame));

    public void UseOpacitySlider() => Internals.ChangeController.TryStartExecutor<StructureMemberOpacityExecutor>();

    public void UseShiftLayerTool() => Internals.ChangeController.TryStartExecutor<ShiftLayerExecutor>();

    public void UsePenTool() => Internals.ChangeController.TryStartExecutor<PenToolExecutor>();

    public void UseEraserTool() => Internals.ChangeController.TryStartExecutor<EraserToolExecutor>();

    public void UseColorPickerTool() => Internals.ChangeController.TryStartExecutor<ColorPickerToolExecutor>();

    public void UseRectangleTool()
    {
        bool force = Internals.ChangeController.GetCurrentExecutorType() == ExecutorType.ToolLinked;
        Internals.ChangeController.TryStartExecutor<RectangleToolExecutor>(force);
    }

    public void UseEllipseTool()
    {
        bool force = Internals.ChangeController.GetCurrentExecutorType() == ExecutorType.ToolLinked;
        Internals.ChangeController.TryStartExecutor<EllipseToolExecutor>(force);
    }

    public void UseLineTool()
    {
        bool force = Internals.ChangeController.GetCurrentExecutorType() == ExecutorType.ToolLinked;
        Internals.ChangeController.TryStartExecutor<LineToolExecutor>(force);
    }

    public void UseSelectTool() => Internals.ChangeController.TryStartExecutor<SelectToolExecutor>();

    public void UseBrightnessTool() => Internals.ChangeController.TryStartExecutor<BrightnessToolExecutor>();

    public void UseFloodFillTool() => Internals.ChangeController.TryStartExecutor<FloodFillToolExecutor>();

    public void UseLassoTool() => Internals.ChangeController.TryStartExecutor<LassoToolExecutor>();

    public void UseMagicWandTool() => Internals.ChangeController.TryStartExecutor<MagicWandToolExecutor>();
}
