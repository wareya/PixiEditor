﻿using System.Collections.Immutable;
using ChunkyImageLib.DataHolders;
using PixiEditor.ChangeableDocument.Actions;
using PixiEditor.Helpers.Extensions;
using PixiEditor.ChangeableDocument.Actions.Generated;
using PixiEditor.ChangeableDocument.Actions.Undo;
using PixiEditor.ChangeableDocument.Changeables.Graph.Nodes;
using PixiEditor.ChangeableDocument.Enums;
using PixiEditor.DrawingApi.Core;
using PixiEditor.DrawingApi.Core.Surfaces.Vector;
using PixiEditor.Extensions.CommonApi.Palettes;
using PixiEditor.Models.Clipboard;
using PixiEditor.Models.DocumentModels.UpdateableChangeExecutors;
using PixiEditor.Models.DocumentPassthroughActions;
using PixiEditor.Models.Handlers;
using PixiEditor.Models.Layers;
using PixiEditor.Models.Position;
using PixiEditor.Models.Tools;
using PixiEditor.Numerics;

namespace PixiEditor.Models.DocumentModels.Public;
#nullable enable
internal class DocumentOperationsModule : IDocumentOperations
{
    private IDocument Document { get; }
    private DocumentInternalParts Internals { get; }

    public DocumentOperationsModule(IDocument document, DocumentInternalParts internals)
    {
        Document = document;
        Internals = internals;
    }

    /// <summary>
    /// Creates a new selection with the size of the document
    /// </summary>
    public void SelectAll() => Select(new RectI(VecI.Zero, Document.SizeBindable), SelectionMode.Add);

    /// <summary>
    /// Creates a new selection with the size of the document
    /// </summary>
    public void Select(RectI rect, SelectionMode mode = SelectionMode.New)
    {
        if (Internals.ChangeController.IsBlockingChangeActive)
            return;
        Internals.ActionAccumulator.AddFinishedActions(
            new SelectRectangle_Action(rect, mode),
            new EndSelectRectangle_Action());
    }

    /// <summary>
    /// Clears the current selection
    /// </summary>
    public void ClearSelection()
    {
        if (Internals.ChangeController.IsBlockingChangeActive)
            return;
        Internals.ActionAccumulator.AddFinishedActions(new ClearSelection_Action());
    }

    /// <summary>
    /// Deletes selected pixels
    /// </summary>
    /// <param name="clearSelection">Should the selection be cleared</param>
    public void DeleteSelectedPixels(int frame, bool clearSelection = false)
    {
        var member = Document.SelectedStructureMember;
        if (Internals.ChangeController.IsBlockingChangeActive || member is null)
            return;
        bool drawOnMask = member is not ILayerHandler layer || layer.ShouldDrawOnMask;
        if (drawOnMask && !member.HasMaskBindable)
            return;
        Internals.ActionAccumulator.AddActions(new ClearSelectedArea_Action(member.Id, drawOnMask, frame));
        if (clearSelection)
            Internals.ActionAccumulator.AddActions(new ClearSelection_Action());
        Internals.ActionAccumulator.AddFinishedActions();
    }

    /// <summary>
    /// Sets the opacity of the member with the guid <paramref name="memberGuid"/>
    /// </summary>
    /// <param name="memberGuid">The Guid of the member</param>
    /// <param name="value">A value between 0 and 1</param>
    public void SetMemberOpacity(Guid memberGuid, float value)
    {
        if (Internals.ChangeController.IsBlockingChangeActive || value is > 1 or < 0)
            return;
        Internals.ActionAccumulator.AddFinishedActions(
            new StructureMemberOpacity_Action(memberGuid, value),
            new EndStructureMemberOpacity_Action());
    }

    /// <summary>
    /// Adds a new viewport or updates a existing one
    /// </summary>
    public void AddOrUpdateViewport(ViewportInfo info) =>
        Internals.ActionAccumulator.AddActions(new RefreshViewport_PassthroughAction(info));

    /// <summary>
    /// Deletes the viewport with the <paramref name="viewportGuid"/>
    /// </summary>
    /// <param name="viewportGuid">The Guid of the viewport to remove</param>
    public void RemoveViewport(Guid viewportGuid) =>
        Internals.ActionAccumulator.AddActions(new RemoveViewport_PassthroughAction(viewportGuid));

