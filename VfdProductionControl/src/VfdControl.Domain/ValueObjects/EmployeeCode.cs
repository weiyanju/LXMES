using System.Text.RegularExpressions;
using VfdControl.Domain.Common;

namespace VfdControl.Domain.ValueObjects;

public sealed record EmployeeCode(string Value)
{
    private static readonly Regex DefaultPattern = new("^EMP\\d{4,8}$", RegexOptions.Compiled);

    public static Result<EmployeeCode> TryCreate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<EmployeeCode>.Failure("EmployeeCode.Empty", "Employee code is required.");
        }

        var normalized = value.Trim().ToUpperInvariant();
        return DefaultPattern.IsMatch(normalized)
            ? Result<EmployeeCode>.Success(new EmployeeCode(normalized))
            : Result<EmployeeCode>.Failure("EmployeeCode.Invalid", "Employee code must match EMP plus 4-8 digits.");
    }
}
