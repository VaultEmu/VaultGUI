namespace Vault;

public struct VaultCoreData
{
    public readonly string CoreName;
    public readonly string SystemName;
    public readonly string Version;
    public readonly string Description;
    public readonly string DLLPath;

    public VaultCoreData(string coreName, string systemName, string version, string description, string dllPath)
    {
        CoreName = coreName;
        SystemName = systemName;
        Version = version;
        Description = description;
        DLLPath = dllPath;
    }
}