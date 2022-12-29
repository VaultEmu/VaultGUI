using McMaster.NETCore.Plugins;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VaultCore.CoreAPI;

namespace Vault;

public class LoadedVaultCore
{
    public VaultCoreBase VaultCore;
    public VaultCoreData VaultCoreData;

    public LoadedVaultCore(VaultCoreBase vaultCore, VaultCoreData vaultCoreData)
    {
        VaultCore = vaultCore;
        VaultCoreData = vaultCoreData;
    }
}

public class VaultCoreLoader
{
    private class LoadedVaultCoreInternal : LoadedVaultCore
    {
        public PluginLoader VaultCorePluginLoader;
        
        public LoadedVaultCoreInternal(VaultCoreBase vaultCore, VaultCoreData vaultCoreData, PluginLoader vaultCorePluginLoader) : base(vaultCore, vaultCoreData)
        {
            VaultCorePluginLoader = vaultCorePluginLoader;
        }
    }
    
    private static string CorePluginFolder => @"D:\DEV\Personal\VaultEmu\VaultCores\PublishedCores";//Path.Combine(AppContext.BaseDirectory, "Cores");
    
    private readonly List<VaultCoreData> _availableCores = new List<VaultCoreData>();
    
    private readonly Logger _logger;

    public IReadOnlyList<VaultCoreData> AvailableCores => _availableCores;

    public VaultCoreLoader(Logger logger)
    {
        _logger = logger;
    }
    
    public void RefreshAvailableVaultCores()
    {
        _logger.Log("Scanning for Emu Cores...");
        _availableCores.Clear();
        
        if(Directory.Exists(CorePluginFolder) == false)
        {
            return;
        }
        
        var coresFound = new List<VaultCoreData>();
        
        foreach (var coreDir in Directory.GetDirectories(CorePluginFolder))
        {
            coresFound.Clear();
            
            var coreName = Path.GetFileName(coreDir);
            var pluginDll = Path.Combine(coreDir, $"{coreName}.dll");
            var pluginJson = Path.Combine(coreDir, $"{coreName}_Manifest.json");
            
            _logger.Log($" - Parsing {coreName} Manifest");
            
            if (File.Exists(pluginDll) == false)
            {
                _logger.LogWarning($"   - Unable to find {coreName}.dll for core {coreName}. Skipping...");
                continue;    
            }
            
            if (File.Exists(pluginJson) == false)
            {
                _logger.LogWarning($"   - Unable to find {coreName}.json for core {coreName}. Skipping...");
                continue;   
            }
            
            try
            {
                using (var reader = File.OpenText(pluginJson))
                {
                    var vaultCoreJsonData = (JArray)JToken.ReadFrom(new JsonTextReader(reader));
                    
                    foreach(var entry in vaultCoreJsonData)
                    {
                        var vaultCoreName = (string?)entry["Name"];
                        var SystemName = (string?)entry["EmulatedSystemName"];
                        var Version = (string?)entry["Version"];
                        var Description = (string?)entry["Description"];
                        var CoreClassName = (string?)entry["CoreClassName"];

                        if(vaultCoreName == null)
                        {
                            _logger.LogWarning($"   - Unable to read 'VaultCoreName' item in vaultCore json file. Skipping...");
                            continue;   
                        }
                    
                        if(SystemName == null)
                        {
                            _logger.LogWarning($"   - Unable to read 'VaultCoreName' item in vaultCore json file. Skipping...");
                            continue;   
                        }
                    
                        if(Version == null)
                        {
                            _logger.LogWarning($"   - Unable to read 'VaultCoreName' item in vaultCore json file. Skipping...");
                            continue;   
                        }
                    
                        if(Description == null)
                        {
                            _logger.LogWarning($"   - Unable to read 'VaultCoreName' item in vaultCore json file. Skipping...");
                            continue;   
                        }
                        
                        if(CoreClassName == null)
                        {
                            _logger.LogWarning($"   - Unable to read 'CoreClassName' item in vaultCore json file. Skipping...");
                            continue;   
                        }
                        
                        var coreFeaturesUsed = new List<string>();
                        
                        var coreFeaturesUsedArray = (JArray)entry["CoreFeaturesUsed"]!;
                        
                        foreach(var featureElement in coreFeaturesUsedArray)
                        {
                            coreFeaturesUsed.Add((string)featureElement!);
                        }

                        _logger.Log($"   Found Core {vaultCoreName} ({CoreClassName})\n" +
                                    $"      - System Name: {SystemName}\n" +
                                    $"      - Description: {Description}\n" +
                                    $"      - Version: {Version}\n" +
                                    $"      - Features Used: {string.Join(", ", coreFeaturesUsed)}");
                        
                        coresFound.Add(new VaultCoreData(vaultCoreName, SystemName, Version, Description, 
                            pluginDll, CoreClassName, coreFeaturesUsed.ToArray()));
                    }
                }
            }
            catch(Exception e)
            {
                _logger.LogWarning($"   - Error when trying to read vaultCore json file {e}. Skipping...");
                coresFound.Clear();
            }
            
            _availableCores.AddRange(coresFound);
        }
    }
    
