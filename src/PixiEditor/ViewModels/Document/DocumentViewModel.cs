﻿using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ChunkyImageLib;
using ChunkyImageLib.DataHolders;
using ChunkyImageLib.Operations;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.DependencyInjection;
using PixiEditor.Models.DocumentPassthroughActions;
using PixiEditor.Models.Position;
using PixiEditor.ViewModels.SubViewModels;
using PixiEditor.ChangeableDocument.Actions;
using PixiEditor.ChangeableDocument.Actions.Generated;
using PixiEditor.ChangeableDocument.Actions.Undo;
using PixiEditor.ChangeableDocument.Changeables.Animations;
using PixiEditor.ChangeableDocument.Changeables.Graph.Interfaces;
using PixiEditor.ChangeableDocument.Changeables.Graph.Nodes;
using PixiEditor.ChangeableDocument.Changeables.Interfaces;
using PixiEditor.ChangeableDocument.ChangeInfos;
using PixiEditor.ChangeableDocument.Changes.NodeGraph;
using PixiEditor.ChangeableDocument.Enums;
using PixiEditor.ChangeableDocument.Rendering;
using PixiEditor.DrawingApi.Core;
using PixiEditor.DrawingApi.Core.Bridge;
using PixiEditor.DrawingApi.Core.Numerics;
using PixiEditor.DrawingApi.Core.Surfaces.ImageData;
using PixiEditor.DrawingApi.Core.Surfaces.PaintImpl;
using PixiEditor.DrawingApi.Core.Surfaces.Vector;
using PixiEditor.Extensions.Common.Localization;
using PixiEditor.Extensions.CommonApi.Palettes;
using PixiEditor.Helpers;
using PixiEditor.Helpers.Collections;
using PixiEditor.Helpers.Extensions;
using PixiEditor.Models.Controllers;
using PixiEditor.Models.DocumentModels;
using PixiEditor.Models.DocumentModels.Public;
using PixiEditor.Models.DocumentModels.UpdateableChangeExecutors.Features;
using PixiEditor.Models.Handlers;
using PixiEditor.Models.Layers;
using PixiEditor.Models.Rendering;
using PixiEditor.Models.Serialization;
using PixiEditor.Models.Serialization.Factories;
using PixiEditor.Models.Structures;
using PixiEditor.Models.Tools;
using PixiEditor.Numerics;
using PixiEditor.Parser.Skia;
using PixiEditor.ViewModels.Document.Nodes;
using PixiEditor.ViewModels.Document.TransformOverlays;
using PixiEditor.Views.Overlays.SymmetryOverlay;
using BlendMode = PixiEditor.DrawingApi.Core.Surfaces.BlendMode;
using Color = PixiEditor.DrawingApi.Core.ColorsImpl.Color;
using Colors = PixiEditor.DrawingApi.Core.ColorsImpl.Colors;
using Node = PixiEditor.Parser.Graph.Node;
using Point = Avalonia.Point;

namespace PixiEditor.ViewModels.Document;

#nullable enable
internal partial class DocumentViewModel : PixiObservableObject, IDocument
{
    public event EventHandler<LayersChangedEventArgs>? LayersChanged;
    public event EventHandler<DocumentSizeChangedEventArgs>? SizeChanged;
    public event Action ToolSessionFinished;

    private bool busy = false;

    public bool Busy
    {
        get => busy;
        set => SetProperty(ref busy, value);
    }


    private string coordinatesString = "";

    public string CoordinatesString
    {
        get => coordinatesString;
        set => SetProperty(ref coordinatesString, value);
    }

    private string? fullFilePath = null;

    public string? FullFilePath
    {
        get => fullFilePath;
        set
        {
            SetProperty(ref fullFilePath, value);
            OnPropertyChanged(nameof(FileName));
        }
    }

    public string FileName
    {
        get => fullFilePath is null ? new LocalizedString("UNNAMED") : Path.GetFileName(fullFilePath);
    }

    private Guid? lastChangeOnSave = null;

    public bool AllChangesSaved
    {
        get
        {
            return Internals.Tracker.LastChangeGuid == lastChangeOnSave;
        }
    }

    public DateTime OpenedUTC { get; } = DateTime.UtcNow;

    private bool horizontalSymmetryAxisEnabled;

    public bool HorizontalSymmetryAxisEnabledBindable
    {
        get => horizontalSymmetryAxisEnabled;
        set
        {
            if (!Internals.ChangeController.IsBlockingChangeActive)
                Internals.ActionAccumulator.AddFinishedActions(
                    new SymmetryAxisState_Action(SymmetryAxisDirection.Horizontal, value));
        }
    }

