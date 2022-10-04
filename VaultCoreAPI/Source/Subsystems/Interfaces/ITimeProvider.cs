namespace Vault;

//Subsystem that can provide time related functionality
public interface ITimeProvider : ISubsystem
{
    //Samples the High Resolution Timer and returns the number of ticks since program start
    ulong HighResolutionTimerSample { get; }
    
    //Returns number of ticks per second of the High Resolution Timer
    ulong HighResolutionTimerSampleFrequency { get; }
    
    //The delta time of the current frame (Seconds)
    float DeltaTime { get; }
    
    //The current fps
    float Fps { get; }
    
    //The delta time averaged over a period of time (Seconds)
    float AverageDeltaTime { get; }
    
    //The delta time averaged over a period of time
    float AverageFps { get; }
    
    //The total seconds since the program started 
    float TimeSinceStartup { get; }
}