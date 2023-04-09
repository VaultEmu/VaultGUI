using System.Numerics;
using ImGuiNET;
using VaultCore.ImGuiWindowsAPI;
using VaultCore.Rendering;
using Veldrid;

namespace Vault;

public class RendererOutputWindow : ImGuiWindow
{
    private const int SCREEN_PADDING = 16;

    private readonly TextureManager _textureManager;
    private readonly ImGuiWindowManager _imGuiWindowManager;
    private readonly VaultGui _guiApplication;
    private readonly string _windowTitle;

    private bool _pixelPerfectScaling;
    private bool _autoScale = true;
    private float _currentScale = 1.0f;

    private Texture2D? _textureToShowOnScreen;
    private readonly Texture2D _testCardTexture;

    private bool _switchFullScreenMode;

    private Texture2D _textureToDraw => _textureToShowOnScreen ?? _testCardTexture;

    private bool _isShowingTestCard => _textureToDraw == _testCardTexture;


    public override string WindowTitle
    {
        get
        {
            if(_isShowingTestCard)
            {
                return _windowTitle;
            }

            //Show resolution and scale in title
            return $"{_windowTitle} ({_textureToDraw.Width}x{_textureToDraw.Height}) - {_currentScale * 100.0f:0}%";
        }
    }

    //CB: override to send raw window title as WindowTitle is dynamic
    public override string WindowID => _windowTitle;

    public override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.HorizontalScrollbar | ImGuiWindowFlags.MenuBar;

    public override bool WindowAlwaysOpen => true;

    public override WindowMenuItem? WindowsMenuItemData => null;

    public RendererOutputWindow(string windowName, TextureManager textureManager, ImGuiWindowManager imGuiWindowManager, VaultGui guiApplication)
    {
        _textureManager = textureManager;
        _imGuiWindowManager = imGuiWindowManager;
        _guiApplication = guiApplication;
        _windowTitle = windowName;
        _testCardTexture = _textureManager.LoadTextureFromDisk(@".\Assets\TestCard.png", false);
    }

    public void SetTextureToShowOnScreen(Texture2D? texture)
    {
        _textureToShowOnScreen = texture;
    }

    public override void OnUpdate()
    {
        //CB: Switch to full screen mode outside draw loop otherwise we get crashes
        if(_switchFullScreenMode)
        {
            _switchFullScreenMode = false;
            if(_imGuiWindowManager.GetFullscreenWindow() == this)
            {
                _imGuiWindowManager.ClearFullscreenWindow();
                _guiApplication.SetApplicationWindowState(WindowState.Normal);
            }
            else
            {
                _imGuiWindowManager.SetWindowAsFullscreen(this);
                _guiApplication.SetApplicationWindowState(WindowState.BorderlessFullScreen);
            }
        }
    }

