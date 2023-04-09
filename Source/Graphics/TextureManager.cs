using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ImGuiNET;
using StbImageSharp;
using VaultCore.ImGuiWindowsAPI;
using VaultCore.Rendering;
using Veldrid;

namespace Vault;

public partial class TextureManager : 
    ITextureManager, 
    IImGuiTextureManager, 
    IDisposable
{
    private readonly GraphicsDevice _parentGraphicsDevice;
    private readonly ImGuiRenderer _imGuiRenderer;
    private readonly CommandList _copyTextureCommandList;
    
    public TextureManager(GraphicsDevice graphicsDevice, ImGuiRenderer imGuiRenderer)
    {
        _parentGraphicsDevice = graphicsDevice;
        _imGuiRenderer = imGuiRenderer;
        _copyTextureCommandList = _parentGraphicsDevice.ResourceFactory.CreateCommandList();
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

            textureCpuWriteData = new TextureFastCpuWriteData(stagingTexture, 
                CreateFastUpdatePixelArray(newTexture.Width, newTexture.Height, newTexture.MipLevels, textureFormat));
        }
        
        return new Texture2DImpl(this, newTexture, textureCpuWriteData);
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
                
                var cpuPixelData = CreateFastUpdatePixelArray(texture.Width, texture.Height, 
                    texture.MipLevels, GetVaultTextureFormatFromPixelFormat(texture.Format));
                
                PopulatePixelArrayFromTexture(texture, stagingTexture, cpuPixelData);
                
                textureCpuWriteData = new TextureFastCpuWriteData(stagingTexture, cpuPixelData);
            }
            
            
            return new Texture2DImpl(this, texture, textureCpuWriteData);
        }
    }

    public ColorFloat[] LoadTextureFromDiskAsColorFloatArray(string path, out uint width, out uint height)
    {
        unsafe
        {
            using (var stream = File.OpenRead(path))
            {
                var image = ImageResultFloat.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
                
                width = (uint)image.Width;
                height = (uint)image.Height;
                
                var pixelsOut = new ColorFloat[width * height];
                
                var sourcePixels = MemoryMarshal.Cast<float, ColorFloat>(image.Data);
                var destPixels = new Span<ColorFloat>(pixelsOut);
                var pixelSize = (uint)sizeof(Color32);

                fixed (void* sourcePin = &MemoryMarshal.GetReference(sourcePixels), destPin = &MemoryMarshal.GetReference(destPixels))
                {
                    Unsafe.CopyBlock(destPin, sourcePin, width * height * pixelSize);
                }

                return pixelsOut;
            }
        }
    }

    public Color32[] LoadTextureFromDiskAsColor32Array(string path, out uint width, out uint height)
    {
        unsafe
        {
            using (var stream = File.OpenRead(path))
            {
                var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
                
                width = (uint)image.Width;
                height = (uint)image.Height;
                
                var pixelsOut = new Color32[width * height];
                
                var sourcePixels = MemoryMarshal.Cast<byte, Color32>(image.Data);
                var destPixels = new Span<Color32>(pixelsOut);
                var pixelSize = (uint)sizeof(Color32);

                fixed (void* sourcePin = &MemoryMarshal.GetReference(sourcePixels), destPin = &MemoryMarshal.GetReference(destPixels))
                {
                    Unsafe.CopyBlock(destPin, sourcePin, width * height * pixelSize);
                }

                return pixelsOut;
            }
        }
    }

    public ImGuiTextureRef GetOrCreateImGuiTextureRefForTexture(Texture2D texture)
    {
        var textureImpl = texture as Texture2DImpl;
            
        if(textureImpl == null)
        {
            throw new InvalidOperationException("This Texture2D was not created buy this implementation");
        }
        
        return new ImGuiTextureRef( _imGuiRenderer.GetOrCreateImGuiBinding(_parentGraphicsDevice.ResourceFactory, textureImpl.VeldridTexture));
    }

    private unsafe void OnFinishWritingPixelsToTexture(Texture2DImpl texture2D)
    {
        //Do fast update copy
        if(texture2D.TextureCpuWriteData != null)
        {
            _copyTextureCommandList.Begin();
            
            var pixelSize = GetSizeInBytes(texture2D.Format);
            
            for (uint level = 0; level < texture2D.VeldridTexture.MipLevels; level++)
            {
                GetMipDimensions(texture2D.VeldridTexture, level, out var mipWidth, out var mipHeight);
                var pixelSpan = new Span<byte>(texture2D.TextureCpuWriteData.CpuPixelData[level]);
   
                fixed (void* pin = &MemoryMarshal.GetReference(pixelSpan))
                {
                    MappedResource map = _parentGraphicsDevice.Map(texture2D.TextureCpuWriteData.StagingTexture, MapMode.Write, level);
                    uint rowWidth = (mipWidth * pixelSize);
                    if (rowWidth == map.RowPitch)
                    {
                        Unsafe.CopyBlock(map.Data.ToPointer(), pin, (mipWidth * mipHeight * pixelSize));
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
                    _parentGraphicsDevice.Unmap(texture2D.TextureCpuWriteData.StagingTexture, level);

                    _copyTextureCommandList.CopyTexture(
                        texture2D.TextureCpuWriteData.StagingTexture, 0, 0, 0, level, 0,
                        texture2D.VeldridTexture, 0, 0, 0, level, 0,
                        mipWidth, mipHeight, 1, 1);

                }
            }
            _copyTextureCommandList.End();
            
            _parentGraphicsDevice.SubmitCommands(_copyTextureCommandList);
        }
    }
    
    private void WritePixelData<T>(
        Texture2DImpl texture2D,
        T[] pixelData,
        uint pixelDataWidth, uint pixelDataHeight,
        uint targetX, uint targetY,
        uint mipLevel = 0)
        where T : unmanaged
    {
        if(texture2D.IsWritingPixelsToTexture == false)
        {
            throw new InvalidOperationException("Trying to writ pixels to texture that has not be set to start writing too");
        }
        
        if(texture2D.TextureCpuWriteData != null)
        {
            //Update fast update internal Data
            var mipLayerData = texture2D.TextureCpuWriteData.CpuPixelData[mipLevel];
            
            GetMipDimensions(texture2D.VeldridTexture, mipLevel, out var mipWidth, out var mipHeight);
            
            if(targetX >= mipWidth)
            {
                throw new ArgumentException($"X Pos is outside width for mip level {mipLevel} of this texture - x: {targetX}, width{pixelDataWidth}, mipwidth: {mipWidth}");
            }
            
            if(targetY >= mipHeight)
            {
                throw new ArgumentException($"y Pos is outside width for mip level {mipLevel} of this texture - y: {targetY}, height{pixelDataHeight}, mipHeight: {mipHeight}");
            }
            
            if((targetX + pixelDataWidth) > mipWidth)
            {
                throw new ArgumentException($"X Pos + width is outside width for mip level {mipLevel} of this texture - x: {targetX}, width{pixelDataWidth}, mipwidth: {mipWidth}");
            }
            
            if((targetY + pixelDataHeight) > mipHeight)
            {
                throw new ArgumentException($"y Pos + height is outside width for mip level {mipLevel} of this texture - y: {targetY}, height{pixelDataHeight}, mipHeight: {mipHeight}");
            }
            
            var sourcePixels = MemoryMarshal.Cast<T, byte>(pixelData);
            var destPixels = new Span<byte>(mipLayerData);
            var pixelSize = (uint)Marshal.SizeOf<T>();
            
            unsafe
            {
                fixed (void* sourcePin = &MemoryMarshal.GetReference(sourcePixels), destPin = &MemoryMarshal.GetReference(destPixels))
                {
                    if(pixelDataWidth == mipWidth)
                    {
                        var dstStart = (byte*)destPin + targetY * mipWidth * pixelSize;
                        Unsafe.CopyBlock(dstStart, sourcePin, pixelDataHeight * pixelDataWidth * pixelSize);
                    }
                    else
                    {
                        for(var row = 0; row < pixelDataHeight; ++row)
                        {
                            var dstStart = (byte*)destPin + targetX * pixelSize + (targetY + row) * mipWidth * pixelSize;
                            var srcStart = (byte*)sourcePin + row * pixelDataWidth * pixelSize;
                            Unsafe.CopyBlock(dstStart, srcStart, pixelDataWidth * pixelSize);
                        } 
                    }
            
                }
            }

        }
        else
        {
            //do simple update path (slower)
            _parentGraphicsDevice.UpdateTexture(
                texture2D.VeldridTexture,
                pixelData,
                targetX,
                targetY,
                0,
                pixelDataWidth,
                pixelDataHeight,
                1,
                mipLevel,
                0);
        }
    }
    
    private void CopyToTexture(
        Texture2DImpl sourceTexture,
        Texture2DImpl destination,
        uint srcX,
        uint srcY,
        uint srcMipLevel,
        uint dstX,
        uint dstY,
        uint dstMipLevel,
        uint width,
        uint height)
    {
        _copyTextureCommandList.Begin();
        _copyTextureCommandList.CopyTexture(
            sourceTexture.VeldridTexture, srcX, srcY, 0, srcMipLevel, 0,
            destination.VeldridTexture, dstX, dstY, 0, dstMipLevel, 0,
            width, height, 1, 1);
        _copyTextureCommandList.End();
        _parentGraphicsDevice.SubmitCommands(_copyTextureCommandList);
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
        
        _copyTextureCommandList.Begin();

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

            _copyTextureCommandList.CopyTexture(
                stagingTexture, 0, 0, 0, 0, 0,
                finalTexture, 0, 0, 0, 0, 0,
                width, height, 1, 1);
        }

        if(mipmaps)
        {
            _copyTextureCommandList.GenerateMipmaps(finalTexture);
        }


        _copyTextureCommandList.End();
        _parentGraphicsDevice.SubmitCommands(_copyTextureCommandList);

        if(keepStagingTexture == false)
        {
            stagingTexture.Dispose();
            stagingTexture = null;
        }
    }

    private byte[][] CreateFastUpdatePixelArray( uint width, uint height, uint mipLevels, TextureFormat textureFormat)
    {
        var cpuPixelData = new byte[mipLevels][];
        
        var mipWidth = width;
        var mipHeight = height;
        
        var pixelSize = GetSizeInBytes(textureFormat);
        
        for(int i = 0; i < mipLevels; ++i)
        {
            cpuPixelData[i] = new byte[mipWidth * mipHeight * pixelSize];
            
            mipWidth /= 2;
            mipHeight /= 2;
        }
        
        return cpuPixelData;
    }
    
    private unsafe void PopulatePixelArrayFromTexture(Texture texture, Texture stagingTexture, byte[][] pixelData)
    {
        _copyTextureCommandList.Begin();
        for (uint level = 0; level < texture.MipLevels; level++)
        {
            //Make sure staging texture has latest data
            GetMipDimensions(texture, level, out var mipWidth, out var mipHeight);
            _copyTextureCommandList.CopyTexture(
                texture, 0, 0, 0, level, 0,
                stagingTexture, 0, 0, 0, level, 0,
                mipWidth, mipHeight, 1, 1);
        }
        
        _copyTextureCommandList.End();
        _parentGraphicsDevice.SubmitCommands(_copyTextureCommandList);

        var pixelSize = GetSizeInBytes(GetVaultTextureFormatFromPixelFormat(texture.Format));
        
        for (uint level = 0; level < texture.MipLevels; level++)
        {
            var pixelSpan = new Span<byte>(pixelData[level]);
            
            fixed (void* pin = &MemoryMarshal.GetReference(pixelSpan))
            {
                GetMipDimensions(texture, level, out var mipWidth, out var mipHeight);
                
                MappedResource map = _parentGraphicsDevice.Map(stagingTexture, MapMode.Read, level);
                uint rowWidth = (mipWidth * pixelSize);
                if (rowWidth == map.RowPitch)
                {
                    //Quick copy
                    Unsafe.CopyBlock(pin, map.Data.ToPointer(), map.RowPitch * mipHeight);
                }
                else
                {
                    //Have to copy each row individually
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
    
    private static uint GetSizeInBytes(TextureFormat textureFormat)
    {
        switch (textureFormat)
        {
           

            case TextureFormat.R_8:
                return 1;

            case TextureFormat.R_16:
            case TextureFormat.RG_16:
            case TextureFormat.R_Half:
                return 2;
            
            case TextureFormat.RGBA_32:
            case TextureFormat.RGBA_32_SRGB:
            case TextureFormat.RG_32:
            case TextureFormat.R_Float:
            case TextureFormat.RG_Half:
                return 4;

            case TextureFormat.RG_Float:
            case TextureFormat.RGBA_Half:
                return 8;

            case TextureFormat.RGBA_Float:
                return 16;

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

    public void Dispose()
    {
        _copyTextureCommandList.Dispose();
    }

    public void OnCoreAcquiresFeature(Type coreType, Type featureType, List<Type> allCoreFeaturesNeeded)
    {
        
    }

    public void OnCoreReleasesFeature(Type coreType, Type featureType, List<Type> allCoreFeaturesNeeded)
    {
        
    }
}