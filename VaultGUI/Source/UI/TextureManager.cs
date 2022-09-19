using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ImGuiNET;
using StbImageSharp;
using Veldrid;

namespace Vault;

public class TextureManager : ITextureManager
{
    private class TextureFastCpuWriteData
    {
        public TexturePixel[][] CpuPixelData;
        public Texture StagingTexture;
        
        public TextureFastCpuWriteData(Texture stagingTexture, TexturePixel[][] cpuPixelData)
        {
            CpuPixelData = cpuPixelData;
            StagingTexture = stagingTexture;
        }
    }

    private class Texture2DVeldridImpl : Texture2D
    {
        public readonly Texture VeldridTexture;
        private readonly TextureManager _parentManager;
        private bool _isWritingPixelsToTexture;
        
        public Texture2DVeldridImpl(TextureManager parentManager, Texture veldridTexture, TextureFastCpuWriteData? textureCpuWriteData)
        {
            VeldridTexture = veldridTexture;
            _parentManager = parentManager;
            TextureCpuWriteData = textureCpuWriteData;
            _isWritingPixelsToTexture = false;
        }

        public override uint Width => VeldridTexture.Width;
        
        public override uint Height => VeldridTexture.Height;
        
        public override TextureFormat Format => TextureManager.GetVaultTextureFormatFromPixelFormat(VeldridTexture.Format);

        public override bool IsWritingPixelsToTexture => _isWritingPixelsToTexture;
        public TextureFastCpuWriteData? TextureCpuWriteData { get; }

