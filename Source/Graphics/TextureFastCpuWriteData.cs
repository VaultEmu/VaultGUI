using Veldrid;

namespace Vault;

public partial class TextureManager
{
    //Helper class that contains the data needed to perform as fast copy of data from Cpu to gpu
    //via a staging texture
    private class TextureFastCpuWriteData
    {
        //PixelData is a 2D array of [MipMapLevels][Pixels]
        public byte[][] CpuPixelData;
        
        //Stating Texture
        public Texture StagingTexture;
        
        public TextureFastCpuWriteData(Texture stagingTexture, byte[][] cpuPixelData)
        {
            CpuPixelData = cpuPixelData;
            StagingTexture = stagingTexture;
        }
    }
}