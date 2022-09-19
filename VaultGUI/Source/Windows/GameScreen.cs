using System.Numerics;
using ImGuiNET;

namespace Vault;

public class GameScreen : IImguiGuiWindow
{
    private const int SCREEN_PADDING = 16;

    private readonly ITextureManager _textureManager;
    private readonly IImguiWindowManager _imguiWindowManager;
    private readonly IGuiApplication _guiApplication;

    private bool _pixelPerfectScaling = true;
    private bool _autoScale = true;
    private float _currentScale = 1.0f;
    
    private Texture2D? _textureToShowOnScreen;
    private readonly Texture2D _testCardTexture;

    private bool _switchFullScreenMode;
    
    private Texture2D _textureToDraw => _textureToShowOnScreen ?? _testCardTexture;
    
    public string CustomWindowID => "Screen";
    public string WindowTitle => $"Screen ({_textureToDraw.Width}x{_textureToDraw.Height}) - {_currentScale * 100.0f:0}%";
    public ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.HorizontalScrollbar | ImGuiWindowFlags.MenuBar;

    public GameScreen()
    {
        _textureManager = SubsystemController.GetSubsystem<ITextureManager>();
        _imguiWindowManager = SubsystemController.GetSubsystem<IImguiWindowManager>();
        _guiApplication = SubsystemController.GetSubsystem<IGuiApplication>();
        
        _testCardTexture = _textureManager.LoadTextureFromDisk(@".\Assets\TestCard.png");
    }
    
    public void SetTextureToShowOnScreen(Texture2D texture)
    {
        _textureToShowOnScreen = texture;
    }

    public void OnUpdate()
    {
        //CB: Switch to full screen mode outside draw loop otherwise we get crashes
        if(_switchFullScreenMode)
        {
            _switchFullScreenMode = false;
            if(_imguiWindowManager.GetFullscreenWindow() == this)
            {
                _imguiWindowManager.ClearFullscreenWindow();
                _guiApplication.SetApplicationWindowMode(IGuiApplication.ApplicationWindowMode.Normal);
                        
            }
            else
            {
                _imguiWindowManager.SetWindowAsFullscreen(this);
                _guiApplication.SetApplicationWindowMode(IGuiApplication.ApplicationWindowMode.BorderlessFullScreen);
            }
        }
    }
    
    public void OnBeforeDrawImguiWindow()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, new Vector2(200, 200));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, 0));
    }
    
    public void OnDrawImGuiWindowContent()
    {
        var contentRectMin = ImGui.GetWindowContentRegionMin();
        var contentRectMax = ImGui.GetWindowContentRegionMax();
        
        CheckForFullScreenDoubleClick(contentRectMin, contentRectMax);
        
        DrawScreenTexture(contentRectMax, contentRectMin, _textureToDraw);

        //CB: Pop for Style vars done in OnBeforeDrawImguiWindow here - We need them popped before we do the menu
        ImGui.PopStyleVar(3);
 
        DrawWindowMenuBar();
    }

    private void DrawScreenTexture(Vector2 contentRectMax, Vector2 contentRectMin, Texture2D textureToDraw)
    {
        var width = contentRectMax.X - contentRectMin.X;
        var height = contentRectMax.Y - contentRectMin.Y;

        if(_autoScale)
        {
            var widthMul = (width - SCREEN_PADDING * 2) / textureToDraw.Width;
            var heightMul = (height - SCREEN_PADDING * 2) / textureToDraw.Height;

            _currentScale = Math.Min(widthMul, heightMul);
        }

        if(_pixelPerfectScaling)
        {
            _currentScale = RoundZoomToNearestPixelPerfectSize(_currentScale);
        }

        //Calculate and draw screen texture
        var imageSize = new Vector2(textureToDraw.Width * _currentScale, textureToDraw.Height * _currentScale);

        var widthMargin = Math.Max(width - imageSize.X, SCREEN_PADDING * 2);
        var heightMargin = Math.Max(height - imageSize.Y, SCREEN_PADDING * 2);

        var startX = contentRectMin.X + ImGui.GetScrollX() + widthMargin * 0.5f;
        var startY = contentRectMin.Y + ImGui.GetScrollY() + heightMargin * 0.5f;

        ImGui.SetCursorPos(new Vector2(startX, startY));
        var imguiTextureRef = _textureManager.GetOrCreateImGuiTextureRefForTexture(textureToDraw);
        
        ImGui.Image(imguiTextureRef.ImGuiRef, imageSize);

        //Add dummy for right/bottom padding so scroll bars appear at correct point
        ImGui.Dummy(new Vector2(imageSize.X + SCREEN_PADDING * 2.0f, SCREEN_PADDING));
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

            ImGui.SetCursorPosX(ImGui.GetWindowWidth() - 95);

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

    public void OnAfterDrawImGuiWindow()
    {
        //Nothing to do 
    }

    public void Dispose()
    {
        //Nothing to do
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