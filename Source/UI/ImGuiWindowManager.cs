using System.Numerics;
using ImGuiNET;
using VaultCore.ImGuiWindowsAPI;

namespace Vault;

public class ImGuiWindowManager : IImGuiWindowManager
{
    private readonly List<ImGuiWindow> _windows = new();
    private ImGuiWindow? _fullscreenWindow;

    public bool IsAnyWindowFullScreen => _fullscreenWindow != null;

    public ImGuiWindow? GetFullscreenWindow()
    {
        return _fullscreenWindow;
    }

    public void RegisterWindow(ImGuiWindow window)
    {
        if(_windows.FindIndex(x => x == window) >= 0)
        {
            throw new InvalidOperationException("Trying to register window that is already registered");
        }

        _windows.Add(window);
    }

    public void UnregisterWindow(ImGuiWindow window)
    {
        if(_windows.FindIndex(x => x == window) < 0)
        {
            throw new InvalidOperationException("Trying to unregister window that is not registered");
        }
        
        //Should the full screen window become closed, exit full screen view
        if(_fullscreenWindow != null && _fullscreenWindow == window)
        {
            ClearFullscreenWindow();
        }

        _windows.Remove(window);
    }

    public void SetWindowAsFullscreen(ImGuiWindow window)
    {
        var windowData = _windows.FirstOrDefault(x => x == window);

        if(windowData == null)
        {
            throw new InvalidOperationException("Trying to set window as fullscreen that is not registered");
        }

        _fullscreenWindow = windowData;
    }

    public void ClearFullscreenWindow()
    {
        _fullscreenWindow = null;
    }

    public void UpdateWindows()
    {
        foreach (var windowData in _windows)
        {
            windowData.OnUpdate();
        }

        //Should the full screen window become closed, exit full screen view
        if(_fullscreenWindow != null && _fullscreenWindow.IsWindowOpen == false)
        {
            ClearFullscreenWindow();
        }
    }

    public void DrawWindows()
    {
        //ImGui.ShowDemoWindow();

        foreach (var window in _windows)
        {
            if(IsAnyWindowFullScreen && _fullscreenWindow != window)
            {
                continue;
            }

            if(window.IsWindowOpen == false)
            {
                continue;
            }

            var windowName = window.WindowTitle;
            var windowID = window.WindowID;
            var windowFlags = window.WindowFlags;

            var fullWindowString = $"{windowName}###{windowID}";

            var viewport = ImGui.GetMainViewport();
            
            ImGui.SetNextWindowPos(new Vector2(viewport.WorkSize.X * 0.5f - 300.0f, viewport.WorkSize.Y * 0.5f - 200.0f), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSize(new Vector2(600, 400), ImGuiCond.FirstUseEver);

            if(IsAnyWindowFullScreen)
            {
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

            bool windowNotCollapsed;

            if(window.ShowCloseButton)
            {
                bool windowOpen = window.IsWindowOpen;
                windowNotCollapsed = ImGui.Begin(fullWindowString, ref windowOpen, windowFlags);
                if(window.IsWindowOpen != windowOpen)
                {
                    window.IsWindowOpen = windowOpen;
                }
            }
            else
            {
                windowNotCollapsed = ImGui.Begin(fullWindowString, windowFlags);
            }

            if(windowNotCollapsed)
            {
                window.OnDrawImGuiWindowContent();
            }

            ImGui.End();

            window.OnAfterDrawImGuiWindow();

            if(IsAnyWindowFullScreen)
            {
                ImGui.PopStyleVar(2);
            }
        }
    }

    public void OnCoreAcquiresFeature(Type coreType, Type featureType, List<Type> allCoreFeaturesNeeded) { }

    public void OnCoreReleasesFeature(Type coreType, Type featureType, List<Type> allCoreFeaturesNeeded) { }
}