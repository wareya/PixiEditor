﻿using System.Diagnostics.CodeAnalysis;
using PixiEditor.ChangeableDocument.Changeables.Animations;
using PixiEditor.ChangeableDocument.Changeables.Interfaces;
using PixiEditor.DrawingApi.Core.Numerics;
using PixiEditor.Numerics;

namespace PixiEditor.ChangeableDocument.Changeables;

internal class Document : IChangeable, IReadOnlyDocument, IDisposable
{
    IReadOnlyFolder IReadOnlyDocument.StructureRoot => StructureRoot;
    IReadOnlySelection IReadOnlyDocument.Selection => Selection;
    IReadOnlyAnimationData IReadOnlyDocument.AnimationData => AnimationData;
    IReadOnlyStructureMember? IReadOnlyDocument.FindMember(Guid guid) => FindMember(guid);
    bool IReadOnlyDocument.TryFindMember(Guid guid, [NotNullWhen(true)] out IReadOnlyStructureMember? member) => TryFindMember(guid, out member);
    IReadOnlyList<IReadOnlyStructureMember> IReadOnlyDocument.FindMemberPath(Guid guid) => FindMemberPath(guid);
    IReadOnlyStructureMember IReadOnlyDocument.FindMemberOrThrow(Guid guid) => FindMemberOrThrow(guid);
    (IReadOnlyStructureMember, IReadOnlyFolder) IReadOnlyDocument.FindChildAndParentOrThrow(Guid guid) => FindChildAndParentOrThrow(guid);

    IReadOnlyReferenceLayer? IReadOnlyDocument.ReferenceLayer => ReferenceLayer;

    /// <summary>
    /// The default size for a new document
    /// </summary>
    public static VecI DefaultSize { get; } = new VecI(64, 64);
    internal Folder StructureRoot { get; } = new() { GuidValue = Guid.Empty };
    internal Selection Selection { get; } = new();
    internal ReferenceLayer? ReferenceLayer { get; set; }
    internal AnimationData AnimationData { get; }
    public VecI Size { get; set; } = DefaultSize;
    public bool HorizontalSymmetryAxisEnabled { get; set; }
    public bool VerticalSymmetryAxisEnabled { get; set; }
    public double HorizontalSymmetryAxisY { get; set; }
    public double VerticalSymmetryAxisX { get; set; }
    
    public Document()
    {
        AnimationData = new AnimationData(this);
    }

    public void Dispose()
    {
        StructureRoot.Dispose();
        Selection.Dispose();
    }
    
    /// <summary>
    ///     Creates a surface for layer image.
    /// </summary>
    /// <param name="layerGuid">Guid of the layer inside structure.</param>
    /// <returns>Surface if the layer has some drawn pixels, null if the image is empty.</returns>
    /// <exception cref="ArgumentException">Exception when guid is not found inside structure or if it's not a layer</exception>
    /// <remarks>So yeah, welcome folks to the multithreaded world, where possibilities are endless! (and chances of objects getting
    /// edited, in between of processing you want to make exist). You might encounter ObjectDisposedException and other mighty creatures here if
    /// you are lucky enough. Have fun!</remarks>
    public Surface? GetLayerRasterizedImage(Guid layerGuid, int frame)
    {
        var layer = (IReadOnlyLayer?)FindMember(layerGuid);

        if (layer is null)
            throw new ArgumentException(@"The given guid does not belong to a layer.", nameof(layerGuid));


        RectI? tightBounds = layer.GetTightBounds();

        if (tightBounds is null)
            return null;

        tightBounds = tightBounds.Value.Intersect(RectI.Create(0, 0, Size.X, Size.Y));

        Surface surface = new Surface(tightBounds.Value.Size);

        layer.Rasterize(frame).DrawMostUpToDateRegionOn(
            tightBounds.Value,
            ChunkResolution.Full,
            surface.DrawingSurface, VecI.Zero);

        return surface;
    }

    public RectI? GetChunkAlignedLayerBounds(Guid layerGuid)
    {
        var layer = (IReadOnlyLayer?)FindMember(layerGuid);

        if (layer is null)
            throw new ArgumentException(@"The given guid does not belong to a layer.", nameof(layerGuid));


        return layer.GetTightBounds();
    }
    