    /// <summary>
    /// Delete the whole undo stack
    /// </summary>
    public void ClearUndo()
    {
        if (Internals.ChangeController.IsBlockingChangeActive)
            return;
        Internals.ActionAccumulator.AddActions(new DeleteRecordedChanges_Action());
    }

    /// <summary>
    /// Pastes the <paramref name="images"/> as new layers
    /// </summary>
    /// <param name="images">The images to paste</param>
    public void PasteImagesAsLayers(List<DataImage> images, int frame)
    {
        if (Internals.ChangeController.IsBlockingChangeActive)
            return;

        RectI maxSize = new RectI(VecI.Zero, Document.SizeBindable);
        foreach (var imageWithName in images)
        {
            maxSize = maxSize.Union(new RectI(imageWithName.Position, imageWithName.Image.Size));
        }

        if (maxSize.Size != Document.SizeBindable)
            Internals.ActionAccumulator.AddActions(new ResizeCanvas_Action(maxSize.Size, ResizeAnchor.TopLeft));

        foreach (var imageWithName in images)
        {
            var layerGuid = Internals.StructureHelper.CreateNewStructureMember(StructureMemberType.Layer,
                Path.GetFileName(imageWithName.Name));
            DrawImage(imageWithName.Image,
                new ShapeCorners(new RectD(imageWithName.Position, imageWithName.Image.Size)),
                layerGuid, true, false, frame, false);
        }

        Internals.ActionAccumulator.AddFinishedActions();
    }

    /// <summary>
    /// Creates a new structure member of type <paramref name="type"/> with the name <paramref name="name"/>
    /// </summary>
    /// <param name="type">The type of the member</param>
    /// <param name="name">The name of the member</param>
    /// <returns>The Guid of the new structure member or null if there is already an active change</returns>
    public Guid? CreateStructureMember(StructureMemberType type, string? name = null, bool finish = true)
    {
        if (Internals.ChangeController.IsBlockingChangeActive)
            return null;
        return Internals.StructureHelper.CreateNewStructureMember(type, name, finish);
    }

    public Guid? CreateStructureMember(Type structureMemberType, string? name = null, bool finish = true)
    {
        if (Internals.ChangeController.IsBlockingChangeActive)
            return null;
        
        return Internals.StructureHelper.CreateNewStructureMember(structureMemberType, name, finish);
    }

    /// <summary>
    /// Duplicates the layer with the <paramref name="guidValue"/>
    /// </summary>
    /// <param name="guidValue">The Guid of the layer</param>
    public void DuplicateLayer(Guid guidValue)
    {
        if (Internals.ChangeController.IsBlockingChangeActive)
            return;
        Internals.ActionAccumulator.AddFinishedActions(new DuplicateLayer_Action(guidValue));
    }

    /// <summary>
    /// Delete the member with the <paramref name="guidValue"/>
    /// </summary>
    /// <param name="guidValue">The Guid of the layer</param>
    public void DeleteStructureMember(Guid guidValue)
    {
        if (Internals.ChangeController.IsBlockingChangeActive)
            return;
        Internals.ActionAccumulator.AddFinishedActions(new DeleteStructureMember_Action(guidValue));
    }

    /// <summary>
    /// Deletes all members with the <paramref name="guids"/>
    /// </summary>
    /// <param name="guids">The Guids of the layers to delete</param>
    public void DeleteStructureMembers(IReadOnlyList<Guid> guids, bool selectNext = true)
    {
        if (Internals.ChangeController.IsBlockingChangeActive)
            return;

        Guid closestMember = FindClosestMember(guids);
        
        IAction[] actions = new IAction[guids.Count + (selectNext ? 1 : 0)];
        for (int i = 0; i < guids.Count; i++)
        {
            actions[i] = new DeleteStructureMember_Action(guids[i]);
        }

        if (selectNext)
        {
            if (closestMember != Guid.Empty)
            {
                actions[^1] = new SetSelectedMember_PassthroughAction(closestMember);
            }
        }

        Internals.ActionAccumulator.AddFinishedActions(actions);
    }