        public override void CopyToTexture(
            Texture2D destination,
            uint srcX, uint srcY, uint srcMipLevel, 
            uint dstX, uint dstY, uint dstMipLevel, 
            uint width, uint height)
        {
            var destTextureImpl = destination as Texture2DVeldridImpl;
            
            if(destTextureImpl == null)
            {
                throw new InvalidOperationException("This Texture2D was not created buy this implementation");
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

        public override void WritePixelData(TexturePixel[] pixelData, uint x, uint y, uint width, uint height, uint mipLevel = 0)
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

    private readonly GraphicsDevice _parentGraphicsDevice;
    private readonly ImGuiRenderer _parentImGuiRenderer;
    
    public TextureManager(GraphicsDevice graphicsDevice, ImGuiRenderer imGuiRenderer)
    {
        _parentGraphicsDevice = graphicsDevice;
        _parentImGuiRenderer = imGuiRenderer;

        SubsystemController.RegisterSubsystem(this);
    }

    public Texture2D CreateTexture(uint width, uint height,
        TextureFormat textureFormat = TextureFormat.Default,
        bool mipmaps = false, bool setupForFastCpuWrite = false)
    {
        var pixelFormat = GetPixelFormatFromVaultTextureFormat(textureFormat);
        
        var newTexture = _parentGraphicsDevice.ResourceFactory.CreateTexture(TextureDescription.Texture2D(
            width,
            height,
            mipmaps ? ComputeMipLevels(width, height) : 1,
            1,
            pixelFormat,
            TextureUsage.Sampled,
            TextureSampleCount.Count1));
        
        TextureFastCpuWriteData? textureCpuWriteData = null;
        
        if(setupForFastCpuWrite)
        {
            var stagingTexture = _parentGraphicsDevice.ResourceFactory.CreateTexture(TextureDescription.Texture2D(
                newTexture.Width,
                newTexture.Height,
                newTexture.MipLevels,
                1,
                newTexture.Format,
                TextureUsage.Staging,
                TextureSampleCount.Count1));

            textureCpuWriteData = new TextureFastCpuWriteData(stagingTexture, CreateFastUpdatePixelArray(newTexture.Width, newTexture.Height, newTexture.MipLevels));
        }
        
        return new Texture2DVeldridImpl(this, newTexture, textureCpuWriteData);
    }
    
    public Texture2D LoadTextureFromDisk(string path, bool srgb = true, bool mipmaps = true, bool setupForFastCpuWrite = false)
    {
        using (var stream = File.OpenRead(path))
        {
            var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
            
            CreateTextureFromBytesViaStaging(image.Data, (uint)image.Width, (uint)image.Height, mipmaps, srgb, setupForFastCpuWrite, out var texture, out var stagingTexture);
            
            TextureFastCpuWriteData? textureCpuWriteData = null;
            
            if(setupForFastCpuWrite)
            {
                if(stagingTexture == null)
                {
                    throw new InvalidOperationException("Staging texture is null, it should have been preserved after call to CreateTextureFromBytesViaStaging");
                }
                
                var cpuPixelData = CreateFastUpdatePixelArray(texture.Width, texture.Height, texture.MipLevels);
                PopulatePixelArrayFromTexture(texture, stagingTexture, cpuPixelData);
                
                textureCpuWriteData = new TextureFastCpuWriteData(stagingTexture, cpuPixelData);
            }
            
            
            return new Texture2DVeldridImpl(this, texture, textureCpuWriteData);
        }
    }
    
    public TexturePixel[] LoadTextureFromDiskAsPixelArray(string path, out uint width, out uint height)
    {
        using (var stream = File.OpenRead(path))
        {
            var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
            
            width = (uint)image.Width;
            height = (uint)image.Height;
            
            var pixelsOut = new TexturePixel[width * height];
            
            var sourcePixels = MemoryMarshal.Cast<byte, TexturePixel>(image.Data);
            var destPixels = new Span<TexturePixel>(pixelsOut);
            
            TexturePixelUtils.DoPixelDataCopy(sourcePixels, destPixels, 0, 0, width, height, width, height);
            
            return pixelsOut;
        }
    }

    public ImGuiTextureRef GetOrCreateImGuiTextureRefForTexture(Texture2D texture)
    {
        var textureImpl = texture as Texture2DVeldridImpl;
            
        if(textureImpl == null)
        {
            throw new InvalidOperationException("This Texture2D was not created buy this implementation");
        }
        
        return new ImGuiTextureRef( _parentImGuiRenderer.GetOrCreateImGuiBinding(_parentGraphicsDevice.ResourceFactory, textureImpl.VeldridTexture));
    }

    private unsafe void OnFinishWritingPixelsToTexture(Texture2DVeldridImpl texture2DVeldrid)
    {
        //Do fast update copy
        if(texture2DVeldrid.TextureCpuWriteData != null)
        {
            CommandList cl = _parentGraphicsDevice.ResourceFactory.CreateCommandList();
            cl.Begin();
            
            for (uint level = 0; level < texture2DVeldrid.VeldridTexture.MipLevels; level++)
            {
                GetMipDimensions(texture2DVeldrid.VeldridTexture, level, out var mipWidth, out var mipHeight);
                var pixelSpan = new Span<TexturePixel>(texture2DVeldrid.TextureCpuWriteData.CpuPixelData[level]);
   
                fixed (void* pin = &MemoryMarshal.GetReference(pixelSpan))
                {
                    MappedResource map = _parentGraphicsDevice.Map(texture2DVeldrid.TextureCpuWriteData.StagingTexture, MapMode.Write, level);
                    uint rowWidth = (mipWidth * 4);
                    if (rowWidth == map.RowPitch)
                    {
                        Unsafe.CopyBlock(map.Data.ToPointer(), pin, (mipWidth * mipHeight * 4));
                    }
                    else
                    {
                        for (uint y = 0; y < mipHeight; y++)
                        {
                            byte* dstStart = (byte*)map.Data.ToPointer() + y * map.RowPitch;
                            byte* srcStart = (byte*)pin + y * rowWidth;
                            Unsafe.CopyBlock(dstStart, srcStart, rowWidth);
                        }
                    }
                    _parentGraphicsDevice.Unmap(texture2DVeldrid.TextureCpuWriteData.StagingTexture, level);

                    cl.CopyTexture(
                        texture2DVeldrid.TextureCpuWriteData.StagingTexture, 0, 0, 0, level, 0,
                        texture2DVeldrid.VeldridTexture, 0, 0, 0, level, 0,
                        mipWidth, mipHeight, 1, 1);

                }
            }
            cl.End();
            
            _parentGraphicsDevice.SubmitCommands(cl);
            cl.Dispose();
        }
    }
    
    private void WritePixelData(Texture2DVeldridImpl texture2DVeldrid,
        TexturePixel[] pixelData,
        uint x, uint y,
        uint width, uint height,
        uint mipLevel = 0)
    {
        if(texture2DVeldrid.IsWritingPixelsToTexture == false)
        {
            throw new InvalidOperationException("Trying to writ pixels to texture that has not be set to start writing too");
        }
        
        if(texture2DVeldrid.TextureCpuWriteData != null)
        {
            //Update fast update internal Data
            var mipLayerData = texture2DVeldrid.TextureCpuWriteData.CpuPixelData[mipLevel];
            
            GetMipDimensions(texture2DVeldrid.VeldridTexture, mipLevel, out var mipWidth, out var mipHeight);
            
            if(x >= mipWidth)
            {
                throw new ArgumentException($"X Pos is outside width for mip level {mipLevel} of this texture - x: {x}, width{width}, mipwidth: {mipWidth}");
            }
            
            if(y >= mipHeight)
            {
                throw new ArgumentException($"y Pos is outside width for mip level {mipLevel} of this texture - y: {y}, height{height}, mipHeight: {mipHeight}");
            }
            
            if((x + width) > mipWidth)
            {
                throw new ArgumentException($"X Pos + width is outside width for mip level {mipLevel} of this texture - x: {x}, width{width}, mipwidth: {mipWidth}");
            }
            
            if((y + height) > mipHeight)
            {
                throw new ArgumentException($"y Pos + height is outside width for mip level {mipLevel} of this texture - y: {y}, height{height}, mipHeight: {mipHeight}");
            }
            
            if(width == 1 && height == 1)
            {
                mipLayerData[x + y * mipWidth] = pixelData[0];
            }
            else
            {
                TexturePixelUtils.DoPixelDataCopy(pixelData, mipLayerData, x, y, width, height, mipWidth, mipHeight);
            }
        }
        else
        {
            //do simple update path (slower)
            _parentGraphicsDevice.UpdateTexture(
                texture2DVeldrid.VeldridTexture,
                pixelData,
                x,
                y,
                0,
                width,
                height,
                1,
                mipLevel,
                0);
        }
        
        
    }
    
    private void CopyToTexture(
        Texture2DVeldridImpl sourceTexture,
        Texture2DVeldridImpl destination,
        uint srcX,
        uint srcY,
        uint srcMipLevel,
        uint dstX,
        uint dstY,
        uint dstMipLevel,
        uint width,
        uint height)
    {
        var cl = _parentGraphicsDevice.ResourceFactory.CreateCommandList();
        cl.Begin();
        cl.CopyTexture(
            sourceTexture.VeldridTexture, srcX, srcY, 0, srcMipLevel, 0,
            destination.VeldridTexture, dstX, dstY, 0, dstMipLevel, 0,
            width, height, 1, 1);
        cl.End();
        _parentGraphicsDevice.SubmitCommands(cl);
        cl.Dispose();
    }
    
    private unsafe void CreateTextureFromBytesViaStaging(byte[] pixelData, uint width, uint height, bool mipmaps, bool srgb, bool keepStagingTexture, 
        out Texture finalTexture, out Texture? stagingTexture)
    {
        var factory = _parentGraphicsDevice.ResourceFactory;
        
        var format = srgb ? PixelFormat.R8_G8_B8_A8_UNorm_SRgb : PixelFormat.R8_G8_B8_A8_UNorm;
        
        var mipLevels = (mipmaps ? ComputeMipLevels(width, height) : 1);
        
        
        stagingTexture = factory.CreateTexture(
            TextureDescription.Texture2D(width, height, mipLevels, 1, format, TextureUsage.Staging));
        
        var finalTextureUsageFlags = TextureUsage.Sampled;
        
        if(mipmaps)
        {
            finalTextureUsageFlags |= TextureUsage.GenerateMipmaps;
        }

        finalTexture = factory.CreateTexture(
            TextureDescription.Texture2D(width, height, mipLevels, 1, format, finalTextureUsageFlags));

        var cl = _parentGraphicsDevice.ResourceFactory.CreateCommandList();
        cl.Begin();

        var pixelSpan = new Span<byte>(pixelData);

        fixed (void* pin = &MemoryMarshal.GetReference(pixelSpan))
        {
            var map = _parentGraphicsDevice.Map(stagingTexture, MapMode.Write, 0);
            var rowWidth = (width * 4);
            if(rowWidth == map.RowPitch)
            {
                Unsafe.CopyBlock(map.Data.ToPointer(), pin, (width * height * 4));
            }
            else
            {
                for (uint y = 0; y < height; y++)
                {
                    var dstStart = (byte*)map.Data.ToPointer() + y * map.RowPitch;
                    var srcStart = (byte*)pin + y * rowWidth;
                    Unsafe.CopyBlock(dstStart, srcStart, rowWidth);
                }
            }

            _parentGraphicsDevice.Unmap(stagingTexture, 0);

            cl.CopyTexture(
                stagingTexture, 0, 0, 0, 0, 0,
                finalTexture, 0, 0, 0, 0, 0,
                width, height, 1, 1);
        }

        if(mipmaps)
        {
            cl.GenerateMipmaps(finalTexture);
        }


        cl.End();
        _parentGraphicsDevice.SubmitCommands(cl);
        cl.Dispose();

        if(keepStagingTexture == false)
        {
            stagingTexture.Dispose();
            stagingTexture = null;
        }
    }

    private TexturePixel[][] CreateFastUpdatePixelArray( uint width, uint height, uint mipLevels)
    {
        var cpuPixelData = new TexturePixel[mipLevels][];
        
        var mipWidth = width;
        var mipHeight = height;
        
        for(int i = 0; i < mipLevels; ++i)
        {
            cpuPixelData[i] = new TexturePixel[mipWidth * mipHeight];
            
            mipWidth /= 2;
            mipHeight /= 2;
        }
        
        return cpuPixelData;
    }
    
    private unsafe void PopulatePixelArrayFromTexture(Texture texture, Texture stagingTexture, 
        TexturePixel[][] pixelData)
    {
        CommandList cl = _parentGraphicsDevice.ResourceFactory.CreateCommandList();
        cl.Begin();
        for (uint level = 0; level < texture.MipLevels; level++)
        {
            //Make sure staging texture has latest data
            GetMipDimensions(texture, level, out var mipWidth, out var mipHeight);
            cl.CopyTexture(
                texture, 0, 0, 0, level, 0,
                stagingTexture, 0, 0, 0, level, 0,
                mipWidth, mipHeight, 1, 1);
        }
        
        cl.End();
        _parentGraphicsDevice.SubmitCommands(cl);
        cl.Dispose();
        
        for (uint level = 0; level < texture.MipLevels; level++)
        {
            var pixelSpan = new Span<TexturePixel>(pixelData[level]);
            
            fixed (void* pin = &MemoryMarshal.GetReference(pixelSpan))
            {
                GetMipDimensions(texture, level, out var mipWidth, out var mipHeight);
                
                MappedResource map = _parentGraphicsDevice.Map(stagingTexture, MapMode.Read, level);
                uint rowWidth = (mipWidth * 4);
                if (rowWidth == map.RowPitch)
                {
                    Unsafe.CopyBlock(pin, map.Data.ToPointer(), (mipWidth * mipHeight * 4));
                }
                else
                {
                    for (uint y = 0; y < mipHeight; y++)
                    {
                        byte* dstStart = (byte*)pin + y * rowWidth;
                        byte* srcStart = (byte*)map.Data.ToPointer() + y * map.RowPitch;
                        Unsafe.CopyBlock(dstStart, srcStart, rowWidth);
                    }
                }
                _parentGraphicsDevice.Unmap(stagingTexture, level);
            }
        }
    }
    
    private static void GetMipDimensions(Texture tex, uint mipLevel, out uint width, out uint height)
    {
        if(mipLevel == 0)
        {
            width = tex.Width;
            height = tex.Height;
            return;
        }
        
        width = GetDimension(tex.Width, mipLevel);
        height = GetDimension(tex.Height, mipLevel);
    }

    private static uint GetDimension(uint largestLevelDimension, uint mipLevel)
    {
        uint ret = largestLevelDimension;
        for (uint i = 0; i < mipLevel; i++)
        {
            ret /= 2;
        }

        return Math.Max(1, ret);
    }

    private static uint ComputeMipLevels(uint width, uint height)
    {
        return 1 + (uint)Math.Floor(Math.Log(Math.Max(width, height), 2));
    }
    
    private static PixelFormat GetPixelFormatFromVaultTextureFormat(TextureFormat textureFormat)
    {
        switch (textureFormat)
        {
            case TextureFormat.RGBA_32:
                return PixelFormat.R8_G8_B8_A8_UNorm;

            case TextureFormat.RGBA_32_SRGB:
                return PixelFormat.R8_G8_B8_A8_UNorm_SRgb;
            
            case TextureFormat.R_8:
                return PixelFormat.R8_UNorm;
            
            case TextureFormat.R_16:
                return PixelFormat.R16_UNorm;
            
            case TextureFormat.RG_16:
                return PixelFormat.R8_G8_UNorm;
            
            case TextureFormat.RG_32:
                return PixelFormat.R16_G16_UNorm;
            
            case TextureFormat.R_Float:
                return PixelFormat.R32_Float;
            
            case TextureFormat.R_Half:
                return PixelFormat.R16_Float;
            
            case TextureFormat.RG_Float:
                return PixelFormat.R32_G32_Float;
            
            case TextureFormat.RG_Half:
                return PixelFormat.R16_G16_Float;
            
            case TextureFormat.RGBA_Float:
                return PixelFormat.R32_G32_B32_A32_Float;
            
            case TextureFormat.RGBA_Half:
                return PixelFormat.R16_G16_B16_A16_Float;
            
            default:
                throw new ArgumentOutOfRangeException(nameof(textureFormat), textureFormat, null);
        }
    }
    
    private static TextureFormat GetVaultTextureFormatFromPixelFormat(PixelFormat pixelFormat)
    {
        switch (pixelFormat)
        {
            case PixelFormat.R8_G8_B8_A8_UNorm:
                return TextureFormat.RGBA_32;
            
            case PixelFormat.R8_G8_B8_A8_UNorm_SRgb:
                return TextureFormat.RGBA_32_SRGB;
            
            case PixelFormat.R8_UNorm:
                return TextureFormat.R_8;
            
            case PixelFormat.R16_UNorm:
                return TextureFormat.R_16;
            
            case PixelFormat.R8_G8_UNorm:
                return TextureFormat.RG_16;
            
            case PixelFormat.R16_G16_UNorm:
                return TextureFormat.RG_32;

            case PixelFormat.R32_Float:
                return TextureFormat.R_Float;

            case PixelFormat.R16_Float:
                return TextureFormat.R_Half;

            case PixelFormat.R32_G32_Float:
                return TextureFormat.RG_Float;
            
            case PixelFormat.R16_G16_Float:
                return TextureFormat.RG_Half;

            case PixelFormat.R32_G32_B32_A32_Float:
                return TextureFormat.RGBA_Float;
            
            case PixelFormat.R16_G16_B16_A16_Float:
                return TextureFormat.RGBA_Half;
            
            default:
                throw new InvalidOperationException("Pixel Format does not have a Vault equivalent");
        }
    }
}