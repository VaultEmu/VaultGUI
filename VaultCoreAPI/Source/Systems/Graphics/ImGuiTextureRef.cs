namespace Vault;

public class ImGuiTextureRef : IEquatable<ImGuiTextureRef>
{
    public readonly IntPtr ImGuiRef;

    public ImGuiTextureRef(IntPtr imGuiRef)
    {
        ImGuiRef = imGuiRef;
    }

    public bool Equals(ImGuiTextureRef? other)
    {
        if(ReferenceEquals(null, other))
        {
            return false;
        }

        if(ReferenceEquals(this, other))
        {
            return true;
        }

        return ImGuiRef.Equals(other.ImGuiRef);
    }

    public override bool Equals(object? obj)
    {
        if(ReferenceEquals(null, obj))
        {
            return false;
        }

        if(ReferenceEquals(this, obj))
        {
            return true;
        }

        if(obj.GetType() != this.GetType())
        {
            return false;
        }

        return Equals((ImGuiTextureRef)obj);
    }

    public override int GetHashCode()
    {
        return ImGuiRef.GetHashCode();
    }

    public static bool operator ==(ImGuiTextureRef? left, ImGuiTextureRef? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(ImGuiTextureRef? left, ImGuiTextureRef? right)
    {
        return !Equals(left, right);
    }
}