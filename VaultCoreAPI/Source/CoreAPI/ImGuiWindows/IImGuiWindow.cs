using ImGuiNET;

namespace Vault;

//Interface to implement to create a ImGui window for the Vault GUI application
public interface IImGuiWindow : IDisposable
{
    //Order Of Execution for functions
    
    //WINDOW UPDATE STEP
    //foreach Window:
    // - OnUpdate()
    //
    //WINDOW DRAW STEP
    ////foreach Window:
    // - OnBeforeDrawImGuiWindow()
    // - OnDrawImGuiWindowContent()
    // - OnAfterDrawImGuiWindow()
    
    //Name to show on the window
    public string WindowTitle { get; }
    
    //Custom Window ID (defaults to Window Title)
    //If Window title is not constant, override this to provide a consistent, unique ID for the window
    public string CustomWindowID => WindowTitle;
    
    //ImGui window flags for this window
    public ImGuiWindowFlags WindowFlags { get; }

    //Call to run any updates on the windows in the update pass
    public void OnUpdate();
    
    //Called to run any ImGui commands before beginning to draw the window
    public void OnBeforeDrawImGuiWindow();
    
    //Called to draw a window's content
    public void OnDrawImGuiWindowContent();
    
    //Called to run any ImGui commands after drawing the window
    public void OnAfterDrawImGuiWindow();
}