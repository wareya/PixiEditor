﻿namespace PixiEditor.OperatingSystem;

public interface IOperatingSystem
{
    public static IOperatingSystem Current { get; protected set; }
    public string Name { get; }

    public IInputKeys InputKeys { get; }

    protected static void SetCurrent(IOperatingSystem operatingSystem)
    {
        if (Current != null)
        {
            throw new InvalidOperationException("Current operating system is already set");
        }

        Current = operatingSystem;
    }
}
