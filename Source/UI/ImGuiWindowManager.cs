using System.Numerics;
using ImGuiNET;
using VaultCore.ImGuiWindowsAPI;

namespace Vault;

public class ImGuiWindowManager : IImGuiWindowManager
{
    private class WindowData
    {
        public readonly ImGuiWindow WindowInstance;
        public bool WindowOpen;

        public WindowData(ImGuiWindow windowInstance)
        {
            WindowInstance = windowInstance;

            //CB TODO: Check settings for current open state
            WindowOpen = windowInstance.WindowOpenByDefault;
        }
    }

    private class WindowMenuSection
    {
        public readonly string SectionName;
        public readonly List<WindowMenuSection> SubWindowsSections = new();
        public ImGuiWindow? WindowToOpen;
        public ImGuiShortcut? WindowOpenShortcut;
        public int Priority;

        public WindowMenuSection(string sectionName)
        {
            SectionName = sectionName;
        }
    }

    private readonly List<WindowData> _subWindowData = new();
    private WindowData? _fullscreenWindow;

    public bool IsAnyWindowFullScreen => _fullscreenWindow != null;

    public ImGuiWindow? GetFullscreenWindow()
    {
        return _fullscreenWindow?.WindowInstance;
    }

    public void RegisterWindow(ImGuiWindow window)
    {
        if(_subWindowData.FindIndex(x => x.WindowInstance == window) >= 0)
        {
            throw new InvalidOperationException("Trying to register window that is already registered");
        }

        _subWindowData.Add(new WindowData(window));
    }

    public void UnregisterWindow(ImGuiWindow window)
    {
        if(_subWindowData.FindIndex(x => x.WindowInstance == window) < 0)
        {
            throw new InvalidOperationException("Trying to unregister window that is not registered");
        }

        _subWindowData.RemoveAll(x => x.WindowInstance == window);
    }

    public void SetWindowOpen(ImGuiWindow window, bool open)
    {
        var windowData = _subWindowData.FirstOrDefault(x => x.WindowInstance == window);

        if(windowData == null)
        {
            throw new InvalidOperationException("Trying to set window as fullscreen that is not registered");
        }

        windowData.WindowOpen = open;
    }

    public void SetWindowAsFullscreen(ImGuiWindow window)
    {
        var windowData = _subWindowData.FirstOrDefault(x => x.WindowInstance == window);

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
        foreach (var windowData in _subWindowData)
        {
            windowData.WindowInstance.OnUpdate();
            
            var menuData = windowData.WindowInstance.WindowsMenuItemData;
            
            if(menuData != null && menuData.Shortcut != null)
            {
                if(menuData.Shortcut.IsShortcutPressed())
                {
                    var windowOpen = _subWindowData.Find(x => x.WindowInstance == windowData.WindowInstance)!.WindowOpen;
                    SetWindowOpen(windowData.WindowInstance, !windowOpen);
                }
            }
        }

        //Should the full screen window become closed, exit full screen view
        if(_fullscreenWindow != null && _fullscreenWindow.WindowOpen == false)
        {
            ClearFullscreenWindow();
        }
    }

    public void DrawWindows()
    {
        //ImGui.ShowDemoWindow();

        foreach (var windowData in _subWindowData)
        {
            if(IsAnyWindowFullScreen && _fullscreenWindow != windowData)
            {
                continue;
            }

            if(windowData.WindowOpen == false)
            {
                continue;
            }

            var windowName = windowData.WindowInstance.WindowTitle;
            var windowID = windowData.WindowInstance.WindowID;
            var windowFlags = windowData.WindowInstance.WindowFlags;

            var fullWindowString = $"{windowName}###{windowID}";

            var viewport = ImGui.GetMainViewport();

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


            ImGui.SetNextWindowPos(new Vector2(viewport.WorkSize.X * 0.5f - 300.0f, viewport.WorkSize.Y * 0.5f - 200.0f), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSize(new Vector2(600, 400), ImGuiCond.FirstUseEver);

            windowData.WindowInstance.OnBeforeDrawImGuiWindow();

            bool windowNotCollapsed;

            if(windowData.WindowInstance.WindowAlwaysOpen)
            {
                windowNotCollapsed = ImGui.Begin(fullWindowString, windowFlags);
            }
            else
            {
                windowNotCollapsed = ImGui.Begin(fullWindowString, ref windowData.WindowOpen, windowFlags);
            }

            if(windowNotCollapsed)
            {
                windowData.WindowInstance.OnDrawImGuiWindowContent();
            }

            ImGui.End();

            windowData.WindowInstance.OnAfterDrawImGuiWindow();

            if(IsAnyWindowFullScreen)
            {
                ImGui.PopStyleVar(2);
            }
        }
    }

    public void PopulateWindowMenu()
    {
        var topLevelMenuSection = new List<WindowMenuSection>();

        //Build our data structure for the window menu
        foreach (var windowData in _subWindowData)
        {
            var menuData = windowData.WindowInstance.WindowsMenuItemData;

            if(menuData == null)
            {
                continue;
            }

            var split = menuData.MenuPath.Split("/");

            var currentLevel = topLevelMenuSection;

            for (var index = 0; index < split.Length; index++)
            {
                var section = split[index];
                var currentItem = currentLevel.Find(x => x.SectionName.Equals(section));

                if(currentItem == null)
                {
                    currentItem = new WindowMenuSection(section);
                    currentItem.Priority = int.MinValue;
                    currentLevel.Add(currentItem);
                }

                if(currentItem.Priority < menuData.Priority)
                {
                    currentItem.Priority = menuData.Priority;
                }

                //Sort items as we go
                currentLevel.Sort((x, y) =>
                {
                    var result = x.Priority.CompareTo(y.Priority);

                    if(result == 0)
                    {
                        result = string.Compare(x.SectionName, y.SectionName, StringComparison.OrdinalIgnoreCase);
                    }

                    return result;
                });

                if(index == split.Length - 1)
                {
                    //Last part of path, this is our section to click on
                    currentItem.WindowToOpen = windowData.WindowInstance;
                    currentItem.WindowOpenShortcut = menuData.Shortcut;
                }
                else
                {
                    //Going down one level
                    currentLevel = currentItem.SubWindowsSections;
                }
            }
        }

        RecursiveDrawMenuItems(topLevelMenuSection, 0);
    }

    private void RecursiveDrawMenuItems(List<WindowMenuSection> levelToDraw, int lastPriority)
    {
        foreach (var item in levelToDraw)
        {
            if(item.Priority - lastPriority > 100)
            {
                ImGui.Separator();
            }

            lastPriority = item.Priority;

            if(item.WindowToOpen == null)
            {
                //Has sublevel
                if(ImGui.BeginMenu(item.SectionName))
                {
                    RecursiveDrawMenuItems(item.SubWindowsSections, lastPriority);
                    ImGui.EndMenu();
                }
            }
            else
            {
                //is leaf
                var shortcutText = "";
                if(item.WindowOpenShortcut != null)
                {
                    shortcutText = item.WindowOpenShortcut.ToString();
                }
                
                var windowOpen = _subWindowData.Find(x => x.WindowInstance == item.WindowToOpen)!.WindowOpen;
                if(ImGui.MenuItem(item.SectionName, shortcutText, windowOpen))
                {
                    SetWindowOpen(item.WindowToOpen, !windowOpen);
                }
            }
        }
    }

    public void OnCoreAcquiresFeature(Type coreType, Type featureType, List<Type> allCoreFeaturesNeeded) { }

    public void OnCoreReleasesFeature(Type coreType, Type featureType, List<Type> allCoreFeaturesNeeded) { }
}