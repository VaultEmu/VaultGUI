using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

namespace Vault;

public class VaultGui : IGuiApplication
{
    private readonly Sdl2Window _window;
    private readonly GraphicsDevice _graphicsDevice;
    private readonly ImGuiUiController _imGuiUiController;
    private readonly TimeProvider _timeProvider;
    private readonly CommandList _mainCommandList;
    private readonly ILogger _logger;
    private readonly VaultCoreLoader _vaultCoreLoader;
    
    private SDL_DisplayMode[] _fullScreenDisplayMode;
    private IGuiApplication.ApplicationWindowMode? _nextWindowModeToSet;

    public VaultGui(ILogger logger)
    {
        _logger = logger;
        try
        {
            GlobalSubsystems.RegisterSubsystem(this);
            
            _logger.Log("Vault GUI - Multi System Emulator\n");
            _logger.Log("Initialising");
        
            var vsync = true;
#if DEBUG
        var debugGraphicsDevice = true;
#else
            var debugGraphicsDevice = false;
#endif

            var graphicDeviceOptions = new GraphicsDeviceOptions(
                debugGraphicsDevice,
                null,
                vsync,
                ResourceBindingModel.Improved,
                true,
                true);
        
            var preferredBackend = VeldridStartup.GetPlatformDefaultBackend();

            // ReSharper disable once BitwiseOperatorOnEnumWithoutFlags
            Sdl2Native.SDL_Init(SDLInitFlags.Timer | SDLInitFlags.Video);
        
            if(preferredBackend == GraphicsBackend.OpenGL || 
               preferredBackend == GraphicsBackend.OpenGLES)
            {
                VeldridStartup.SetSDLGLContextAttributes(graphicDeviceOptions, preferredBackend);
            }
            
            var flags = 
                SDL_WindowFlags.OpenGL | 
                SDL_WindowFlags.Resizable | 
                SDL_WindowFlags.AllowHighDpi;
            
            _window = new Sdl2Window(
                "VaultGui",
                50, 50,
                1280, 720,
                flags, false);
            
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
            
            _timeProvider = new TimeProvider();
            _vaultCoreLoader = new VaultCoreLoader();

            _imGuiUiController = new ImGuiUiController(_graphicsDevice, _window);
            _mainCommandList = _graphicsDevice.ResourceFactory.CreateCommandList();
            
            _fullScreenDisplayMode = Array.Empty<SDL_DisplayMode>();
            
            CalculateFullScreenDisplayModeToUse();

            _logger.Log("Initialising Finished");
        }
        catch(Exception e)
        {
            _logger.LogFatal("Exception thrown during Initialisation", e);
            throw new ShutdownDueToFatalErrorException("Error During Initialisation");
        }
    }
    
    private unsafe void CalculateFullScreenDisplayModeToUse()
    {
        _logger.Log("Calculating Full Screen Display Mode");
        unsafe
        {
            var numDisplays = Sdl2Native.SDL_GetNumVideoDisplays();
            
            _fullScreenDisplayMode = new SDL_DisplayMode[numDisplays];
            
            for (int displayIndex = 0; displayIndex < numDisplays; ++displayIndex)
            {
                var numDisplayModes = SDLExtensions.SDL_GetNumDisplayModes(0);

                var displayMode = new SDL_DisplayMode();
                _fullScreenDisplayMode[displayIndex] = new SDL_DisplayMode();

                for (int displayModeIndex = 0; displayModeIndex < numDisplayModes; ++displayModeIndex)
                {
                    if(SDLExtensions.SDL_GetDisplayMode(0, displayModeIndex, &displayMode) == 0)
                    {
                        if(displayMode.w * displayMode.h * displayMode.refresh_rate >
                           _fullScreenDisplayMode[displayIndex].w * 
                           _fullScreenDisplayMode[displayIndex].h * 
                           _fullScreenDisplayMode[displayIndex].refresh_rate)
                        {
                            _fullScreenDisplayMode[displayIndex] = displayMode;
                        }
                    }
                }
                
                _logger.Log($"Display {displayIndex}: " +
                            $"{_fullScreenDisplayMode[displayIndex] .w}x{_fullScreenDisplayMode[displayIndex] .h} @ " +
                            $"{_fullScreenDisplayMode[displayIndex] .refresh_rate}");
            }
        }
        
        _logger.Log($"DPI Scale {DpiAwareUtils.GetDPIScale(_window)}");
    }

