namespace Vault;

//Subsystem for interfacing with the GUI applications
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
    
    //Sets the window mode for the application
    public void SetApplicationWindowMode(ApplicationWindowMode newWindowMode);
    
    // //Gets the size of the application window
    // public void GetApplicationWindowSize(out int width, out int height);
}