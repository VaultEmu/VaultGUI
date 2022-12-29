using ImGuiNET;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

namespace Vault;

public class VaultGUIGraphics : IDisposable
{
    private readonly Sdl2Window _window;
    private readonly GraphicsDevice _graphicsDevice;
    private readonly TextureManager _textureManager;
    private readonly ImGuiRenderer _imGuiRenderer;
    private readonly CommandList _mainCommandList;
    private readonly CommandList _uiCommandList;
    private readonly Logger _logger;
    
    public TextureManager TextureManager => _textureManager;
    public ImGuiRenderer ImGuiRenderer => _imGuiRenderer;
    public GraphicsDevice GraphicsDevice => _graphicsDevice;
    
    
    public VaultGUIGraphics(Logger logger, Sdl2Window window, GraphicsDeviceOptions graphicDeviceOptions, GraphicsBackend graphicsBackend)
    {
        _logger = logger;
        _window = window;

        var vsync = false;
#if DEBUG
        var debugGraphicsDevice = true;
#else
        var debugGraphicsDevice = false;
#endif
        
        _logger.Log("Init Graphics Systems");
        _logger.Log($"Vsync: {vsync} | Debug Graphics Device {debugGraphicsDevice}");
        
        _graphicsDevice = VeldridStartup.CreateGraphicsDevice(window, graphicDeviceOptions, graphicsBackend);

        if(_graphicsDevice == null)
        {
            throw new InvalidOperationException("Veldrid Graphics Device Failed to initialise");
        }
        
        _imGuiRenderer = new ImGuiRenderer(_graphicsDevice,
            _graphicsDevice.MainSwapchain.Framebuffer.OutputDescription,
            window.Width, window.Height,
            DpiAwareUtils.GetDPIScale(window));

        _textureManager = new TextureManager(_graphicsDevice, _imGuiRenderer);

        _mainCommandList = _graphicsDevice.ResourceFactory.CreateCommandList();
        _uiCommandList = _graphicsDevice.ResourceFactory.CreateCommandList();

        window.Resized += OnWindowOnResized;
    }

    private void OnWindowOnResized()
    {
        _logger.Log($"Window Resized: {_window.Width}x{_window.Height}");
        _graphicsDevice.MainSwapchain.Resize((uint)_window.Width, (uint)_window.Height);
        _imGuiRenderer.WindowResized(_window.Width, _window.Height);
    }
    
    public void OnNewFrameStart()
    {
        _imGuiRenderer.OnNewFrameStart();
    }
    
    public void Render()
    {
        //Clear the backbuffer
        _mainCommandList.Begin();
        _mainCommandList.SetFramebuffer(_graphicsDevice.MainSwapchain.Framebuffer);
        _mainCommandList.ClearColorTarget(0, new RgbaFloat(0.4f, 0.4f, 0.4f, 1f));
        
        // TODO: any other rendering outside ui?
        _mainCommandList.End();
        _graphicsDevice.SubmitCommands(_mainCommandList);
        
        //Render ImGui
        _uiCommandList.Begin();
        _uiCommandList.SetFramebuffer(_graphicsDevice.MainSwapchain.Framebuffer);
        _imGuiRenderer.Render(_graphicsDevice, _uiCommandList);
        _uiCommandList.End();
        _graphicsDevice.SubmitCommands(_uiCommandList);


        //And swap
        _graphicsDevice.SwapBuffers(_graphicsDevice.MainSwapchain);
    }

    public void Dispose()
    {
        _graphicsDevice.WaitForIdle();
        _textureManager.Dispose();
        _imGuiRenderer.Dispose();
        _mainCommandList.Dispose();
        _uiCommandList.Dispose();
        _graphicsDevice.Dispose();
    }
}