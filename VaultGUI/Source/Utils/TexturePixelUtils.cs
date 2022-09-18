using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Vault;

public static class TexturePixelUtils
{
    public static void DoPixelDataCopy(
        TexturePixel[] sourcePixelData,
        TexturePixel[] destinationArray,
        uint targetX, uint targetY,
        uint sourceWidth, uint sourceHeight,
        uint destWidth, uint destHeight)
    {
        var sourcePixelSpan = new Span<TexturePixel>(sourcePixelData);
        var destPixelSpan = new Span<TexturePixel>(destinationArray);
        
        DoPixelDataCopy(sourcePixelSpan, destPixelSpan, targetX, targetY,
            sourceWidth, sourceHeight,
            destWidth, destHeight);
    }
    
    public static unsafe void DoPixelDataCopy(
        Span<TexturePixel> sourcePixelSpan,
        Span<TexturePixel> destPixelSpan,
        uint targetX, uint targetY,
        uint sourceWidth, uint sourceHeight,
        uint destWidth, uint destHeight)
    {
        if(targetX >= destWidth)
        {
            throw new ArgumentException($"X Pos is outside width for dest target - x: {targetX}, sourceWidth{sourceWidth}, destWidth: {destWidth}");
        }
            
        if(targetY >= destHeight)
        {
            throw new ArgumentException($"Y Pos is outside height for dest target - y: {targetY}, sourceHeight{sourceHeight}, destHeight: {destHeight}");
        }
            
        if((targetX + sourceWidth) > destWidth)
        {
            throw new ArgumentException($"X Pos  + sourceWidth is outside width for dest target - x: {targetX}, sourceWidth{sourceWidth}, destWidth: {destWidth}");
        }
            
        if((targetY + sourceHeight) > destHeight)
        {
            throw new ArgumentException($"Y Pos + sourceHeight is outside height for dest target - y: {targetY}, sourceHeight{sourceHeight}, destHeight: {destHeight}");
        }

        var itemSize = (uint)sizeof(TexturePixel);
        
        fixed (void* sourcePin = &MemoryMarshal.GetReference(sourcePixelSpan), destPin = &MemoryMarshal.GetReference(destPixelSpan))
        {
            if(sourceWidth == destWidth)
            {
                var dstStart = (byte*)destPin + targetY * destWidth * itemSize;
                Unsafe.CopyBlock(dstStart, sourcePin, sourceHeight * sourceWidth * itemSize);
            }
            else
            {
                for(var row = 0; row < sourceHeight; ++row)
                {
                    var dstStart = (byte*)destPin + targetX * itemSize + (targetY + row) * destWidth * itemSize;
                    var srcStart = (byte*)sourcePin + row * sourceWidth * itemSize;
                    Unsafe.CopyBlock(dstStart, srcStart, sourceWidth * itemSize);
                } 
            }
            
        }
    }
}