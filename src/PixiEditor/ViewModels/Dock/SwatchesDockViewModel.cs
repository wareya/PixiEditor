﻿using Avalonia;
using PixiEditor.Helpers.Converters;
using PixiEditor.Extensions.Common.Localization;
using PixiEditor.UI.Common.Fonts;
using PixiEditor.ViewModels.Document;

namespace PixiEditor.ViewModels.Dock;

internal class SwatchesDockViewModel : DockableViewModel
{
    public override string Id => "Swatches";
    public override string Title => new LocalizedString("SWATCHES_TITLE");
    public override bool CanFloat => true;
    public override bool CanClose => true;

    private DocumentManagerViewModel documentManagerSubViewModel;

    public DocumentManagerViewModel DocumentManagerSubViewModel
    {
        get => documentManagerSubViewModel;
        set => SetProperty(ref documentManagerSubViewModel, value);
    }

    public SwatchesDockViewModel(DocumentManagerViewModel documentManagerViewModel)
    {
        DocumentManagerSubViewModel = documentManagerViewModel;
        TabCustomizationSettings.Icon = PixiPerfectIcons.ToIcon(PixiPerfectIcons.Swatches);
    }
}
