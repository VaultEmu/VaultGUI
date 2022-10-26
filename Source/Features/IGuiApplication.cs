using VaultCore.CoreAPI;

namespace Vault;

//Feature for exposing GUI Application functionality
public interface IGuiApplication : IFeature
{
    public enum ApplicationWindowMode
    {
        Normal,
        FullScreen,
        Maximized,
        Minimized,
        BorderlessFullScreen,
    }
    
    //Sets the window mode for the gui application
    public void SetApplicationWindowMode(ApplicationWindowMode newWindowMode);
}