namespace Vault;

public abstract class Texture2D : IDisposable
{
    //Width of the texture
    public abstract uint Width { get; }
    
    //Height of the texture
    public abstract uint Height { get; }
    
    //The format of the texture
    public abstract TextureFormat Format { get; }
    
    //Returns true if this texture is currently having date written to it on the CPU
    public abstract bool IsWritingPixelsToTexture { get; }
    
    //Copies the data from this texture to another
    public abstract void CopyToTexture(
        Texture2D destination,
        uint srcX,
        uint srcY,
        uint srcMipLevel,
        uint dstX,
        uint dstY,
        uint dstMipLevel,
        uint width,
        uint height);

    //Updating Textures
    //Make sure to wrap your calls with StartWritingPixelsToTexture and FinishWritingPixelsToTexture.
    //If doing often then consider creating with setupForFastCpuWrite for better performance
    
    //Call to place the texture in a state ready for updating
    public abstract void StartWritingPixelsToTexture();
    
    //Call when finished updating texture data
    public abstract void FinishWritingPixelsToTexture();
    
    //Updates a textures data from an array of TexturePixel data. Make sure to call StartWritingPixelsToTexture first
    public abstract void WritePixelData(
        TexturePixel[] pixelData,
        uint x, uint y,
        uint width, uint height,
        uint mipLevel = 0);
    
    protected abstract void Dispose(bool disposing);

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}