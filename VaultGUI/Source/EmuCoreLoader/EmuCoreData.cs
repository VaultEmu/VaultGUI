namespace Vault;

public struct EmuCoreData
{
    public string CoreName;
    public string SystemName;
    public string Version;
    public string Description;
    public string DLLPath;

    public EmuCoreData(string coreName, string systemName, string version, string description, string dllPath)
    {
        CoreName = coreName;
        SystemName = systemName;
        Version = version;
        Description = description;
        DLLPath = dllPath;
    }
}