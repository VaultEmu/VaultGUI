using ImGuiNET;
using VaultCore.ImGuiWindowsAPI;

namespace Vault;

public class ImGuiWindowManager : IImGuiWindowManager, IDisposable
{
    private readonly List<IImGuiWindow> _subWindows = new List<IImGuiWindow>();
    private IImGuiWindow? _fullscreenWindow;
    
    public bool IsAnyWindowFullScreen => _fullscreenWindow != null;
    
    public IImGuiWindow? GetFullscreenWindow()
    {
        return _fullscreenWindow;
    }
    
    public void RegisterWindow(IImGuiWindow window)
    {
        if(_subWindows.Contains(window))
        {
            throw new InvalidOperationException("Trying to register window that is already registered");
        }
        
        _subWindows.Add(window);
    }
    
    public void UnregisterWindow(IImGuiWindow window)
    {
        if(_subWindows.Contains(window) == false)
        {
            throw new InvalidOperationException("Trying to unregister window that is not registered");
        }
        
        _subWindows.Remove(window);
    }
    
    public void SetWindowAsFullscreen(IImGuiWindow window)
    {
        if(_subWindows.Contains(window) == false)
        {
            throw new InvalidOperationException("Trying to set window as fullscreen that is not registered");
        }
        
        _fullscreenWindow = window;
    }
    
    public T? GetWindow<T>() where T : IImGuiWindow
    {
        foreach(var window in _subWindows)
        {
            if(window is T)
            {
                return (T)window;
            }
        }
        
        return default;
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
            
            var windowName = window.WindowTitle;
            var windowID = window.CustomWindowID;
            var windowFlags = window.WindowFlags;
            
            var fullWindowString = $"{windowName}###{windowID}";

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
                fullWindowString += "_FullScreen";
            }
            
            window.OnBeforeDrawImGuiWindow();
            
            if(ImGui.Begin(fullWindowString, windowFlags))
            {
                window.OnDrawImGuiWindowContent();
            }
            
            window.OnAfterDrawImGuiWindow();
            
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

    public void OnCoreAcquiresFeature(Type coreType, Type featureType, List<Type> allCoreFeaturesNeeded)
    {
        
    }

    public void OnCoreReleasesFeature(Type coreType, Type featureType, List<Type> allCoreFeaturesNeeded)
    {
        
    }
}