    private bool verticalSymmetryAxisEnabled;

    public bool VerticalSymmetryAxisEnabledBindable
    {
        get => verticalSymmetryAxisEnabled;
        set
        {
            if (!Internals.ChangeController.IsBlockingChangeActive)
                Internals.ActionAccumulator.AddFinishedActions(
                    new SymmetryAxisState_Action(SymmetryAxisDirection.Vertical, value));
        }
    }

    public bool AnySymmetryAxisEnabledBindable =>
        HorizontalSymmetryAxisEnabledBindable || VerticalSymmetryAxisEnabledBindable;

    private VecI size = new VecI(64, 64);
    public int Width => size.X;
    public int Height => size.Y;
    public VecI SizeBindable => size;

    private double horizontalSymmetryAxisY;
    public double HorizontalSymmetryAxisYBindable => horizontalSymmetryAxisY;

    private double verticalSymmetryAxisX;
    public double VerticalSymmetryAxisXBindable => verticalSymmetryAxisX;

    private readonly HashSet<IStructureMemberHandler> softSelectedStructureMembers = new();

    public bool BlockingUpdateableChangeActive => Internals.ChangeController.IsBlockingChangeActive;

    public bool IsChangeFeatureActive<T>() where T : IExecutorFeature =>
        Internals.ChangeController.IsChangeOfTypeActive<T>();

    public bool PointerDragChangeInProgress =>
        Internals.ChangeController.IsBlockingChangeActive && Internals.ChangeController.LeftMousePressed;

    public bool HasSavedUndo => Internals.Tracker.HasSavedUndo;
    public bool HasSavedRedo => Internals.Tracker.HasSavedRedo;

    public NodeGraphViewModel NodeGraph { get; }
    public DocumentStructureModule StructureHelper { get; }
    public DocumentToolsModule Tools { get; }
    public DocumentOperationsModule Operations { get; }
    public DocumentRenderer Renderer { get; }
    public SceneRenderer SceneRenderer { get; }
    public DocumentEventsModule EventInlet { get; }

    public ActionDisplayList ActionDisplays { get; } =
        new(() => ViewModelMain.Current.NotifyToolActionDisplayChanged());

    public IStructureMemberHandler? SelectedStructureMember { get; private set; } = null;


    private PreviewPainter previewSurface;

    public PreviewPainter PreviewPainter
    {
        get => previewSurface;
        set
        {
            SetProperty(ref previewSurface, value);
        }
    }

    private VectorPath selectionPath = new VectorPath();
    public VectorPath SelectionPathBindable => selectionPath;
    public ObservableCollection<PaletteColor> Swatches { get; set; } = new();
    public ObservableRangeCollection<PaletteColor> Palette { get; set; } = new();
    public SnappingViewModel SnappingViewModel { get; }
    ISnappingHandler IDocument.SnappingHandler => SnappingViewModel;
    public IReadOnlyCollection<Guid> SelectedMembers => GetSelectedMembers().AsReadOnly();
    public DocumentTransformViewModel TransformViewModel { get; }
    public ReferenceLayerViewModel ReferenceLayerViewModel { get; }
    public LineToolOverlayViewModel LineToolOverlayViewModel { get; }
    public AnimationDataViewModel AnimationDataViewModel { get; }

    public IReadOnlyCollection<IStructureMemberHandler> SoftSelectedStructureMembers => softSelectedStructureMembers;
    private DocumentInternalParts Internals { get; }
    INodeGraphHandler IDocument.NodeGraphHandler => NodeGraph;
    IDocumentOperations IDocument.Operations => Operations;
    ITransformHandler IDocument.TransformHandler => TransformViewModel;
    ILineOverlayHandler IDocument.LineToolOverlayHandler => LineToolOverlayViewModel;
    IReferenceLayerHandler IDocument.ReferenceLayerHandler => ReferenceLayerViewModel;
    IAnimationHandler IDocument.AnimationHandler => AnimationDataViewModel;


