using ImGuiNET;

namespace Vault;

public class ImGuiWindowManager : IImguiWindowManager, IDisposable
{
    private readonly List<IImguiGuiWindow> _subWindows = new List<IImguiGuiWindow>();
    private IImguiGuiWindow? _fullscreenWindow;
    
    public bool IsAnyWindowFullScreen => _fullscreenWindow != null;
    
    public ImGuiWindowManager()
    {
        SubsystemController.RegisterSubsystem(this);
    }
    
    public IImguiGuiWindow? GetFullscreenWindow()
    {
        return _fullscreenWindow;
    }
    
    public void RegisterWindow(IImguiGuiWindow window)
    {
        if(_subWindows.Contains(window))
        {
            throw new InvalidOperationException("Trying to register window that is already registered");
        }
        
        _subWindows.Add(window);
    }
    
    public void UnregisterWindow(IImguiGuiWindow window)
    {
        if(_subWindows.Contains(window) == false)
        {
            throw new InvalidOperationException("Trying to unregister window that is not registered");
        }
        
        _subWindows.Remove(window);
    }
    
    public void SetWindowAsFullscreen(IImguiGuiWindow window)
    {
        if(_subWindows.Contains(window) == false)
        {
            throw new InvalidOperationException("Trying to set window as fullscreen that is not registered");
        }
        
        _fullscreenWindow = window;
    }

    public void ClearFullscreenWindow()
    {
        _fullscreenWindow = null;
    }
    
    public void UpdateWindows()
    {
        foreach(var window in _subWindows)
        {
            window.OnUpdate();
        }
    }
    
    public void DrawWindows()
    {
        //ImGui.ShowDemoWindow();
        
        foreach(var window in _subWindows)
        {
            if(IsAnyWindowFullScreen && _fullscreenWindow != window)
            {
                continue;
            }

            var windowName = window.WindowName;
            var windowFlags = window.WindowFlags;
            
            if(IsAnyWindowFullScreen)
            {
                var viewport = ImGui.GetMainViewport();
                ImGui.SetNextWindowPos(viewport.WorkPos);
                ImGui.SetNextWindowSize(viewport.WorkSize);
                ImGui.SetNextWindowViewport(viewport.ID);
                ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0.0f);
                ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0.0f);

                windowFlags |= ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove;
                windowFlags &= ~ImGuiWindowFlags.MenuBar;
                windowName += "(FullScreen)";
            }
            
            window.OnBeforeDrawImguiWindow();
            
            if(ImGui.Begin(windowName, windowFlags))
            {
                window.OnImguiGui();
            }
            
            window.OnAfterDrawImguiWindow();
            
            if(IsAnyWindowFullScreen)
            {
                ImGui.PopStyleVar(2);
            }
        }
    }

    public void Dispose()
    {
        foreach(var window in _subWindows)
        {
            window.Dispose();
        }
    }
}