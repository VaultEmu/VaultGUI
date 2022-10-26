using VaultCore.Rendering;
using Veldrid;

namespace Vault;

public partial class TextureManager
{
    private class Texture2DImpl : Texture2D
    {
        public readonly Texture VeldridTexture;
        private readonly TextureManager _parentManager;
        private bool _isWritingPixelsToTexture;
        
        public override uint Width => VeldridTexture.Width;
        
        public override uint Height => VeldridTexture.Height;
        
        public override TextureFormat Format => TextureManager.GetVaultTextureFormatFromPixelFormat(VeldridTexture.Format);

        public override bool IsWritingPixelsToTexture => _isWritingPixelsToTexture;
        public TextureFastCpuWriteData? TextureCpuWriteData { get; }
        
        public Texture2DImpl(TextureManager parentManager, Texture veldridTexture, TextureFastCpuWriteData? textureCpuWriteData)
        {
            VeldridTexture = veldridTexture;
            _parentManager = parentManager;
            TextureCpuWriteData = textureCpuWriteData;
            _isWritingPixelsToTexture = false;
        }


        public override void CopyToTexture(
            Texture2D destination,
            uint srcX, uint srcY, uint srcMipLevel, 
            uint dstX, uint dstY, uint dstMipLevel, 
            uint width, uint height)
        {
            var destTextureImpl = destination as Texture2DImpl;
            
            if(destTextureImpl == null)
            {
                throw new InvalidOperationException("This Texture2D was not created by this implementation");
            }
            
            _parentManager.CopyToTexture(
                this, destTextureImpl, 
                srcX, srcY, srcMipLevel, 
                dstX, dstY, dstMipLevel, 
                width, height);
        }

        public override void StartWritingPixelsToTexture()
        {
            if(_isWritingPixelsToTexture)
            {
                throw new InvalidOperationException("Trying to start writing of pixels to texture that is already in the middle of been written too");
            }
        
            _isWritingPixelsToTexture = true;
        }

        public override void FinishWritingPixelsToTexture()
        {
            if(_isWritingPixelsToTexture == false)
            {
                throw new InvalidOperationException("Trying to finish writing of pixels to texture that is not been written too");
            }
        
            _isWritingPixelsToTexture = false;
            
            _parentManager.OnFinishWritingPixelsToTexture(this);
        }

        public override void WritePixelData<T>(
            T[] pixelData,
            uint x, 
            uint y, 
            uint width, 
            uint height, 
            uint mipLevel = 0) 
            where T : struct
        {
            _parentManager.WritePixelData(this, pixelData, x, y, width, height, mipLevel);
        }
        
        protected override void Dispose(bool disposing)
        {
            if(disposing)
            {
                if(TextureCpuWriteData != null)
                {
                    TextureCpuWriteData.StagingTexture.Dispose();
                }
            }
        }
    }
}