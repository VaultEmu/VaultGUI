using System.Numerics;
using ImGuiNET;
using VaultCore.ImGuiWindowsAPI;
using VaultCore.Rendering;
using Veldrid;
using Veldrid.Sdl2;

namespace Vault;

public class ImGuiUiManager : IDisposable
{
    private readonly Sdl2Window _parentWindow;
    private readonly ImGuiInput _ImGuiInput;
    private readonly Logger _logger;

    private readonly TextureManager _textureManager;
    private readonly Texture2D _backgroundTexture;
    private readonly ImGuiTextureRef _backgroundTextureImGuiRef;

    private readonly List<ImGuiWindow> _defaultWindows = new();

    public ImGuiWindowManager ImGuiWindowManager { get; }
    public ImGuiMenuManager ImGuiMenuManager { get; }

    public ImGuiUiManager(TextureManager textureManager, Sdl2Window window, Logger logger)
    {
        _textureManager = textureManager;
        _parentWindow = window;
        _logger = logger;
        _ImGuiInput = new ImGuiInput();
        
        ImGuiWindowManager = new ImGuiWindowManager();
        ImGuiMenuManager = new ImGuiMenuManager();

        ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.DockingEnable;

        _backgroundTexture = _textureManager.LoadTextureFromDisk(@".\Assets\VaultBg.png");
        _backgroundTextureImGuiRef = _textureManager.GetOrCreateImGuiTextureRefForTexture(_backgroundTexture);

        AddDefaultWindows();
        AddDefaultMenuItems();
    }

    public void Update(InputSnapshot snapshot, double deltaSeconds)
    {
        _ImGuiInput.Update(snapshot, deltaSeconds);
        ImGuiWindowManager.UpdateWindows();
    }

    public void GenerateImGuiRenderCalls()
    {
        DrawBackgroundImage();

        //Create Dockable Workspace
        ImGui.DockSpaceOverViewport(ImGui.GetMainViewport(), ImGuiDockNodeFlags.PassthruCentralNode);

        DrawMenuBar();

        ImGuiWindowManager.DrawWindows();
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
        _defaultWindows.Add(new ConsoleWindow(ImGuiMenuManager));

        foreach (var window in _defaultWindows)
        {
            ImGuiWindowManager.RegisterWindow(window);
        }
    }
    
    private void AddDefaultMenuItems()
    {
        ImGuiMenuManager.RegisterMenuItem(
            new ImGuiMenuItem(
                "File/Load Core",
                () => _logger.LogError("Load Core Menu Not Implemented Yet"),
                new ImGuiShortcut(ImGuiKey.O, ImGuiModFlags.Ctrl),
                null,
                -100000));
        
        ImGuiMenuManager.RegisterMenuItem(
            new ImGuiMenuItem(
                "File/Exit",
                () => _parentWindow.Close(),
                new ImGuiShortcut(ImGuiKey.F4, ImGuiModFlags.Alt),
                null,
                100000));
        
        ImGuiMenuManager.RegisterMenuItem(
            new ImGuiMenuItem(
                "Help/About",
                () => ImGui.OpenPopup("About Vault"),
                new ImGuiShortcut(ImGuiKey.F4, ImGuiModFlags.Alt),
                null,
                100000));
        
        ImGuiMenuManager.SetTopLevelMenuPriority("File", -1000000);
        ImGuiMenuManager.SetTopLevelMenuPriority("Help", 1000000);
    }

    public void Dispose()
    {
        foreach (var window in _defaultWindows)
        {
            ImGuiWindowManager.UnregisterWindow(window);
            window.Dispose();
        }
    }

    private void DrawMenuBar()
    {
        //Hide menu bar in full screen mode
        if(_parentWindow.WindowState == WindowState.FullScreen ||
           _parentWindow.WindowState == WindowState.BorderlessFullScreen)
        {
            return;
        }
        
        if(ImGui.BeginMainMenuBar())
        {
            ImGuiMenuManager.PopulateMainMenu();
            ImGui.EndMainMenuBar();
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