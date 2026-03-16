using System.Runtime.InteropServices;

namespace SdrRadioWpf.Services;

/// <summary>
/// Provides P/Invoke bindings for the RTL-SDR library (rtlsdr.dll / librtlsdr.so).
/// </summary>
internal static class RtlSdrNative
{
    private const string LibraryName = "rtlsdr";

    [DllImport(LibraryName, EntryPoint = "rtlsdr_get_device_count", CallingConvention = CallingConvention.Cdecl)]
    public static extern uint GetDeviceCount();

    [DllImport(LibraryName, EntryPoint = "rtlsdr_get_device_name", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetDeviceName(uint index);

    [DllImport(LibraryName, EntryPoint = "rtlsdr_open", CallingConvention = CallingConvention.Cdecl)]
    public static extern int Open(out IntPtr dev, uint index);

    [DllImport(LibraryName, EntryPoint = "rtlsdr_close", CallingConvention = CallingConvention.Cdecl)]
    public static extern int Close(IntPtr dev);

    [DllImport(LibraryName, EntryPoint = "rtlsdr_set_center_freq", CallingConvention = CallingConvention.Cdecl)]
    public static extern int SetCenterFreq(IntPtr dev, uint freq);

    [DllImport(LibraryName, EntryPoint = "rtlsdr_get_center_freq", CallingConvention = CallingConvention.Cdecl)]
    public static extern uint GetCenterFreq(IntPtr dev);

    [DllImport(LibraryName, EntryPoint = "rtlsdr_set_sample_rate", CallingConvention = CallingConvention.Cdecl)]
    public static extern int SetSampleRate(IntPtr dev, uint rate);

    [DllImport(LibraryName, EntryPoint = "rtlsdr_get_sample_rate", CallingConvention = CallingConvention.Cdecl)]
    public static extern uint GetSampleRate(IntPtr dev);

    [DllImport(LibraryName, EntryPoint = "rtlsdr_set_tuner_gain_mode", CallingConvention = CallingConvention.Cdecl)]
    public static extern int SetTunerGainMode(IntPtr dev, int manual);

    [DllImport(LibraryName, EntryPoint = "rtlsdr_set_tuner_gain", CallingConvention = CallingConvention.Cdecl)]
    public static extern int SetTunerGain(IntPtr dev, int gain);

    [DllImport(LibraryName, EntryPoint = "rtlsdr_get_tuner_gain", CallingConvention = CallingConvention.Cdecl)]
    public static extern int GetTunerGain(IntPtr dev);

    [DllImport(LibraryName, EntryPoint = "rtlsdr_get_tuner_gains", CallingConvention = CallingConvention.Cdecl)]
    public static extern int GetTunerGains(IntPtr dev, [Out] int[]? gains);

    [DllImport(LibraryName, EntryPoint = "rtlsdr_set_freq_correction", CallingConvention = CallingConvention.Cdecl)]
    public static extern int SetFreqCorrection(IntPtr dev, int ppm);

    [DllImport(LibraryName, EntryPoint = "rtlsdr_reset_buffer", CallingConvention = CallingConvention.Cdecl)]
    public static extern int ResetBuffer(IntPtr dev);

    [DllImport(LibraryName, EntryPoint = "rtlsdr_read_sync", CallingConvention = CallingConvention.Cdecl)]
    public static extern int ReadSync(IntPtr dev, byte[] buf, int len, out int nRead);

    [DllImport(LibraryName, EntryPoint = "rtlsdr_read_async", CallingConvention = CallingConvention.Cdecl)]
    public static extern int ReadAsync(IntPtr dev, RtlSdrReadAsyncCallback cb, IntPtr ctx, uint bufNum, uint bufLen);

    [DllImport(LibraryName, EntryPoint = "rtlsdr_cancel_async", CallingConvention = CallingConvention.Cdecl)]
    public static extern int CancelAsync(IntPtr dev);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void RtlSdrReadAsyncCallback(IntPtr buf, uint len, IntPtr ctx);
}
