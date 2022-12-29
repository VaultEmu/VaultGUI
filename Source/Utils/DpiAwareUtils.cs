using System.Runtime.InteropServices;
using Veldrid.Sdl2;

namespace Vault;

public static class DpiAwareUtils
{
    private const int MDT_EFFECTIVE_DPI = 0;            //Maps to MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI in win32 api
    private const int MONITOR_DEFAULT_TO_NEAREST = 2;   //Maps to MONITOR_DEFAULTTONEAREST in win32 api
    

    [DllImport("SHCore.dll", SetLastError = true)]
    private static extern uint GetDpiForMonitor(IntPtr hmonitor, int dpiType, out uint dpiX, out uint dpiY);
    
    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);
    
    [DllImport("user32.dll")]
    public static extern bool SetProcessDPIAware();
    
    public static float GetDPIScale(Sdl2Window window)
    {
        var nativeHandle = window.Handle;
        var monitor = MonitorFromWindow(nativeHandle, MONITOR_DEFAULT_TO_NEAREST);
        
        GetDpiForMonitor(monitor, MDT_EFFECTIVE_DPI, out var dpiX, out _);
        
        return dpiX / 96.0f;
    }
}