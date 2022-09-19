namespace Vault;

//Simple renderer that can be used to set and read pixels from a 'backbuffer' which can be then used as a texture
public class PixelRenderer : IDisposable
{
    private const uint DEFAULT_INITIAL_BACKBUFFER_WIDTH = 1024;
    private const uint DEFAULT_INITIAL_BACKBUFFER_HEIGHT = 768;
    
    public uint BackBufferWidth { get; private set; }
    public uint BackBufferHeight { get; private set; }
    
    public Texture2D BackBufferTexture => _backbufferTexture;
    
    private TexturePixel[] _screenBuffer = null!;
    private Texture2D _backbufferTexture = null!;
    
    private readonly ITextureManager _textureManager;
    
    public PixelRenderer(
        ITextureManager textureManager,
        uint backBufferWidth = DEFAULT_INITIAL_BACKBUFFER_WIDTH,
        uint backBufferHeight = DEFAULT_INITIAL_BACKBUFFER_HEIGHT)
    {
        _textureManager = textureManager;
        InitialiseScreenBufferData(backBufferWidth, backBufferHeight, true);
    }
    
    //Changes the backbuffer size (Buffer will be reset from this call)
    public void SetScreenBackBufferSize(uint width, uint height)
    {
        InitialiseScreenBufferData(width, height, false);
    }
    
    //Sets a pixel in the backbuffer
    public void SetPixel(TexturePixel pixel, uint x, uint y)
    {
        if(x >= BackBufferWidth)
        {
            throw new ArgumentException("x should be less then BackBufferWidth");
        }

        if(y >= BackBufferHeight)
        {
            throw new ArgumentException("y should be less then BackBufferHeight");
        }

        var index = x + y * BackBufferWidth;

        _screenBuffer[index] = pixel;
    }

    //Sets a Region of pixels in the backbuffer
    public void SetPixels(TexturePixel[] pixels, uint x, uint y, uint width, uint height)
    {
        if(x >= BackBufferWidth)
        {
            throw new ArgumentException("x should be less then BackBufferWidth");
        }

        if(y >= BackBufferHeight)
        {
            throw new ArgumentException("y should be less then BackBufferHeight");
        }

        if(x + width > BackBufferWidth)
        {
            throw new ArgumentException("x + width should be less then BackBufferWidth");
        }

        if(y + height > BackBufferHeight)
        {
            throw new ArgumentException("y + height should be less then BackBufferHeight");
        }

        TexturePixelUtils.DoPixelDataCopy(pixels, _screenBuffer, x, y, width, height, BackBufferWidth, BackBufferHeight);
    }
    
    //Gets a pixel set in the backbuffer
    public TexturePixel GetPixel(uint x, uint y)
    {
        if(x >= BackBufferWidth)
        {
            throw new ArgumentException("x should be less then BackBufferWidth");
        }

        if(y >= BackBufferHeight)
        {
            throw new ArgumentException("y should be less then BackBufferHeight");
        }

        var index = x + y * BackBufferWidth;

        return _screenBuffer[index];
    }
    
    //Call this once you have finished setting pixels to flush the changes to the backbuffer texture
    public void FlushPixelChanges()
    {
        //Blit window data to texture
        _backbufferTexture.StartWritingPixelsToTexture();
        _backbufferTexture.WritePixelData(_screenBuffer, 0, 0, BackBufferWidth, BackBufferHeight);
        _backbufferTexture.FinishWritingPixelsToTexture();
    }
    
    public void Dispose()
    {
        _backbufferTexture.Dispose();
    }
    
    private void InitialiseScreenBufferData(uint width, uint height, bool calledFromConstructor)
    {
        //CB: will be null when called from constructor, always valid otherwise
        if(calledFromConstructor == false)
        {
            _backbufferTexture.Dispose();
        }

        _backbufferTexture = _textureManager.CreateTexture(
            width,
            height,
            TextureFormat.RGBA_32,
            false,
            true);

        _screenBuffer = new TexturePixel[width * height];

        BackBufferWidth = width;
        BackBufferHeight = height;
    }
}