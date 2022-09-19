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
    private readonly EmuCoreLoader _emuCoreLoader;
    
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

            // ReSharper disable once BitwiseOperatorOnEnumWithoutFlags
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
        
            _timeProvider = new TimeProvider();
            _emuCoreLoader = new EmuCoreLoader();

            _imGuiUiController = new ImGuiUiController(_graphicsDevice, _window);
            _mainCommandList = _graphicsDevice.ResourceFactory.CreateCommandList();
            
            funcA();
        
            _logger.Log("Initialising Finished");
        }
        catch(Exception e)
        {
            _logger.LogFatal("Exception thrown during Initialisation", e);
            throw new ShutdownDueToFatalErrorException("Error During Initialisation");
        }
    }

    private void funcA()
    {
       funcB();
    }

    private void funcB()
    {
        int test2 = 1;
            
        int test = test2 / 0;
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

        switch(_nextWindowModeToSet)
        {
            case IGuiApplication.ApplicationWindowMode.Normal:
                _window.WindowState = WindowState.Normal;
                break;
            case IGuiApplication.ApplicationWindowMode.FullScreen:
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
        
        _nextWindowModeToSet = null;
    }

    private void OnWindowOnResized()
    {
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