﻿// using PixiEditor.Helpers;
// using PixiEditor.Models.DocumentModels;
// using PixiEditor.Models.Handlers;
// using PixiEditor.ViewModels.Document.Nodes;
//
// namespace PixiEditor.ViewModels.Document;
//
// internal class FolderHandlerFactory : IFolderHandlerFactory
// {
//     public DocumentViewModel Document { get; }
//     IDocument IFolderHandlerFactory.Document => Document;
//
//     public FolderHandlerFactory(DocumentViewModel document)
//     {
//         Document = document;
//     }
//
//     public IFolderHandler CreateFolderHandler(DocumentInternalParts helper, Guid infoGuidValue)
//     {
//         return new FolderNodeViewModel(Document, helper, infoGuidValue);
//     }
// }
