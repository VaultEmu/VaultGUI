namespace Vault;

public interface IGuiApplication : ISubsystem
{
    public enum ApplicationWindowMode
    {
        Normal,
        FullScreen,
        Maximized,
        Minimized,
        BorderlessFullScreen,
    }
    
    public void SetApplicationWindowMode(ApplicationWindowMode newWindowMode);
}