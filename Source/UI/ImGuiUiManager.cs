using System.Numerics;
using ImGuiNET;
using Veldrid;
using Veldrid.Sdl2;
using VaultCore.Rendering;
using VaultCore.ImGuiWindowsAPI;

namespace Vault;


public class ImGuiUiManager : IDisposable
{
    private readonly Sdl2Window _parentWindow;
    private readonly ImGuiWindowManager _imGuiWindowManager;
    private readonly ImGuiInput _ImGuiInput;
    
    private readonly TextureManager _textureManager;
    private readonly Texture2D _backgroundTexture;
    private readonly ImGuiTextureRef _backgroundTextureImGuiRef;
    
    public ImGuiWindowManager ImGuiWindowManager => _imGuiWindowManager;

    public ImGuiUiManager(TextureManager textureManager, Sdl2Window window)
    {
        _textureManager = textureManager;
        _parentWindow = window;
        _imGuiWindowManager = new ImGuiWindowManager();
        _ImGuiInput = new ImGuiInput();

        ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.DockingEnable;
        
        _backgroundTexture = _textureManager.LoadTextureFromDisk(@".\Assets\VaultBg.png");
        _backgroundTextureImGuiRef = _textureManager.GetOrCreateImGuiTextureRefForTexture(_backgroundTexture);

        AddDefaultWindows();
    }
    
    public void Update(InputSnapshot snapshot, double deltaSeconds)
    {
        _ImGuiInput.Update(snapshot, deltaSeconds);
        _imGuiWindowManager.UpdateWindows();
    }

    public void GenerateImGuiRenderCalls()
    {
        DrawBackgroundImage();

        //Create Dockable Workspace
        ImGui.DockSpaceOverViewport(ImGui.GetMainViewport(), ImGuiDockNodeFlags.PassthruCentralNode);

        DrawMenuBar();
        
        _imGuiWindowManager.DrawWindows();
    }

    private void DrawBackgroundImage()
    {
        var imageWidth = _backgroundTexture.Width;
        var imageHeight = _backgroundTexture.Height;

        var halfDisplaySize = ImGui.GetMainViewport().WorkSize * 0.5f;

        ImGui.GetBackgroundDrawList().AddImage(_backgroundTextureImGuiRef.ImGuiRef, 
            new Vector2(
                halfDisplaySize.X - imageWidth * 0.5f,
                halfDisplaySize.Y - imageHeight * 0.5f), 
            new Vector2(
                halfDisplaySize.X + imageWidth * 0.5f,
                halfDisplaySize.Y + imageHeight * 0.5f),
            new Vector2(), new Vector2(1f, 1f), ImGui.GetColorU32(new Vector4(0.35f, 0.35f, 0.35f, 1.0f)));
    }

    private void AddDefaultWindows()
    {
       
    }

    public void Dispose()
    {
        _imGuiWindowManager.Dispose();
    }

    private void DrawMenuBar()
    {
        //Hide menu bar in full screen mode
        if(_parentWindow.WindowState == WindowState.FullScreen ||
           _parentWindow.WindowState == WindowState.BorderlessFullScreen)
        {
            return;
        }
        
        bool openAboutPopup = false;
        if(ImGui.BeginMainMenuBar())
        {
            if(ImGui.BeginMenu("File"))
            {
                if(ImGui.MenuItem("Load Core", "CTRL+O"))
                {
                    
                }

                ImGui.Separator();
                if(ImGui.MenuItem("Exit", "Alt-F4"))
                {
                    _parentWindow.Close();
                }

                ImGui.EndMenu();
            }
            
            if(ImGui.BeginMenu("Windows"))
            {
                var consoleWindowOpen = _imGuiWindowManager.GetWindow<ConsoleWindow>();
                
                if(ImGui.MenuItem("Console", "", consoleWindowOpen != null))
                {
                    if(consoleWindowOpen == null)
                    {
                        consoleWindowOpen = new ConsoleWindow();
                        _imGuiWindowManager.RegisterWindow(consoleWindowOpen);
                    }
                    else
                    {
                        _imGuiWindowManager.UnregisterWindow(consoleWindowOpen);
                    }
                }
                ImGui.EndMenu();
            }

            if(ImGui.BeginMenu("Help"))
            {
                if(ImGui.MenuItem("About"))
                {
                    openAboutPopup = true;
                }


                ImGui.EndMenu();
            }

            ImGui.EndMainMenuBar();
        }

        if(openAboutPopup)
        {
            ImGui.OpenPopup("About Vault");
        }

        ProcessAboutPopup();
    }

    private void ProcessAboutPopup()
    {
        var center = new Vector2(_parentWindow.Width / 2.0f, _parentWindow.Height / 2.0f);
        ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));

        var dummy = true;
        if(ImGui.BeginPopupModal("About Vault", ref dummy,
               ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse))
        {
            var version = GetType().Assembly.GetName().Version;
            var versionString = "v(Unknown)";

            if(version != null)
            {
                versionString = version.ToString();
            }

            ImGui.Text($"Vault - {versionString}");
            ImGui.Separator();
            ImGui.Text("By Chris Butler");
            ImGui.Text("Vault is licensed under the MIT License, see LICENSE for more information.");
            ImGui.Dummy(new Vector2(0.0f, 10.0f));
            ImGui.Dummy(new Vector2(0.0f, 0.0f));

            ImGui.SameLine(ImGui.GetWindowWidth() * 0.5f - 40.0f);

            if(ImGui.Button("OK", new Vector2(80, 0)))
            {
                ImGui.CloseCurrentPopup();
            }

            ImGui.SetItemDefaultFocus();

            ImGui.EndPopup();
        }
    }
}