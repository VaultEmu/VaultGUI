namespace Vault;

public interface ISubsystemResolver
{
    public T GetSubsystem<T>() where T : ISubsystem;
}