    public LoadedVaultCore? LoadVaultCore(VaultCoreData coreToLoad)
    {
        PluginLoader? vaultCorePluginLoader = null;
        
        try
        {
            _logger.Log($"Loading Core {coreToLoad.CoreName}...");
            
            vaultCorePluginLoader = PluginLoader.CreateFromAssemblyFile(
                coreToLoad.DLLPath,
                config =>
                {
                    config.IsUnloadable = true;
                    config.PreferSharedTypes = true;
                });

            //find the core
            var vaultCoreClasses = vaultCorePluginLoader
                .LoadDefaultAssembly()
                .GetTypes()
                .Where(t => typeof(VaultCoreBase).IsAssignableFrom(t) && !t.IsAbstract && coreToLoad.CoreClassName.Equals(t.Name, StringComparison.OrdinalIgnoreCase))
                .ToList();
        
            if(vaultCoreClasses.Count == 0)
            {
                _logger.LogError($"   - Error when loading Core: Core DLL contains no class derived from VaultCoreBase named {coreToLoad.CoreClassName}");
                CleanupLoadedVaultCore(vaultCorePluginLoader);
                return null;
            }
        
            if(vaultCoreClasses.Count > 1)
            {
                _logger.LogError($"   - Error when loading Core: Core DLL contains multiple class derived from VaultCoreBase named {coreToLoad.CoreClassName}.");
                CleanupLoadedVaultCore(vaultCorePluginLoader);
                return null;
            }
        
            // This assumes the implementation of IPlugin has a parameterless constructor
            var loadedCore = (VaultCoreBase?)Activator.CreateInstance(vaultCoreClasses[0]);
        
            if(loadedCore == null)
            {
                _logger.LogError($"   - Error when loading Core: Unable to create instance of class {vaultCoreClasses[0].Name}. Is Constructor Correct?");
                CleanupLoadedVaultCore(vaultCorePluginLoader);
                return null;
            }
        
            _logger.Log($"Core {coreToLoad.CoreName} - Dll Loaded and Core Instantiated");
        
            return new LoadedVaultCoreInternal(loadedCore, coreToLoad, vaultCorePluginLoader);
        }
        catch (Exception e)
        {
            _logger.LogError($"   - Exception Thrown when loading Core:", e);
            CleanupLoadedVaultCore(vaultCorePluginLoader);
            return null;
        }
    }
    
    public void UnloadVaultCore(LoadedVaultCore coreToUnload)
    {
        var internalData = coreToUnload as LoadedVaultCoreInternal;
        
        if(internalData == null)
        {
            throw new InvalidOperationException("Trying to unload core not created by this is loader");
        }
        
        CleanupLoadedVaultCore(internalData.VaultCorePluginLoader);
    }

    private void CleanupLoadedVaultCore(PluginLoader? vaultCorePluginLoader)
    {
        if(vaultCorePluginLoader != null)
        {
            vaultCorePluginLoader.Dispose();
            
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }
}