    private DocumentViewModel()
    {
        var serviceProvider = ViewModelMain.Current.Services;
        Internals = new DocumentInternalParts(this, serviceProvider);
        Internals.ChangeController.ToolSessionFinished += () => ToolSessionFinished?.Invoke();

        Tools = new DocumentToolsModule(this, Internals);
        StructureHelper = new DocumentStructureModule(this);
        EventInlet = new DocumentEventsModule(this, Internals);
        Operations = new DocumentOperationsModule(this, Internals);

        AnimationDataViewModel = new(this, Internals);

        NodeGraph = new NodeGraphViewModel(this, Internals);

        TransformViewModel = new(this);
        TransformViewModel.TransformMoved += (_, args) => Internals.ChangeController.TransformMovedInlet(args);

        LineToolOverlayViewModel = new();
        LineToolOverlayViewModel.LineMoved += (_, args) =>
            Internals.ChangeController.LineOverlayMovedInlet(args.Item1, args.Item2);

        SnappingViewModel = new();
        SnappingViewModel.AddFromDocumentSize(SizeBindable);
        SizeChanged += (_, args) =>
        {
            SnappingViewModel.AddFromDocumentSize(args.NewSize);
        };
        LayersChanged += (sender, args) =>
        {
            if (args.LayerChangeType == LayerAction.Add)
            {
                IReadOnlyStructureNode layer = Internals.Tracker.Document.FindMember(args.LayerAffectedGuid);
                SnappingViewModel.AddFromBounds(layer.Id.ToString(),
                    () => layer.GetTightBounds(AnimationDataViewModel.ActiveFrameTime) ?? RectD.Empty);
            }
            else if (args.LayerChangeType == LayerAction.Remove)
            {
                SnappingViewModel.Remove(args.LayerAffectedGuid.ToString());
            }
        };

        ReferenceLayerViewModel = new(this, Internals);

        Renderer = new DocumentRenderer(Internals.Tracker.Document);
        SceneRenderer = new SceneRenderer(Internals.Tracker.Document, this);
    }

