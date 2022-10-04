using System.Runtime.InteropServices;
using Veldrid.Sdl2;

namespace Vault;

public static class DpiAwareUtils
{
    private enum MonitorDpiType
    {
        MDT_EFFECTIVE_DPI = 0,
        MDT_ANGULAR_DPI = 1,
        MDT_RAW_DPI = 2,
    }
    
    private enum MonitorFromWindowFlags
    {
        MONITOR_DEFAULTTONULL = 0,
        MONITOR_DEFAULTTOPRIMARY = 1, 
        MONITOR_DEFAULTTONEAREST = 2,
    }

    [DllImport("SHCore.dll", SetLastError = true)]
    private static extern uint GetDpiForMonitor(IntPtr hmonitor, MonitorDpiType dpiType, out uint dpiX, out uint dpiY);
    
    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);
    
    [DllImport("user32.dll")]
    public static extern bool SetProcessDPIAware();
    
    public static float GetDPIScale(Sdl2Window window)
    {
        var nativeHandle = window.Handle;
        var monitor = MonitorFromWindow(nativeHandle, (uint)MonitorFromWindowFlags.MONITOR_DEFAULTTONEAREST);
        
        GetDpiForMonitor(monitor, MonitorDpiType.MDT_EFFECTIVE_DPI, out var dpiX, out _);
        
        return dpiX / 96.0f;
    }
}