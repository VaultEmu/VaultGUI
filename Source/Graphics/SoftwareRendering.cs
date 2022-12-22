using VaultCore.ImGuiWindowsAPI;
using VaultCore.Rendering;

namespace Vault;

public class SoftwareRendering : ISoftwareRendering
{
    private readonly ITextureManager _textureManager;
    private Texture2D? _renderOutput;
    private GameScreen _gameScreen;

    public SoftwareRendering()
    {
        _textureManager = GlobalFeatures.Resolver.GetFeature<ITextureManager>() ?? throw new InvalidOperationException("Unable to acquire Texture Manager");
        
        var windowManager = GlobalFeatures.Resolver.GetFeature<IImGuiWindowManager>() ?? throw new InvalidOperationException("Unable to acquire IImGuiWindowManager");
        _gameScreen = windowManager.GetWindow<GameScreen>();
        
        if(_gameScreen == null)
        {
            throw new InvalidOperationException("Unable to get GameScreen");
        }
        
        GlobalFeatures.RegisterFeature(this);
    }

    public void OnFrameReadyToDisplay(PixelData pixelData)
    {
        if(_renderOutput == null || _renderOutput.Width != pixelData.Width || _renderOutput.Height != pixelData.Height)
        {
            if(_renderOutput != null)
            {
                _renderOutput.Dispose();
            }
            
            _renderOutput = _textureManager.CreateTexture(
                pixelData.Width,
                pixelData.Height,
                TextureFormat.RGBA_32,
                false,
                true);
        }
        
        _renderOutput.StartWritingPixelsToTexture();
        _renderOutput.WritePixelData(pixelData.Data, pixelData.Width, pixelData.Height, 0, 0);
        _renderOutput.FinishWritingPixelsToTexture();
        
        _gameScreen.SetTextureToShowOnScreen(_renderOutput);
    }
}