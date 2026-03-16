using SdrRadioWpf.Models;

namespace SdrRadioWpf.Services;

/// <summary>
/// Abstraction over RTL-SDR hardware. Opens the device, configures it and
/// streams IQ samples to registered consumers.
/// </summary>
public sealed class SdrService : IDisposable
{
    private IntPtr _device = IntPtr.Zero;
    private Thread? _streamThread;
    private volatile bool _running;
    private bool _disposed;

    private RtlSdrNative.RtlSdrReadAsyncCallback? _asyncCallback;

    public event EventHandler<IqSamplesEventArgs>? SamplesAvailable;
    public event EventHandler<string>? StatusChanged;

    public bool IsOpen => _device != IntPtr.Zero;

    // -----------------------------------------------------------------------
    // Device enumeration
    // -----------------------------------------------------------------------

    public static IEnumerable<string> GetDeviceNames()
    {
        uint count;
        try { count = RtlSdrNative.GetDeviceCount(); }
        catch (DllNotFoundException) { yield break; }

        for (uint i = 0; i < count; i++)
        {
            var ptr = RtlSdrNative.GetDeviceName(i);
            yield return ptr == IntPtr.Zero
                ? $"Device {i}"
                : System.Runtime.InteropServices.Marshal.PtrToStringAnsi(ptr) ?? $"Device {i}";
        }
    }

    // -----------------------------------------------------------------------
    // Open / Close
    // -----------------------------------------------------------------------

    public bool Open(int deviceIndex = 0)
    {
        if (_device != IntPtr.Zero)
            Close();

        int result;
        try { result = RtlSdrNative.Open(out _device, (uint)deviceIndex); }
        catch (DllNotFoundException ex)
        {
            RaiseStatus($"RTL-SDR library not found: {ex.Message}");
            return false;
        }

        if (result != 0 || _device == IntPtr.Zero)
        {
            RaiseStatus($"Failed to open device {deviceIndex} (error {result}).");
            _device = IntPtr.Zero;
            return false;
        }

        RaiseStatus($"Device {deviceIndex} opened.");
        return true;
    }

    public void Close()
    {
        StopStreaming();
        if (_device != IntPtr.Zero)
        {
            RtlSdrNative.Close(_device);
            _device = IntPtr.Zero;
            RaiseStatus("Device closed.");
        }
    }

    // -----------------------------------------------------------------------
    // Configuration
    // -----------------------------------------------------------------------

    public bool Configure(RadioSettings settings)
    {
        if (_device == IntPtr.Zero) return false;

        RtlSdrNative.SetFreqCorrection(_device, settings.PpmCorrection);
        RtlSdrNative.SetSampleRate(_device, (uint)settings.SampleRate);
        RtlSdrNative.SetCenterFreq(_device, (uint)settings.FrequencyHz);

        if (settings.GainTenths == 0)
        {
            // Auto gain
            RtlSdrNative.SetTunerGainMode(_device, 0);
        }
        else
        {
            RtlSdrNative.SetTunerGainMode(_device, 1);
            RtlSdrNative.SetTunerGain(_device, settings.GainTenths);
        }

        RtlSdrNative.ResetBuffer(_device);
        RaiseStatus($"Tuned to {settings.FrequencyHz / 1e6:F3} MHz, mode {settings.Mode}.");
        return true;
    }

    public bool SetFrequency(double frequencyHz)
    {
        if (_device == IntPtr.Zero) return false;
        return RtlSdrNative.SetCenterFreq(_device, (uint)frequencyHz) == 0;
    }

    public int[] GetSupportedGains()
    {
        if (_device == IntPtr.Zero) return Array.Empty<int>();
        int count = RtlSdrNative.GetTunerGains(_device, null);
        if (count <= 0) return Array.Empty<int>();
        var gains = new int[count];
        RtlSdrNative.GetTunerGains(_device, gains);
        return gains;
    }

    // -----------------------------------------------------------------------
    // Streaming
    // -----------------------------------------------------------------------

    public void StartStreaming()
    {
        if (_running || _device == IntPtr.Zero) return;
        _running = true;
        _asyncCallback = OnAsyncData;
        _streamThread = new Thread(StreamProc) { IsBackground = true, Name = "RtlSdr-Stream" };
        _streamThread.Start();
        RaiseStatus("Streaming started.");
    }

    public void StopStreaming()
    {
        if (!_running) return;
        _running = false;
        if (_device != IntPtr.Zero)
            RtlSdrNative.CancelAsync(_device);
        _streamThread?.Join(2000);
        _streamThread = null;
        RaiseStatus("Streaming stopped.");
    }

    private void StreamProc()
    {
        // 16 buffers, each 16 KB of IQ bytes (8192 complex samples)
        RtlSdrNative.ReadAsync(_device, _asyncCallback!, IntPtr.Zero, 16, 16384);
    }

    private void OnAsyncData(IntPtr buf, uint len, IntPtr ctx)
    {
        if (!_running) return;
        var data = new byte[len];
        System.Runtime.InteropServices.Marshal.Copy(buf, data, 0, (int)len);
        SamplesAvailable?.Invoke(this, new IqSamplesEventArgs(data));
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private void RaiseStatus(string message) =>
        StatusChanged?.Invoke(this, message);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Close();
    }
}

public sealed class IqSamplesEventArgs : EventArgs
{
    public byte[] Data { get; }

    public IqSamplesEventArgs(byte[] data)
    {
        Data = data;
    }
}
