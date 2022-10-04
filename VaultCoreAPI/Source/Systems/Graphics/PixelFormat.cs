namespace Vault;

public enum TextureFormat : byte
{
    // RGBA component order. Each component is an 8-bit unsigned normalized integer.
    RGBA_32,
    
    // RGBA component order. Each component is an 8-bit unsigned normalized integer.
    // This is an sRGB format version.
    RGBA_32_SRGB,
    
    // Single-channel, 8-bit unsigned normalized integer.
    R_8,
    
    // Single-channel, 16-bit unsigned normalized integer
    R_16,
    
    // RG component order. Each component is an 8-bit signed normalized integer.
    RG_16,
    
    // RG component order. Each component is an 16-bit signed normalized integer.
    RG_32,
    
    // Single-channel, 32-bit signed floating-point value.
    R_Float,
    
    // Single-channel, 16-bit signed floating-point value.
    R_Half,
    
    // RG component order. Each component is an 32-bit signed floating-point value.
    RG_Float,
    
    // RG component order. Each component is an 16-bit signed floating-point value.
    RG_Half,
    
    // RGBA component order. Each component is an 32-bit signed floating-point value.
    RGBA_Float,
    
    // RGBA component order. Each component is an 16-bit signed floating-point value.
    RGBA_Half,
    
    //Default format used if non specified
    Default = RGBA_32
}