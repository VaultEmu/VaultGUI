namespace Vault;

public class AverageTimeCounter
{
    private readonly float[] _sampleRingBuffer;
    private int _ringBufferIndex;
    private float _avgAccum;
    private readonly float _oneOverNumSamples;
    
    private bool _firstUpdate = true;
    
    public AverageTimeCounter(uint numSamples = 100)
    {
        _sampleRingBuffer = new float[numSamples];
        _ringBufferIndex = 0;
        _avgAccum = 0;
        _oneOverNumSamples = 1.0f / numSamples;
    }
    
    public float Update(float newSample)
    {
        if(_firstUpdate)
        {
            for(int index = 0; index < _sampleRingBuffer.Length; ++index)
            {
                _sampleRingBuffer[index] = newSample;
            }
            _avgAccum = newSample * _sampleRingBuffer.Length;
            _firstUpdate = false;
        }
        _avgAccum += newSample - _sampleRingBuffer[_ringBufferIndex];
        _sampleRingBuffer[_ringBufferIndex] = newSample;
        _ringBufferIndex = (_ringBufferIndex + 1) % _sampleRingBuffer.Length;
        
        return _avgAccum * _oneOverNumSamples;
    }
}