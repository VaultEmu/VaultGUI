namespace Vault;

//Feature for exposing Vault Core Management
public class VaultCoreManager
{
    private readonly TimeProvider _timeProvider;
    private readonly Logger _logger;
    private readonly VaultCoreLoader _vaultCoreLoader;
    private ulong _prevHighResTimerSample;
    private double _updateAccum;
    
    public LoadedVaultCore? CurrentlyLoadedCore;
    
    
    public delegate void OnCoreUpdatedHandler(float deltaTime);
    public event OnCoreUpdatedHandler? OnCoreUpdated; 

    public IReadOnlyList<VaultCoreData> AvailableCores => _vaultCoreLoader.AvailableCores; 
    
    public VaultCoreFeatureResolver FeatureResolver { get; }

    public VaultCoreManager(TimeProvider timeProvider, Logger logger)
    {
        _timeProvider = timeProvider;
        _logger = logger;
        _vaultCoreLoader = new VaultCoreLoader(_logger);
        FeatureResolver = new VaultCoreFeatureResolver();
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
        CurrentlyLoadedCore = _vaultCoreLoader.LoadVaultCore(coreToLoad);
        
        if(CurrentlyLoadedCore != null)
        {
            CurrentlyLoadedCore.VaultCore.Initialise(FeatureResolver);
        }
    }

    public void UnloadVaultCore()
    {
        if(CurrentlyLoadedCore == null)
        {
            return;
        }
        
        CurrentlyLoadedCore.VaultCore.ShutDown();
        _vaultCoreLoader.UnloadVaultCore(CurrentlyLoadedCore);
    }
    
    public void Update()
    {
        if(CurrentlyLoadedCore == null)
        {
            return;
        }
        
        var vaultCore = CurrentlyLoadedCore.VaultCore;
        
        var newTimeSample = _timeProvider.HighResolutionTimerSample;
        var frameTime =  (double)(newTimeSample - _prevHighResTimerSample) / _timeProvider.HighResolutionTimerSampleFrequency;
        _prevHighResTimerSample = newTimeSample;
        
        if(vaultCore.FixedUpdateRateMs <= 0.0f)
        {
            //Update ever Frame
            vaultCore.Update((float)frameTime);
            _timeProvider.OnCoreUpdate((float)frameTime);
            OnCoreUpdated?.Invoke((float)frameTime);
        }
        else
        {
            //Update with fixed timesteps
            _updateAccum += frameTime;
            int numUpdatesAllowed = vaultCore.MaxNumFixedUpdatesInOneFrame;
            
            while(_updateAccum >= vaultCore.FixedUpdateRateMs)
            {
                vaultCore.Update(vaultCore.FixedUpdateRateMs);
                _timeProvider.OnCoreUpdate(vaultCore.FixedUpdateRateMs);
                OnCoreUpdated?.Invoke(vaultCore.FixedUpdateRateMs);
                _updateAccum -= vaultCore.FixedUpdateRateMs;
                
                if(numUpdatesAllowed > 0)
                {
                    numUpdatesAllowed--;
                    
                    if(numUpdatesAllowed <= 0)
                    {
                        _logger.LogWarning("Max Number of Updates per frame reached when trying to run fixed update Core. Breaking update loop");
                        break;
                    }
                }
            }
        }
    }
}