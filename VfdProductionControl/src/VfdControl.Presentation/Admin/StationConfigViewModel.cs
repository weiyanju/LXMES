using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VfdControl.Application.Admin;
using VfdControl.Application.Common;
using VfdControl.Domain.Stations;

namespace VfdControl.Presentation.Admin;

public sealed partial class StationConfigViewModel : ObservableObject
{
    public const string NoPortDisplay = "\u65E0";

    private readonly StationConfigurationService _stationConfigurationService;
    private readonly ISerialPortCatalog _serialPortCatalog;
    private readonly StationConfigurationChangeNotifier _changeNotifier;
    private readonly DeviceModelCatalog _deviceModelCatalog;
    private Station? _station;
    private bool _isRefreshingSlots;
    private bool _hasUnsavedSlotChanges;

    [ObservableProperty]
    private string _stationName = "\u672A\u52A0\u8F7D\u5DE5\u4F4D";

    [ObservableProperty]
    private StationSlotRowViewModel? _selectedSlot;

    [ObservableProperty]
    private string _statusMessage = "\u7CFB\u7EDF\u7BA1\u7406\u5C31\u7EEA\u3002";

    [ObservableProperty]
    private DeviceModelRowViewModel? _selectedDeviceModel;

    [ObservableProperty]
    private LogicalPointRowViewModel? _selectedLogicalPoint;

    public StationConfigViewModel(StationConfigurationService stationConfigurationService)
        : this(stationConfigurationService, new EmptySerialPortCatalog(), new StationConfigurationChangeNotifier(), new DeviceModelCatalog())
    {
    }

    public StationConfigViewModel(
        StationConfigurationService stationConfigurationService,
        ISerialPortCatalog serialPortCatalog,
        StationConfigurationChangeNotifier changeNotifier)
        : this(stationConfigurationService, serialPortCatalog, changeNotifier, new DeviceModelCatalog())
    {
    }

    public StationConfigViewModel(
        StationConfigurationService stationConfigurationService,
        ISerialPortCatalog serialPortCatalog,
        StationConfigurationChangeNotifier changeNotifier,
        DeviceModelCatalog deviceModelCatalog)
    {
        _stationConfigurationService = stationConfigurationService;
        _serialPortCatalog = serialPortCatalog;
        _changeNotifier = changeNotifier;
        _deviceModelCatalog = deviceModelCatalog;
    }

    public ObservableCollection<StationSlotRowViewModel> Slots { get; } = [];

    public ObservableCollection<string> PortOptions { get; } = [NoPortDisplay];

    public IReadOnlyList<string> AdminSections { get; } =
    [
        "\u5DE5\u4F4D / \u69FD\u4F4D\u914D\u7F6E",
        "\u8BBE\u5907\u578B\u53F7\u914D\u7F6E"
    ];

    public ObservableCollection<DeviceModelRowViewModel> DeviceModels => _deviceModelCatalog.DeviceModels;

    public IReadOnlyList<LogicalPointRowViewModel> LogicalPoints => _deviceModelCatalog.LogicalPoints;

    public IReadOnlyList<LogicalPointRowViewModel> SelectedDeviceModelPoints =>
        SelectedDeviceModel?.Points ?? [];

    public string SelectedDeviceModelPointCountText =>
        SelectedDeviceModel is null
            ? "\u672A\u9009\u62E9\u8BBE\u5907\u578B\u53F7"
            : $"{SelectedDeviceModel.Points.Count} \u4E2A\u903B\u8F91\u70B9\u4F4D";

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        RefreshPortOptions();
        SelectedDeviceModel ??= DeviceModels.FirstOrDefault();
        var stations = await _stationConfigurationService.ListStationsAsync(cancellationToken);
        _station = stations.FirstOrDefault();
        StationName = _station?.Name ?? "\u672A\u52A0\u8F7D\u5DE5\u4F4D";

        if (_station is not null)
        {
            await _stationConfigurationService.AssignPortsBySlotOrderAsync(_station.Id, ScannedPorts(), cancellationToken);
        }