    /// <summary>
    /// Creates a new document using the <paramref name="builder"/>
    /// </summary>
    /// <returns>The created document</returns>
    public static DocumentViewModel Build(Action<DocumentViewModelBuilder> builder)
    {
        var builderInstance = new DocumentViewModelBuilder();
        builder(builderInstance);

        Dictionary<int, Guid> mappedNodeIds = new();
        Dictionary<int, Guid> mappedKeyFrameIds = new();

        var viewModel = new DocumentViewModel();
        viewModel.Operations.ResizeCanvas(new VecI(builderInstance.Width, builderInstance.Height), ResizeAnchor.Center);

        var acc = viewModel.Internals.ActionAccumulator;

        viewModel.Internals.ChangeController.SymmetryDraggedInlet(
            new SymmetryAxisDragInfo(SymmetryAxisDirection.Horizontal, builderInstance.Height / 2));
        viewModel.Internals.ChangeController.SymmetryDraggedInlet(
            new SymmetryAxisDragInfo(SymmetryAxisDirection.Vertical, builderInstance.Width / 2));

        acc.AddActions(
            new SymmetryAxisPosition_Action(SymmetryAxisDirection.Horizontal, (double)builderInstance.Height / 2),
            new EndSymmetryAxisPosition_Action(),
            new SymmetryAxisPosition_Action(SymmetryAxisDirection.Vertical, (double)builderInstance.Width / 2),
            new EndSymmetryAxisPosition_Action());

        if (builderInstance.ReferenceLayer is { } refLayer)
        {
            acc.AddActions(new SetReferenceLayer_Action(refLayer.Shape, refLayer.ImageBgra8888Bytes.ToImmutableArray(),
                refLayer.ImageSize));
        }

        viewModel.Swatches = new ObservableCollection<PaletteColor>(builderInstance.Swatches);
        viewModel.Palette = new ObservableRangeCollection<PaletteColor>(builderInstance.Palette);

        SerializationConfig config =
            new SerializationConfig(BuiltInEncoders.Encoders[builderInstance.ImageEncoderUsed]);

        List<SerializationFactory> allFactories =
            ViewModelMain.Current.Services.GetServices<SerializationFactory>().ToList();

        AddNodes(builderInstance.Graph);

        if (builderInstance.Graph.AllNodes.Count == 0)
        {
            Guid outputNodeGuid = Guid.NewGuid();
            acc.AddActions(new CreateNode_Action(typeof(OutputNode), outputNodeGuid));
        }

        AddAnimationData(builderInstance.AnimationData, mappedNodeIds, mappedKeyFrameIds);

        acc.AddFinishedActions(new ChangeBoundary_Action(), new DeleteRecordedChanges_Action());
        viewModel.MarkAsSaved();

        return viewModel;


        void AddNodes(NodeGraphBuilder graph)
        {
            foreach (var node in graph.AllNodes)
            {
                AddNode(node.Id, node);
            }

            foreach (var node in graph.AllNodes)
            {
                Guid nodeGuid = mappedNodeIds[node.Id];

                var serializedNode = graph.AllNodes.First(x => x.Id == node.Id);

                if (serializedNode.AdditionalData != null && serializedNode.AdditionalData.Count > 0)
                {
                    acc.AddActions(new DeserializeNodeAdditionalData_Action(nodeGuid,
                        SerializationUtil.DeserializeDict(serializedNode.AdditionalData, config, allFactories)));
                }

                if (node.InputConnections != null)
                {
                    foreach (var connections in node.InputConnections)
                    {
                        if (mappedNodeIds.TryGetValue(connections.Key, out Guid outputNodeId))
                        {
                            foreach (var connection in connections.Value)
                            {
                                acc.AddActions(new ConnectProperties_Action(nodeGuid, outputNodeId,
                                    connection.inputPropName, connection.outputPropName));
                            }
                        }
                    }
                }
            }
        }

        void AddNode(int id, NodeGraphBuilder.NodeBuilder serializedNode)
        {
            Guid guid = Guid.NewGuid();
            mappedNodeIds.Add(id, guid);
            acc.AddActions(new CreateNodeFromName_Action(serializedNode.UniqueNodeName, guid));
            acc.AddFinishedActions(new NodePosition_Action(guid, serializedNode.Position.ToVecD()),
                new EndNodePosition_Action());

            if (serializedNode.InputValues != null)
            {
                foreach (var propertyValue in serializedNode.InputValues)
                {
                    object value = SerializationUtil.Deserialize(propertyValue.Value, config, allFactories);
                    acc.AddActions(new UpdatePropertyValue_Action(guid, propertyValue.Key, value));
                }
            }

            if (serializedNode.KeyFrames != null)
            {
                foreach (var keyFrame in serializedNode.KeyFrames)
                {
                    Guid keyFrameGuid = Guid.NewGuid();
                    /*Add should be here I think, but it crashes while deserializing multiple layers with no frames*/
                    mappedKeyFrameIds[keyFrame.Id] = keyFrameGuid;
                    acc.AddActions(
                        new SetKeyFrameData_Action(
                            guid,
                            keyFrameGuid,
                            SerializationUtil.Deserialize(keyFrame.Data, config, allFactories),
                            keyFrame.StartFrame,
                            keyFrame.Duration, keyFrame.AffectedElement, keyFrame.IsVisible));
                }
            }

            if (!string.IsNullOrEmpty(serializedNode.Name))
            {
                acc.AddActions(new SetNodeName_Action(guid, serializedNode.Name));
            }
        }

        /*void AddMember(Guid parentGuid, DocumentViewModelBuilder.StructureMemberBuilder member)
        {
            acc.AddActions(
                new CreateStructureMember_Action(parentGuid, member.Id,
                    member is DocumentViewModelBuilder.LayerBuilder
                        ? StructureMemberType.Layer
                        : StructureMemberType.Folder),
                new StructureMemberName_Action(member.Id, member.Name)
            );

            if (!member.IsVisible)
                acc.AddActions(new StructureMemberIsVisible_Action(member.IsVisible, member.Id));

            acc.AddActions(new StructureMemberBlendMode_Action(member.BlendMode, member.Id));

            acc.AddActions(new StructureMemberClipToMemberBelow_Action(member.ClipToMemberBelow, member.Id));

            if (member is DocumentViewModelBuilder.LayerBuilder layerBuilder)
            {
                acc.AddActions(new LayerLockTransparency_Action(layerBuilder.Id, layerBuilder.LockAlpha));
            }

            if (member is DocumentViewModelBuilder.LayerBuilder layer && layer.Surface is not null)
            {
                PasteImage(member.Id, layer.Surface, layer.Width, layer.Height, layer.OffsetX, layer.OffsetY,
                    false, 0);
            }

            acc.AddActions(
                new StructureMemberOpacity_Action(member.Id, member.Opacity),
                new EndStructureMemberOpacity_Action());

            if (member.HasMask)
            {
                var maskSurface = member.Mask.Surface.Surface;

                acc.AddActions(new CreateStructureMemberMask_Action(member.Id));

                if (!member.Mask.IsVisible)
                    acc.AddActions(new StructureMemberMaskIsVisible_Action(member.Mask.IsVisible, member.Id));

                PasteImage(member.Id, member.Mask.Surface, maskSurface.Size.X, maskSurface.Size.Y, 0, 0, true, 0);
            }

            acc.AddFinishedActions();

            if (member is DocumentViewModelBuilder.FolderBuilder { Children: not null } folder)
            {
                AddMembers(member.Id, folder.Children);
            }
        }*/

        /*void PasteImage(Guid guid, DocumentViewModelBuilder.SurfaceBuilder surface, int width, int height, int offsetX,
            int offsetY, bool onMask, int frame, Guid? keyFrameGuid = default)
        {
            acc.AddActions(
                new PasteImage_Action(surface.Surface, new(new RectD(new VecD(offsetX, offsetY), new(width, height))),
                    guid, true, onMask, frame, keyFrameGuid ?? default),
                new EndPasteImage_Action());
        }*/

        /*void AddMembers(Guid parentGuid, IEnumerable<DocumentViewModelBuilder.StructureMemberBuilder> builders)
        {
            foreach (var child in builders.Reverse())
            {
                if (child.Id == default)
                {
                    child.Id = Guid.NewGuid();
                }

                AddMember(parentGuid, child);
            }
        }*/

        void AddAnimationData(AnimationDataBuilder? data, Dictionary<int, Guid> mappedIds,
            Dictionary<int, Guid> mappedKeyFrameIds)
        {
            if (data is null)
                return;

            acc.AddActions(new SetFrameRate_Action(data.FrameRate));
            acc.AddActions(new SetOnionSettings_Action(data.OnionFrames, data.OnionOpacity));
            foreach (var keyFrame in data.KeyFrameGroups)
            {
                if (keyFrame is GroupKeyFrameBuilder group)
                {
                    foreach (var child in group.Children)
                    {
                        acc.AddActions(
                            new CreateRasterKeyFrame_Action(
                                mappedIds[child.NodeId],
                                mappedKeyFrameIds[child.KeyFrameId],
                                -1, -1, default));

                        acc.AddFinishedActions();
                    }
                }
            }
        }
    }

