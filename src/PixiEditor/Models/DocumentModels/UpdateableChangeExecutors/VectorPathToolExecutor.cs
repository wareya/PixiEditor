﻿using Avalonia.Input;
using Avalonia.Media;
using ChunkyImageLib.DataHolders;
using Drawie.Backend.Core.Vector;
using Drawie.Numerics;
using PixiEditor.ChangeableDocument;
using PixiEditor.ChangeableDocument.Changeables.Graph.Nodes.Shapes.Data;
using PixiEditor.Models.Handlers;
using PixiEditor.Models.Handlers.Tools;
using PixiEditor.ChangeableDocument.Actions.Generated;
using PixiEditor.ChangeableDocument.Changeables.Animations;
using PixiEditor.ChangeableDocument.Changeables.Graph.Interfaces.Shapes;
using PixiEditor.ChangeableDocument.Changeables.Graph.Nodes;
using PixiEditor.Helpers.Extensions;
using PixiEditor.Models.Controllers.InputDevice;
using PixiEditor.Models.DocumentModels.UpdateableChangeExecutors.Features;
using PixiEditor.Models.Handlers.Toolbars;
using PixiEditor.Models.Tools;
using PixiEditor.ViewModels.Tools.Tools;
using PixiEditor.ViewModels.Tools.ToolSettings.Settings;
using PixiEditor.Views.Overlays.PathOverlay;
using Color = Drawie.Backend.Core.ColorsImpl.Color;
using Colors = Drawie.Backend.Core.ColorsImpl.Colors;

namespace PixiEditor.Models.DocumentModels.UpdateableChangeExecutors;

internal class VectorPathToolExecutor : UpdateableChangeExecutor, IPathExecutorFeature, IMidChangeUndoableExecutor
{
    private IStructureMemberHandler member;
    private VectorPath startingPath;
    private IVectorPathToolHandler vectorPathToolHandler;
    private IFillableShapeToolbar toolbar;
    private IColorsHandler colorHandler;
    private bool isValidPathLayer;

    public override ExecutorType Type => ExecutorType.ToolLinked;

    public bool CanUndo => document.PathOverlayHandler.HasUndo;
    public bool CanRedo => document.PathOverlayHandler.HasRedo;

    public override bool BlocksOtherActions => false;

    private bool mouseDown;

    public override ExecutionState Start()
    {
        vectorPathToolHandler = GetHandler<IVectorPathToolHandler>();

        member = document.SelectedStructureMember;

        if (member is null)
        {
            return ExecutionState.Error;
        }

        toolbar = (IFillableShapeToolbar)vectorPathToolHandler.Toolbar;
        colorHandler = GetHandler<IColorsHandler>();

        if (member is IVectorLayerHandler vectorLayerHandler)
        {
            var shapeData = vectorLayerHandler.GetShapeData(document.AnimationHandler.ActiveFrameTime);
            bool wasNull = false;
            isValidPathLayer = true;
            if (shapeData is PathVectorData pathData)
            {
                startingPath = new VectorPath(pathData.Path);
                ApplySettings(pathData);
                startingPath.Transform(pathData.TransformationMatrix);
            }
            else if (shapeData is null)
            {
                wasNull = true;
                startingPath = new VectorPath();
            }
            else
            {
                isValidPathLayer = false;
                return ExecutionState.Success;
            }

            document.PathOverlayHandler.Show(startingPath, false);
            if (controller.LeftMousePressed)
            {
                var snapped =
                    document.SnappingHandler.SnappingController.GetSnapPoint(controller.LastPrecisePosition, out _,
                        out _);
                if (wasNull)
                {
                    startingPath.MoveTo((VecF)snapped);
                }
                else
                {
                    startingPath.LineTo((VecF)snapped);
                }

                if (toolbar.SyncWithPrimaryColor)
                {
                    toolbar.StrokeColor = colorHandler.PrimaryColor.ToColor();
                    toolbar.FillColor = colorHandler.PrimaryColor.ToColor();
                }

                //below forces undo before starting new path
                //internals.ActionAccumulator.AddFinishedActions(new EndSetShapeGeometry_Action());

                //internals.ActionAccumulator.AddActions(new SetShapeGeometry_Action(member.Id, ConstructShapeData()));
            }
        }
        else
        {
            return ExecutionState.Error;
        }

        document.SnappingHandler.Remove(member.Id.ToString()); // This disables self-snapping
        return ExecutionState.Success;
    }

    public override void OnPrecisePositionChange(VecD pos)
    {
        if (mouseDown)
        {
            return;
        }

        VecD mouseSnap =
            document.SnappingHandler.SnappingController.GetSnapPoint(pos, out string snapXAxis,
                out string snapYAxis);
        HighlightSnapping(snapXAxis, snapYAxis);

        if (!string.IsNullOrEmpty(snapXAxis) || !string.IsNullOrEmpty(snapYAxis))
        {
            document.SnappingHandler.SnappingController.HighlightedPoint = mouseSnap;
        }
        else
        {
            document.SnappingHandler.SnappingController.HighlightedPoint = null;
        }
    }

