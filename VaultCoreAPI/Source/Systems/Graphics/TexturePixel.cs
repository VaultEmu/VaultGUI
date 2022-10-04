namespace Vault;

public struct TexturePixel
{
    public byte R;
    public byte G;
    public byte B;
    public byte A;
    
    public TexturePixel(byte r, byte g, byte b)
    {
        R = r;
        G = g;
        B = b;
        A = 255;
    }
    
    public TexturePixel(byte r, byte g, byte b, byte a)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }
}