    /// <summary>
    /// Resizes the canvas (Does not upscale the content of the image)
    /// </summary>
    /// <param name="newSize">The size the canvas should be resized to</param>
    /// <param name="anchor">Where the existing content should be put</param>
    public void ResizeCanvas(VecI newSize, ResizeAnchor anchor)
    {
        if (Internals.ChangeController.IsBlockingChangeActive || newSize.X > 9999 || newSize.Y > 9999 || newSize.X < 1 ||
            newSize.Y < 1)
            return;

        if (Document.ReferenceLayerHandler.ReferenceBitmap is not null)
        {
            VecI offset = anchor.FindOffsetFor(Document.SizeBindable, newSize);
            ShapeCorners curShape = Document.ReferenceLayerHandler.ReferenceShapeBindable;
            ShapeCorners offsetCorners = new ShapeCorners()
            {
                TopLeft = curShape.TopLeft + offset,
                TopRight = curShape.TopRight + offset,
                BottomLeft = curShape.BottomLeft + offset,
                BottomRight = curShape.BottomRight + offset,
            };
            Internals.ActionAccumulator.AddActions(new TransformReferenceLayer_Action(offsetCorners),
                new EndTransformReferenceLayer_Action());
        }

        Internals.ActionAccumulator.AddFinishedActions(new ResizeCanvas_Action(newSize, anchor));
    }

    /// <summary>
    /// Resizes the image (Upscales the content of the image)
    /// </summary>
    /// <param name="newSize">The size the image should be resized to</param>
    /// <param name="resampling">The resampling method to use</param>
    public void ResizeImage(VecI newSize, ResamplingMethod resampling)
    {
        if (Internals.ChangeController.IsBlockingChangeActive || newSize.X > 9999 || newSize.Y > 9999 || newSize.X < 1 ||
            newSize.Y < 1)
            return;

        if (Document.ReferenceLayerHandler.ReferenceBitmap is not null)
        {
            VecD scale = ((VecD)newSize).Divide(Document.SizeBindable);
            ShapeCorners curShape = Document.ReferenceLayerHandler.ReferenceShapeBindable;
            ShapeCorners offsetCorners = new ShapeCorners()
            {
                TopLeft = curShape.TopLeft.Multiply(scale),
                TopRight = curShape.TopRight.Multiply(scale),
                BottomLeft = curShape.BottomLeft.Multiply(scale),
                BottomRight = curShape.BottomRight.Multiply(scale),
            };
            Internals.ActionAccumulator.AddActions(new TransformReferenceLayer_Action(offsetCorners),
                new EndTransformReferenceLayer_Action());
        }

        Internals.ActionAccumulator.AddFinishedActions(new ResizeImage_Action(newSize, resampling));
    }

    /// <summary>
    /// Replaces all <paramref name="oldColor"/> with <paramref name="newColor"/>
    /// </summary>
    /// <param name="oldColor">The color to replace</param>
    /// <param name="newColor">The new color</param>
    public void ReplaceColor(PaletteColor oldColor, PaletteColor newColor, int frame)
    {
        if (Internals.ChangeController.IsBlockingChangeActive || oldColor == newColor)
            return;

        Internals.ActionAccumulator.AddFinishedActions(new ReplaceColor_Action(oldColor.ToColor(), newColor.ToColor(),
            frame));
        ReplaceInPalette(oldColor, newColor);
    }

    private void ReplaceInPalette(PaletteColor oldColor, PaletteColor newColor)
    {
        int indexOfOldColor = Document.Palette.IndexOf(oldColor);
        if (indexOfOldColor == -1)
            return;

        Document.Palette.RemoveAt(indexOfOldColor);
        Document.Palette.Insert(indexOfOldColor, newColor);
    }

    /// <summary>
    /// Creates a new mask on the <paramref name="member"/>
    /// </summary>
    public void CreateMask(IStructureMemberHandler member)
    {
        if (Internals.ChangeController.IsBlockingChangeActive)
            return;
        if (!member.MaskIsVisibleBindable)
            Internals.ActionAccumulator.AddActions(new StructureMemberMaskIsVisible_Action(true, member.Id));
        Internals.ActionAccumulator.AddFinishedActions(new CreateStructureMemberMask_Action(member.Id));
    }

