using System.Numerics;
using ImGuiNET;
using Veldrid;
using Veldrid.Sdl2;

namespace Vault.UI;

public class UIController : IDisposable
{
    private readonly ImGuiRenderer _imGuiRenderer;
    private readonly GraphicsDevice _parentGraphicsDevice;
    private readonly Sdl2Window _parentWindow;
    private readonly CommandList _uiCommandList;
    
    const int SCREEN_WIDTH = 64;
    const int SCREEN_HEIGHT = 32;

    private readonly Texture _screenTexture;
    private readonly IntPtr _screenTextureImGuiRef;

    public UIController(GraphicsDevice graphicsDevice, Sdl2Window window)
    {
        _parentGraphicsDevice = graphicsDevice;
        _parentWindow = window;
        _imGuiRenderer = new ImGuiRenderer(graphicsDevice, graphicsDevice.MainSwapchain.Framebuffer.OutputDescription, window.Width, window.Height);

        _uiCommandList = _parentGraphicsDevice.ResourceFactory.CreateCommandList();
        _parentWindow.Resized += OnWindowOnResized;

        ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.DockingEnable;
        
        _screenTexture =  _parentGraphicsDevice.ResourceFactory.CreateTexture(TextureDescription.Texture2D(
            SCREEN_WIDTH,
            SCREEN_HEIGHT,
            1,
            1,
            PixelFormat.R8_G8_B8_A8_UNorm,
            TextureUsage.Sampled));
        
        var textureData = new byte[SCREEN_WIDTH * SCREEN_HEIGHT * 4];
        
        
        for(var indexY = 0; indexY < SCREEN_HEIGHT; ++indexY)
        {
            for(var indexX = 0; indexX < SCREEN_WIDTH; ++indexX)
            {
                var index = indexX * 4 + indexY * (SCREEN_WIDTH * 4);
                
                textureData[index + 0] = (byte)(255 * (indexX / (float)SCREEN_WIDTH));;
                textureData[index + 1] = (byte)(255 * (indexY / (float)SCREEN_HEIGHT));;
                textureData[index + 2] = 0;
                textureData[index + 3] = 255;
            }
        }
        
        _parentGraphicsDevice.UpdateTexture(
            _screenTexture,
            textureData,
            0,
            0,
            0,
            SCREEN_WIDTH,
            SCREEN_HEIGHT,
            1,
            0,
            0);
        
        _screenTextureImGuiRef = _imGuiRenderer.GetOrCreateImGuiBinding(_parentGraphicsDevice.ResourceFactory, _screenTexture);
    }

    public void UpdateUi(float deltaTime, InputSnapshot snapshot)
    {
        _imGuiRenderer.Update(deltaTime, snapshot);
    }


    public void RenderUi()
    {
        //Create Dockable Workspace
        ImGui.DockSpaceOverViewport(ImGui.GetMainViewport());

        DrawMenuBar();

        DrawTempOutputWindow();

        CreateAndSubmitRenderCommands();
    }

    private void DrawTempOutputWindow()
    {
        const int SCREEN_PADDING = 16;
        var screenSize = new Vector2(SCREEN_WIDTH, SCREEN_HEIGHT);
        
        ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, new Vector2(128, 128));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0,0));
        if(ImGui.Begin("Screen", ImGuiWindowFlags.NoCollapse))
        {
            Vector2 vMin = ImGui.GetWindowContentRegionMin();
            Vector2 vMax = ImGui.GetWindowContentRegionMax();
            
            var width = vMax.X - vMin.X;
            var height = vMax.Y - vMin.Y;

            var widthMul = Math.Max(1, (int)((width - SCREEN_PADDING * 2) / screenSize.X));
            var heightMul = Math.Max(1, (int)((height  - SCREEN_PADDING * 2) / screenSize.Y));
            
            var finalMul = Math.Min(widthMul, heightMul);
            
            var imageSize = new Vector2(screenSize.X * finalMul, screenSize.Y * finalMul);
            
            var widthMargin = width - imageSize.X;
            var heightMargin = height - imageSize.Y;
            
            ImGui.GetForegroundDrawList().AddRect(
                new Vector2(
                    ImGui.GetWindowPos().X + ImGui.GetWindowContentRegionMin().X, 
                    ImGui.GetWindowPos().Y + ImGui.GetWindowContentRegionMin().Y), 
                new Vector2(
                    ImGui.GetWindowPos().X + ImGui.GetWindowContentRegionMin().X + widthMargin * 0.5f, 
                    ImGui.GetWindowPos().Y + ImGui.GetWindowContentRegionMin().Y + heightMargin * 0.5f)
                , ImGui.GetColorU32(new Vector4(255, 0, 0, 255)));

            ImGui.SetCursorPos(new Vector2(vMin.X + widthMargin * 0.5f, vMin.Y + heightMargin * 0.5f));
            ImGui.Image(_screenTextureImGuiRef, imageSize);
            
            ImGui.End();
            
        }
        ImGui.PopStyleVar();
        ImGui.PopStyleVar();
        
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
        _screenTexture.Dispose();

        //Dont dispose _parentGraphicsDevice, we do not own it, only have a reference to it
    }

    private void DrawMenuBar()
    {
        bool openAboutPopup = false;
        if(ImGui.BeginMainMenuBar())
        {
            if(ImGui.BeginMenu("File"))
            {
                if(ImGui.MenuItem("Open", "CTRL+O")) { }

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