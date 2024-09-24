﻿using PixiEditor.ChangeableDocument.Actions;
using PixiEditor.ChangeableDocument.Actions.Generated;
using PixiEditor.ChangeableDocument.Changeables.Graph.Interfaces.Shapes;
using PixiEditor.DrawingApi.Core.Surfaces.PaintImpl;
using PixiEditor.Models.Handlers.Tools;
using PixiEditor.Numerics;

namespace PixiEditor.Models.DocumentModels.UpdateableChangeExecutors;
#nullable enable
internal class RasterLineToolExecutor : LineExecutor<ILineToolHandler>
{
    protected override bool InitShapeData(IReadOnlyLineData? data)
    {
        return false;
    }

    protected override IAction DrawLine(VecD pos)
    {
        VecD dir = GetSignedDirection(startDrawingPos, pos);
        VecD oppositeDir = new VecD(-dir.X, -dir.Y);
        return new DrawRasterLine_Action(memberId, ToPixelPos(startDrawingPos, oppositeDir), ToPixelPos(pos, dir), StrokeWidth,
            StrokeColor, StrokeCap.Butt, drawOnMask, document!.AnimationHandler.ActiveFrameBindable);
    }

    protected override IAction TransformOverlayMoved(VecD start, VecD end)
    {
        VecD dir = GetSignedDirection(start, end);
        VecD oppositeDir = new VecD(-dir.X, -dir.Y);
        return new DrawRasterLine_Action(memberId, ToPixelPos(start, oppositeDir), ToPixelPos(end, dir), 
            StrokeWidth, StrokeColor, StrokeCap.Butt, drawOnMask, document!.AnimationHandler.ActiveFrameBindable);
    }

    protected override IAction SettingsChange()
    {
        VecD dir = GetSignedDirection(startDrawingPos, curPos);
        VecD oppositeDir = new VecD(-dir.X, -dir.Y);
        return new DrawRasterLine_Action(memberId, ToPixelPos(startDrawingPos, oppositeDir), ToPixelPos(curPos, dir), StrokeWidth,
            StrokeColor, StrokeCap.Butt, drawOnMask, document!.AnimationHandler.ActiveFrameBindable);
    }

    private VecI ToPixelPos(VecD pos, VecD dir)
    {
        double xAdjustment = dir.X > 0 ? 0.5 : -0.5;
        double yAdjustment = dir.Y > 0 ? 0.5 : -0.5;
        
        VecD adjustment = new VecD(xAdjustment, yAdjustment);
        
        VecI finalPos = (VecI)(pos - adjustment);

        return finalPos;
    }
    
    private VecD GetSignedDirection(VecD start, VecD end)
    {
        return new VecD(Math.Sign(end.X - start.X), Math.Sign(end.Y - start.Y));
    }

    protected override IAction EndDraw()
    {
        return new EndDrawRasterLine_Action();
    }
}
