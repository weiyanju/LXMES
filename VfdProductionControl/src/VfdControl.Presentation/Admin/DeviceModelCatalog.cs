using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace VfdControl.Presentation.Admin;

public sealed class DeviceModelCatalog
{
    public DeviceModelCatalog()
    {
        DeviceModels =
        [
            new(
                "\u6807\u51C6 VFD",
                "VFD",
                [
                    new LogicalPointRowViewModel(
                        "Vfd:Control",
                        "VFD \u542F\u505C\u63A7\u5236",
                        "VFD",
                        "\u5199\u5165",
                        "06",
                        "0x2000",
                        "Int16",
                        "",
                        "2000H \u547D\u4EE4\u5B57\uFF1A0001 \u6B63\u8F6C\u8FD0\u884C\uFF0C0002 \u53CD\u8F6C\u8FD0\u884C\uFF0C0003 \u6B63\u8F6C\u70B9\u52A8\uFF0C0004 \u53CD\u8F6C\u70B9\u52A8\uFF0C0005 \u81EA\u7531\u505C\u673A\uFF0C0006 \u51CF\u901F\u505C\u673A\uFF0C0007 \u6545\u969C\u590D\u4F4D",
                        writeOptions:
                        [
                            new("1", "\u6B63\u8F6C\u8FD0\u884C"),
                            new("2", "\u53CD\u8F6C\u8FD0\u884C"),
                            new("3", "\u6B63\u8F6C\u70B9\u52A8"),
                            new("4", "\u53CD\u8F6C\u70B9\u52A8"),
                            new("5", "\u81EA\u7531\u505C\u673A"),
                            new("6", "\u51CF\u901F\u505C\u673A"),
                            new("7", "\u6545\u969C\u590D\u4F4D")
                        ]),
                    new LogicalPointRowViewModel("Vfd:State", "VFD \u8FD0\u884C\u72B6\u6001", "VFD", "\u8BFB\u53D6", "03", "0x3000", "Enum", "", "3000H \u72B6\u6001\u5B57\uFF1A0001 \u6B63\u8F6C\u8FD0\u884C\uFF0C0002 \u53CD\u8F6C\u8FD0\u884C\uFF0C0003 \u505C\u673A"),
                    new LogicalPointRowViewModel("Vfd:FaultCode", "VFD \u6545\u969C\u7801", "VFD", "\u8BFB\u53D6", "03", "0x8000", "Enum", "", "8000H \u53D8\u9891\u5668\u6545\u969C\u4FE1\u606F\uFF0C0000 \u65E0\u6545\u969C"),
                    new LogicalPointRowViewModel("Vfd:Voltage", "VFD \u8F93\u51FA\u7535\u538B", "VFD", "\u8BFB\u53D6", "03", "0x1003", "Decimal", "V", "1003H \u8F93\u51FA\u7535\u538B\uFF0C\u5C0F\u6570\u4F4D\u548C\u6BD4\u4F8B\u7CFB\u6570\u9700\u73B0\u573A\u6821\u51C6"),
                    new LogicalPointRowViewModel("Vfd:Current", "VFD \u8F93\u51FA\u7535\u6D41", "VFD", "\u8BFB\u53D6", "03", "0x1004", "Decimal", "A", "1004H \u8F93\u51FA\u7535\u6D41\uFF0C\u5C0F\u6570\u4F4D\u548C\u6BD4\u4F8B\u7CFB\u6570\u9700\u73B0\u573A\u6821\u51C6"),
                    new LogicalPointRowViewModel("Vfd:RunningFrequency", "VFD \u8FD0\u884C\u9891\u7387", "VFD", "\u8BFB\u53D6", "03", "0x1001", "Decimal", "Hz", "1001H \u8FD0\u884C\u9891\u7387"),
                    new LogicalPointRowViewModel("Vfd:BusVoltage", "VFD \u6BCD\u7EBF\u7535\u538B", "VFD", "\u8BFB\u53D6", "03", "0x1002", "Decimal", "V", "1002H \u6BCD\u7EBF\u7535\u538B"),
                    new LogicalPointRowViewModel("Vfd:Power", "VFD \u8F93\u51FA\u529F\u7387", "VFD", "\u8BFB\u53D6", "03", "0x1005", "Decimal", "kW", "1005H \u8F93\u51FA\u529F\u7387"),
                    new LogicalPointRowViewModel("Vfd:Torque", "VFD \u8F93\u51FA\u8F6C\u77E9", "VFD", "\u8BFB\u53D6", "03", "0x1006", "Decimal", "%", "1006H \u8F93\u51FA\u8F6C\u77E9"),
                    new LogicalPointRowViewModel("Vfd:Speed", "VFD \u8FD0\u884C\u901F\u5EA6", "VFD", "\u8BFB\u53D6", "03", "0x1007", "Decimal", "rpm", "1007H \u8FD0\u884C\u901F\u5EA6")
                ]),
            new(
                "\u6807\u51C6\u7535\u538B\u8868",
                "\u4EEA\u8868",
                [
                    new LogicalPointRowViewModel("Instrument:Voltage", "\u4EEA\u8868\u7535\u538B", "\u4EEA\u8868", "\u8BFB\u53D6", "03", "40001", "Decimal", "V", "\u540C\u578B\u53F7\u4EEA\u8868\u5171\u7528\u5BC4\u5B58\u5668")
                ]),
            new(
                "\u6807\u51C6\u7535\u6D41\u8868",
                "\u4EEA\u8868",
                [
                    new LogicalPointRowViewModel("Instrument:Current", "\u4EEA\u8868\u7535\u6D41", "\u4EEA\u8868", "\u8BFB\u53D6", "03", "40002", "Decimal", "A", "\u540C\u578B\u53F7\u4EEA\u8868\u5171\u7528\u5BC4\u5B58\u5668")
                ])
        ];

        foreach (var model in DeviceModels)
        {
            WatchModel(model);
        }
    }

    public ObservableCollection<DeviceModelRowViewModel> DeviceModels { get; }

    public IReadOnlyList<LogicalPointRowViewModel> LogicalPoints => DeviceModels
        .SelectMany(model => model.Points)
        .ToList();

    public event EventHandler? Changed;

    public void NotifyChanged()
    {
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void WatchModel(DeviceModelRowViewModel model)
    {
        model.Points.CollectionChanged += OnPointsChanged;
        foreach (var point in model.Points)
        {
            WatchPoint(point);
        }
    }

    private void OnPointsChanged(object? sender, NotifyCollectionChangedEventArgs args)
    {
        if (args.NewItems is not null)
        {
            foreach (var item in args.NewItems.OfType<LogicalPointRowViewModel>())
            {
                WatchPoint(item);
            }
        }

        if (args.OldItems is not null)
        {
            foreach (var item in args.OldItems.OfType<LogicalPointRowViewModel>())
            {
                item.PropertyChanged -= OnPointPropertyChanged;
            }
        }

        NotifyChanged();
    }

    private void WatchPoint(LogicalPointRowViewModel point)
    {
        point.PropertyChanged -= OnPointPropertyChanged;
        point.PropertyChanged += OnPointPropertyChanged;
    }

    private void OnPointPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        NotifyChanged();
    }
}
