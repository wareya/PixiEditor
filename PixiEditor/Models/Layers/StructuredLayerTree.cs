﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using PixiEditor.Helpers;
using PixiEditor.Helpers.Extensions;

namespace PixiEditor.Models.Layers
{
    public class StructuredLayerTree : NotifyableObject
    {
        private List<Guid> layersInStructure = new();

        public ObservableCollection<object> RootDirectoryItems { get; } = new ObservableCollection<object>();

        public StructuredLayerTree(ObservableCollection<Layer> layers, LayerStructure structure)
        {
            if (structure.Folders == null || structure.Folders.Count == 0)
            {
                RootDirectoryItems.AddRange(layers);
                return;
            }

            var parsedFolders = ParseFolders(structure.Folders, layers);

            parsedFolders = parsedFolders.OrderBy(x => x.DisplayIndex).ToList();

            PlaceItems(parsedFolders, layers);

            layersInStructure.Clear();
        }

        private void PlaceItems(List<LayerFolder> parsedFolders, ObservableCollection<Layer> layers)
        {
            LayerFolder currentFolder = null;
            List<LayerFolder> foldersAtIndex = new ();
            Stack<LayerFolder> unfinishedFolders = new ();
            for (int i = 0; i < layers.Count; i++)
            {
                if (currentFolder != null && layers[i].LayerGuid == currentFolder.StructureData.EndLayerGuid)
                {
                    if (unfinishedFolders.Count > 0)
                    {
                        currentFolder = unfinishedFolders.Pop();
                    }
                    else
                    {
                        currentFolder = null;
                    }

                    continue;
                }

                if (parsedFolders.Any(x => x.StructureData.StartLayerGuid == layers[i].LayerGuid))
                {
                    foldersAtIndex = parsedFolders.Where(x => x.StructureData.StartLayerGuid == layers[i].LayerGuid).ToList();
                    for (int j = 0; j < foldersAtIndex.Count; j++)
                    {
                        LayerFolder folder = foldersAtIndex[j];

                        if (currentFolder != null)
                        {
                            unfinishedFolders.Push(currentFolder);
                        }

                        foldersAtIndex[j] = parsedFolders.First(x => x.StructureData.StartLayerGuid == layers[i].LayerGuid);
                        foldersAtIndex[j].DisplayIndex = RootDirectoryItems.Count;
                        foldersAtIndex[j].TopIndex = CalculateTopIndex(folder.DisplayIndex, folder.StructureData, layers);
                        currentFolder = foldersAtIndex[j];
                    }
                }

                if (currentFolder == null && !layersInStructure.Contains(layers[i].LayerGuid))
                {
                    RootDirectoryItems.Add(layers[i]);
                }
                else if (!RootDirectoryItems.Contains(currentFolder))
                {
                    RootDirectoryItems.AddRange(foldersAtIndex.Where(x => !RootDirectoryItems.Contains(x)));
                }
            }
        }

        private int CalculateTopIndex(int displayIndex, GuidStructureItem structureData, ObservableCollection<Layer> layers)
        {
            int originalTopIndex = layers.IndexOf(layers.First(x => x.LayerGuid == structureData.EndLayerGuid));
            int originalBottomIndex = layers.IndexOf(layers.First(x => x.LayerGuid == structureData.StartLayerGuid));

            return displayIndex + (originalTopIndex - originalBottomIndex);
        }

        private List<LayerFolder> ParseFolders(IEnumerable<GuidStructureItem> folders, ObservableCollection<Layer> layers)
        {
            List<LayerFolder> parsedFolders = new();
            foreach (var structureItem in folders)
            {
                parsedFolders.Add(ParseFolder(structureItem, layers));
            }

            return parsedFolders;
        }

        private LayerFolder ParseFolder(GuidStructureItem structureItem, ObservableCollection<Layer> layers)
        {
            List<Layer> structureItemLayers = new();

            Guid[] layersInFolder = GetLayersInFolder(layers, structureItem);

            var subFolders = new List<LayerFolder>();

            if (structureItem.Subfolders.Count > 0)
            {
                subFolders = ParseFolders(structureItem.Subfolders, layers);
            }

            foreach (var guid in layersInFolder)
            {
                var layer = layers.First(x => x.LayerGuid == guid);
                if (!layersInStructure.Contains(layer.LayerGuid))
                {
                    layersInStructure.Add(layer.LayerGuid);
                    structureItemLayers.Add(layer);
                }
            }

            int displayIndex = layersInFolder.Length > 0 ? layers.IndexOf(layers.First(x => x.LayerGuid == structureItem.StartLayerGuid)) : 0;

            structureItemLayers.Reverse();

            LayerFolder folder = new(structureItemLayers, subFolders, structureItem.Name,
                structureItem.FolderGuid, displayIndex, displayIndex + structureItemLayers.Count - 1, structureItem)
            {
                IsExpanded = structureItem.IsExpanded
            };
            return folder;
        }

        private Guid[] GetLayersInFolder(ObservableCollection<Layer> layers, GuidStructureItem structureItem)
        {
            if (structureItem.EndLayerGuid == null || structureItem.StartLayerGuid == null)
            {
                return Array.Empty<Guid>();
            }

            int startIndex = layers.IndexOf(layers.First(x => x.LayerGuid == structureItem.StartLayerGuid));
            int endIndex = layers.IndexOf(layers.First(x => x.LayerGuid == structureItem.EndLayerGuid));

            if (startIndex > endIndex)
            {
                Swap(ref startIndex, ref endIndex);
            }

            int len = endIndex - startIndex + 1;

            Guid[] guids = new Guid[len];

            for (int i = 0; i < len; i++)
            {
                guids[i] = layers[i + startIndex].LayerGuid;
            }

            return guids;
        }

        private static void Swap(ref int startIndex, ref int endIndex)
        {
            int tmp = startIndex;
            startIndex = endIndex;
            endIndex = tmp;
        }
    }
}