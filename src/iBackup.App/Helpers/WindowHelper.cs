using System;
using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;
using WinRT.Interop;

namespace iBackup.App.Helpers;

/// <summary>
/// Static helper for WinUI 3 window interop and sizing management.
/// </summary>
public static class WindowHelper
{
    private const int SwRestore = 9;
    private const int WmGetMinMaxInfo = 0x0024;
    private const uint SubclassId = 1001;

    private static SubclassProc? _subclassProcDelegate;

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MINMAXINFO
    {
        public POINT ptReserved;
        public POINT ptMaxSize;
        public POINT ptMaxPosition;
        public POINT ptMinTrackSize;
        public POINT ptMaxTrackSize;
    }

    private delegate nint SubclassProc(nint hWnd, uint uMsg, nint wParam, nint lParam, nuint uIdSubclass, nint dwRefData);

    [DllImport("comctl32.dll", SetLastError = true)]
    private static extern bool SetWindowSubclass(nint hWnd, SubclassProc pfnSubclass, nuint uIdSubclass, nint dwRefData);

    [DllImport("comctl32.dll", SetLastError = true)]
    private static extern nint DefSubclassProc(nint hWnd, uint uMsg, nint wParam, nint lParam);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(nint hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(nint hWnd);

    private static int _minWidth = 600;
    private static int _minHeight = 500;

    /// <summary>
    /// Gets the native window handle (HWND) for the specified WinUI 3 Window.
    /// </summary>
    /// <param name="window">The WinUI 3 window.</param>
    /// <returns>The HWND handle for interop.</returns>
    /// <exception cref="ArgumentNullException">Thrown if window is null.</exception>
    public static nint GetWindowHandle(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);
        return WindowNative.GetWindowHandle(window);
    }

    /// <summary>
    /// Gets the <see cref="AppWindow"/> for the specified WinUI 3 Window.
    /// </summary>
    /// <param name="window">The WinUI 3 window.</param>
    /// <returns>The associated AppWindow.</returns>
    public static AppWindow GetAppWindow(Window window)
    {
        var hwnd = GetWindowHandle(window);
        var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
        return AppWindow.GetFromWindowId(windowId);
    }

    /// <summary>
    /// Sets the minimum allowable window width and height by intercepting WM_GETMINMAXINFO.
    /// </summary>
    /// <param name="window">The WinUI 3 window.</param>
    /// <param name="minWidth">The minimum width in pixels.</param>
    /// <param name="minHeight">The minimum height in pixels.</param>
    public static void SetMinWindowSize(Window window, int minWidth, int minHeight)
    {
        _minWidth = minWidth;
        _minHeight = minHeight;

        var hwnd = GetWindowHandle(window);
        _subclassProcDelegate = SubclassWindowProc;
        SetWindowSubclass(hwnd, _subclassProcDelegate, SubclassId, 0);
    }

    /// <summary>
    /// Resizes the specified window to the given width and height.
    /// </summary>
    /// <param name="window">The WinUI 3 window.</param>
    /// <param name="width">The target width.</param>
    /// <param name="height">The target height.</param>
    public static void SetWindowSize(Window window, int width, int height)
    {
        var appWindow = GetAppWindow(window);
        appWindow.Resize(new SizeInt32(Math.Max(width, _minWidth), Math.Max(height, _minHeight)));
    }

    /// <summary>
    /// Shows and restores the window to the foreground (e.g. from system tray).
    /// </summary>
    /// <param name="window">The WinUI 3 window.</param>
    public static void ShowWindow(Window window)
    {
        var hwnd = GetWindowHandle(window);
        ShowWindow(hwnd, SwRestore);
        SetForegroundWindow(hwnd);
        window.Activate();
    }

    private static nint SubclassWindowProc(nint hWnd, uint uMsg, nint wParam, nint lParam, nuint uIdSubclass, nint dwRefData)
    {
        if (uMsg == WmGetMinMaxInfo)
        {
            var minMaxInfo = Marshal.PtrToStructure<MINMAXINFO>(lParam);
            minMaxInfo.ptMinTrackSize.X = _minWidth;
            minMaxInfo.ptMinTrackSize.Y = _minHeight;
            Marshal.StructureToPtr(minMaxInfo, lParam, true);
            return 0;
        }

        return DefSubclassProc(hWnd, uMsg, wParam, lParam);
    }
}