    /// <summary>
    /// Deletes the mask of the <paramref name="member"/>
    /// </summary>
    public void DeleteMask(IStructureMemberHandler member)
    {
        if (Internals.ChangeController.IsBlockingChangeActive)
            return;
        Internals.ActionAccumulator.AddFinishedActions(new DeleteStructureMemberMask_Action(member.Id));
    }

    /// <summary>
    /// Applies the mask to the image
    /// </summary>
    public void ApplyMask(IStructureMemberHandler member, int frame)
    {
        if (Internals.ChangeController.IsBlockingChangeActive)
            return;

        Internals.ActionAccumulator.AddFinishedActions(new ApplyMask_Action(member.Id, frame),
            new DeleteStructureMemberMask_Action(member.Id));
    }

    /// <summary>
    /// Sets the selected structure memeber
    /// </summary>
    /// <param name="memberGuid">The Guid of the member to select</param>
    public void SetSelectedMember(Guid memberGuid) =>
        Internals.ActionAccumulator.AddActions(new SetSelectedMember_PassthroughAction(memberGuid));

    /// <summary>
    /// Adds a member to the soft selection
    /// </summary>
    /// <param name="memberGuid">The Guid of the member to add</param>
    public void AddSoftSelectedMember(Guid memberGuid) =>
        Internals.ActionAccumulator.AddActions(new AddSoftSelectedMember_PassthroughAction(memberGuid));

    /// <summary>
    /// Removes a member from the soft selection
    /// </summary>
    /// <param name="memberGuid">The Guid of the member to remove</param>
    public void RemoveSoftSelectedMember(Guid memberGuid) =>
        Internals.ActionAccumulator.AddActions(new RemoveSoftSelectedMember_PassthroughAction(memberGuid));

    /// <summary>
    /// Clears the soft selection
    /// </summary>
    public void ClearSoftSelectedMembers() =>
        Internals.ActionAccumulator.AddActions(new ClearSoftSelectedMembers_PassthroughAction());

    public void SetActiveFrame(int newFrame) =>
        Internals.ActionAccumulator.AddActions(new SetActiveFrame_PassthroughAction(newFrame));

    public void AddSelectedKeyFrame(Guid keyFrameGuid) =>
        Internals.ActionAccumulator.AddActions(new AddSelectedKeyFrame_PassthroughAction(keyFrameGuid));

    public void RemoveSelectedKeyFrame(Guid keyFrameGuid) =>
        Internals.ActionAccumulator.AddActions(new RemoveSelectedKeyFrame_PassthroughAction(keyFrameGuid));

    public void ClearSelectedKeyFrames() =>
        Internals.ActionAccumulator.AddActions(new ClearSelectedKeyFrames_PassthroughAction());

    /// <summary>
    /// Undo last change
    /// </summary>
    public void Undo()
    {
        if (Internals.ChangeController.IsBlockingChangeActive)
        {
            Internals.ChangeController.MidChangeUndoInlet();
            return;
        }

        Internals.ActionAccumulator.AddActions(new Undo_Action());
    }

    /// <summary>
    /// Redo previously undone change
    /// </summary>
    public void Redo()
    {
        if (Internals.ChangeController.IsBlockingChangeActive)
        {
            Internals.ChangeController.MidChangeRedoInlet();
            return;
        }

        Internals.ActionAccumulator.AddActions(new Redo_Action());
    }

    public void NudgeSelectedObject(VecI distance)
    {
        if (Internals.ChangeController.IsBlockingChangeActive)
        {
            Internals.ChangeController.SelectedObjectNudgedInlet(distance);
        }
    }

