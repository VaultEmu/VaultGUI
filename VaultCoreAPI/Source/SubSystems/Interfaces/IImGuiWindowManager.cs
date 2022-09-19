namespace Vault;

//Subsystem for interfacing with the ImGui Window Manager
public interface IImGuiWindowManager : ISubsystem
{
    //Returns true if any window is set as the full screen window
    public bool IsAnyWindowFullScreen { get; }
    
    //Returns the window that is currently full screen, or null if no window is full screen
    public IImGuiWindow? GetFullscreenWindow();
    
    //Registers a window in the manager
    public void RegisterWindow(IImGuiWindow window);
    
    //Unregisters a window in the manager
    public void UnregisterWindow(IImGuiWindow window);
    
    //Sets this window as the full screen window
    public void SetWindowAsFullscreen(IImGuiWindow window);
    
    //Clears any window that is full screen
    public void ClearFullscreenWindow();
}