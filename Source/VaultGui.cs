using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;
// ReSharper disable BitwiseOperatorOnEnumWithoutFlags

namespace Vault;

public class VaultGui
{
    private readonly Sdl2Window _window;
    private readonly TimeProvider _timeProvider;
    private readonly Logger _logger;
    private readonly VaultGUIGraphics _vaultGuiGraphics;
    private readonly VaultCoreManager _vaultCoreManager;
    private readonly ImGuiUiManager _imGuiUiManager;
    private readonly VaultCoreSoftwareRendering _vaultCoreSoftwareRendering;
    private readonly InputManager _inputManager;

    private SDL_DisplayMode[] _fullScreenDisplayMode;
    private WindowState? _nextWindowModeToSet;

    public VaultGui(Logger logger)
    {
        _logger = logger;
        try
        {
            _logger.Log("Initialising");
            
            var vsync = false;
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

            _logger.Log($"Vsync: {vsync} | Debug Graphics Device {debugGraphicsDevice}");
            
            var preferredBackend = VeldridStartup.GetPlatformDefaultBackend();
            
            Sdl2Native.SDL_Init(SDLInitFlags.Timer | SDLInitFlags.Video | SDLInitFlags.GameController);

            if(preferredBackend == GraphicsBackend.OpenGL ||
               preferredBackend == GraphicsBackend.OpenGLES)
            {
                VeldridStartup.SetSDLGLContextAttributes(graphicDeviceOptions, preferredBackend);
            }
            var flags =
                SDL_WindowFlags.OpenGL |
                SDL_WindowFlags.Resizable |
                SDL_WindowFlags.AllowHighDpi;

            _logger.Log("Init Window");
            
            _window = new Sdl2Window(
                "VaultGui",
                50, 50,
                1280, 720,
                flags, false);

            if(_window == null)
            {
                throw new InvalidOperationException("Veldrid Window Failed to initialise");
            }
            
            _fullScreenDisplayMode = Array.Empty<SDL_DisplayMode>();
            CalculateFullScreenDisplayModeToUse();
            
            //Create Main Components
            _vaultGuiGraphics = new VaultGUIGraphics(_logger, _window, graphicDeviceOptions, preferredBackend);
            _imGuiUiManager = new ImGuiUiManager(_vaultGuiGraphics.TextureManager, _window, _logger);
            _timeProvider = new TimeProvider();
            _vaultCoreManager = new VaultCoreManager(_timeProvider, _logger);
            _vaultCoreSoftwareRendering = new VaultCoreSoftwareRendering(_logger, _vaultGuiGraphics.TextureManager, _imGuiUiManager, this);
            _inputManager = new InputManager(_logger);
            
            SetupCoreFeatureResolver();
            
            _vaultCoreManager.OnCoreUpdated += OnCoreUpdate;
            _vaultCoreManager.RefreshAvailableVaultCores();

            //TEMP: Load core
            var exampleCoreData = _vaultCoreManager.AvailableCores.First(x => x.CoreName == "Example Core");
            _vaultCoreManager.LoadVaultCore(exampleCoreData);

            _logger.Log("Initialising Finished");
        }
        catch(Exception e)
        {
            _logger.LogFatal("Exception thrown during Initialisation", e);
            throw new ShutdownDueToFatalErrorException("Error During Initialisation");
        }
    }
    
    private void SetupCoreFeatureResolver()
    {
        var featureResolver = _vaultCoreManager.FeatureResolver;
        
        featureResolver.RegisterFeatureImplementation(_logger); //ILogging
        featureResolver.RegisterFeatureImplementation(_timeProvider); //IHighResTimer
        featureResolver.RegisterFeatureImplementation(_vaultGuiGraphics.TextureManager); //ITextureManager, IImGuiTextureManager
        featureResolver.RegisterFeatureImplementation(_imGuiUiManager.ImGuiWindowManager); //IImGuiWindowManager
        featureResolver.RegisterFeatureImplementation(_imGuiUiManager.ImGuiMenuManager); //IImGuiMenuManager
        featureResolver.RegisterFeatureImplementation(_vaultCoreSoftwareRendering); //ISoftwareRendering
        featureResolver.RegisterFeatureImplementation(_inputManager); //IInputReceiver
    }

    private unsafe void CalculateFullScreenDisplayModeToUse()
    {
        _logger.Log("Calculating Full Screen Display Mode");

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

                //UPDATE
                PerFrameUpdate(snapshot);
                
                //RENDER
                _vaultGuiGraphics.OnNewFrameStart();
                _imGuiUiManager.GenerateImGuiRenderCalls();
                _vaultGuiGraphics.Render();
                
                //POST RENDER
                UpdateWindowModeIfNeeded();
            }
        }
        finally
        {
            ShutDown();
        }
    }
    
    private void PerFrameUpdate(InputSnapshot snapshot)
    {
        _timeProvider.OnFrameUpdate();
        
        _imGuiUiManager.Update(snapshot, _timeProvider.RenderFrameDeltaTime);
        _vaultCoreManager.Update();
                
        UpdateTitle();
    }
    
    private void OnCoreUpdate(float deltaTime)
    {
        _inputManager.SnapshotDevices(deltaTime);
    }
    
    public void SetApplicationWindowState(WindowState windowState)
    {
        _nextWindowModeToSet = windowState;
    }
    private void UpdateWindowModeIfNeeded()
    {
        if(_nextWindowModeToSet.HasValue == false)
        {
            return;
        }
        
        _logger.Log("Changing Window Mode: " + _nextWindowModeToSet.Value);
        
        unsafe
        {
            if(_nextWindowModeToSet.Value == WindowState.FullScreen)
            {
                //Need to perform extra logic of working out display mode to use
                var currentMonitor = SDLExtensions.SDL_GetWindowDisplayIndex(_window);
                    
                var displayModeToUse = _fullScreenDisplayMode[currentMonitor];
                    
                if(SDLExtensions.SDL_SetWindowDisplayMode(_window, &displayModeToUse) != 0)
                {
                    var errorString = SDLUtils.GetErrorStringIfSet();
                    _logger.LogError("Unable To set Window Display Mode - Error: " + (errorString ?? "Unknown Error"));
                }
            }
            
            _window.WindowState = _nextWindowModeToSet.Value;
        }
        
        _nextWindowModeToSet = null;
    }

    private void UpdateTitle()
    {
        var renderFps = _timeProvider.AverageRenderFrameFps;
        var renderMs = _timeProvider.AverageFrameDeltaTime * 1000f;
        
        var coreFps = _timeProvider.AverageCoreUpdateFps;
        var coreMs = _timeProvider.AverageCoreUpdateDeltaTime * 1000f;
        
        _window.Title = $"Vault Gui - Render: {renderMs:0.00} ms/frame ({renderFps:0.0} FPS) | Update: {coreMs:0.00} ms/frame ({coreFps:0.0} FPS)";
    }
    
    private void ShutDown()
    {
        _logger.Log("Shutting Down");
        _vaultCoreManager.UnloadVaultCore();
        _imGuiUiManager.Dispose();
        _vaultGuiGraphics.Dispose();
        _logger.Log("Shut Down Finished");
    }
}