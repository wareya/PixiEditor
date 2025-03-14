﻿using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using PixiEditor.Extensions.Common.Localization;
using PixiEditor.Helpers;
using PixiEditor.Models.Dialogs;
using PixiEditor.Models.ExceptionHandling;
using PixiEditor.Views;
using PixiEditor.Views.Dialogs;

namespace PixiEditor.ViewModels;

internal partial class CrashReportViewModel : Window
{
    private bool hasRecoveredDocuments = true;

    public CrashReport CrashReport { get; }

    public string ReportText { get; }

    public int DocumentCount { get; }

    public bool IsDebugBuild { get; set; }

    public RelayCommand OpenSendCrashReportCommand { get; }

    public CrashReportViewModel(CrashReport report)
    {
        SetIsDebug();

        CrashReport = report;
        ReportText = report.ReportText;
        DocumentCount = report.GetDocumentCount();
        OpenSendCrashReportCommand = new RelayCommand(() => new SendCrashReportDialog(CrashReport).Show());

        if (!IsDebugBuild)
            _ = CrashHelper.SendReportTextToWebhookAsync(report);
        _ = CrashHelper.SendReportToAnalyticsApiAsync(report);
    }

    [RelayCommand(CanExecute = nameof(CanRecoverDocuments))]
    public async Task RecoverDocuments()
    {
        if (!hasRecoveredDocuments)
        {
            return;
        }

        MainWindow window = MainWindow.CreateWithRecoveredDocuments(CrashReport, out var showMissingFilesDialog);

        window.Loaded += (sender, args) =>
        {
            if (showMissingFilesDialog)
            {
                var dialog = new OptionsDialog<LocalizedString>(
                    "CRASH_NOT_ALL_DOCUMENTS_RECOVERED_TITLE",
                    new LocalizedString("CRASH_NOT_ALL_DOCUMENTS_RECOVERED"),
                    MainWindow.Current!)
                {
                    {
                        "SEND", _ =>
                        {
                            var sendReportDialog = new SendCrashReportDialog(CrashReport);
                            sendReportDialog.ShowDialog(window);
                        }
                    },
                    "CLOSE"
                };

                _ = dialog.ShowDialog(true);
            }
        };

        hasRecoveredDocuments = false;
        Application.Current.Run(window);
    }

    public bool CanRecoverDocuments()
    {
        return hasRecoveredDocuments;
    }

    public static void ShowMissingFilesDialog(CrashReport crashReport)
    {
        var dialog = new OptionsDialog<LocalizedString>(
            "CRASH_NOT_ALL_DOCUMENTS_RECOVERED_TITLE",
            new LocalizedString("CRASH_NOT_ALL_DOCUMENTS_RECOVERED"), MainWindow.Current)
        {
            {
                "SEND", _ =>
                {
                    var sendReportDialog = new SendCrashReportDialog(crashReport);
                    sendReportDialog.ShowDialog(MainWindow.Current);
                }
            },
            "CLOSE"
        };

        dialog.ShowDialog(true);
    }


    [Conditional("DEBUG")]
    private void SetIsDebug()
    {
        IsDebugBuild = true;
    }

    [RelayCommand]
    private void AttachDebugger()
    {
        if (!Debugger.Launch())
        {
            /*TODO: MessageBox.Show("Starting debugger failed", "Starting debugger failed", MessageBoxButton.OK, MessageBoxImage.Error);*/
        }
    }
}
