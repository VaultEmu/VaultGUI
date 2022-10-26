using System.Runtime.InteropServices;
using Veldrid.Sdl2;
namespace Vault
{
    public class HighResolutionTimer
    {
        //use the high res timer provided by SDL
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate ulong SdlGetPerformanceFrequencyT();
        private static readonly SdlGetPerformanceFrequencyT _sdlGetPerformanceFrequencyT = 
            Sdl2Native.LoadFunction<SdlGetPerformanceFrequencyT>("SDL_GetPerformanceFrequency");
    
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate ulong SdlGetPerformanceCounterT();
        private static readonly SdlGetPerformanceCounterT _sdlGetPerformanceCounterT = 
            Sdl2Native.LoadFunction<SdlGetPerformanceCounterT>("SDL_GetPerformanceCounter");

        //Returns number of ticks since timer was created
        public ulong Sample => _sdlGetPerformanceCounterT();

        //Returns number of ticks per second of the counter
        public ulong SampleFrequency => _sdlGetPerformanceFrequencyT();
    }
}