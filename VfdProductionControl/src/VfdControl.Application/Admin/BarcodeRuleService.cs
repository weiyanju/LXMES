using VfdControl.Domain.Common;
using VfdControl.Domain.ValueObjects;

namespace VfdControl.Application.Admin;

public sealed class BarcodeRuleService
{
    public string EmployeeRuleDisplay => "员工码：EMP + 4 到 8 位数字";

    public string VfdRuleDisplay => "VFD 条码：VFD + 8 到 20 位大写字母或数字";

    public Result<EmployeeCode> ValidateEmployeeCode(string value)
    {
        return EmployeeCode.TryCreate(value);
    }

    public Result<Barcode> ValidateVfdBarcode(string value)
    {
        return Barcode.TryCreateVfd(value);
    }
}
