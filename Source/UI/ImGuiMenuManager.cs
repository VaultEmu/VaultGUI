using ImGuiNET;
using VaultCore.ImGuiWindowsAPI;

namespace Vault;

public class ImGuiMenuManager : IImGuiMenuManager
{
    private class MenuSection
    {
        public readonly string SectionName;
        public readonly List<MenuSection> SubSections = new();
        public ImGuiMenuItem? MenuItemData;
        public int Priority;
        
        public MenuSection(string sectionName)
        {
            SectionName = sectionName;
        }
    }
    
    private List<ImGuiMenuItem> _menuItems = new List<ImGuiMenuItem>();
    private Dictionary<string, int> _topLevelMenuItemsExplicitPriorities = 
        new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase);

    public void RegisterMenuItem(ImGuiMenuItem menuItem)
    {
        if(_menuItems.Contains(menuItem))
        {
            throw new InvalidOperationException("Trying to add menu item that is already added");
        }
        
        if(menuItem.MenuPath.Count(x => x == '/') < 1)
        {
            throw new InvalidOperationException("Menu items path needs to be at least 2 items long");
        }

        _menuItems.Add(menuItem);
    }

    public void UnregisterMenuItem(ImGuiMenuItem menuItem)
    {
        if(_menuItems.Contains(menuItem) == false)
        {
            throw new InvalidOperationException("Trying to remove menu item that is not added");
        }

        _menuItems.Remove(menuItem);
    }

    public void SetTopLevelMenuPriority(string itemName, int priority)
    {
        _topLevelMenuItemsExplicitPriorities[itemName] = priority;
    }

    public void PopulateMainMenu()
    {
        var topLevelMenuSection = new List<MenuSection>();

        //Build our data structure for the window menu
        foreach (var menuItemData in _menuItems)
        {
            //Check Shortcut
            if(menuItemData.Shortcut != null)
            {
                if(menuItemData.Shortcut.IsShortcutPressed())
                {
                    menuItemData.MenuAction();
                }
            }
            
            var split = menuItemData.MenuPath.Split("/");

            var currentLevel = topLevelMenuSection;
            
            for (var index = 0; index < split.Length; index++)
            {
                var section = split[index];
                var currentItem = currentLevel.Find(x => x.SectionName.Equals(section));

                if(currentItem == null)
                {
                    currentItem = new MenuSection(section);
                    currentItem.Priority = int.MinValue;
                    currentLevel.Add(currentItem);
                }
                
                if(index == 0)
                {
                    //For Top Level Priority, use the explicit set priority, or 0 if not set
                    if(_topLevelMenuItemsExplicitPriorities.TryGetValue(section, out var explicitPriority))
                    {
                        currentItem.Priority = explicitPriority;
                    }
                    else
                    {
                        currentItem.Priority = 0;
                    }
                }
                else if(currentItem.Priority < menuItemData.Priority)
                {
                    //For a submenu, use the lowest value of all sub elements
                    currentItem.Priority = menuItemData.Priority;
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
                    currentItem.MenuItemData = menuItemData;
                }
                else
                {
                    //Going down one level
                    currentLevel = currentItem.SubSections;
                }
            }
        }

        RecursiveDrawMenuItems(topLevelMenuSection, 0, true);
    }

    public void OnCoreAcquiresFeature(Type coreType, Type featureType, List<Type> allCoreFeaturesNeeded) { }

    public void OnCoreReleasesFeature(Type coreType, Type featureType, List<Type> allCoreFeaturesNeeded) { }

    private void RecursiveDrawMenuItems(List<MenuSection> levelToDraw, int lastPriority, bool isTopLevel)
    {
        foreach (var item in levelToDraw)
        {
            if(isTopLevel == false)
            {
                if(item.Priority - lastPriority > 100)
                {
                    ImGui.Separator();
                }

                lastPriority = item.Priority;
            }
            

            if(item.MenuItemData == null)
            {
                //Has sublevel
                if(ImGui.BeginMenu(item.SectionName))
                {
                    RecursiveDrawMenuItems(item.SubSections, lastPriority, false);
                    ImGui.EndMenu();
                }
            }
            else
            {
                //is leaf
                var shortcutText = "";
                if(item.MenuItemData.Shortcut != null)
                {
                    shortcutText = item.MenuItemData.Shortcut.ToString();
                }
                
                bool isItemSelected = item.MenuItemData.IsItemChecked?.Invoke() ?? false; 
                
                if(ImGui.MenuItem(item.SectionName, shortcutText, isItemSelected))
                {
                    item.MenuItemData.MenuAction();
                }
            }
        }
    }
}