    public void MarkAsSaved()
    {
        lastChangeOnSave = Internals.Tracker.LastChangeGuid;
        OnPropertyChanged(nameof(AllChangesSaved));
    }

    public void MarkAsUnsaved()
    {
        lastChangeOnSave = Guid.NewGuid();
        OnPropertyChanged(nameof(AllChangesSaved));
    }


    /// <summary>
    /// Tries rendering the whole document
    /// </summary>
    /// <returns><see cref="Error"/> if the ChunkyImage was disposed, otherwise a <see cref="Surface"/> of the rendered document</returns>
    public OneOf<Error, Surface> TryRenderWholeImage(KeyFrameTime frameTime)
    {
        try
        {
            Surface finalSurface = null; 
            DrawingBackendApi.Current.RenderingDispatcher.Invoke(() =>
            {
                using Texture texture = new Texture(SizeBindable);
                Renderer.RenderDocument(texture.DrawingSurface, frameTime);
                
                finalSurface = new Surface(SizeBindable);
                finalSurface.DrawingSurface.Canvas.DrawImage(texture.DrawingSurface.Snapshot(), 0, 0);
            });

            return finalSurface;
        }
        catch (ObjectDisposedException)
        {
            return new Error();
        }
    }

    /// <summary>
    /// Takes the selected area and converts it into a surface
    /// </summary>
    /// <returns><see cref="Error"/> on error, <see cref="None"/> for empty <see cref="Surface"/>, <see cref="Surface"/> otherwise.</returns>
    public OneOf<Error, None, (Surface, RectI)> TryExtractAreaFromSelected(
        RectI bounds)
    {
        var selectedLayers = ExtractSelectedLayers();
        if (selectedLayers.Count == 0)
            return new Error();
        if (bounds.IsZeroOrNegativeArea)
            return new None();

        RectI finalBounds = default;

        for (int i = 0; i < selectedLayers.Count; i++)
        {
            var layerVm = StructureHelper.Find(selectedLayers[i]);
            IReadOnlyStructureNode? layer = Internals.Tracker.Document.FindMember(layerVm.Id);
            if (layer is null)
                return new Error();

            RectI? memberImageBounds;
            try
            {
                if (layer is IReadOnlyImageNode imgNode)
                {
                    memberImageBounds = imgNode.GetLayerImageAtFrame(AnimationDataViewModel.ActiveFrameBindable)
                        .FindChunkAlignedMostUpToDateBounds();
                }
                else
                {
                    memberImageBounds = (RectI?)layer.GetTightBounds(AnimationDataViewModel.ActiveFrameTime);
                }
            }
            catch (ObjectDisposedException)
            {
                return new Error();
            }

            if (memberImageBounds is null)
                continue;

            RectI combinedBounds = bounds.Intersect(memberImageBounds.Value);
            combinedBounds = combinedBounds.Intersect(new RectI(VecI.Zero, SizeBindable));

            if (combinedBounds.IsZeroOrNegativeArea)
                continue;

            if (i == 0 || finalBounds == default)
            {
                finalBounds = combinedBounds;
            }
            else
            {
                finalBounds = finalBounds.Union(combinedBounds);
            }
        }

        Surface output = new(finalBounds.Size);

        VectorPath clipPath = new VectorPath(SelectionPathBindable) { FillType = PathFillType.EvenOdd };
        clipPath.Transform(Matrix3X3.CreateTranslation(-bounds.X, -bounds.Y));
        output.DrawingSurface.Canvas.Save();
        if (!clipPath.IsEmpty)
        {
            output.DrawingSurface.Canvas.ClipPath(clipPath);
        }

        using Paint paint = new Paint() { BlendMode = BlendMode.SrcOver };

        foreach (var layer in selectedLayers)
        {
            try
            {
                var layerVm = Internals.Tracker.Document.FindMember(layer);

                DrawingBackendApi.Current.RenderingDispatcher.Invoke(() =>
                {
                    using Surface toPaintOn = new Surface(SizeBindable);

                    Renderer.RenderLayer(toPaintOn.DrawingSurface, layerVm.Id, ChunkResolution.Full,
                        AnimationDataViewModel.ActiveFrameTime);
                    using Image snapshot = toPaintOn.DrawingSurface.Snapshot(bounds);
                    output.DrawingSurface.Canvas.DrawImage(snapshot, 0, 0, paint);
                });
            }
            catch (ObjectDisposedException)
            {
                output.Dispose();
                return new Error();
            }
        }

        output.DrawingSurface.Canvas.Restore();
        return (output, bounds);
    }