    public void ForEveryReadonlyMember(Action<IReadOnlyStructureMember> action) => ForEveryReadonlyMember(StructureRoot, action);

    /// <summary>
    /// Performs the specified action on each member of the document
    /// </summary>
    public void ForEveryMember(Action<StructureMember> action) => ForEveryMember(StructureRoot, action);

    private void ForEveryReadonlyMember(IReadOnlyFolder folder, Action<IReadOnlyStructureMember> action)
    {
        foreach (var child in folder.Children)
        {
            action(child);
            if (child is IReadOnlyFolder innerFolder)
                ForEveryReadonlyMember(innerFolder, action);
        }
    }

    private void ForEveryMember(Folder folder, Action<StructureMember> action)
    {
        foreach (var child in folder.Children)
        {
            action(child);
            if (child is Folder innerFolder)
                ForEveryMember(innerFolder, action);
        }
    }

    /// <summary>
    /// Checks if a member with the <paramref name="guid"/> exists
    /// </summary>
    /// <param name="guid">The <see cref="StructureMember.GuidValue"/> of the member</param>
    /// <returns>True if the member can be found, otherwise false</returns>
    public bool HasMember(Guid guid)
    {
        var list = FindMemberPath(guid);
        return list.Count > 0;
    }

    /// <summary>
    /// Checks if a member with the <paramref name="guid"/> exists and is of type <typeparamref name="T"/>
    /// </summary>
    /// <param name="guid">The <see cref="StructureMember.GuidValue"/> of the member</param>
    /// <returns>True if the member can be found and is of type <typeparamref name="T"/>, otherwise false</returns>
    public bool HasMember<T>(Guid guid) 
        where T : StructureMember
    {
        var list = FindMemberPath(guid);
        return list.Count > 0 && list[0] is T;
    }
    
    /// <summary>
    /// Finds the member with the <paramref name="guid"/> or throws a ArgumentException if not found
    /// </summary>
    /// <param name="guid">The <see cref="StructureMember.GuidValue"/> of the member</param>
    /// <exception cref="ArgumentException">Thrown if the member could not be found</exception>
    public StructureMember FindMemberOrThrow(Guid guid) => FindMember(guid) ?? throw new ArgumentException($"Could not find member with guid '{guid}'");

    /// <summary>
    /// Finds the member of type <typeparamref name="T"/> with the <paramref name="guid"/> or throws an exception
    /// </summary>
    /// <param name="guid">The <see cref="StructureMember.GuidValue"/> of the member</param>
    /// <exception cref="ArgumentException">Thrown if the member could not be found</exception>
    /// <exception cref="InvalidCastException">Thrown if the member is not of type <typeparamref name="T"/></exception>
    public T FindMemberOrThrow<T>(Guid guid) where T : StructureMember => (T?)FindMember(guid) ?? throw new ArgumentException($"Could not find member with guid '{guid}'");

    /// <summary>
    /// Finds the member with the <paramref name="guid"/> or returns null if not found
    /// </summary>
    /// <param name="guid">The <see cref="StructureMember.GuidValue"/> of the member</param>
    public StructureMember? FindMember(Guid guid)
    {
        var list = FindMemberPath(guid);
        return list.Count > 0 ? list[0] : null;
    }

    /// <summary>
    /// Tries finding the member with the <paramref name="guid"/> and returns true if it was found
    /// </summary>
    /// <param name="guid">The <see cref="StructureMember.GuidValue"/> of the <paramref name="member"/></param>
    /// <param name="member">The member</param>
    /// <returns>True if the member could be found, otherwise false</returns>
    public bool TryFindMember(Guid guid, [NotNullWhen(true)] out StructureMember? member)
    {
        var list = FindMemberPath(guid);
        if (list.Count == 0)
        {
            member = null;
            return false;
        }

        member = list[0];
        return true;
    }

