using VfdControl.Domain.Enums;

namespace VfdControl.Presentation.Operator;

public sealed class DeviceRunTableRowViewModel
{
    public DeviceRunTableRowViewModel(string slotName, string barcode, Conclusion conclusion)
    {
        SlotName = slotName;
        Barcode = barcode;
        Conclusion = conclusion;
    }

    public string SlotName { get; }

    public string Barcode { get; }

    public Conclusion Conclusion { get; }

    public string ConclusionText => Conclusion switch
    {
        Conclusion.Pass => "通过",
        Conclusion.Fail => "失败",
        Conclusion.Warning => "警告",
        _ => "无结论"
    };
}