    /// <summary>
    /// Picks the color at <paramref name="pos"/>
    /// </summary>
    /// <param name="includeReference">Should the color be picked from the reference layer</param>
    /// <param name="includeCanvas">Should the color be picked from the canvas</param>
    /// <param name="referenceTopmost">Is the reference layer topmost. (Only affects the result is includeReference and includeCanvas are set.)</param>
    public Color PickColor(VecD pos, DocumentScope scope, bool includeReference, bool includeCanvas, int frame,
        bool referenceTopmost = false)
    {
        if (scope == DocumentScope.SingleLayer && includeReference && includeCanvas)
            includeReference = false;

        if (includeCanvas && includeReference)
        {
            Color canvasColor = PickColorFromCanvas((VecI)pos, scope, frame);
            Color? potentialReferenceColor = PickColorFromReferenceLayer(pos);
            if (potentialReferenceColor is not { } referenceColor)
                return canvasColor;

            if (!referenceTopmost)
            {
                return ColorHelpers.BlendColors(referenceColor, canvasColor);
            }

            byte referenceAlpha = canvasColor.A == 0
                ? referenceColor.A
                : (byte)(referenceColor.A * ReferenceLayerViewModel.TopMostOpacity);

            referenceColor = new Color(referenceColor.R, referenceColor.G, referenceColor.B, referenceAlpha);
            return ColorHelpers.BlendColors(canvasColor, referenceColor);
        }

        if (includeCanvas)
            return PickColorFromCanvas((VecI)pos, scope, frame);
        if (includeReference)
            return PickColorFromReferenceLayer(pos) ?? Colors.Transparent;
        return Colors.Transparent;
    }

    public Color? PickColorFromReferenceLayer(VecD pos)
    {
        Texture? bitmap = ReferenceLayerViewModel.ReferenceBitmap;
        if (bitmap is null)
            return null;

        Matrix matrix = ReferenceLayerViewModel.ReferenceTransformMatrix;
        matrix = matrix.Invert();
        var transformed = matrix.Transform(new Point(pos.X, pos.Y));

        if (transformed.X < 0 || transformed.Y < 0 || transformed.X >= bitmap.Size.X || transformed.Y >= bitmap.Size.Y)
            return null;

        return bitmap.GetSRGBPixel(new VecI((int)transformed.X, (int)transformed.Y));
    }

    public Color PickColorFromCanvas(VecI pos, DocumentScope scope, KeyFrameTime frameTime)
    {
        // there is a tiny chance that the image might get disposed by another thread
        try
        {
            // it might've been a better idea to implement this function asynchronously
            // via a passthrough action to avoid all the try catches
            if (scope == DocumentScope.AllLayers)
            {
                VecI chunkPos = OperationHelper.GetChunkPos(pos, ChunkyImage.FullChunkSize);
                // TODO: Implement this
                /*return Renderer.RenderChunk(chunkPos, ChunkResolution.Full,
                        frameTime)
                    .Match(
                        chunk =>
                        {
                            VecI posOnChunk = pos - chunkPos * ChunkyImage.FullChunkSize;
                            var color = chunk.Surface.GetSRGBPixel(posOnChunk);
                            chunk.Dispose();
                            return color;
                        },
                        _ => Colors.Transparent);*/
            }

            if (SelectedStructureMember is not ILayerHandler layerVm)
                return Colors.Transparent;
            IReadOnlyStructureNode? maybeMember = Internals.Tracker.Document.FindMember(layerVm.Id);
            if (maybeMember is not IReadOnlyImageNode layer)
                return Colors.Transparent;
            return layer.GetLayerImageAtFrame(frameTime.Frame).GetMostUpToDatePixel(pos);
        }
        catch (ObjectDisposedException)
        {
            return Colors.Transparent;
        }
    }

