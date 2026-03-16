namespace SdrRadioWpf.Models;

public class RadioSettings
{
    public double FrequencyHz { get; set; } = 100_000_000; // 100 MHz default
    public int SampleRate { get; set; } = 2_048_000;       // 2.048 MSPS
    public int GainTenths { get; set; } = 0;               // Auto gain
    public DemodulationMode Mode { get; set; } = DemodulationMode.WBFM;
    public double Volume { get; set; } = 0.5;
    public int PpmCorrection { get; set; } = 0;
    public int DeviceIndex { get; set; } = 0;
}
