using System.Reflection;
using McMaster.NETCore.Plugins;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VaultCoreAPI;

namespace Vault;

public class EmuCoreLoader
{
    private static string CorePluginFolder => Path.Combine(AppContext.BaseDirectory, "Cores");
    
    private readonly List<EmuCoreData> _availableEmuCores = new List<EmuCoreData>();
    private readonly Type[] _sharedTypes;
    
    private PluginLoader? _activeEmuCorePluginLoader;
    private IEmuCoreApi? _activeEmuCore;
    
    private readonly ILogger _logger;
    
    public IReadOnlyList<EmuCoreData> AvailableEmuCores => _availableEmuCores;
    public IEmuCoreApi? ActiveEmuCore => _activeEmuCore;

    public EmuCoreLoader()
    {
        _logger = GlobalSubsystems.Resolver.GetSubsystem<ILogger>();
        
        //All interface types in VaultCoreAPI are shared between main app and plugin
        _sharedTypes = typeof(IEmuCoreApi).Assembly.GetTypes().Where(x=>x.IsInterface).ToArray();
    }

    public void RefreshAvailableEmuCores()
    {
        _logger.Log("Scanning for Emu Cores...");
        _availableEmuCores.Clear();
        
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
                    var emuCoreJsonData = (JObject)JToken.ReadFrom(new JsonTextReader(reader));
                    
                    var emuCoreName = (string?)emuCoreJsonData["EmuCoreName"];
                    var SystemName = (string?)emuCoreJsonData["SystemName"];
                    var Version = (string?)emuCoreJsonData["Version"];
                    var Description = (string?)emuCoreJsonData["Description"];
                    
                    if(emuCoreName == null)
                    {
                        _logger.LogWarning($"   - Unable to read 'EmuCoreName' item in emuCore json file. Skipping...");
                        continue;   
                    }
                    
                    if(SystemName == null)
                    {
                        _logger.LogWarning($"   - Unable to read 'EmuCoreName' item in emuCore json file. Skipping...");
                        continue;   
                    }
                    
                    if(Version == null)
                    {
                        _logger.LogWarning($"   - Unable to read 'EmuCoreName' item in emuCore json file. Skipping...");
                        continue;   
                    }
                    
                    if(Description == null)
                    {
                        _logger.LogWarning($"   - Unable to read 'EmuCoreName' item in emuCore json file. Skipping...");
                        continue;   
                    }

                    _availableEmuCores.Add(new EmuCoreData(emuCoreName, SystemName, Version, Description, pluginDll));
                }

            }
            catch(Exception e)
            {
                _logger.LogWarning($"   - Error when trying to read emuCore json file {e}. Skipping...");
            }
        }
    }
    
    public void LoadEmuCore(EmuCoreData coreToLoad)
    {
        _logger.Log($"Loading Core {coreToLoad.CoreName}...");
        CleanupActiveEmuCore();
        
        _activeEmuCorePluginLoader = PluginLoader.CreateFromAssemblyFile(
            coreToLoad.DLLPath, _sharedTypes, config =>
             {
                 config.IsUnloadable = true;
             });
        
        
        //var find the core
        var emuCoreClasses = _activeEmuCorePluginLoader
            .LoadDefaultAssembly()
            .GetTypes()
            .Where(t => typeof(IEmuCoreApi).IsAssignableFrom(t) && !t.IsAbstract)
            .ToList();
        
        if(emuCoreClasses.Count == 0)
        {
            _logger.LogError($"   - Error when loading Core: Core DLL contains no class derived from IEmuCoreApi");
            CleanupActiveEmuCore();
            return;
        }
        
        if(emuCoreClasses.Count > 1)
        {
            _logger.LogError($"   - Error when loading Core: Core DLL contains more then 1 class derived from IEmuCoreApi. Only one should be in the DLL");
            CleanupActiveEmuCore();
            return;
        }
        
        // This assumes the implementation of IPlugin has a parameterless constructor
        _activeEmuCore = (IEmuCoreApi?)Activator.CreateInstance(emuCoreClasses[0]);
        
        if(_activeEmuCore == null)
        {
            _logger.LogError($"   - Error when loading Core: Unable to create instance of class {emuCoreClasses[0].Name}. Is Constructor Correct?");
            CleanupActiveEmuCore();
            return;
        }
        
        _logger.Log($"Core {coreToLoad.CoreName} - Dll Loaded and Core Instantiated");
    }
    
    public void UnloadCore()
    {
        CleanupActiveEmuCore();
    }
    
    private void CleanupActiveEmuCore()
    {
        _activeEmuCore = null;
        
        if(_activeEmuCorePluginLoader != null)
        {
            _activeEmuCorePluginLoader.Dispose();
            _activeEmuCorePluginLoader = null;
            
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }
}