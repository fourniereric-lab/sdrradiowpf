using NAudio.Wave;
using SdrRadioWpf.Models;

namespace SdrRadioWpf.Services;

/// <summary>
/// Demodulates IQ samples and plays audio using NAudio.
/// Supports FM, AM, USB, LSB, CW and WBFM modes.
/// </summary>
public sealed class AudioService : IDisposable
{
    private WaveOut? _waveOut;
    private BufferedWaveProvider? _waveProvider;
    private float _volume = 0.5f;
    private DemodulationMode _mode = DemodulationMode.WBFM;
    private bool _disposed;

    // FM demodulation state
    private float _prevI;
    private float _prevQ;

    private const int AudioSampleRate = 48000;

    public float Volume
    {
        get => _volume;
        set
        {
            _volume = Math.Clamp(value, 0f, 1f);
            if (_waveOut != null) _waveOut.Volume = _volume;
        }
    }

    public DemodulationMode Mode
    {
        get => _mode;
        set => _mode = value;
    }

    public void Start()
    {
        var format = WaveFormat.CreateIeeeFloatWaveFormat(AudioSampleRate, 1);
        _waveProvider = new BufferedWaveProvider(format)
        {
            BufferDuration = TimeSpan.FromSeconds(2),
            DiscardOnBufferOverflow = true
        };

        _waveOut = new WaveOut { Volume = _volume };
        _waveOut.Init(_waveProvider);
        _waveOut.Play();
    }

    public void Stop()
    {
        _waveOut?.Stop();
        _waveOut?.Dispose();
        _waveOut = null;
        _waveProvider = null;
    }

    /// <summary>
    /// Processes raw RTL-SDR IQ bytes (unsigned 8-bit, interleaved I/Q)
    /// and feeds demodulated audio samples into the audio buffer.
    /// </summary>
    public void ProcessSamples(byte[] iqBytes, int inputSampleRate)
    {
        if (_waveProvider == null) return;

        int sampleCount = iqBytes.Length / 2;
        var audio = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float sampleI = (iqBytes[i * 2] - 128) / 128f;
            float sampleQ = (iqBytes[i * 2 + 1] - 128) / 128f;

            audio[i] = _mode switch
            {
                DemodulationMode.AM => DemodAm(sampleI, sampleQ),
                DemodulationMode.USB => DemodSsb(sampleI, sampleQ, upper: true),
                DemodulationMode.LSB => DemodSsb(sampleI, sampleQ, upper: false),
                DemodulationMode.CW => DemodCw(sampleI, sampleQ),
                _ => DemodFm(sampleI, sampleQ)   // FM / WBFM
            };
        }

        // Resample from inputSampleRate to AudioSampleRate (simple decimation)
        int decimation = Math.Max(1, inputSampleRate / AudioSampleRate);
        var decimated = new float[sampleCount / decimation];
        for (int i = 0; i < decimated.Length; i++)
            decimated[i] = audio[i * decimation];

        // Convert to bytes and add to buffer
        var bytes = new byte[decimated.Length * 4];
        Buffer.BlockCopy(decimated, 0, bytes, 0, bytes.Length);
        _waveProvider.AddSamples(bytes, 0, bytes.Length);
    }

    // -----------------------------------------------------------------------
    // Demodulation implementations
    // -----------------------------------------------------------------------

    private float DemodFm(float i, float q)
    {
        float demod = i * _prevQ - q * _prevI;
        float denom = i * i + q * q;
        float result = denom > 1e-10f ? demod / denom : 0f;
        _prevI = i;
        _prevQ = q;
        return Math.Clamp(result, -1f, 1f);
    }

    private static float DemodAm(float i, float q) =>
        MathF.Sqrt(i * i + q * q);

    private static float DemodSsb(float i, float q, bool upper) =>
        upper ? i + q : i - q;

    // CW uses the same envelope detection as AM; kept separate to allow future
    // extensions such as narrow-band filtering or BFO tone injection.
    private static float DemodCw(float i, float q) =>
        MathF.Sqrt(i * i + q * q);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
    }
}
