using System.Runtime.InteropServices;
using Veldrid.Sdl2;
namespace Vault
{
    public class HighResTimer
    {
        //use the high res timer provided by SDL
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate ulong SDL_GetPerformanceFrequency_t();
        private static SDL_GetPerformanceFrequency_t s_sdl_getPerformanceFrequency_t = 
            Sdl2Native.LoadFunction<SDL_GetPerformanceFrequency_t>("SDL_GetPerformanceFrequency");
    
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate ulong SDL_GetPerformanceCounter_t();
        private static SDL_GetPerformanceCounter_t s_sdl_getPerformanceCounter_t = 
            Sdl2Native.LoadFunction<SDL_GetPerformanceCounter_t>("SDL_GetPerformanceCounter");

        //Returns number of ticks since timer was created
        public ulong Sample => s_sdl_getPerformanceCounter_t();

        //Returns number of ticks per second of the counter
        public ulong SampleFrequency => s_sdl_getPerformanceFrequency_t();
    }
}