using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace SVVideoDownloader.App;

internal static class NativeWindowTheme
{
    private const int UseImmersiveDarkMode = 20;

    public static void Apply(Window window, bool useDarkMode)
    {
        ArgumentNullException.ThrowIfNull(window);
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
        {
            return;
        }

        var handle = new WindowInteropHelper(window).Handle;
        if (handle == nint.Zero)
        {
            return;
        }

        var enabled = useDarkMode ? 1 : 0;
        _ = DwmSetWindowAttribute(
            handle,
            UseImmersiveDarkMode,
            ref enabled,
            sizeof(int));
    }

    [DllImport("dwmapi.dll", ExactSpelling = true)]
    private static extern int DwmSetWindowAttribute(
        nint windowHandle,
        int attribute,
        ref int attributeValue,
        int attributeSize);
}