    #region Internal Methods

// these are intended to only be called from DocumentUpdater

    public void InternalRaiseLayersChanged(LayersChangedEventArgs args) => LayersChanged?.Invoke(this, args);

    public void RaiseSizeChanged(DocumentSizeChangedEventArgs args) => SizeChanged?.Invoke(this, args);

    public void ISetVerticalSymmetryAxisEnabled(bool verticalSymmetryAxisEnabled)
    {
        this.verticalSymmetryAxisEnabled = verticalSymmetryAxisEnabled;
        OnPropertyChanged(nameof(VerticalSymmetryAxisEnabledBindable));
    }

    public void SetHorizontalSymmetryAxisEnabled(bool horizontalSymmetryAxisEnabled)
    {
        this.horizontalSymmetryAxisEnabled = horizontalSymmetryAxisEnabled;
        OnPropertyChanged(nameof(HorizontalSymmetryAxisEnabledBindable));
        OnPropertyChanged(nameof(AnySymmetryAxisEnabledBindable));
    }

    public void SetVerticalSymmetryAxisEnabled(bool infoState)
    {
        verticalSymmetryAxisEnabled = infoState;
        OnPropertyChanged(nameof(VerticalSymmetryAxisEnabledBindable));
        OnPropertyChanged(nameof(AnySymmetryAxisEnabledBindable));
    }

    public void SetVerticalSymmetryAxisX(double verticalSymmetryAxisX)
    {
        this.verticalSymmetryAxisX = verticalSymmetryAxisX;
        OnPropertyChanged(nameof(VerticalSymmetryAxisXBindable));
    }

    public void SetSelectedMember(IStructureMemberHandler member)
    {
        SelectedStructureMember = member;
        Internals.ChangeController.MembersSelectedInlet(GetSelectedMembers());
        OnPropertyChanged(nameof(SelectedStructureMember));
    }

    public void SetHorizontalSymmetryAxisY(double horizontalSymmetryAxisY)
    {
        this.horizontalSymmetryAxisY = horizontalSymmetryAxisY;
        OnPropertyChanged(nameof(HorizontalSymmetryAxisYBindable));
    }

    public void SetSize(VecI size)
    {
        var oldSize = size;
        this.size = size;
        OnPropertyChanged(nameof(SizeBindable));
        OnPropertyChanged(nameof(Width));
        OnPropertyChanged(nameof(Height));

        // TODO: Make sure this is correct, it was in InternalRaiseSizeChanged previously, check DocumentUpdater.cs ProcessSize
        SizeChanged?.Invoke(this, new DocumentSizeChangedEventArgs(this, oldSize, size));
    }

    public void UpdateSelectionPath(VectorPath vectorPath)
    {
        (VectorPath? toDispose, this.selectionPath) = (this.selectionPath, vectorPath);
        toDispose.Dispose();
        OnPropertyChanged(nameof(SelectionPathBindable));
    }

    public void AddSoftSelectedMember(IStructureMemberHandler member)
    {
        softSelectedStructureMembers.Add(member);
        Internals.ChangeController.MembersSelectedInlet(GetSelectedMembers());
    }

    public void RemoveSoftSelectedMember(IStructureMemberHandler member)
    {
        softSelectedStructureMembers.Remove(member);
        Internals.ChangeController.MembersSelectedInlet(GetSelectedMembers());
    }

    public void ClearSoftSelectedMembers()
    {
        softSelectedStructureMembers.Clear();
        Internals.ChangeController.MembersSelectedInlet(GetSelectedMembers());
    }

    #endregion

    /// <summary>
    /// Returns a list of all selected members (Hard and Soft selected)
    /// </summary>
    public List<Guid> GetSelectedMembers()
    {
        List<Guid> layerGuids = new List<Guid>();
        if (SelectedStructureMember is not null)
            layerGuids.Add(SelectedStructureMember.Id);

        layerGuids.AddRange(softSelectedStructureMembers.Select(x => x.Id));
        return layerGuids;
    }

    /// <summary>
    ///     Gets all selected layers extracted from selected folders.
    /// </summary>
    /// <param name="includeFoldersWithMask">Should folders with mask be included</param>
    /// <returns>A list of GUIDs of selected layers</returns>
    public List<Guid> ExtractSelectedLayers(bool includeFoldersWithMask = false)
    {
        var result = new List<Guid>();
        List<Guid> selectedMembers = GetSelectedMembers();
        var allLayers = StructureHelper.GetAllLayers();
        foreach (var member in allLayers)
        {
            if (selectedMembers.Contains(member.Id))
            {
                result.Add(member.Id);
            }
        }

        return result;
    }

