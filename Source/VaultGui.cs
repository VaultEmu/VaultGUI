using Vault.UI;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

namespace Vault;

public static class VaultGui
{
    private static Sdl2Window _window = null!;
    private static GraphicsDevice _graphicsDevice = null!;
    private static UIController _uiController = null!;
    
    private static CommandList _mainCommandList = null!;
    private static HighResTimer _highResTimer = null!;
    private static AverageTimeCounter _avgFpsTimer = null!;
    
    private static ulong _prevDeltaTimeSample;
    private static bool _isInitialised;
    
    public static void Initialise()
    {
        if(_isInitialised)
        {
            throw new InvalidOperationException("VaultGUI already initialised");
        }
        
        var vsync = true;
#if DEBUG
        var debugGraphicsDevice = true;
#else
        var debugGraphicsDevice = false;
#endif
        var windowCreateInfo = new WindowCreateInfo(
            50, 50,
            1280, 720,
            WindowState.Normal,
            "VaultGui");

        var graphicDeviceOptions = new GraphicsDeviceOptions(
            debugGraphicsDevice,
            null,
            vsync,
            ResourceBindingModel.Improved,
            true,
            true);
        
        var preferredBackend = VeldridStartup.GetPlatformDefaultBackend();

        Sdl2Native.SDL_Init(SDLInitFlags.Timer | SDLInitFlags.Video);
        
        if(preferredBackend == GraphicsBackend.OpenGL || 
           preferredBackend == GraphicsBackend.OpenGLES)
        {
            VeldridStartup.SetSDLGLContextAttributes(graphicDeviceOptions, preferredBackend);
        }

        _window = VeldridStartup.CreateWindow(ref windowCreateInfo);
        _graphicsDevice = VeldridStartup.CreateGraphicsDevice(_window, graphicDeviceOptions, preferredBackend);

        if(_graphicsDevice == null)
        {
            throw new InvalidOperationException("Veldrid Graphics Device Failed to initialise");
        }

        if(_window == null)
        {
            throw new InvalidOperationException("Veldrid Window Failed to initialise");
        }

        _window.Resized += OnWindowOnResized;
        
        _uiController = new UIController(_graphicsDevice, _window);
        _mainCommandList = _graphicsDevice.ResourceFactory.CreateCommandList();

        _highResTimer = new HighResTimer();
        _prevDeltaTimeSample = _highResTimer.Sample;
        _avgFpsTimer = new AverageTimeCounter(60);
        
        _isInitialised = true;
    }

    public static void Run()
    {
        if(_isInitialised == false)
        {
            throw new InvalidOperationException("Vault GUI is not initialised. Call Initialise() before Run()");
        }
        
        try
        {
            while (_window.Exists)
            {
                var snapshot = _window.PumpEvents();
                if(!_window.Exists)
                {
                    break;
                }

                UpdateTimers(out var deltaTime, out var avgDeltaTime);
                
                UpdateTitle(avgDeltaTime);

                _uiController.UpdateUi(deltaTime, snapshot);

                Render();
            }
        }
        finally
        {
            Cleanup();
        }
    }

    private static void UpdateTimers(out float deltaTimeThisFrame, out float avgDeltaTime)
    {
        var sample = _highResTimer.Sample;
        
        deltaTimeThisFrame = (float)(sample - _prevDeltaTimeSample) / _highResTimer.SampleFrequency;
        avgDeltaTime = _avgFpsTimer.Update(deltaTimeThisFrame);
        
        _prevDeltaTimeSample = sample;
    }

    private static void OnWindowOnResized()
    {
        _graphicsDevice.MainSwapchain.Resize((uint)_window.Width, (uint)_window.Height);
    }
    

    private static void UpdateTitle(float avgDeltaTime)
    {
        var fps = 1.0f / avgDeltaTime;
        var ms = avgDeltaTime * 1000f;
        _window.Title = $"Vault Gui - {ms:0.00} ms/frame ({fps:0.0} FPS)";
    }

    private static void Render()
    {
        //Clear the backbuffer (TODO: any other rendering outside ui?)
        _mainCommandList.Begin();
        _mainCommandList.SetFramebuffer(_graphicsDevice.MainSwapchain.Framebuffer);
        _mainCommandList.ClearColorTarget(0, new RgbaFloat(0.45f, 0.55f, 0.6f, 1f));
        _mainCommandList.End();
        _graphicsDevice.SubmitCommands(_mainCommandList);

        
        _uiController.RenderUi();

        //And swap
        _graphicsDevice.SwapBuffers(_graphicsDevice.MainSwapchain);
    }

    private static void Cleanup()
    {
        _graphicsDevice.WaitForIdle();
        _mainCommandList.Dispose();
        _uiController.Dispose();
        _graphicsDevice.Dispose();
    }
}