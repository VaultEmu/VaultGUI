using VaultCore.CoreAPI;

namespace Vault;

public class TimeProvider : ITimeProvider
{
    private readonly HighResolutionTimer _highResolutionTimer;
    private readonly AverageTimeCounter _averageFpsTimer;
    private readonly ulong _startupTimeSample;
    
    private ulong _prevDeltaTimeSample;
    private float _lastDeltaTime;
    private float _averageDeltaTime;

    public ulong HighResolutionTimerSample => _highResolutionTimer.Sample;
    public ulong HighResolutionTimerSampleFrequency => _highResolutionTimer.SampleFrequency;
    public double DeltaTime => _lastDeltaTime;
    public float Fps =>  1.0f / (float)DeltaTime;
    public float AverageDeltaTime => _averageDeltaTime;
    public float AverageFps =>  1.0f / AverageDeltaTime;
    public double TimeSinceStartup => (float)(_highResolutionTimer.Sample - _startupTimeSample) / _highResolutionTimer.SampleFrequency;
    
    public TimeProvider()
    {
        _highResolutionTimer = new HighResolutionTimer();
        _prevDeltaTimeSample = _highResolutionTimer.Sample;
        _startupTimeSample = _prevDeltaTimeSample;
        _averageFpsTimer = new AverageTimeCounter(60);
        
        GlobalFeatures.RegisterFeature(this);
    }
    
    public void Update()
    {
        var sample = _highResolutionTimer.Sample;
        
        _lastDeltaTime = (float)(sample - _prevDeltaTimeSample) / _highResolutionTimer.SampleFrequency;
        _averageDeltaTime = _averageFpsTimer.Update(_lastDeltaTime);
        
        _prevDeltaTimeSample = sample;
    }
}
