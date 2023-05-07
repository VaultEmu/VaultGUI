using System.Text;
using Vault.Input.Gamepad;
using Veldrid.Sdl2;

namespace Vault;

public class SDLGamepad : GamepadDevice, IDisposable
{
    private readonly SDL_GameController _sdlGameControllerHandle;
    public int SDLDeviceInstanceID { get; private set; }
    
    public SDLGamepad(int sdlDeviceIndex) : base($"Gamepad {sdlDeviceIndex}")
    {
        unsafe
        {
            _sdlGameControllerHandle = Sdl2Native.SDL_GameControllerOpen(sdlDeviceIndex);
            var joystick = Sdl2Native.SDL_GameControllerGetJoystick(_sdlGameControllerHandle);
            SDLDeviceInstanceID = Sdl2Native.SDL_JoystickInstanceID(joystick);
        
            var nameBytes = Sdl2Native.SDL_GameControllerName(_sdlGameControllerHandle);
            
            if ((IntPtr) nameBytes != IntPtr.Zero)
            {
                var byteCount = 0;
                while (nameBytes[byteCount] != 0)
                    ++byteCount;
                DeviceName = Encoding.UTF8.GetString(nameBytes, byteCount);
            }
        }
    }

    public void Dispose()
    {
        Sdl2Native.SDL_GameControllerClose(_sdlGameControllerHandle);
    }
}