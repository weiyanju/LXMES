using System.Text.RegularExpressions;
using VfdControl.Domain.Common;

namespace VfdControl.Domain.ValueObjects;

public sealed record Barcode(string Value)
{
    private static readonly Regex DefaultVfdPattern = new("^VFD[A-Z0-9]{8,20}$", RegexOptions.Compiled);

    public static Result<Barcode> TryCreateVfd(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<Barcode>.Failure("Barcode.Empty", "Barcode is required.");
        }

        var normalized = value.Trim().ToUpperInvariant();
        return DefaultVfdPattern.IsMatch(normalized)
            ? Result<Barcode>.Success(new Barcode(normalized))
            : Result<Barcode>.Failure("Barcode.Invalid", "VFD barcode must match VFD plus 8-20 uppercase letters or digits.");
    }
}