    public override void OnBeforeDrawImGuiWindow()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, new Vector2(350, 350));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, 0));
        ImGui.PushStyleColor(ImGuiCol.WindowBg, ImGui.GetColorU32(new Vector4(0.2f, 0.2f, 0.2f, 1.0f)));
    }

    public override void OnDrawImGuiWindowContent()
    {
        var contentRectMin = ImGui.GetWindowContentRegionMin();
        var contentRectMax = ImGui.GetWindowContentRegionMax();

        CheckForFullScreenDoubleClick(contentRectMin, contentRectMax);

        DrawScreenTexture(contentRectMax, contentRectMin, _textureToDraw);

        //CB: Pop for Style vars done in OnBeforeDrawImGuiWindow here - We need them popped before we do the menu
        ImGui.PopStyleVar(3);

        DrawWindowMenuBar();
    }


    public override void OnAfterDrawImGuiWindow()
    {
        ImGui.PopStyleColor();
    }

    private void DrawScreenTexture(Vector2 contentRectMax, Vector2 contentRectMin, Texture2D textureToDraw)
    {
        var width = contentRectMax.X - contentRectMin.X;
        var height = contentRectMax.Y - contentRectMin.Y;

        var isFullscreen = _imGuiWindowManager.GetFullscreenWindow() == this;

        var padding = SCREEN_PADDING;

        if(isFullscreen)
        {
            padding = 0;
        }

        if(_autoScale)
        {
            var widthMul = (width - padding * 2) / textureToDraw.Width;
            var heightMul = (height - padding * 2) / textureToDraw.Height;

            _currentScale = Math.Min(widthMul, heightMul);
        }

        if(_pixelPerfectScaling)
        {
            _currentScale = RoundZoomToNearestPixelPerfectSize(_currentScale);
        }

        if(_isShowingTestCard)
        {
            //Clamp Testcard to max size4
            _currentScale = Math.Min(_currentScale, 1.0f);
        }

        //Calculate and draw screen texture
        var imageSize = new Vector2(textureToDraw.Width * _currentScale, textureToDraw.Height * _currentScale);

        var widthMargin = Math.Max(width - imageSize.X, padding * 2);
        var heightMargin = Math.Max(height - imageSize.Y, padding * 2);

        var startX = contentRectMin.X + ImGui.GetScrollX() + widthMargin * 0.5f;
        var startY = contentRectMin.Y + ImGui.GetScrollY() + heightMargin * 0.5f;

        ImGui.SetCursorPos(new Vector2(startX, startY));
        var imguiTextureRef = _textureManager.GetOrCreateImGuiTextureRefForTexture(textureToDraw);

        ImGui.Image(imguiTextureRef.ImGuiRef, imageSize);

        //Add dummy for right/bottom padding so scroll bars appear at correct point
        ImGui.Dummy(new Vector2(imageSize.X + padding * 2.0f, padding));
    }

    private void CheckForFullScreenDoubleClick(Vector2 contentRectMin, Vector2 contentRectMax)
    {
        if(ImGui.IsWindowHovered())
        {
            var clickAreaRectMin = ImGui.GetWindowPos() + contentRectMin;
            var clickAreaRectMax = ImGui.GetWindowPos() + contentRectMax;

            if(ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
            {
                if(ImGui.IsMouseHoveringRect(clickAreaRectMin, clickAreaRectMax, true))
                {
                    _switchFullScreenMode = true;
                }
            }
        }
    }

    private void DrawWindowMenuBar()
    {
        //Menu Bar
        if(ImGui.BeginMenuBar())
        {
            ImGui.SetCursorPosX(ImGui.GetStyle().WindowPadding.X);
            if(ImGui.BeginMenu("Display Options..."))
            {
                if(ImGui.BeginMenu("Scaling"))
                {
                    ImGui.MenuItem("Auto Scale", null, ref _autoScale);
                    ImGui.MenuItem("Pixel Perfect Scaling", null, ref _pixelPerfectScaling);
                    ImGui.EndMenu();
                }

                ImGui.EndMenu();
            }

            ImGui.SetCursorPosX(ImGui.GetWindowWidth() - 120);

            if(_autoScale)
            {
                ImGui.Dummy(new Vector2(20, 0));
            }
            else
            {
                if(ImGui.Button("-", new Vector2(20, 0)))
                {
                    if(_pixelPerfectScaling)
                    {
                        _currentScale *= 0.5f;
                    }
                    else
                    {
                        _currentScale -= 0.1f;
                        _currentScale = MathF.Round(_currentScale, 1, MidpointRounding.ToEven);
                    }

                    _currentScale = MathF.Max(0.1f, _currentScale);
                }
            }

            ImGui.Text($"{_currentScale * 100.0f:0}%%");

            if(_autoScale)
            {
                ImGui.Dummy(new Vector2(20, 0));
            }
            else
            {
                if(ImGui.Button("+", new Vector2(20, 0)))
                {
                    if(_pixelPerfectScaling)
                    {
                        _currentScale *= 2.0f;
                    }
                    else
                    {
                        _currentScale += 0.1f;
                        _currentScale = MathF.Round(_currentScale, 1, MidpointRounding.ToEven);
                    }

                    _currentScale = MathF.Min(4.0f, _currentScale);
                }
            }

            ImGui.EndMenuBar();
        }
    }

    private float RoundZoomToNearestPixelPerfectSize(float currentScale)
    {
        if(currentScale >= 4.0f) //400%
        {
            currentScale = 4.0f;
        }
        else if(currentScale >= 2.0f) //200%
        {
            currentScale = 2.0f;
        }
        else if(currentScale >= 1.0f) //100%
        {
            currentScale = 1.0f;
        }
        else if(currentScale >= 0.5f) //50%
        {
            currentScale = 0.5f;
        }
        else if(currentScale >= 0.25f) //25%
        {
            currentScale = 0.25f;
        }
        else //12.5%
        {
            currentScale = 0.125f;
        }

        return currentScale;
    }
}