        RefreshSlots();
        _hasUnsavedSlotChanges = false;
        StatusMessage = FormatConfigurationSummary(ScannedPorts().Count);
    }

    [RelayCommand]
    private async Task RefreshPortsAsync(CancellationToken cancellationToken)
    {
        RefreshPortOptions();
        if (_station is not null)
        {
            await _stationConfigurationService.AssignPortsBySlotOrderAsync(_station.Id, ScannedPorts(), cancellationToken);
            RefreshSlots();
            await _changeNotifier.NotifyChangedAsync(cancellationToken);
        }

        _hasUnsavedSlotChanges = false;
        StatusMessage = $"{FormatConfigurationSummary(ScannedPorts().Count)} / \u4E0A\u6B21\u626B\u63CF\uFF1A{DateTime.Now:HH:mm}";
    }

    [RelayCommand]
    private async Task AddSlotAsync(CancellationToken cancellationToken)
    {
        if (_station is null)
        {
            StatusMessage = "\u5DE5\u4F4D\u5C1A\u672A\u52A0\u8F7D\u3002";
            return;
        }

        var nextSlotNumber = Slots.Count == 0 ? 1 : Slots.Max(slot => slot.SlotNumber) + 1;
        var portName = ScannedPorts().ElementAtOrDefault(Slots.Count) ?? NoPortDisplay;
        var result = await _stationConfigurationService.AddNextSlotAsync(
            _station.Id,
            portName,
            nextSlotNumber,
            nextSlotNumber + 10,
            nextSlotNumber + 20,
            9600,
            cancellationToken);

        StatusMessage = result.IsSuccess ? "\u69FD\u4F4D\u5DF2\u6DFB\u52A0\u3002" : result.Message;
        RefreshSlots(result.Value?.Number.Value ?? nextSlotNumber);
        if (result.IsSuccess)
        {
            _hasUnsavedSlotChanges = false;
            await _changeNotifier.NotifyChangedAsync(cancellationToken);
        }
    }

    [RelayCommand]
    private async Task SaveSlotAsync(StationSlotRowViewModel? slot, CancellationToken cancellationToken)
    {
        if (_station is null || slot is null)
        {
            StatusMessage = "\u8BF7\u5148\u9009\u62E9\u69FD\u4F4D\u3002";
            return;
        }

        var result = await SaveSlotCoreAsync(slot, cancellationToken);

        StatusMessage = result.IsSuccess ? "\u69FD\u4F4D\u914D\u7F6E\u5DF2\u4FDD\u5B58\u3002" : result.Message;
        RefreshSlots(slot.SlotNumber);
        if (result.IsSuccess)
        {
            _hasUnsavedSlotChanges = false;
            await _changeNotifier.NotifyChangedAsync(cancellationToken);
        }
    }

    [RelayCommand]
    private async Task SaveAllSlotsAsync(CancellationToken cancellationToken)
    {
        if (_station is null)
        {
            StatusMessage = "\u5DE5\u4F4D\u5C1A\u672A\u52A0\u8F7D\u3002";
            return;
        }

        foreach (var slot in Slots.ToList())
        {
            var result = await SaveSlotCoreAsync(slot, cancellationToken);
            if (!result.IsSuccess)
            {
                StatusMessage = result.Message;
                RefreshSlots(slot.SlotNumber);
                return;
            }
        }

        RefreshSlots(SelectedSlot?.SlotNumber);
        _hasUnsavedSlotChanges = false;
        await _changeNotifier.NotifyChangedAsync(cancellationToken);
        StatusMessage = "\u914D\u7F6E\u5DF2\u4FDD\u5B58\u3002";
    }

    [RelayCommand]
    private async Task DeleteSlotAsync(StationSlotRowViewModel? slot, CancellationToken cancellationToken)
    {
        if (_station is null || slot is null)
        {
            StatusMessage = "\u8BF7\u5148\u9009\u62E9\u69FD\u4F4D\u3002";
            return;
        }

        var result = await _stationConfigurationService.DeleteSlotAsync(_station.Id, slot.SlotNumber, cancellationToken);
        StatusMessage = result.IsSuccess ? "\u69FD\u4F4D\u5DF2\u5220\u9664\u3002" : result.Message;
        RefreshSlots();
        if (result.IsSuccess)
        {
            _hasUnsavedSlotChanges = false;
            await _changeNotifier.NotifyChangedAsync(cancellationToken);
        }
    }

    [RelayCommand]
    private Task AddLogicalPointAsync()
    {
        if (SelectedDeviceModel is null)
        {
            StatusMessage = "\u8BF7\u5148\u9009\u62E9\u8BBE\u5907\u578B\u53F7\u3002";
            return Task.CompletedTask;
        }

        var source = SelectedDeviceModel.DeviceRole;
        var point = new LogicalPointRowViewModel(
            NextCustomLogicalKey(SelectedDeviceModel),
            "\u65B0\u589E\u903B\u8F91\u70B9\u4F4D",
            source,
            "\u8BFB\u53D6",
            "03",
            "",
            "Decimal",
            "",
            "\u81EA\u5B9A\u4E49\u70B9\u4F4D",
            isCustom: true);

        SelectedDeviceModel.Points.Add(point);
        SelectedLogicalPoint = point;
        _deviceModelCatalog.NotifyChanged();
        RefreshSelectedDeviceModelPointState();
        StatusMessage = "\u81EA\u5B9A\u4E49\u903B\u8F91\u70B9\u4F4D\u5DF2\u6DFB\u52A0\uFF0C\u8BF7\u8865\u5145\u5BC4\u5B58\u5668\u548C\u663E\u793A\u540D\u3002";
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task DeleteLogicalPointAsync()
    {
        if (SelectedDeviceModel is null || SelectedLogicalPoint is null)
        {
            StatusMessage = "\u8BF7\u5148\u9009\u62E9\u903B\u8F91\u70B9\u4F4D\u3002";
            return Task.CompletedTask;
        }

        if (!SelectedLogicalPoint.IsCustom)
        {
            StatusMessage = "\u5185\u7F6E\u903B\u8F91\u70B9\u4F4D\u4E0D\u5141\u8BB8\u5220\u9664\uFF0C\u53EF\u4EE5\u76F4\u63A5\u4FEE\u6539\u914D\u7F6E\u3002";
            return Task.CompletedTask;
        }

        var removedIndex = SelectedDeviceModel.Points.IndexOf(SelectedLogicalPoint);
        SelectedDeviceModel.Points.Remove(SelectedLogicalPoint);
        SelectedLogicalPoint = SelectedDeviceModel.Points.ElementAtOrDefault(Math.Max(removedIndex - 1, 0));
        _deviceModelCatalog.NotifyChanged();
        RefreshSelectedDeviceModelPointState();
        StatusMessage = "\u81EA\u5B9A\u4E49\u903B\u8F91\u70B9\u4F4D\u5DF2\u5220\u9664\u3002";
        return Task.CompletedTask;
    }

    private void RefreshPortOptions()
    {
        PortOptions.Clear();
        PortOptions.Add(NoPortDisplay);
        foreach (var port in ScannedPorts())
        {
            PortOptions.Add(port);
        }
    }

    private IReadOnlyList<string> ScannedPorts()
    {
        return _serialPortCatalog
            .ListPortNames()
            .Where(port => !string.IsNullOrWhiteSpace(port))
            .Select(port => port.Trim().ToUpperInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private void RefreshSlots(int? selectedSlotNumber = null)
    {
        _isRefreshingSlots = true;
        Slots.Clear();
        if (_station is null)
        {
            _isRefreshingSlots = false;
            return;
        }

        foreach (var slot in _station.Slots.OrderBy(slot => slot.Number.Value))
        {
            var row = new StationSlotRowViewModel(slot);
            row.PropertyChanged += OnSlotRowPropertyChanged;
            Slots.Add(row);
        }

        SelectedSlot = selectedSlotNumber is null
            ? Slots.FirstOrDefault()
            : Slots.FirstOrDefault(slot => slot.SlotNumber == selectedSlotNumber) ?? Slots.FirstOrDefault();
        _isRefreshingSlots = false;
    }

    private void OnSlotRowPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs args)
    {
        if (_isRefreshingSlots)
        {
            return;
        }

        if (args.PropertyName is nameof(StationSlotRowViewModel.DisplayName)
            or nameof(StationSlotRowViewModel.PortName)
            or nameof(StationSlotRowViewModel.VfdAddress)
            or nameof(StationSlotRowViewModel.VoltageMeterAddress)
            or nameof(StationSlotRowViewModel.CurrentMeterAddress)
            or nameof(StationSlotRowViewModel.BaudRate))
        {
            _hasUnsavedSlotChanges = true;
            StatusMessage = FormatConfigurationSummary(ScannedPorts().Count);
        }
    }

    private async Task<AppResult<StationSlot>> SaveSlotCoreAsync(
        StationSlotRowViewModel slot,
        CancellationToken cancellationToken)
    {
        var result = await _stationConfigurationService.UpdateSlotConfigurationAsync(
            _station!.Id,
            slot.SlotNumber,
            slot.PortName,
            slot.VfdAddress,
            slot.VoltageMeterAddress,
            slot.CurrentMeterAddress,
            slot.BaudRate,
            cancellationToken);
        if (!result.IsSuccess)
        {
            return result;
        }

        return await _stationConfigurationService.UpdateSlotDisplayNameAsync(
            _station.Id,
            slot.SlotNumber,
            slot.DisplayName,
            cancellationToken);
    }

    private static string FormatPortDetectionStatus(int portCount)
    {
        return portCount == 0
            ? "\u7B49\u5F85\u626B\u63CF\u4E32\u53E3\u3002"
            : $"\u5DF2\u68C0\u6D4B\u5230 {portCount} \u4E2A\u4E32\u53E3";
    }

    private string FormatConfigurationSummary(int portCount)
    {
        var unsavedCount = _hasUnsavedSlotChanges ? 1 : 0;
        return $"{FormatPortDetectionStatus(portCount).TrimEnd('\u3002')} / \u5DF2\u914D\u7F6E {Slots.Count} \u4E2A\u69FD\u4F4D / \u672A\u4FDD\u5B58 {unsavedCount} \u9879";
    }

    partial void OnSelectedDeviceModelChanged(DeviceModelRowViewModel? value)
    {
        SelectedLogicalPoint = value?.Points.FirstOrDefault();
        RefreshSelectedDeviceModelPointState();
    }

    private void RefreshSelectedDeviceModelPointState()
    {
        OnPropertyChanged(nameof(LogicalPoints));
        OnPropertyChanged(nameof(SelectedDeviceModelPoints));
        OnPropertyChanged(nameof(SelectedDeviceModelPointCountText));
    }

    private static string NextCustomLogicalKey(DeviceModelRowViewModel model)
    {
        var prefix = model.DeviceRole.Equals("VFD", StringComparison.OrdinalIgnoreCase)
            ? "Vfd"
            : "Instrument";

        var index = 1;
        string candidate;
        do
        {
            candidate = $"{prefix}:Custom{index}";
            index++;
        }
        while (model.Points.Any(point => point.LogicalKey.Equals(candidate, StringComparison.OrdinalIgnoreCase)));

        return candidate;
    }
}

public interface ISerialPortCatalog
{
    IReadOnlyList<string> ListPortNames();
}

public sealed class EmptySerialPortCatalog : ISerialPortCatalog
{
    public IReadOnlyList<string> ListPortNames() => [];
}

public sealed class StationConfigurationChangeNotifier
{
    public event Func<CancellationToken, Task>? Changed;

    public async Task NotifyChangedAsync(CancellationToken cancellationToken = default)
    {
        if (Changed is null)
        {
            return;
        }

        foreach (var handler in Changed.GetInvocationList().Cast<Func<CancellationToken, Task>>())
        {
            await handler(cancellationToken);
        }
    }
}

public sealed partial class StationSlotRowViewModel : ObservableObject
{
    public StationSlotRowViewModel(StationSlot slot)
    {
        SlotNumber = slot.Number.Value;
        DisplayName = slot.DisplayName;
        PortName = slot.CommunicationConfig.PortDisplay;
        VfdAddress = slot.CommunicationConfig.VfdAddress.Value;
        VoltageMeterAddress = slot.CommunicationConfig.VoltageMeterAddress.Value;
        CurrentMeterAddress = slot.CommunicationConfig.CurrentMeterAddress.Value;
        BaudRate = slot.CommunicationConfig.BaudRate;
        StatusText = slot.CommunicationConfig.HasPort ? "\u5728\u7EBF" : "\u672A\u68C0\u6D4B";
        Instruments = slot.Instruments.Select(instrument => new InstrumentRowViewModel(instrument)).ToList();
    }

    public int SlotNumber { get; }

    [ObservableProperty]
    private string _displayName;

    [ObservableProperty]
    private string _portName;

    [ObservableProperty]
    private int _vfdAddress;

    [ObservableProperty]
    private int _voltageMeterAddress;

    [ObservableProperty]
    private int _currentMeterAddress;

    [ObservableProperty]
    private int _baudRate;

    [ObservableProperty]
    private string _statusText;

    public IReadOnlyList<InstrumentRowViewModel> Instruments { get; }

    partial void OnPortNameChanged(string value)
    {
        StatusText = value == StationConfigViewModel.NoPortDisplay
            ? "\u672A\u68C0\u6D4B"
            : "\u5728\u7EBF";
    }
}

public sealed class InstrumentRowViewModel
{
    public InstrumentRowViewModel(SlotInstrument instrument)
    {
        Name = instrument.Name;
        Points = instrument.Points.Select(point => new InstrumentPointRowViewModel(
            point.Key,
            point.Name,
            point.DataType.ToString(),
            point.Unit)).ToList();
    }

    public string Name { get; }

    public IReadOnlyList<InstrumentPointRowViewModel> Points { get; }
}

public sealed record InstrumentPointRowViewModel(
    string Key,
    string Name,
    string DataType,
    string Unit);

public sealed partial class DeviceModelRowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private string _deviceRole;

    public DeviceModelRowViewModel(
        string name,
        string deviceRole,
        IReadOnlyList<LogicalPointRowViewModel> points)
    {
        _name = name;
        _deviceRole = deviceRole;
        Points = new ObservableCollection<LogicalPointRowViewModel>(points);
    }

    public ObservableCollection<LogicalPointRowViewModel> Points { get; }
}

