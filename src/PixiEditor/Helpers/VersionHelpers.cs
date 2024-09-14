﻿using System.Reflection;
using System.Text;

namespace PixiEditor.Helpers;

internal static class VersionHelpers
{
    public static Version GetCurrentAssemblyVersion() => Assembly.GetExecutingAssembly().GetName().Version;

    public static string GetCurrentAssemblyVersion(Func<Version, string> toString) => toString(GetCurrentAssemblyVersion());

    public static string GetCurrentAssemblyVersionString(bool moreSpecific = false)
    {
        StringBuilder builder = new($"{GetCurrentAssemblyVersion().ToString()} Closed Beta");

        // TODO: Dev Build removed for closed beta
#if MSIX_DEBUG
        builder.Append(" MSIX Debug Build");
        return builder.ToString();
#elif DEBUG
        builder.Append(" Debug Build");
        return builder.ToString();
#endif

        if (!moreSpecific)
            return builder.ToString();

#if STEAM
        builder.Append(" Steam Build");
#elif MSIX
        builder.Append(" MSIX Build");
#elif RELEASE
        builder.Append(" Release Build");
#endif
        return builder.ToString();
    }

    // BuildId should not contain spaces
    public static string GetBuildId()
    {
#if DEBUG
        return "ClosedBetaDebug";
#elif DEVRELEASE
        return "ClosedBetaDevRelease";
#elif RELEASE
        return "ClosedBetaRelease";
#elif STEAM
        return "ClosedBetaSteam";
#elif MSIX
        return "ClosedBetaMSIX";
#else
        #error No build name configured for this configuration
#endif
    }
}
