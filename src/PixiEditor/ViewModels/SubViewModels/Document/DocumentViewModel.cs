﻿using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ChunkyImageLib;
using ChunkyImageLib.DataHolders;
using ChunkyImageLib.Operations;
using Models.DocumentModels.Public;
using PixiEditor.ChangeableDocument.Changeables.Interfaces;
using PixiEditor.ChangeableDocument.Enums;
using PixiEditor.ChangeableDocument.Rendering;
using PixiEditor.Helpers;
using PixiEditor.Models.Controllers;
using PixiEditor.Models.DataHolders;
using PixiEditor.Models.DocumentModels;
using PixiEditor.Models.DocumentModels.Public;

namespace PixiEditor.ViewModels.SubViewModels.Document;

#nullable enable
internal class DocumentViewModel : NotifyableObject
{
    public event EventHandler<LayersChangedEventArgs>? LayersChanged;

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
            RaisePropertyChanged(nameof(FileName));
        }
    }
    public string FileName
    {
        get => fullFilePath is null ? "Unnamed" : Path.GetFileName(fullFilePath);
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
        set => Internals.ActionAccumulator.AddFinishedActions(new SymmetryAxisState_Action(SymmetryAxisDirection.Horizontal, value));
    }

    private bool verticalSymmetryAxisEnabled;
    public bool VerticalSymmetryAxisEnabledBindable
    {
        get => verticalSymmetryAxisEnabled;
        set => Internals.ActionAccumulator.AddFinishedActions(new SymmetryAxisState_Action(SymmetryAxisDirection.Vertical, value));
    }

    private VecI size = new VecI(64, 64);
    public int Width => size.X;
    public int Height => size.Y;
    public VecI SizeBindable => size;

    private int horizontalSymmetryAxisY;
    public int HorizontalSymmetryAxisYBindable => horizontalSymmetryAxisY;

    private int verticalSymmetryAxisX;
    public int VerticalSymmetryAxisXBindable => verticalSymmetryAxisX;

    private HashSet<StructureMemberViewModel> softSelectedStructureMembers = new();
    public IReadOnlyCollection<StructureMemberViewModel> SoftSelectedStructureMembers => softSelectedStructureMembers;


    public bool UpdateableChangeActive => Internals.ChangeController.IsChangeActive;
    public bool HasSavedUndo => Internals.Tracker.HasSavedUndo;
    public bool HasSavedRedo => Internals.Tracker.HasSavedRedo;
    public IReadOnlyReferenceLayer? ReferenceLayer => Internals.Tracker.Document.ReferenceLayer;
    public BitmapSource? ReferenceBitmap => ReferenceLayer?.Image.ToWriteableBitmap();
    public VecI ReferenceBitmapSize => ReferenceLayer?.Image.Size ?? VecI.Zero;
    public ShapeCorners ReferenceShape => ReferenceLayer?.Shape ?? default;
    public Matrix ReferenceTransformMatrix
    {
        get
        {
            if (ReferenceLayer is null)
                return Matrix.Identity;
            SKMatrix skiaMatrix = OperationHelper.CreateMatrixFromPoints(ReferenceLayer.Shape, ReferenceLayer.Image.Size);
            return new Matrix(skiaMatrix.ScaleX, skiaMatrix.SkewY, skiaMatrix.SkewX, skiaMatrix.ScaleY, skiaMatrix.TransX, skiaMatrix.TransY);
        }
    }

    public FolderViewModel StructureRoot { get; }
    public DocumentStructureModule StructureHelper { get; }
    public DocumentToolsModule Tools { get; }
    public DocumentOperationsModule Operations { get; }
    public DocumentEventsModule EventInlet { get; }

    public StructureMemberViewModel? SelectedStructureMember { get; private set; } = null;

    public Dictionary<ChunkResolution, SKSurface> Surfaces { get; set; } = new();
    public Dictionary<ChunkResolution, WriteableBitmap> Bitmaps { get; set; } = new()
    {
        [ChunkResolution.Full] = new WriteableBitmap(64, 64, 96, 96, PixelFormats.Pbgra32, null),
        [ChunkResolution.Half] = new WriteableBitmap(32, 32, 96, 96, PixelFormats.Pbgra32, null),
        [ChunkResolution.Quarter] = new WriteableBitmap(16, 16, 96, 96, PixelFormats.Pbgra32, null),
        [ChunkResolution.Eighth] = new WriteableBitmap(8, 8, 96, 96, PixelFormats.Pbgra32, null),
    };
    public WriteableBitmap PreviewBitmap { get; set; }
    public SKSurface PreviewSurface { get; set; }


    private SKPath selectionPath = new SKPath();
    public SKPath SelectionPathBindable => selectionPath;

    public WpfObservableRangeCollection<SKColor> Swatches { get; set; } = new WpfObservableRangeCollection<SKColor>();
    public WpfObservableRangeCollection<SKColor> Palette { get; set; } = new WpfObservableRangeCollection<SKColor>();

    public ExecutionTrigger<VecI> CenterViewportTrigger { get; } = new ExecutionTrigger<VecI>();
    public ExecutionTrigger<double> ZoomViewportTrigger { get; } = new ExecutionTrigger<double>();
    public DocumentTransformViewModel TransformViewModel { get; }


    private DocumentInternalParts Internals { get; }

    public DocumentViewModel()
    {
        Internals = new DocumentInternalParts(this);
        Tools = new DocumentToolsModule(this, Internals);
        StructureHelper = new DocumentStructureModule(this);
        EventInlet = new DocumentEventsModule(this, Internals);
        Operations = new DocumentOperationsModule(this, Internals);

        StructureRoot = new FolderViewModel(this, Internals, Internals.Tracker.Document.StructureRoot.GuidValue);

        TransformViewModel = new();
        TransformViewModel.TransformMoved += (_, args) => Internals.ChangeController.TransformMovedInlet(args);

        foreach (KeyValuePair<ChunkResolution, WriteableBitmap> bitmap in Bitmaps)
        {
            SKSurface? surface = SKSurface.Create(
                new SKImageInfo(bitmap.Value.PixelWidth, bitmap.Value.PixelHeight, SKColorType.Bgra8888, SKAlphaType.Premul, SKColorSpace.CreateSrgb()),
                bitmap.Value.BackBuffer, bitmap.Value.BackBufferStride);
            Surfaces[bitmap.Key] = surface;
        }

        VecI previewSize = StructureMemberViewModel.CalculatePreviewSize(SizeBindable);
        PreviewBitmap = new WriteableBitmap(previewSize.X, previewSize.Y, 96, 96, PixelFormats.Pbgra32, null);
        PreviewSurface = SKSurface.Create(new SKImageInfo(previewSize.X, previewSize.Y, SKColorType.Bgra8888), PreviewBitmap.BackBuffer, PreviewBitmap.BackBufferStride);
    }

    public void MarkAsSaved()
    {
        lastChangeOnSave = Internals.Tracker.LastChangeGuid;
        RaisePropertyChanged(nameof(AllChangesSaved));
    }

    public Surface? MaybeExtractSelectedArea()
    {
        if (SelectedStructureMember is null || SelectedStructureMember is not LayerViewModel layerVm || SelectionPathBindable.IsEmpty)
            return null;

        IReadOnlyLayer? layer = (IReadOnlyLayer?)Internals.Tracker.Document.FindMember(layerVm.GuidValue);
        if (layer is null)
            return null;
            
        RectI bounds = (RectI)SelectionPathBindable.TightBounds;
        Surface output = new(bounds.Size);

        SKPath clipPath = new SKPath(SelectionPathBindable) { FillType = SKPathFillType.EvenOdd };
        clipPath.Transform(SKMatrix.CreateTranslation(-bounds.X, -bounds.Y));
        output.SkiaSurface.Canvas.Save();
        output.SkiaSurface.Canvas.ClipPath(clipPath);
        try
        {
            layer.LayerImage.DrawMostUpToDateRegionOn(bounds, ChunkResolution.Full, output.SkiaSurface, VecI.Zero);
        }
        catch (ObjectDisposedException)
        {
            output.Dispose();
            return null;
        }
        output.SkiaSurface.Canvas.Restore();

        return output;
    }

    public SKColor PickColor(VecI pos, bool fromAllLayers)
    {
        // there is a tiny chance that the image might get disposed by another thread
        try
        {
            // it might've been a better idea to implement this function asynchonously
            // via a passthrough action to avoid all the try catches
            if (fromAllLayers)
            {
                VecI chunkPos = OperationHelper.GetChunkPos(pos, ChunkyImage.FullChunkSize);
                return ChunkRenderer.MergeWholeStructure(chunkPos, ChunkResolution.Full, Internals.Tracker.Document.StructureRoot)
                    .Match<SKColor>(
                        (Chunk chunk) =>
                        {
                            VecI posOnChunk = pos - chunkPos * ChunkyImage.FullChunkSize;
                            SKColor color = chunk.Surface.GetSRGBPixel(posOnChunk);
                            chunk.Dispose();
                            return color;
                        },
                        _ => SKColors.Transparent
                    );
            }

            if (SelectedStructureMember is not LayerViewModel layerVm)
                return SKColors.Transparent;
            IReadOnlyStructureMember? maybeMember = Internals.Tracker.Document.FindMember(layerVm.GuidValue);
            if (maybeMember is not IReadOnlyLayer layer)
                return SKColors.Transparent;
            return layer.LayerImage.GetMostUpToDatePixel(pos);
        }
        catch (ObjectDisposedException)
        {
            return SKColors.Transparent;
        }
    }

    #region Internal Methods
    // these are intended to only be called from DocumentUpdater

    public void InternalRaiseLayersChanged(LayersChangedEventArgs args) => LayersChanged?.Invoke(this, args);

    public void InternalSetVerticalSymmetryAxisEnabled(bool verticalSymmetryAxisEnabled)
    {
        this.verticalSymmetryAxisEnabled = verticalSymmetryAxisEnabled;
        RaisePropertyChanged(nameof(VerticalSymmetryAxisEnabledBindable));
    }

    public void InternalSetHorizontalSymmetryAxisEnabled(bool horizontalSymmetryAxisEnabled)
    {
        this.horizontalSymmetryAxisEnabled = horizontalSymmetryAxisEnabled;
        RaisePropertyChanged(nameof(HorizontalSymmetryAxisEnabledBindable));
    }

    public void InternalSetVerticalSymmetryAxisX(int verticalSymmetryAxisX)
    {
        this.verticalSymmetryAxisX = verticalSymmetryAxisX;
        RaisePropertyChanged(nameof(VerticalSymmetryAxisXBindable));
    }

    public void InternalSetHorizontalSymmetryAxisY(int horizontalSymmetryAxisY)
    {
        this.horizontalSymmetryAxisY = horizontalSymmetryAxisY;
        RaisePropertyChanged(nameof(HorizontalSymmetryAxisYBindable));
    }

    public void InternalSetSize(VecI size)
    {
        this.size = size;
        RaisePropertyChanged(nameof(SizeBindable));
        RaisePropertyChanged(nameof(Width));
        RaisePropertyChanged(nameof(Height));
    }

    public void InternalUpdateSelectionPath(SKPath selectionPath)
    {
        (SKPath? toDispose, this.selectionPath) = (this.selectionPath, selectionPath);
        toDispose.Dispose();
        RaisePropertyChanged(nameof(SelectionPathBindable));
    }

    public void InternalSetSelectedMember(StructureMemberViewModel? member)
    {
        SelectedStructureMember = member;
        RaisePropertyChanged(nameof(SelectedStructureMember));
    }

    public void InternalClearSoftSelectedMembers() => softSelectedStructureMembers.Clear();

    public void InternalAddSoftSelectedMember(StructureMemberViewModel member) => softSelectedStructureMembers.Add(member);
    public void InternalRemoveSoftSelectedMember(StructureMemberViewModel member) => softSelectedStructureMembers.Remove(member);

    #endregion
}
