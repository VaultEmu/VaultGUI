using System.Runtime.InteropServices;
using SharpDX.Text;
using Veldrid.Sdl2;
using Encoding = System.Text.Encoding;

namespace Vault;

public static class SDLExtensions
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int SdlGetWindowDisplayIndexT(SDL_Window SDL2Window);
    private static readonly SdlGetWindowDisplayIndexT _sdlGetWindowDisplayIndexT = 
        Sdl2Native.LoadFunction<SdlGetWindowDisplayIndexT>("SDL_GetWindowDisplayIndex");
    
    public static int SDL_GetWindowDisplayIndex(Sdl2Window window) => _sdlGetWindowDisplayIndexT(window.SdlWindowHandle);
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int SdlGetNumDisplayModesT(int displayIndex);
    private static readonly SdlGetNumDisplayModesT _sdlGetNumDisplayModesT = 
        Sdl2Native.LoadFunction<SdlGetNumDisplayModesT>("SDL_GetNumDisplayModes");
    
    public static int SDL_GetNumDisplayModes(int displayIndex) => _sdlGetNumDisplayModesT(displayIndex);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private unsafe delegate int SdlSetWindowDisplayModeT(SDL_Window SDL2Window, SDL_DisplayMode* displayMode);
    private static readonly SdlSetWindowDisplayModeT _sdlSetWindowDisplayModeT = 
        Sdl2Native.LoadFunction<SdlSetWindowDisplayModeT>("SDL_SetWindowDisplayMode");
    
    public static unsafe int SDL_SetWindowDisplayMode(Sdl2Window window, SDL_DisplayMode* displayMode)
        => _sdlSetWindowDisplayModeT(window.SdlWindowHandle, displayMode);
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private unsafe delegate int SdlGetCurrentDisplayModeT(int index, SDL_DisplayMode* displayMode);
    private static readonly SdlGetCurrentDisplayModeT _sdlGetCurrentDisplayModeT = 
        Sdl2Native.LoadFunction<SdlGetCurrentDisplayModeT>("SDL_GetCurrentDisplayMode");
    
    public static unsafe int SDL_GetCurrentDisplayMode(int index, SDL_DisplayMode* displayMode)
        => _sdlGetCurrentDisplayModeT(index, displayMode);
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private unsafe delegate int SdlGetDisplayModeT(int index, int modeIndex, SDL_DisplayMode* displayMode);
    private static readonly SdlGetDisplayModeT _sdlGetDisplayModeT = 
        Sdl2Native.LoadFunction<SdlGetDisplayModeT>("SDL_GetDisplayMode");
    
    public static unsafe int SDL_GetDisplayMode(int index, int modeIndex, SDL_DisplayMode* displayMode)
        => _sdlGetDisplayModeT(index, modeIndex, displayMode);

    
}