public sealed partial class LogicalPointRowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _logicalKey;

    [ObservableProperty]
    private string _displayName;

    [ObservableProperty]
    private string _source;

    [ObservableProperty]
    private string _accessMode;

    [ObservableProperty]
    private string _functionCode;

    private string _registerAddress;

    [ObservableProperty]
    private string _dataType;

    [ObservableProperty]
    private string _unit;

    [ObservableProperty]
    private string _notes;

    [ObservableProperty]
    private bool _isCustom;

    public ObservableCollection<LogicalPointWriteOption> WriteOptions { get; }

    public string PointKindDisplay => IsCustom
        ? "\u81EA\u5B9A\u4E49"
        : "\u5185\u7F6E";

    public string RegisterAddress
    {
        get => _registerAddress;
        set => SetProperty(ref _registerAddress, NormalizeRegisterAddress(value));
    }

    public LogicalPointRowViewModel(
        string logicalKey,
        string displayName,
        string source,
        string accessMode,
        string functionCode,
        string registerAddress,
        string dataType,
        string unit,
        string notes,
        bool isCustom = false,
        IEnumerable<LogicalPointWriteOption>? writeOptions = null)
    {
        _logicalKey = logicalKey;
        _displayName = displayName;
        _source = source;
        _accessMode = accessMode;
        _functionCode = functionCode;
        _registerAddress = NormalizeRegisterAddress(registerAddress);
        _dataType = dataType;
        _unit = unit;
        _notes = notes;
        _isCustom = isCustom;
        WriteOptions = new ObservableCollection<LogicalPointWriteOption>(writeOptions ?? []);
    }

    partial void OnIsCustomChanged(bool value)
    {
        OnPropertyChanged(nameof(PointKindDisplay));
    }

    private static string NormalizeRegisterAddress(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Trim();
        if (normalized.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            var body = normalized[2..].ToUpperInvariant();
            return string.IsNullOrWhiteSpace(body) ? "0x" : $"0x{body}";
        }

        return normalized.Length == 4 && normalized.All(Uri.IsHexDigit)
            ? $"0x{normalized.ToUpperInvariant()}"
            : normalized;
    }
}

public sealed record LogicalPointWriteOption(string Value, string DisplayName)
{
    public string DisplayText => string.IsNullOrWhiteSpace(Value)
        ? DisplayName
        : $"{DisplayName} ({Value})";
}