    /// <summary>
    /// Tries finding the member with the <paramref name="guid"/> of type <typeparamref name="T"/> and returns true if it was found
    /// </summary>
    /// <param name="guid">The <see cref="StructureMember.GuidValue"/> of the <paramref name="member"/></param>
    /// <param name="member">The member</param>
    /// <typeparam name="T">The type of the <see cref="StructureMember"/></typeparam>
    /// <returns>True if the member could be found and is of type <typeparamref name="T"/>, otherwise false</returns>
    public bool TryFindMember<T>(Guid guid, [NotNullWhen(true)] out T? member) 
        where T : IReadOnlyStructureMember
    {
        if (!TryFindMember(guid, out var structureMember) || structureMember is not T cast)
        {
            member = default;
            return false;
        }

        member = cast;
        return true;
    }

    /// <summary>
    /// Finds a member with the <paramref name="childGuid"/>  and its parent, throws a ArgumentException if they can't be found
    /// </summary>
    /// <param name="childGuid">The <see cref="StructureMember.GuidValue"/> of the member</param>
    /// <returns>A value tuple consisting of child (<see cref="ValueTuple{T, T}.Item1"/>) and parent (<see cref="ValueTuple{T, T}.Item2"/>)</returns>
    /// <exception cref="ArgumentException">Thrown if the member and parent could not be found</exception>
    public (StructureMember, Folder) FindChildAndParentOrThrow(Guid childGuid)
    {
        var path = FindMemberPath(childGuid);
        if (path.Count < 2)
            throw new ArgumentException("Couldn't find child and parent");
        return (path[0], (Folder)path[1]);
    }

    /// <summary>
    /// Finds a member with the <paramref name="childGuid"/> and its parent
    /// </summary>
    /// <param name="childGuid">The <see cref="StructureMember.GuidValue"/> of the member</param>
    /// <returns>A value tuple consisting of child (<see cref="ValueTuple{T, T}.Item1"/>) and parent (<see cref="ValueTuple{T, T}.Item2"/>)<para>Child and parent can be null if not found!</para></returns>
    public (StructureMember?, Folder?) FindChildAndParent(Guid childGuid)
    {
        var path = FindMemberPath(childGuid);
        return path.Count switch
        {
            1 => (path[0], null),
            > 1 => (path[0], (Folder)path[1]),
            _ => (null, null),
        };
    }

    /// <summary>
    /// Finds the path to the member with <paramref name="guid"/>, the first element will be the member
    /// </summary>
    /// <param name="guid">The <see cref="StructureMember.GuidValue"/> of the member</param>
    public List<StructureMember> FindMemberPath(Guid guid)
    {
        var list = new List<StructureMember>();
        if (FillMemberPath(StructureRoot, guid, list))
            list.Add(StructureRoot);
        return list;
    }

    private bool FillMemberPath(Folder folder, Guid guid, List<StructureMember> toFill)
    {
        if (folder.GuidValue == guid)
        {
            return true;
        }

        foreach (var member in folder.Children)
        {
            if (member is Layer childLayer && childLayer.GuidValue == guid)
            {
                toFill.Add(member);
                return true;
            }
            if (member is Folder childFolder)
            {
                if (FillMemberPath(childFolder, guid, toFill))
                {
                    toFill.Add(childFolder);
                    return true;
                }
            }
        }
        return false;
    }
    
    public List<Guid> ExtractLayers(IList<Guid> members)
    {
        var result = new List<Guid>();
        foreach (var member in members)
        {
            if (TryFindMember(member, out var structureMember))
            {
                if (structureMember is Layer layer && !result.Contains(layer.GuidValue))
                {
                    result.Add(layer.GuidValue);
                }
                else if (structureMember is Folder folder)
                {
                    ExtractLayers(folder, result);
                }
            }
        }
        return result;
    }

    private void ExtractLayers(Folder folder, List<Guid> list)
    {
        foreach (var member in folder.Children)
        {
            if (member is Layer layer && !list.Contains(layer.GuidValue))
            {
                list.Add(layer.GuidValue);
            }
            else if (member is Folder childFolder)
            {
                ExtractLayers(childFolder, list);
            }
        }
    }
}
