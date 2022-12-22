using VaultCore.CoreAPI;

namespace Vault;

//Feature for exposing Vault Core Management
public class VaultCoreManager : IFeature
{
    private readonly TimeProvider _timeProvider;
    private readonly ILogger _logger;
    private readonly VaultCoreLoader _vaultCoreLoader;
    private LoadedVaultCore? _currentlyLoadedCore;
    private ulong _prevHighResTimerSample;
    private double _updateAccum;
    
    public IReadOnlyList<VaultCoreData> _AvailableCores => _vaultCoreLoader.AvailableCores; 

    public VaultCoreManager()
    {
        _timeProvider = GlobalFeatures.Resolver.GetFeature<TimeProvider>();
        _logger = GlobalFeatures.Resolver.GetFeature<ILogger>();
        _vaultCoreLoader = new VaultCoreLoader();
        _updateAccum = 0.0;
        _prevHighResTimerSample = _timeProvider.HighResolutionTimerSample;
    }

    public void RefreshAvailableVaultCores()
    {
        _vaultCoreLoader.RefreshAvailableVaultCores();
    }
    
    public void LoadVaultCore(VaultCoreData coreToLoad)
    {
        UnloadVaultCore();
        _currentlyLoadedCore = _vaultCoreLoader.LoadVaultCore(coreToLoad);
        
        if(_currentlyLoadedCore != null)
        {
            _currentlyLoadedCore.VaultCore.Initialise(GlobalFeatures.Resolver);
        }
    }
    
    public void UnloadVaultCore()
    {
        if(_currentlyLoadedCore == null)
        {
            return;
        }
        
        _currentlyLoadedCore.VaultCore.ShutDown();
        _vaultCoreLoader.UnloadVaultCore(_currentlyLoadedCore);
    }
    
    public void Update()
    {
        if(_currentlyLoadedCore == null)
        {
            return;
        }
        
        var vaultCore = _currentlyLoadedCore.VaultCore;
        
        var newTimeSample = _timeProvider.HighResolutionTimerSample;
        var frameTime =  (double)(newTimeSample - _prevHighResTimerSample) / _timeProvider.HighResolutionTimerSampleFrequency;
        _prevHighResTimerSample = newTimeSample;
        
        if(vaultCore.UpdateRateMs <= 0.0f)
        {
            //Update ever Frame
            vaultCore.Update((float)frameTime);
            _timeProvider.OnCoreUpdate((float)frameTime);
        }
        else
        {
            //Update with fixed timesteps
            _updateAccum += frameTime;
            int numUpdatesAllowed = vaultCore.maxNumUpdates;
            
            while(_updateAccum >= vaultCore.UpdateRateMs)
            {
                vaultCore.Update(vaultCore.UpdateRateMs);
                _timeProvider.OnCoreUpdate(vaultCore.UpdateRateMs);
                _updateAccum -= vaultCore.UpdateRateMs;
                
                if(numUpdatesAllowed > 0)
                {
                    numUpdatesAllowed--;
                    
                    if(numUpdatesAllowed <= 0)
                    {
                        _logger.LogWarning("Max Number of Updates per frame reached when trying to update Core. Breaking update loop");
                        break;
                    }
                }
            }
        }
    }
}