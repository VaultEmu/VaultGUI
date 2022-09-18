using System.Numerics;
using ImGuiNET;

namespace Vault;

public class GameScreen : IImguiGuiWindow
{
    private const uint DEFAULT_INITIAL_WIDTH = 256;
    private const uint DEFAULT_INITIAL_HEIGHT = 256;
    
    private const int SCREEN_PADDING = 16;

    private Texture2D _screenTexture = null!;
    private ImGuiTextureRef _screenTextureImguiRef = null!;
    
    private TexturePixel[] _screenBuffer = null!;

    private readonly TextureManager _textureManager;
    private readonly ITimeProvider _timeProvider;
    private readonly IImguiWindowManager _imguiWindowManager;
    private readonly IGuiApplication _guiApplication;

    private bool _pixelPerfectScaling = true;
    private bool _autoScale = true;
    private float _currentScale = 1.0f;

    private readonly TexturePixel[] _splatPixelsTestA = new TexturePixel[256 * 64];
    private readonly TexturePixel[] _splatPixelsTestB = new TexturePixel[80 * 80];
    private readonly TexturePixel[] _splatPixelsTestLoaded;
    private readonly uint _splatPixelsTestLoadedWidth;
    private readonly uint _splatPixelsTestLoadedHeight;
    
    private bool _switchFullScreenMode;

    public uint ScreenBackBufferWidth { get; private set; }
    public uint ScreenBackBufferHeight { get; private set; }
    
    public string WindowName => "Screen";
    public ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.HorizontalScrollbar | ImGuiWindowFlags.MenuBar;

    public GameScreen(uint backBufferWidth = DEFAULT_INITIAL_WIDTH, uint backBufferHeight = DEFAULT_INITIAL_HEIGHT)
    {
        _textureManager = SubsystemController.GetSubsystem<TextureManager>();
        _timeProvider = SubsystemController.GetSubsystem<ITimeProvider>();
        _imguiWindowManager = SubsystemController.GetSubsystem<IImguiWindowManager>();
        _guiApplication = SubsystemController.GetSubsystem<IGuiApplication>();
        
        InitialiseScreenBufferData(backBufferWidth, backBufferHeight, true);

        for (var index = 0; index < _splatPixelsTestA.Length; ++index)
        {
            _splatPixelsTestA[index] = new TexturePixel(0, 255, 255);
        }

        for (var index = 0; index < _splatPixelsTestB.Length; ++index)
        {
            _splatPixelsTestB[index] = new TexturePixel(255, 255, 0);
        }

        _splatPixelsTestLoaded = _textureManager.LoadTextureFromDiskAsPixelArray(@".\Assets\Debug.png",
            out _splatPixelsTestLoadedWidth, out _splatPixelsTestLoadedHeight);
        
        //Temp Color update test
        var timeSinceStartup = _timeProvider.TimeSinceStartup;
        for (uint indexY = 0; indexY < ScreenBackBufferHeight; ++indexY)
        {
            for (uint indexX = 0; indexX < ScreenBackBufferWidth; ++indexX)
            {
                var offsetR = timeSinceStartup * 0.1f % 1.0f;
                var offsetG = timeSinceStartup * 0.2f % 1.0f;

                var r = (byte)(255 * (indexX / (float)ScreenBackBufferWidth - offsetR));
                var g = (byte)(255 * (indexY / (float)ScreenBackBufferHeight - offsetG));

                SetPixel(new TexturePixel
                {
                    R = r,
                    G = g,
                    B = 0,
                    A = 255
                }, indexX, indexY);
            }
        }

        SetPixels(_splatPixelsTestA, 0, 20, 256, 64);
        SetPixels(_splatPixelsTestB, 124, 163, 80, 80);
        SetPixels(_splatPixelsTestLoaded, 10, 256 - (_splatPixelsTestLoadedHeight + 10), _splatPixelsTestLoadedWidth, _splatPixelsTestLoadedHeight);
    }

