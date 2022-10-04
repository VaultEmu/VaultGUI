using System.Reflection;
using McMaster.NETCore.Plugins;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VaultCoreAPI;

namespace Vault;

public class VaultCoreLoader
{
    private static string CorePluginFolder => Path.Combine(AppContext.BaseDirectory, "Cores");
    
    private readonly List<VaultCoreData> _availableCores = new List<VaultCoreData>();

    private PluginLoader? _activeVaultCorePluginLoader;
    private IVaultCore? _activeCore;
    
    private readonly ILogger _logger;
    
    public IReadOnlyList<VaultCoreData> AvailableCores => _availableCores;
    public IVaultCore? ActiveCore => _activeCore;

    public VaultCoreLoader()
    {
        _logger = GlobalSubsystems.Resolver.GetSubsystem<ILogger>();
    }

    public void RefreshAvailableVaultCores()
    {
        _logger.Log("Scanning for Emu Cores...");
        _availableCores.Clear();
        
        if(Directory.Exists(CorePluginFolder) == false)
        {
            return;
        }
        
        foreach (var coreDir in Directory.GetDirectories(CorePluginFolder))
        {
            var coreName = Path.GetFileName(coreDir);
            var pluginDll = Path.Combine(coreDir, $"{coreName}.dll");
            var pluginJson = Path.Combine(coreDir, $"{coreName}.json");
            _logger.Log($" - {coreName}");
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
                    var vaultCoreJsonData = (JObject)JToken.ReadFrom(new JsonTextReader(reader));
                    
                    var vaultCoreName = (string?)vaultCoreJsonData["VaultCoreName"];
                    var SystemName = (string?)vaultCoreJsonData["SystemName"];
                    var Version = (string?)vaultCoreJsonData["Version"];
                    var Description = (string?)vaultCoreJsonData["Description"];
                    
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

                    _availableCores.Add(new VaultCoreData(vaultCoreName, SystemName, Version, Description, pluginDll));
                }

            }
            catch(Exception e)
            {
                _logger.LogWarning($"   - Error when trying to read vaultCore json file {e}. Skipping...");
            }
        }
    }
    
    public void LoadVaultCore(VaultCoreData coreToLoad)
    {
        _logger.Log($"Loading Core {coreToLoad.CoreName}...");
        CleanupActiveVaultCore();
        
        _activeVaultCorePluginLoader = PluginLoader.CreateFromAssemblyFile(
            coreToLoad.DLLPath, config =>
             {
                 config.IsUnloadable = true;
                 config.PreferSharedTypes = true;
             });
        
        
        //var find the core
        var vaultCoreClasses = _activeVaultCorePluginLoader
            .LoadDefaultAssembly()
            .GetTypes()
            .Where(t => typeof(IVaultCore).IsAssignableFrom(t) && !t.IsAbstract)
            .ToList();
        
        if(vaultCoreClasses.Count == 0)
        {
            _logger.LogError($"   - Error when loading Core: Core DLL contains no class derived from IVaultCore");
            CleanupActiveVaultCore();
            return;
        }
        
        if(vaultCoreClasses.Count > 1)
        {
            _logger.LogError($"   - Error when loading Core: Core DLL contains more then 1 class derived from IVaultCore. Only one should be in the DLL");
            CleanupActiveVaultCore();
            return;
        }
        
        // This assumes the implementation of IPlugin has a parameterless constructor
        _activeCore = (IVaultCore?)Activator.CreateInstance(vaultCoreClasses[0]);
        
        if(_activeCore == null)
        {
            _logger.LogError($"   - Error when loading Core: Unable to create instance of class {vaultCoreClasses[0].Name}. Is Constructor Correct?");
            CleanupActiveVaultCore();
            return;
        }
        
        _logger.Log($"Core {coreToLoad.CoreName} - Dll Loaded and Core Instantiated");
    }
    
    public void UnloadCore()
    {
        CleanupActiveVaultCore();
    }
    
    private void CleanupActiveVaultCore()
    {
        _activeCore = null;
        
        if(_activeVaultCorePluginLoader != null)
        {
            _activeVaultCorePluginLoader.Dispose();
            _activeVaultCorePluginLoader = null;
            
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }
}