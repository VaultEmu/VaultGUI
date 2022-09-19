namespace Vault;

public interface IImguiWindowManager : ISubsystem
{
    public bool IsAnyWindowFullScreen { get; }
    
    public IImguiGuiWindow? GetFullscreenWindow();
    
    public void RegisterWindow(IImguiGuiWindow window);
    
    public void UnregisterWindow(IImguiGuiWindow window);
    
    public void SetWindowAsFullscreen(IImguiGuiWindow window);
    
    public void ClearFullscreenWindow();
}