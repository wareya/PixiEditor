﻿using Avalonia.Platform.Storage;
using PixiEditor.AvaloniaUI.Models.Files;
using PixiEditor.AvaloniaUI.Models.IO;

namespace PixiEditor.AvaloniaUI.Helpers;

internal class SupportedFilesHelper
{
    public static string[] AllSupportedExtensions { get; private set; }
    public static string[] PrimaryExtensions { get; private set; }
    
    public static List<IoFileType> FileTypes { get; private set; }
    
    public static void InitFileTypes(IEnumerable<IoFileType> fileTypes)
    {
        FileTypes = fileTypes.ToList();

        AllSupportedExtensions = FileTypes.SelectMany(i => i.Extensions).ToArray();
        PrimaryExtensions = FileTypes.Select(i => i.PrimaryExtension).ToArray();
    }

    public static string FixFileExtension(string pathWithOrWithoutExtension, IoFileType requestedType)
    {
        if (requestedType == null)
            throw new ArgumentException("A valid filetype is required", nameof(requestedType));

        var typeFromPath = ParseImageFormat(Path.GetExtension(pathWithOrWithoutExtension));
        if (typeFromPath != null && typeFromPath == requestedType)
            return pathWithOrWithoutExtension;
        return AppendExtension(pathWithOrWithoutExtension, requestedType);
    }

    public static string AppendExtension(string path, IoFileType data)
    {
        string ext = data.Extensions.First();
        string filename = Path.GetFileName(path);
        if (filename.Length + ext.Length > 255)
            filename = filename.Substring(0, 255 - ext.Length);
        filename += ext;
        return Path.Combine(Path.GetDirectoryName(path), filename);
    }

    public static bool IsSupported(string path)
    {
        var ext = Path.GetExtension(path.ToLower());
        if (string.IsNullOrEmpty(ext))
        {
            ext = $".{path.ToLower()}";
        }

        return IsExtensionSupported(ext);
    }

    public static bool IsExtensionSupported(string fileExtension)
    {
        return AllSupportedExtensions.Contains(fileExtension);
    }
    public static IoFileType ParseImageFormat(string extension)
    {
        var allExts = FileTypes;
        var fileData = allExts.SingleOrDefault(i => i.Extensions.Contains(extension));
        return fileData;
    }

    public static List<IoFileType> GetAllSupportedFileTypes(FileTypeDialogDataSet.SetKind setKind)
    {
        var allExts = FileTypes.Where(x => x.SetKind.HasFlag(setKind)).ToList();
        return allExts;
    }

    public static List<FilePickerFileType> BuildSaveFilter(FileTypeDialogDataSet.SetKind setKind = FileTypeDialogDataSet.SetKind.Any)
    {
        var allSupportedExtensions = GetAllSupportedFileTypes(setKind);
        var filter = allSupportedExtensions.Select(i => i.SaveFilter).ToList();

        return filter;
    }

    public static IoFileType GetSaveFileType(FileTypeDialogDataSet.SetKind setKind, IStorageFile file)
    {
        var allSupportedExtensions = GetAllSupportedFileTypes(setKind);

        if (file is null)
            return null;

        string extension = Path.GetExtension(file.Path.LocalPath);
        return allSupportedExtensions.Single(i => i.Extensions.Contains(extension));
    }

    public static List<FilePickerFileType> BuildOpenFilter()
    {
        var any = new FileTypeDialogDataSet(FileTypeDialogDataSet.SetKind.Any).GetFormattedTypes(true);
        return any.ToList();
    }
}
