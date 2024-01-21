﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using PixiEditor.AvaloniaUI.Helpers;
using PixiEditor.AvaloniaUI.Models.IO;
using PixiEditor.Extensions;
using PixiEditor.Extensions.Metadata;
using PixiEditor.Platform;

namespace PixiEditor.AvaloniaUI.Models.AppExtensions;

internal class ExtensionLoader
{
    private readonly Dictionary<string, OfficialExtensionData> _officialExtensionsKeys = new Dictionary<string, OfficialExtensionData>();
    public List<Extension> LoadedExtensions { get; } = new();

    public ExtensionLoader()
    {
        ValidateExtensionFolder();
        _officialExtensionsKeys.Add("pixieditor.supporterpack", new OfficialExtensionData("supporter-pack.snk", AdditionalContentProduct.SupporterPack));
    }

    public void LoadExtensions()
    {
        var directories = Directory.GetDirectories(Paths.ExtensionsFullPath);
        foreach (var directory in directories)
        {
            string packageJsonPath = Path.Combine(directory, "extension.json");
            bool isExtension = File.Exists(packageJsonPath);
            if (isExtension)
            {
                LoadExtension(packageJsonPath);
            }
        }
    }

    // Uncomment when PixiEditor.Core extension concept is implemented
    /*private void LoadCore()
    {
        Type entry = typeof(PixiEditorCoreExtension);
        Assembly assembly = entry.Assembly;
        var serializer = new JsonSerializer();

        Uri uri = new Uri("avares://PixiEditor.Core/extension.json");

        if (!AssetLoader.Exists(uri))
        {
            throw new FileNotFoundException("Core metadata not found", uri.ToString());
        }

        using var sr = new StreamReader(AssetLoader.Open(uri));
        using var jsonTextReader = new JsonTextReader(sr);
        ExtensionMetadata? metadata = serializer.Deserialize<ExtensionMetadata>(jsonTextReader);
        LoadExtensionFrom(assembly, entry, metadata);
    }*/

    public void InitializeExtensions(ExtensionServices pixiEditorApi)
    {
        try
        {
            foreach (var extension in LoadedExtensions)
            {
                extension.Initialize(pixiEditorApi);
            }
        }
        catch (Exception ex)
        {
            // TODO: Log exception
            // Maybe it's not a good idea to send webhook exceptions in the extension loader
            //CrashHelper.SendExceptionInfoToWebhook(ex);
        }
    }

    private void LoadExtension(string packageJsonPath)
    {
        string json = File.ReadAllText(packageJsonPath);
        try
        {
            var metadata = JsonConvert.DeserializeObject<ExtensionMetadata>(json);
            string directory = Path.GetDirectoryName(packageJsonPath);
            Assembly entry = GetEntryAssembly(directory, out Type extensionType);
            if (entry is null)
            {
                throw new NoEntryAssemblyException(directory);
            }

            if (!ValidateMetadata(metadata, entry))
            {
                return;
            }

            LoadExtensionFrom(entry, extensionType, metadata);
        }
        catch (JsonException)
        {
            //MessageBox.Show(new LocalizedString("ERROR_INVALID_PACKAGE", packageJsonPath), "ERROR");
        }
        catch (ExtensionException ex)
        {
            //MessageBox.Show(ex.DisplayMessage, "ERROR");
        }
        catch (Exception ex)
        {
            //MessageBox.Show(new LocalizedString("ERROR_LOADING_PACKAGE", packageJsonPath), "ERROR");
            CrashHelper.SendExceptionInfoToWebhook(ex);
        }
    }

    private void LoadExtensionFrom(Assembly entry, Type extensionType, ExtensionMetadata metadata)
    {
        var extension = LoadExtensionEntry(entry, extensionType, metadata);
        extension.Load();
        LoadedExtensions.Add(extension);
    }

    private Assembly? GetEntryAssembly(string assemblyFolder, out Type extensionType)
    {
        string[] dlls = Directory.GetFiles(assemblyFolder, "*.dll");
        Assembly? entryAssembly = GetEntryAssembly(dlls, out extensionType);

        return entryAssembly;
    }

