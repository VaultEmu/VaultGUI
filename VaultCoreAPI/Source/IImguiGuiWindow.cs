using ImGuiNET;

namespace Vault;

public interface IImguiGuiWindow : IDisposable
{
    //Order Of Execution for functions
    
    //WINDOW UPDATE STEP
    //foreach Window:
    // - OnUpdate()
    //
    //WINDOW DRAW STEP
    ////foreach Window:
    // - OnBeforeDrawImguiWindow()
    // - OnDrawImGuiWindowContent()
    // - OnAfterDrawImGuiWindow()
    
    //Name to show on the window
    public string WindowTitle { get; }
    
    //Custom Window ID (defaults to Window Title)
    public string CustomWindowID => WindowTitle;
    
    //ImGui window flags for this window
    public ImGuiWindowFlags WindowFlags { get; }

    //Call to run any updates on the windows in the update pass
    public void OnUpdate();
    
    //Called to run any Imgui commands before beginning to draw the window
    public void OnBeforeDrawImguiWindow();
    
    //Called to draw a window's content
    public void OnDrawImGuiWindowContent();
    
    //Called to run any Imgui commands after drawing the window
    public void OnAfterDrawImGuiWindow();
}