    public override void OnLeftMouseButtonDown(MouseOnCanvasEventArgs args)
    {
        bool allClosed = WholePathClosed();
        if (!isValidPathLayer || allClosed)
        {
            if (NeedsNewLayer(document.SelectedStructureMember, document.AnimationHandler.ActiveFrameTime))
            {
                Guid? created =
                    document.Operations.CreateStructureMember(typeof(VectorLayerNode), ActionSource.Automated);

                if (created is null) return;

                document.Operations.SetSelectedMember(created.Value);
            }
        }
    }
    
    private bool WholePathClosed()
    {
        EditableVectorPath editablePath = new EditableVectorPath(startingPath);
        
        return editablePath.SubShapes.Count > 0 && editablePath.SubShapes.All(x => x.IsClosed);
    }

    public override void OnLeftMouseButtonUp(VecD pos)
    {
        mouseDown = false;
    }

    public override void OnColorChanged(Color color, bool primary)
    {
        if (primary && toolbar.SyncWithPrimaryColor)
        {
            toolbar.StrokeColor = color.ToColor();
            toolbar.FillColor = color.ToColor();
        }
    }

    public override void OnSettingsChanged(string name, object value)
    {
        if (document.PathOverlayHandler.IsActive)
        {
            internals.ActionAccumulator.AddActions(new SetShapeGeometry_Action(member.Id, ConstructShapeData()));
        }
    }

    public override void ForceStop()
    {
        document.PathOverlayHandler.Hide();
        document.SnappingHandler.AddFromBounds(member.Id.ToString(), () => member.TightBounds ?? RectD.Empty);
        HighlightSnapping(null, null);
        internals.ActionAccumulator.AddFinishedActions(new EndSetShapeGeometry_Action());
    }

    private PathVectorData ConstructShapeData()
    {
        if(startingPath == null)
        {
            return new PathVectorData(new VectorPath() { FillType = (PathFillType)vectorPathToolHandler.FillMode })
            {
                StrokeWidth = (float)toolbar.ToolSize,
                StrokeColor = toolbar.StrokeColor.ToColor(),
                FillColor = toolbar.Fill ? toolbar.FillColor.ToColor() : Colors.Transparent,
            };
        }
        
        return new PathVectorData(new VectorPath(startingPath) { FillType = (PathFillType)vectorPathToolHandler.FillMode })
        {
            StrokeWidth = (float)toolbar.ToolSize,
            StrokeColor = toolbar.StrokeColor.ToColor(),
            FillColor = toolbar.Fill ? toolbar.FillColor.ToColor() : Colors.Transparent,
        };
    }

    public void OnPathChanged(VectorPath path)
    {
        if (document.PathOverlayHandler.IsActive)
        {
            startingPath = path;
            internals.ActionAccumulator.AddActions(new SetShapeGeometry_Action(member.Id, ConstructShapeData()));
        }
    }

    public bool IsFeatureEnabled(IExecutorFeature feature)
    {
        return feature switch
        {
            IPathExecutorFeature _ => true,
            IMidChangeUndoableExecutor _ => true,
            ITransformableExecutor _ => true,
            _ => false
        };
    }

    public void OnMidChangeUndo()
    {
        document.PathOverlayHandler.Undo();
    }

    public void OnMidChangeRedo()
    {
        document.PathOverlayHandler.Redo();
    }

    protected void HighlightSnapping(string? snapX, string? snapY)
    {
        document!.SnappingHandler.SnappingController.HighlightedXAxis = snapX;
        document!.SnappingHandler.SnappingController.HighlightedYAxis = snapY;
        document.SnappingHandler.SnappingController.HighlightedPoint = null;
    }

    private bool NeedsNewLayer(IStructureMemberHandler? member, KeyFrameTime frameTime)
    {
        var shapeData = (member as IVectorLayerHandler).GetShapeData(frameTime);
        if (shapeData is null)
        {
            return false;
        }

        return shapeData is not IReadOnlyPathData pathData || pathData.Path.IsClosed;
    }
    
    private void ApplySettings(PathVectorData pathData)
    {
        toolbar.ToolSize = pathData.StrokeWidth;
        toolbar.StrokeColor = pathData.StrokeColor.ToColor();
        toolbar.ToolSize = pathData.StrokeWidth;
        toolbar.Fill = pathData.Fill;
        toolbar.FillColor = pathData.FillColor.ToColor();
        toolbar.GetSetting<EnumSettingViewModel<VectorPathFillType>>(nameof(VectorPathToolViewModel.FillMode)).Value = (VectorPathFillType)pathData.Path.FillType;
    }
}
