﻿using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using Avalonia.Media.Imaging;
using ChunkyImageLib;
using CommunityToolkit.Mvvm.ComponentModel;
using PixiEditor.AvaloniaUI.Exceptions;
using PixiEditor.AvaloniaUI.Helpers;
using PixiEditor.AvaloniaUI.Helpers.Extensions;
using PixiEditor.AvaloniaUI.ViewModels.Document;
using PixiEditor.DrawingApi.Core;
using PixiEditor.DrawingApi.Core.Numerics;
using PixiEditor.DrawingApi.Core.Surfaces;
using PixiEditor.DrawingApi.Core.Surfaces.ImageData;
using PixiEditor.DrawingApi.Core.Surfaces.PaintImpl;
using PixiEditor.Extensions.Common.Localization;
using PixiEditor.Extensions.Exceptions;
using PixiEditor.Numerics;
using PixiEditor.Parser;
using PixiEditor.Parser.Deprecated;
using Bitmap = Avalonia.Media.Imaging.Bitmap;
using BlendMode = PixiEditor.DrawingApi.Core.Surfaces.BlendMode;

namespace PixiEditor.AvaloniaUI.Models.IO;

internal class Importer : ObservableObject
{
    /// <summary>
    ///     Imports image from path and resizes it to given dimensions.
    /// </summary>
    /// <param name="path">Path of the image.</param>
    /// <param name="size">New size of the image.</param>
    /// <returns>WriteableBitmap of imported image.</returns>
    public static Surface? ImportImage(string path, VecI size)
    {
        if (!Path.Exists(path)) 
            throw new MissingFileException();
        
        Surface original;
        try
        {
            original = Surface.Load(path);
        }
        catch (Exception e) when (e is ArgumentException or FileNotFoundException)
        {
            throw new CorruptedFileException(e);
        }
        
        if (original.Size == size || size == VecI.NegativeOne)
        {
            return original;
        }

        Surface resized = original.ResizeNearestNeighbor(size);
        original.Dispose();
        return resized;
    }

    public static Bitmap ImportBitmap(string path)
    {
        try
        {
            return new Bitmap(path);
        }
        catch (NotSupportedException e)
        {
            throw new InvalidFileTypeException(new LocalizedString("FILE_EXTENSION_NOT_SUPPORTED", Path.GetExtension(path)), e);
        }
        /*catch (FileFormatException e) TODO: Not found in Avalonia
        {
            throw new CorruptedFileException("FAILED_TO_OPEN_FILE", e);
        }*/
        catch (Exception e)
        {
            throw new RecoverableException("ERROR_IMPORTING_IMAGE", e);
        }
    }

    public static WriteableBitmap ImportWriteableBitmap(string path)
    {
        return ImportBitmap(path).ToWriteableBitmap();
    }

    public static DocumentViewModel ImportDocument(string path, bool associatePath = true)
    {
        try
        {
            using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
            var pixiDocument = PixiParser.DeserializeUsingCompatible(fileStream);

            var document = pixiDocument switch
            {
                Document v5 => v5.ToDocument(),
                DeprecatedDocument v4 => v4.ToDocument()
            };

            if (associatePath)
            {
                document.FullFilePath = path;
            }

            return document;
        }
        catch (DirectoryNotFoundException)
        {
            //TODO: Handle
            throw new RecoverableException();
        }
        catch (InvalidFileException e)
        {
            throw new CorruptedFileException("FAILED_TO_OPEN_FILE", e);
        }
        catch (OldFileFormatException e)
        {
            throw new CorruptedFileException("FAILED_TO_OPEN_FILE", e);
        }
    }

    public static DocumentViewModel ImportDocument(byte[] file, string? originalFilePath)
    {
        try
        {
            if (!PixiParser.TryGetCompatibleVersion(file, out var parser))
            {
                // TODO: Handle
                throw new RecoverableException();
            }
            
            var pixiDocument = parser.Deserialize(file);

            var document = pixiDocument switch
            {
                Document v5 => v5.ToDocument(),
                DeprecatedDocument v4 => v4.ToDocument()
            };

            document.FullFilePath = originalFilePath;

            return document;
        }
        catch (InvalidFileException e)
        {
            throw new CorruptedFileException("FAILED_TO_OPEN_FILE", e);
        }
        catch (OldFileFormatException e)
        {
            throw new CorruptedFileException("FAILED_TO_OPEN_FILE", e);
        }
    }

    public static Surface GetPreviewBitmap(string path)
    {
        if (!IsSupportedFile(path))
        {
            throw new InvalidFileTypeException(new LocalizedString("FILE_EXTENSION_NOT_SUPPORTED", Path.GetExtension(path)));
        }
        
        if (Path.GetExtension(path) != ".pixi")
            return Surface.Load(path);

        using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);

        return Surface.Load(PixiParser.ReadPreview(fileStream));
    }

    public static bool IsSupportedFile(string path)
    {
        return SupportedFilesHelper.IsSupported(path);
    }

    public static Surface LoadFromGZippedBytes(string path)
    {
        using FileStream compressedData = new(path, FileMode.Open);
        using GZipStream uncompressedData = new(compressedData, CompressionMode.Decompress);
        using MemoryStream resultBytes = new();
        uncompressedData.CopyTo(resultBytes);

        byte[] bytes = resultBytes.ToArray();
        int width = BitConverter.ToInt32(bytes, 0);
        int height = BitConverter.ToInt32(bytes, 4);

        ImageInfo info = new ImageInfo(width, height, ColorType.RgbaF16);
        IntPtr ptr = Marshal.AllocHGlobal(bytes.Length - 8);
        try
        {
            Marshal.Copy(bytes, 8, ptr, bytes.Length - 8);
            Pixmap map = new(info, ptr);
            DrawingSurface surface = DrawingSurface.Create(map);
            Surface finalSurface = new Surface(new VecI(width, height));
            using Paint paint = new() { BlendMode = BlendMode.Src };
            surface.Draw(finalSurface.DrawingSurface.Canvas, 0, 0, paint);
            return finalSurface;
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
    }
}
