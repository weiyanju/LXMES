using VfdControl.Application.Admin;

namespace VfdControl.Presentation.Admin;

public sealed class BarcodeRuleViewModel
{
    public BarcodeRuleViewModel(BarcodeRuleService barcodeRuleService)
    {
        EmployeeRuleDisplay = barcodeRuleService.EmployeeRuleDisplay;
        VfdRuleDisplay = barcodeRuleService.VfdRuleDisplay;
    }

    public string EmployeeRuleDisplay { get; }

    public string VfdRuleDisplay { get; }
}