    /// <summary>
    /// Moves a member next to or inside another structure member
    /// </summary>
    /// <param name="memberToMove">The member to move</param>
    /// <param name="memberToMoveIntoOrNextTo">The target member</param>
    /// <param name="placement">Where to place the <paramref name="memberToMove"/></param>
    public void MoveStructureMember(Guid memberToMove, Guid memberToMoveIntoOrNextTo,
        StructureMemberPlacement placement)
    {
        if (Internals.ChangeController.IsBlockingChangeActive || memberToMove == memberToMoveIntoOrNextTo)
            return;
        Internals.StructureHelper.TryMoveStructureMember(memberToMove, memberToMoveIntoOrNextTo, placement);
    }

    /// <summary>
    /// Merge all structure members with the Guids inside <paramref name="members"/>
    /// </summary>
    public void MergeStructureMembers(IReadOnlyList<Guid> members)
    {
        if (Internals.ChangeController.IsBlockingChangeActive || members.Count < 2)
            return;

        IStructureMemberHandler? node = Document.StructureHelper.FindNode<IStructureMemberHandler>(members[0]);

        if (node is null)
            return;

        INodeHandler? parent = null;

        node.TraverseForwards(traversedNode =>
        {
            if (!members.Contains(traversedNode.Id))
            {
                parent = traversedNode;
                return false;
            }

            return true;
        });

        if (parent is null)
            return;

        Guid newGuid = Guid.NewGuid();

        //make a new layer, put combined image onto it, delete layers that were merged
        Internals.ActionAccumulator.AddActions(
            new CreateStructureMember_Action(parent.Id, newGuid, typeof(ImageLayerNode)),
            new StructureMemberName_Action(newGuid, node.NodeNameBindable),
            new CombineStructureMembersOnto_Action(members.ToHashSet(), newGuid,
                Document.AnimationHandler.ActiveFrameBindable));
        foreach (var member in members)
            Internals.ActionAccumulator.AddActions(new DeleteStructureMember_Action(member));
        Internals.ActionAccumulator.AddActions(new ChangeBoundary_Action());
    }

    /// <summary>
    /// Starts a image transform and pastes the transformed image on the currently selected layer
    /// </summary>
    /// <param name="image">The image to paste</param>
    /// <param name="startPos">Where the transform should start</param>
    public void PasteImageWithTransform(Surface image, VecI startPos)
    {
        if (Document.SelectedStructureMember is null)
            return;
        Internals.ChangeController.TryStartExecutor(new PasteImageExecutor(image, startPos));
    }

    /// <summary>
    /// Starts a image transform and pastes the transformed image on the currently selected layer
    /// </summary>
    /// <param name="image">The image to paste</param>
    /// <param name="startPos">Where the transform should start</param>
    public void PasteImageWithTransform(Surface image, VecI startPos, Guid memberGuid, bool drawOnMask)
    {
        Internals.ChangeController.TryStartExecutor(new PasteImageExecutor(image, startPos, memberGuid, drawOnMask));
    }

    /// <summary>
    /// Starts a transform on the selected area
    /// </summary>
    /// <param name="toolLinked">Is this transform started by a tool</param>
    public void TransformSelectedArea(bool toolLinked)
    {
        if (Document.SelectedStructureMember is null ||
            Internals.ChangeController.IsBlockingChangeActive && !toolLinked)
            return;
        Internals.ChangeController.TryStartExecutor(new TransformSelectedExecutor(toolLinked));
    }

    /// <summary>
    /// Ties stopping the currently executing tool linked executor
    /// </summary>
    public void TryStopToolLinkedExecutor()
    {
        if (Internals.ChangeController.GetCurrentExecutorType() == ExecutorType.ToolLinked)
            Internals.ChangeController.TryStopActiveExecutor();
    }

    public void DrawImage(Surface image, ShapeCorners corners, Guid memberGuid, bool ignoreClipSymmetriesEtc,
        bool drawOnMask, int frame) =>
        DrawImage(image, corners, memberGuid, ignoreClipSymmetriesEtc, drawOnMask, frame, true);

