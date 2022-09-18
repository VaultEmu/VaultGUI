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
    // - OnImguiGui()
    // - OnAfterDrawImguiWindow()
    
    //Name to show on the window
    public string WindowName { get; }
    
    //ImGui window flags for this window
    public ImGuiWindowFlags WindowFlags { get; }

    //Call to run any updates on the windows in the update pass
    public void OnUpdate();
    
    //Called to run any Imgui commands before beginning to draw the window
    public void OnBeforeDrawImguiWindow();
    
    //Called to draw a window's content
    public void OnImguiGui();
    
    //Called to run any Imgui commands after drawing the window
    public void OnAfterDrawImguiWindow();
}