    public void UpdateSavedState()
    {
        OnPropertyChanged(nameof(AllChangesSaved));
    }

    private void ExtractSelectedLayers(IFolderHandler folder, List<Guid> list,
        bool includeFoldersWithMask)
    {
        foreach (var member in folder.Children)
        {
            if (member is ImageLayerNodeViewModel layer && !list.Contains(layer.Id))
            {
                list.Add(layer.Id);
            }
            else if (member is FolderNodeViewModel childFolder)
            {
                if (includeFoldersWithMask && childFolder.HasMaskBindable && !list.Contains(childFolder.Id))
                    list.Add(childFolder.Id);

                ExtractSelectedLayers(childFolder, list, includeFoldersWithMask);
            }
        }
    }

    public Image[] RenderFrames(Func<Surface, Surface> processFrameAction = null, CancellationToken token = default)
    {
        if (AnimationDataViewModel.KeyFrames.Count == 0)
            return [];

        if (token.IsCancellationRequested)
            return [];

        int firstFrame = AnimationDataViewModel.FirstFrame;
        int framesCount = AnimationDataViewModel.FramesCount;
        int lastFrame = firstFrame + framesCount;

        Image[] images = new Image[framesCount];

        // TODO: Multi-threading
        for (int i = firstFrame; i < lastFrame; i++)
        {
            if (token.IsCancellationRequested)
                return [];

            double normalizedTime = (double)(i - firstFrame) / framesCount;
            KeyFrameTime frameTime = new KeyFrameTime(i, normalizedTime);
            var surface = TryRenderWholeImage(frameTime);
            if (surface.IsT0)
            {
                continue;
            }

            if (processFrameAction is not null)
            {
                surface = processFrameAction(surface.AsT1);
            }

            images[i - firstFrame] = surface.AsT1.DrawingSurface.Snapshot();
            surface.AsT1.Dispose();
        }

        return images;
    }

    /// <summary>
    ///     Render frames progressively and disposes the surface after processing.
    /// </summary>
    /// <param name="processFrameAction">Action to perform on rendered frame</param>
    /// <param name="token"></param>
    public void RenderFramesProgressive(Action<Surface, int> processFrameAction, CancellationToken token)
    {
        if (AnimationDataViewModel.KeyFrames.Count == 0)
            return;

        int firstFrame = AnimationDataViewModel.FirstFrame;
        int framesCount = AnimationDataViewModel.FramesCount;
        int lastFrame = firstFrame + framesCount;

        int activeFrame = AnimationDataViewModel.ActiveFrameBindable;

        for (int i = firstFrame; i < lastFrame; i++)
        {
            if (token.IsCancellationRequested)
                return;

            KeyFrameTime frameTime = new KeyFrameTime(i, (double)(i - firstFrame) / framesCount);

            var surface = TryRenderWholeImage(frameTime);
            if (surface.IsT0)
            {
                continue;
            }

            processFrameAction(surface.AsT1, i - firstFrame);
            surface.AsT1.Dispose();
        }
    }

    public bool RenderFrames(List<Image> frames, Func<Surface, Surface> processFrameAction = null)
    {
        if (AnimationDataViewModel.KeyFrames.Count == 0)
            return false;

        var keyFrames = AnimationDataViewModel.KeyFrames;
        var firstFrame = keyFrames.Min(x => x.StartFrameBindable);
        var lastFrame = keyFrames.Max(x => x.StartFrameBindable + x.DurationBindable);

        for (int i = firstFrame; i < lastFrame; i++)
        {
            KeyFrameTime frameTime = new KeyFrameTime(i, (double)(i - firstFrame) / (lastFrame - firstFrame));
            var surface = TryRenderWholeImage(frameTime);
            if (surface.IsT0)
            {
                return false;
            }

            if (processFrameAction is not null)
            {
                surface = processFrameAction(surface.AsT1);
            }


            var snapshot = surface.AsT1.DrawingSurface.Snapshot();
            frames.Add(snapshot);
        }

        return true;
    }

    private static void ClearTempFolder(string tempRenderingPath)
    {
        string[] files = Directory.GetFiles(tempRenderingPath);
        for (var i = 0; i < files.Length; i++)
        {
            var file = files[i];
            File.Delete(file);
        }
    }

    public void Dispose()
    {
        Internals.Tracker.Dispose();
        Internals.Tracker.Document.Dispose();
    }
}
