using System.Text;
using Veldrid.Sdl2;

namespace Vault;

public static class SDLUtils
{
    public static unsafe string? GetErrorStringIfSet()
    {
        byte* error = Sdl2Native.SDL_GetError();
        if ((IntPtr) error != IntPtr.Zero)
        {
            var byteCount = 0;
            while (error[byteCount] != 0)
                ++byteCount;
            return Encoding.UTF8.GetString(error, byteCount);
        }
        
        return null;
    }
}