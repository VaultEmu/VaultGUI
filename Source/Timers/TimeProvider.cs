using VaultCore.CoreAPI;

namespace Vault;

public class TimeProvider : IHighResTimer
{
    private readonly HighResolutionTimer _highResolutionTimer;
    
    private readonly AverageTimeCounter _averageRenderFpsTimer;
    private readonly AverageTimeCounter _averageCoreUpdateFpsTimer;
    
    private readonly ulong _startupTimeSample;
    
    private ulong _prevFrameDeltaTimeSample;
    private float _lastFrameDeltaTime;
    private float _averageFrameDeltaTime;
    
    private float _lastCoreUpdateDeltaTime;
    private float _averageCoreUpdateDeltaTime;


    public ulong HighResolutionTimerSample => _highResolutionTimer.Sample;
    public ulong HighResolutionTimerSampleFrequency => _highResolutionTimer.SampleFrequency;
    
    public float CoreUpdateDeltaTime => _lastCoreUpdateDeltaTime;
    public float AverageCoreUpdateDeltaTime => _averageCoreUpdateDeltaTime;
    public float CoreUpdateFps =>  1.0f / _lastCoreUpdateDeltaTime;
    public float AverageCoreUpdateFps =>  1.0f / _averageCoreUpdateDeltaTime;
    
    public float RenderFrameDeltaTime => _lastFrameDeltaTime;
    public float AverageFrameDeltaTime => _averageFrameDeltaTime;
    public float RenderFrameFps =>  1.0f / _lastFrameDeltaTime;
    public float AverageRenderFrameFps =>  1.0f / _averageFrameDeltaTime;
    public double TimeSinceStartup => (float)(_highResolutionTimer.Sample - _startupTimeSample) / _highResolutionTimer.SampleFrequency;
    
    public TimeProvider()
    {
        _highResolutionTimer = new HighResolutionTimer();
        _prevFrameDeltaTimeSample = _highResolutionTimer.Sample;
        _startupTimeSample = _prevFrameDeltaTimeSample;
        _averageRenderFpsTimer = new AverageTimeCounter(60);
        _averageCoreUpdateFpsTimer = new AverageTimeCounter();
        
        GlobalFeatures.RegisterFeature(this);
    }
    
    public void OnFrameUpdate()
    {
        var sample = _highResolutionTimer.Sample;
        
        _lastFrameDeltaTime = (float)(sample - _prevFrameDeltaTimeSample) / _highResolutionTimer.SampleFrequency;
        _averageFrameDeltaTime = _averageRenderFpsTimer.Update(_lastFrameDeltaTime);
        
        _prevFrameDeltaTimeSample = sample;
    }
    
    public void OnCoreUpdate(float deltaTime)
    {
        _lastCoreUpdateDeltaTime = deltaTime;
        _averageCoreUpdateDeltaTime = _averageCoreUpdateFpsTimer.Update(_lastCoreUpdateDeltaTime);
    }
}
