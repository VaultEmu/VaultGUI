namespace Vault;

public interface SubsystemResolver
{
    public T GetSubsystem<T>() where T : ISubsystem;
}