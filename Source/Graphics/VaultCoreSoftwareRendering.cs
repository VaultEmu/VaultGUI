using VaultCore.Rendering;

namespace Vault;

public class VaultCoreSoftwareRendering : ISoftwareRendering
{
    private readonly TextureManager _textureManager;
    private readonly VaultGui _parentGuiApplication;
    private readonly ImGuiUiManager _imGuiManager;
    
    private Texture2D? _renderOutput;
    private GameScreen? _outputGameScreenWindow;

    public VaultCoreSoftwareRendering(TextureManager textureManager, ImGuiUiManager imGuiManager, VaultGui parentGuiApplication)
    {
        _textureManager = textureManager;
        _imGuiManager = imGuiManager;
        _parentGuiApplication = parentGuiApplication;
    }
    
    public void OnCoreAcquiresFeature(Type coreType, Type featureType, List<Type> allCoreFeaturesNeeded)
    {
        //Add gamescreen window to show output
        _outputGameScreenWindow = new GameScreen(_textureManager, _imGuiManager.ImGuiWindowManager, _parentGuiApplication);
        _imGuiManager.ImGuiWindowManager.RegisterWindow(_outputGameScreenWindow);
        
        if(_renderOutput != null)
        {
            _outputGameScreenWindow.SetTextureToShowOnScreen(_renderOutput);
        }
    }

    public void OnCoreReleasesFeature(Type coreType, Type featureType, List<Type> allCoreFeaturesNeeded)
    {
        if(_outputGameScreenWindow != null)
        {
            _imGuiManager.ImGuiWindowManager.UnregisterWindow(_outputGameScreenWindow);
            _outputGameScreenWindow.Dispose();
            _outputGameScreenWindow = null;
        }
    }

    public void OnFrameReadyToDisplay(PixelData pixelData)
    {
        //Init the output texture as needed
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
            
            if(_outputGameScreenWindow != null)
            {
                _outputGameScreenWindow.SetTextureToShowOnScreen(_renderOutput);
            }
        }
        
        //and just copy the software renderer output into the texture for displaying by the gamescreen
        _renderOutput.StartWritingPixelsToTexture();
        _renderOutput.WritePixelData(pixelData.Data, pixelData.Width, pixelData.Height, 0, 0);
        _renderOutput.FinishWritingPixelsToTexture();
    }
}