    public void SetScreenBackBufferSize(uint width, uint height)
    {
        InitialiseScreenBufferData(width, height, false);
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
        //Blit window data to texture
        _screenTexture.StartWritingPixelsToTexture();
        _screenTexture.WritePixelData(_screenBuffer, 0, 0, ScreenBackBufferWidth, ScreenBackBufferHeight);
        _screenTexture.FinishWritingPixelsToTexture();
        
        ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, new Vector2(200, 200));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, 0));
    }
    
    public void OnImguiGui()
    {
        var contentRectMin = ImGui.GetWindowContentRegionMin();
        var contentRectMax = ImGui.GetWindowContentRegionMax();
        
        CheckForFullScreenDoubleClick(contentRectMin, contentRectMax);
        
        DrawScreenTexture(contentRectMax, contentRectMin);

        //CB: Pop for Style vars done in OnBeforeDrawImguiWindow here - We need them popped before we do the menu
        ImGui.PopStyleVar(3);
 
        DrawWindowMenuBar();
    }

    private void DrawScreenTexture(Vector2 contentRectMax, Vector2 contentRectMin)
    {
        var width = contentRectMax.X - contentRectMin.X;
        var height = contentRectMax.Y - contentRectMin.Y;

        if(_autoScale)
        {
            var widthMul = (width - SCREEN_PADDING * 2) / ScreenBackBufferWidth;
            var heightMul = (height - SCREEN_PADDING * 2) / ScreenBackBufferHeight;

            _currentScale = Math.Min(widthMul, heightMul);
        }

        if(_pixelPerfectScaling)
        {
            _currentScale = RoundZoomToNearestPixelPerfectSize(_currentScale);
        }

        //Calculate and draw screen texture
        var imageSize = new Vector2(ScreenBackBufferWidth * _currentScale, ScreenBackBufferHeight * _currentScale);

        var widthMargin = Math.Max(width - imageSize.X, SCREEN_PADDING * 2);
        var heightMargin = Math.Max(height - imageSize.Y, SCREEN_PADDING * 2);

        var startX = contentRectMin.X + ImGui.GetScrollX() + widthMargin * 0.5f;
        var startY = contentRectMin.Y + ImGui.GetScrollY() + heightMargin * 0.5f;

        ImGui.SetCursorPos(new Vector2(startX, startY));
        ImGui.Image(_screenTextureImguiRef.ImGuiRef, imageSize);

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

    public void OnAfterDrawImguiWindow()
    {
        
    }

    public void SetPixel(TexturePixel pixel, uint x, uint y)
    {
        if(x >= ScreenBackBufferWidth)
        {
            throw new ArgumentException("x should be less then ScreenBackBufferWidth");
        }

        if(y >= ScreenBackBufferHeight)
        {
            throw new ArgumentException("y should be less then ScreenBackBufferHeight");
        }

        var index = x + y * ScreenBackBufferWidth;

        _screenBuffer[index] = pixel;
    }

    public void SetPixels(TexturePixel[] pixels, uint x, uint y, uint width, uint height)
    {
        if(x >= ScreenBackBufferWidth)
        {
            throw new ArgumentException("x should be less then ScreenBackBufferWidth");
        }

        if(y >= ScreenBackBufferHeight)
        {
            throw new ArgumentException("y should be less then ScreenBackBufferHeight");
        }

        if(x + width > ScreenBackBufferWidth)
        {
            throw new ArgumentException("x + width should be less then ScreenBackBufferWidth");
        }

        if(y + height > ScreenBackBufferHeight)
        {
            throw new ArgumentException("y + height should be less then ScreenBackBufferHeight");
        }

        TexturePixelUtils.DoPixelDataCopy(pixels, _screenBuffer, x, y, width, height, ScreenBackBufferWidth, ScreenBackBufferHeight);
    }

    public void Dispose()
    {
        _screenTexture.Dispose();
    }

    private void InitialiseScreenBufferData(uint width, uint height, bool calledFromConstructor)
    {
        //CB: will be null when called from constructor, always valid otherwise
        if(calledFromConstructor == false)
        {
            _screenTexture.Dispose();
        }

        _screenTexture = _textureManager.CreateTexture(
            width,
            height,
            TextureFormat.RGBA_32,
            false,
            true);
        
        _screenTextureImguiRef = _textureManager.GetImGuiTextureRefForTexture(_screenTexture);

        _screenBuffer = new TexturePixel[width * height];

        ScreenBackBufferWidth = width;
        ScreenBackBufferHeight = height;
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