    /// <summary>
    /// Draws a image on the member with the <paramref name="memberGuid"/>
    /// </summary>
    /// <param name="image">The image to draw onto the layer</param>
    /// <param name="corners">The shape the image should fit into</param>
    /// <param name="memberGuid">The Guid of the member to paste on</param>
    /// <param name="ignoreClipSymmetriesEtc">Ignore selection clipping and symmetry (See DrawingChangeHelper.ApplyClipsSymmetriesEtc of UpdateableDocument)</param>
    /// <param name="drawOnMask">Draw on the mask or on the image</param>
    /// <param name="finish">Is this a finished action</param>
    private void DrawImage(Surface image, ShapeCorners corners, Guid memberGuid, bool ignoreClipSymmetriesEtc,
        bool drawOnMask, int atFrame, bool finish)
    {
        if (Internals.ChangeController.IsBlockingChangeActive)
            return;
        Internals.ActionAccumulator.AddActions(
            new PasteImage_Action(image, corners, memberGuid, ignoreClipSymmetriesEtc, drawOnMask, atFrame, default),
            new EndPasteImage_Action());
        if (finish)
            Internals.ActionAccumulator.AddFinishedActions();
    }

    /// <summary>
    /// Resizes the canvas to fit the content
    /// </summary>
    public void ClipCanvas()
    {
        if (Internals.ChangeController.IsBlockingChangeActive)
            return;
        Internals.ActionAccumulator.AddFinishedActions(
            new ClipCanvas_Action(Document.AnimationHandler.ActiveFrameBindable));
    }

    /// <summary>
    /// Flips the image on the <paramref name="flipType"/> axis
    /// </summary>
    public void FlipImage(FlipType flipType, int frame) => FlipImage(flipType, null, frame);

    /// <summary>
    /// Flips the members with the Guids of <paramref name="membersToFlip"/> on the <paramref name="flipType"/> axis
    /// </summary>
    public void FlipImage(FlipType flipType, List<Guid> membersToFlip, int frame)
    {
        if (Internals.ChangeController.IsBlockingChangeActive)
            return;

        Internals.ActionAccumulator.AddFinishedActions(new FlipImage_Action(flipType, frame, membersToFlip));
    }

    /// <summary>
    /// Rotates the image
    /// </summary>
    /// <param name="rotation">The degrees to rotate the image by</param>
    public void RotateImage(RotationAngle rotation) => RotateImage(rotation, null, -1);

    /// <summary>
    /// Rotates the members with the Guids of <paramref name="membersToRotate"/>
    /// </summary>
    /// <param name="rotation">The degrees to rotate the members by</param>
    public void RotateImage(RotationAngle rotation, List<Guid> membersToRotate, int frame)
    {
        if (Internals.ChangeController.IsBlockingChangeActive)
            return;

        Internals.ActionAccumulator.AddFinishedActions(new RotateImage_Action(rotation, membersToRotate, frame));
    }

    /// <summary>
    /// Puts the content of the image in the middle of the canvas
    /// </summary>
    public void CenterContent(IReadOnlyList<Guid> structureMembers, int frame)
    {
        if (Internals.ChangeController.IsBlockingChangeActive)
            return;

        Internals.ActionAccumulator.AddFinishedActions(new CenterContent_Action(structureMembers.ToList(), frame));
    }

    /// <summary>
    /// Imports a reference layer from a Pbgra Int32 array
    /// </summary>
    /// <param name="imageSize">The size of the image</param>
    public void ImportReferenceLayer(ImmutableArray<byte> imageBgra8888Bytes, VecI imageSize)
    {
        if (Internals.ChangeController.IsBlockingChangeActive)
            return;

        RectD referenceImageRect =
            new RectD(VecD.Zero, Document.SizeBindable).AspectFit(new RectD(VecD.Zero, imageSize));
        ShapeCorners corners = new ShapeCorners(referenceImageRect);
        Internals.ActionAccumulator.AddFinishedActions(new SetReferenceLayer_Action(corners, imageBgra8888Bytes,
            imageSize));
    }

    /// <summary>
    /// Deletes the reference layer
    /// </summary>
    public void DeleteReferenceLayer()
    {
        if (Internals.ChangeController.IsBlockingChangeActive || Document.ReferenceLayerHandler.ReferenceBitmap is null)
            return;

        Internals.ActionAccumulator.AddFinishedActions(new DeleteReferenceLayer_Action());
    }

