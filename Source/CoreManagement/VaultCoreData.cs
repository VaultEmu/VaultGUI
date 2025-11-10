namespace Vault;

public readonly struct VaultCoreData
{
    public readonly string CoreName;
    public readonly string SystemName;
    public readonly string Version;
    public readonly string Description;
    public readonly string CoreClassName;
    public readonly string[] CoreFeaturesUsed;
    public readonly string DLLPath;
    public readonly bool IsValid;

    public VaultCoreData(string coreName, string systemName, string version, string description, 
        string dllPath, string coreClassName, string[] coreFeaturesUsed)
    {
        CoreName = coreName;
        SystemName = systemName;
        Version = version;
        Description = description;
        DLLPath = dllPath;
        CoreClassName = coreClassName;
        CoreFeaturesUsed = coreFeaturesUsed;
        IsValid = File.Exists(DLLPath);
    }
}