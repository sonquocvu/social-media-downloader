using System;
using System.IO;

namespace SVVideoDownloader.Infrastructure.Processes;

internal static class ExternalExecutablePolicy
{
    private static readonly string[] BlockedShellNames =
    {
        "cmd",
        "command",
        "powershell",
        "powershell_ise",
        "pwsh",
    };

    public static bool IsBlockedShell(string executablePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(executablePath);
        foreach (var blockedName in BlockedShellNames)
        {
            if (string.Equals(fileName, blockedName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