    /// <summary>
    /// Starts a transform on the reference layer
    /// </summary>
    public void TransformReferenceLayer()
    {
        if (Document.ReferenceLayerHandler.ReferenceBitmap is null || Internals.ChangeController.IsBlockingChangeActive)
            return;
        Internals.ChangeController.TryStartExecutor(new TransformReferenceLayerExecutor());
    }

    /// <summary>
    /// Resets the reference layer transform
    /// </summary>
    public void ResetReferenceLayerPosition()
    {
        if (Document.ReferenceLayerHandler.ReferenceBitmap is null || Internals.ChangeController.IsBlockingChangeActive)
            return;


        VecD size = new(Document.ReferenceLayerHandler.ReferenceBitmap.Size.X,
            Document.ReferenceLayerHandler.ReferenceBitmap.Size.Y);
        RectD referenceImageRect = new RectD(VecD.Zero, Document.SizeBindable).AspectFit(new RectD(VecD.Zero, size));
        ShapeCorners corners = new ShapeCorners(referenceImageRect);
        Internals.ActionAccumulator.AddFinishedActions(
            new TransformReferenceLayer_Action(corners),
            new EndTransformReferenceLayer_Action()
        );
    }

    public void SelectionToMask(SelectionMode mode, int frame)
    {
        if (Document.SelectedStructureMember is not { } member || Document.SelectionPathBindable.IsEmpty)
            return;

        if (!Document.SelectedStructureMember.HasMaskBindable)
        {
            Internals.ActionAccumulator.AddActions(new CreateStructureMemberMask_Action(member.Id));
        }

        Internals.ActionAccumulator.AddFinishedActions(new SelectionToMask_Action(member.Id, mode, frame));
    }

    public void CropToSelection(int frame, bool clearSelection = true)
    {
        var bounds = Document.SelectionPathBindable.TightBounds;
        if (Document.SelectionPathBindable.IsEmpty || bounds.Width <= 0 || bounds.Height <= 0)
            return;

        Internals.ActionAccumulator.AddActions(new Crop_Action((RectI)bounds));

        if (clearSelection)
        {
            Internals.ActionAccumulator.AddFinishedActions(new ClearSelection_Action());
        }
        else
        {
            Internals.ActionAccumulator.AddFinishedActions();
        }
    }

    public void InvertSelection()
    {
        var selection = Document.SelectionPathBindable;
        var inverse = new VectorPath();
        inverse.AddRect(new RectI(new(0, 0), Document.SizeBindable));

        Internals.ActionAccumulator.AddFinishedActions(
            new SetSelection_Action(inverse.Op(selection, VectorPathOp.Difference)));
    }

    private Guid FindClosestMember(IReadOnlyList<Guid> guids)
    {
        IStructureMemberHandler? firstNode = Document.StructureHelper.FindNode<IStructureMemberHandler>(guids[0]);
        if (firstNode is null)
            return Guid.Empty;

        INodeHandler? parent = null;

        firstNode.TraverseForwards(traversedNode =>
        {
            if (!guids.Contains(traversedNode.Id) && traversedNode is IStructureMemberHandler)
            {
                parent = traversedNode;
                return false;
            }

            return true;
        });

        if (parent is null)
        {
            var lastNode = Document.StructureHelper.FindNode<IStructureMemberHandler>(guids[^1]);
            if (lastNode is null)
                return Guid.Empty;
            
            lastNode.TraverseBackwards(traversedNode =>
            {
                if (!guids.Contains(traversedNode.Id) && traversedNode is IStructureMemberHandler)
                {
                    parent = traversedNode;
                    return false;
                }

                return true;
            });
        }
        
        if (parent is null)
            return Guid.Empty;

        return parent.Id;
    }

    public void Rasterize(Guid memberId)
    {
        if (Internals.ChangeController.IsBlockingChangeActive)
            return;

        Internals.ActionAccumulator.AddFinishedActions(new RasterizeMember_Action(memberId));    
    }

    public void InvokeCustomAction(Action action)
    {
        if (Internals.ChangeController.IsBlockingChangeActive)
            return;

        IAction targetAction = new InvokeAction_PassthroughAction(action);
        
        Internals.ActionAccumulator.AddActions(targetAction);
    }
}
