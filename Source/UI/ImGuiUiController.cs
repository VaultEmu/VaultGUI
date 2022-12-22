using System.Numerics;
using ImGuiNET;
using VaultCore.CoreAPI;
using Veldrid;
using Veldrid.Sdl2;
using VaultCore.Rendering;
using VaultCore.ImGuiWindowsAPI;

namespace Vault;


public class ImGuiUiController : IDisposable
{
    private readonly ImGuiRenderer _imGuiRenderer;
    private readonly GraphicsDevice _parentGraphicsDevice;
    private readonly Sdl2Window _parentWindow;
    private readonly CommandList _uiCommandList;
    private readonly ImGuiWindowManager _imGuiWindowManager;
    
    private readonly TextureManager _textureManager;
    private readonly Texture2D _backgroundTexture;
    private readonly ImGuiTextureRef _backgroundTextureImGuiRef;

    public ImGuiUiController(GraphicsDevice graphicsDevice, Sdl2Window window)
    {
        _parentGraphicsDevice = graphicsDevice;
        _parentWindow = window;
        _imGuiRenderer = new ImGuiRenderer(graphicsDevice, 
            graphicsDevice.MainSwapchain.Framebuffer.OutputDescription,
            window.Width, window.Height,
            DpiAwareUtils.GetDPIScale(window));
        _imGuiWindowManager = new ImGuiWindowManager();
        
        _uiCommandList = _parentGraphicsDevice.ResourceFactory.CreateCommandList();
        _parentWindow.Resized += OnWindowOnResized;

        ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.DockingEnable;
        
        _textureManager = new TextureManager(_parentGraphicsDevice, _imGuiRenderer);
        
        _backgroundTexture = _textureManager.LoadTextureFromDisk(@".\Assets\VaultBg.png");
        _backgroundTextureImGuiRef = _textureManager.GetOrCreateImGuiTextureRefForTexture(_backgroundTexture);

        AddDefaultWindows();
    }
    
    public void UpdateUi(InputSnapshot snapshot, float frameDeltaTime)
    {
        _imGuiRenderer.Update(frameDeltaTime, snapshot);
        
        _imGuiWindowManager.UpdateWindows();
    }

    public void RenderUi()
    {
        DrawBackgroundImage();

        //Create Dockable Workspace
        ImGui.DockSpaceOverViewport(ImGui.GetMainViewport(), ImGuiDockNodeFlags.PassthruCentralNode);

        DrawMenuBar();
        
        _imGuiWindowManager.DrawWindows();

        CreateAndSubmitRenderCommands();
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
        _imGuiWindowManager.RegisterWindow(new GameScreen());
    }

    private void CreateAndSubmitRenderCommands()
    {
        _uiCommandList.Begin();
        _uiCommandList.SetFramebuffer(_parentGraphicsDevice.MainSwapchain.Framebuffer);
        _imGuiRenderer.Render(_parentGraphicsDevice, _uiCommandList);
        _uiCommandList.End();

        _parentGraphicsDevice.SubmitCommands(_uiCommandList);
    }

    public void Dispose()
    {
        _imGuiRenderer.Dispose();
        _uiCommandList.Dispose();
        _imGuiWindowManager.Dispose();
        _textureManager.Dispose();

        //Dont dispose _parentGraphicsDevice, we do not own it, only have a reference to it
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

            ImGui.SameLine(ImGui.GetWindowWidth() - 90);

            if(ImGui.Button("OK", new Vector2(80, 0)))
            {
                ImGui.CloseCurrentPopup();
            }

            ImGui.SetItemDefaultFocus();

            ImGui.EndPopup();
        }
    }

    private void OnWindowOnResized()
    {
        _imGuiRenderer.WindowResized(_parentWindow.Width, _parentWindow.Height);
    }
}