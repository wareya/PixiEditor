﻿using ChunkyImageLib.DataHolders;
using PixiEditor.ChangeableDocument.Actions;
using PixiEditor.ChangeableDocument.Actions.Generated;
using PixiEditor.DrawingApi.Core.Numerics;
using PixiEditor.Models.Handlers.Tools;
using PixiEditor.Models.Tools;
using PixiEditor.Numerics;

namespace PixiEditor.Models.DocumentModels.UpdateableChangeExecutors;
#nullable enable
internal class EllipseToolExecutor : ShapeToolExecutor<IEllipseToolHandler>
{
    private void DrawEllipseOrCircle(VecI curPos, bool firstDraw)
    {
        RectI rect;
        if (firstDraw)
            rect = new RectI(curPos, VecI.Zero);
        else if (toolViewModel!.DrawCircle)
            rect = GetSquaredCoordinates(startPos, curPos);
        else
            rect = RectI.FromTwoPixels(startPos, curPos);

        lastRect = rect;

        internals!.ActionAccumulator.AddActions(new DrawEllipse_Action(memberGuid, rect, 0, strokeColor, fillColor, strokeWidth, drawOnMask, document!.AnimationHandler.ActiveFrameBindable));
    }

    public override ExecutorType Type => ExecutorType.ToolLinked;
    protected override DocumentTransformMode TransformMode => DocumentTransformMode.Scale_Rotate_Shear_NoPerspective;
    protected override void DrawShape(VecI currentPos, bool firstDraw) => DrawEllipseOrCircle(currentPos, firstDraw);

    protected override IAction TransformMovedAction(ShapeData data, ShapeCorners corners) =>
        new DrawEllipse_Action(memberGuid, (RectI)RectD.FromCenterAndSize(data.Center, data.Size), corners.RectRotation, strokeColor,
            fillColor, strokeWidth, drawOnMask, document!.AnimationHandler.ActiveFrameBindable);

    protected override IAction EndDrawAction() => new EndDrawEllipse_Action();
}
