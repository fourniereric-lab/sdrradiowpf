using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using SdrRadioWpf.Models;
using SdrRadioWpf.Services;

namespace SdrRadioWpf.ViewModels;

public sealed class MainViewModel : ViewModelBase, IDisposable
{
    private readonly SdrService _sdrService;
    private readonly AudioService _audioService;

    // -----------------------------------------------------------------------
    // Constructor
    // -----------------------------------------------------------------------

    public MainViewModel()
    {
        _sdrService = new SdrService();
        _audioService = new AudioService();

        _sdrService.StatusChanged += (_, msg) =>
            Application.Current?.Dispatcher.Invoke(() => StatusMessage = msg);

        _sdrService.SamplesAvailable += (_, args) =>
            _audioService.ProcessSamples(args.Data, Settings.SampleRate);

        // Populate device list
        RefreshDevices();

        // Commands
        StartCommand = new RelayCommand(Start, () => !_isRunning && SelectedDeviceIndex >= 0);
        StopCommand = new RelayCommand(Stop, () => _isRunning);
        RefreshDevicesCommand = new RelayCommand(RefreshDevices, () => !_isRunning);
        StepUpCommand = new RelayCommand(() => FrequencyMHz += FrequencyStepMHz);
        StepDownCommand = new RelayCommand(() => FrequencyMHz -= FrequencyStepMHz);
    }

    // -----------------------------------------------------------------------
    // Properties bound to the UI
    // -----------------------------------------------------------------------

    private RadioSettings _settings = new();
    public RadioSettings Settings
    {
        get => _settings;
        private set => SetProperty(ref _settings, value);
    }

    // Frequency in MHz for convenient UI binding
    private double _frequencyMHz = 100.0;
    public double FrequencyMHz
    {
        get => _frequencyMHz;
        set
        {
            if (!SetProperty(ref _frequencyMHz, Math.Clamp(value, 0.1, 1766.0))) return;
            _settings.FrequencyHz = _frequencyMHz * 1e6;
            if (_isRunning) _sdrService.SetFrequency(_settings.FrequencyHz);
        }
    }

    private double _frequencyStepMHz = 0.1;
    public double FrequencyStepMHz
    {
        get => _frequencyStepMHz;
        set => SetProperty(ref _frequencyStepMHz, value);
    }

    private DemodulationMode _selectedMode = DemodulationMode.WBFM;
    public DemodulationMode SelectedMode
    {
        get => _selectedMode;
        set
        {
            if (!SetProperty(ref _selectedMode, value)) return;
            _settings.Mode = value;
            _audioService.Mode = value;
        }
    }

    public IEnumerable<DemodulationMode> AvailableModes =>
        Enum.GetValues<DemodulationMode>();

    private double _volume = 50;
    public double Volume
    {
        get => _volume;
        set
        {
            if (!SetProperty(ref _volume, value)) return;
            _audioService.Volume = (float)(value / 100.0);
        }
    }

    private int _selectedDeviceIndex = -1;
    public int SelectedDeviceIndex
    {
        get => _selectedDeviceIndex;
        set => SetProperty(ref _selectedDeviceIndex, value);
    }

    public ObservableCollection<string> Devices { get; } = new();

    private string _statusMessage = "Ready. Please connect an RTL-SDR device and press Start.";
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    private double _signalLevel;
    public double SignalLevel
    {
        get => _signalLevel;
        set => SetProperty(ref _signalLevel, value);
    }

    private bool _isRunning;
    public bool IsRunning
    {
        get => _isRunning;
        private set => SetProperty(ref _isRunning, value);
    }

    private int _ppmCorrection;
    public int PpmCorrection
    {
        get => _ppmCorrection;
        set
        {
            if (!SetProperty(ref _ppmCorrection, value)) return;
            _settings.PpmCorrection = value;
        }
    }

    // -----------------------------------------------------------------------
    // Commands
    // -----------------------------------------------------------------------

    public ICommand StartCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand RefreshDevicesCommand { get; }
    public ICommand StepUpCommand { get; }
    public ICommand StepDownCommand { get; }

    // -----------------------------------------------------------------------
    // Actions
    // -----------------------------------------------------------------------

    private void RefreshDevices()
    {
        Devices.Clear();
        foreach (var name in SdrService.GetDeviceNames())
            Devices.Add(name);

        if (Devices.Count > 0)
        {
            SelectedDeviceIndex = 0;
            StatusMessage = $"{Devices.Count} device(s) found.";
        }
        else
        {
            SelectedDeviceIndex = -1;
            StatusMessage = "No RTL-SDR devices found. Please connect a device.";
        }
    }

    private void Start()
    {
        _settings.FrequencyHz = FrequencyMHz * 1e6;
        _settings.Mode = SelectedMode;
        _settings.DeviceIndex = SelectedDeviceIndex;

        if (!_sdrService.Open(SelectedDeviceIndex)) return;
        if (!_sdrService.Configure(_settings))
        {
            _sdrService.Close();
            return;
        }

        _audioService.Start();
        _audioService.Volume = (float)(Volume / 100.0);
        _audioService.Mode = SelectedMode;

        _sdrService.StartStreaming();
        IsRunning = true;
    }

    private void Stop()
    {
        _sdrService.StopStreaming();
        _audioService.Stop();
        _sdrService.Close();
        IsRunning = false;
    }

    // -----------------------------------------------------------------------
    // IDisposable
    // -----------------------------------------------------------------------

    public void Dispose()
    {
        Stop();
        _sdrService.Dispose();
        _audioService.Dispose();
    }
}