    public void Run()
    {
        try
        {
            while (_window.Exists)
            {
                var snapshot = _window.PumpEvents();
                if(!_window.Exists)
                {
                    break;
                }

                _timeProvider.Update();
                
                UpdateTitle();

                _imGuiUiController.UpdateUi(snapshot);

                Render();
                
                UpdateWindowModeIfNeeded();
            }
        }
        finally
        {
            ShutDown();
        }
    }
    
    public void SetApplicationWindowMode(IGuiApplication.ApplicationWindowMode newWindowMode)
    {
        _nextWindowModeToSet = newWindowMode;
    }
    private void UpdateWindowModeIfNeeded()
    {
        if(_nextWindowModeToSet == null)
        {
            return;
        }
        
        unsafe
        {
            switch(_nextWindowModeToSet)
            {
                case IGuiApplication.ApplicationWindowMode.Normal:
                    _window.WindowState = WindowState.Normal;
                    break;
                case IGuiApplication.ApplicationWindowMode.FullScreen:
                    var currentMonitor = SDLExtensions.SDL_GetWindowDisplayIndex(_window);
                    
                    var displayModeToUse = _fullScreenDisplayMode[currentMonitor];
                    
                    if(SDLExtensions.SDL_SetWindowDisplayMode(_window, &displayModeToUse) != 0)
                    {
                        var errorString = SDLUtils.GetErrorStringIfSet();
                        _logger.LogError("Unable To set Window Display Mode - Error: " + (errorString ?? "Unknown Error"));
                    }
                    
                    _window.WindowState = WindowState.FullScreen;
                    break;
                case IGuiApplication.ApplicationWindowMode.Maximized:
                    _window.WindowState = WindowState.Maximized;
                    break;
                case IGuiApplication.ApplicationWindowMode.Minimized:
                    _window.WindowState = WindowState.Minimized;
                    break;
                case IGuiApplication.ApplicationWindowMode.BorderlessFullScreen:
                    _window.WindowState = WindowState.BorderlessFullScreen;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(_nextWindowModeToSet), _nextWindowModeToSet, null);
            }
        }
        
        _nextWindowModeToSet = null;
    }

    private void OnWindowOnResized()
    {
        _logger.Log($"Window Resized: {_window.Width}x{_window.Height}");
        _graphicsDevice.MainSwapchain.Resize((uint)_window.Width, (uint)_window.Height);
    }

    private void UpdateTitle()
    {
        var fps = _timeProvider.AverageFps;
        var ms = _timeProvider.AverageDeltaTime * 1000f;
        _window.Title = $"Vault Gui - {ms:0.00} ms/frame ({fps:0.0} FPS)";
    }

    private void Render()
    {
        //Clear the backbuffer (TODO: any other rendering outside ui?)
        _mainCommandList.Begin();
        _mainCommandList.SetFramebuffer(_graphicsDevice.MainSwapchain.Framebuffer);
        _mainCommandList.ClearColorTarget(0, new RgbaFloat(0.4f, 0.4f, 0.4f, 1f));
        _mainCommandList.End();
        _graphicsDevice.SubmitCommands(_mainCommandList);
        
        _imGuiUiController.RenderUi();

        //And swap
        _graphicsDevice.SwapBuffers(_graphicsDevice.MainSwapchain);
    }

    private void ShutDown()
    {
        _logger.Log("Shutting Down");
        _graphicsDevice.WaitForIdle();
        _mainCommandList.Dispose();
        _imGuiUiController.Dispose();
        _graphicsDevice.Dispose();
        _logger.Log("Shut Down Finished");
    }
}