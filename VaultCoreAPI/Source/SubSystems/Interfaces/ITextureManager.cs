namespace Vault;

//Subsystem for creating and working with Textures
public interface ITextureManager : ISubsystem
{
    //Creates a texture for use with imgui
    //Use setupForFastCpuWrite if you plan to update the texture often
    public Texture2D CreateTexture(uint width, uint height, 
        TextureFormat textureFormat = TextureFormat.Default, bool mipmaps = false, bool setupForFastCpuWrite = false);

    //Loads a texture from disk
    //Use setupForFastCpuWrite if you plan to update the texture often
    public Texture2D LoadTextureFromDisk(string path, bool srgb = true, bool mipmaps = true, bool setupForFastCpuWrite = false);
    
    //Loads a texture from disk as an array of TexturePixel
    public TexturePixel[] LoadTextureFromDiskAsPixelArray(string path, out uint width, out uint height);
    
    public ImGuiTextureRef GetOrCreateImGuiTextureRefForTexture(Texture2D texture);
}