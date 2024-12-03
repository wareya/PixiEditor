﻿using System.Diagnostics;
using System.Text;
using PixiEditor.UpdateInstaller.ViewModels;

UpdateController controller = new UpdateController();
StringBuilder log = new StringBuilder();

try
{
    log.AppendLine($"{DateTime.Now}: Starting update installation...");
    controller.InstallUpdate(log);
}
catch (Exception ex)
{
    log.AppendLine($"{DateTime.Now}: Error during update installation: {ex.Message}");
    File.AppendAllText("ErrorLog.txt",
        $"Error PixiEditor.UpdateInstaller: {DateTime.Now}\n{ex.Message}\n{ex.StackTrace}\n-----\n");
}
finally
{
    try
    {
        File.WriteAllText("UpdateLog.txt", log.ToString());
    }
    catch
    {
       // probably permissions or disk full, the best we can do is to ignore this 
    }
    
    var files = Directory.GetFiles(controller.UpdateDirectory, "PixiEditor.exe");
    if (files.Length > 0)
    {
        string pixiEditorExecutablePath = files[0];
        Process.Start(pixiEditorExecutablePath);
    }
}