    private bool ValidateMetadata(ExtensionMetadata metadata, Assembly assembly)
    {
        if (string.IsNullOrEmpty(metadata.UniqueName))
        {
            throw new MissingMetadataException("Description");
        }

        string fixedUniqueName = metadata.UniqueName.ToLower().Trim();

        if (fixedUniqueName.StartsWith("pixieditor".Trim(), StringComparison.OrdinalIgnoreCase))
        {
            if(!IsOfficialAssemblyLegit(fixedUniqueName, assembly))
            {
                throw new ForbiddenUniqueNameExtension();
            }

            if (!IsAdditionalContentInstalled(fixedUniqueName))
            {
                return false;
            }
        }
        // TODO: Validate if unique name is unique

        if (string.IsNullOrEmpty(metadata.DisplayName))
        {
            throw new MissingMetadataException("DisplayName");
        }

        if (string.IsNullOrEmpty(metadata.Version))
        {
            throw new MissingMetadataException("Version");
        }

        return true;
    }

    private bool IsAdditionalContentInstalled(string fixedUniqueName)
    {
        if (!_officialExtensionsKeys.ContainsKey(fixedUniqueName)) return false;
        AdditionalContentProduct? product = _officialExtensionsKeys[fixedUniqueName].Product;

        if (product == null) return true;

        return IPlatform.Current.AdditionalContentProvider?.IsContentInstalled(product.Value) ?? false;
    }

    private bool IsOfficialAssemblyLegit(string metadataUniqueName, Assembly assembly)
    {
        if (assembly == null) return false; // All official extensions must have a valid assembly
        if (!_officialExtensionsKeys.ContainsKey(metadataUniqueName)) return false;
        bool wasVerified = false;
        bool verified = StrongNameSignatureVerificationEx(assembly.Location, true, ref wasVerified);
        if (!verified || !wasVerified) return false;

        byte[]? assemblyPublicKey = assembly.GetName().GetPublicKey();
        if (assemblyPublicKey == null) return false;

        return PublicKeysMatch(assemblyPublicKey, _officialExtensionsKeys[metadataUniqueName].PublicKeyName);
    }

    private bool PublicKeysMatch(byte[] assemblyPublicKey, string pathToPublicKey)
    {
        Assembly currentAssembly = Assembly.GetExecutingAssembly();
        using Stream? stream = currentAssembly.GetManifestResourceStream($"{currentAssembly.GetName().Name}.OfficialExtensions.{pathToPublicKey}");
        if (stream == null) return false;

        using MemoryStream memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        byte[] publicKey = memoryStream.ToArray();

        return assemblyPublicKey.SequenceEqual(publicKey);
    }

    [DllImport("mscoree.dll", CharSet=CharSet.Unicode)]
    static extern bool StrongNameSignatureVerificationEx(string wszFilePath, bool fForceVerification, ref bool pfWasVerified);

    private Extension LoadExtensionEntry(Assembly entryAssembly, Type extensionType, ExtensionMetadata metadata)
    {
        var extension = (Extension)Activator.CreateInstance(extensionType);
        if (extension is null)
        {
            throw new NoClassEntryException(entryAssembly.Location);
        }

        extension.ProvideMetadata(metadata);
        return extension;
    }

    private Assembly? GetEntryAssembly(string[] dlls, out Type extensionType)
    {
        foreach (var dll in dlls)
        {
            try
            {
                var assembly = Assembly.LoadFrom(dll);
                extensionType = assembly.GetTypes().FirstOrDefault(x => x.IsSubclassOf(typeof(Extension)));
                if (extensionType is not null)
                {
                    return assembly;
                }
            }
            catch
            {
                // ignored
            }
        }

        extensionType = null;
        return null;
    }

    private void ValidateExtensionFolder()
    {
        if (!Directory.Exists(Paths.ExtensionsFullPath))
        {
            Directory.CreateDirectory(Paths.ExtensionsFullPath);
        }
    }

    public string? GetTypeId(Type id)
    {
        if (id.Assembly == Assembly.GetExecutingAssembly())
        {
            return $"PixiEditor.{id.Name}";
        }

        foreach (var extension in LoadedExtensions)
        {
            Type? foundType = extension.Assembly.GetTypes().FirstOrDefault(x => x == id);
            if (foundType != null)
            {
                return $"{extension.Metadata.UniqueName}:{foundType.Name}";
            }
        }

        return null;
    }
}

internal struct OfficialExtensionData
{
    public string PublicKeyName { get; }
    public AdditionalContentProduct? Product { get; }
    public string? PurchaseLink { get; }

    public OfficialExtensionData(string publicKeyName, AdditionalContentProduct product, string? purchaseLink = null)
    {
        PublicKeyName = publicKeyName;
        Product = product;
    }
}
