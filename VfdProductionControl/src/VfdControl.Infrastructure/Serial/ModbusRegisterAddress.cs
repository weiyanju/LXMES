using System.Globalization;

namespace VfdControl.Infrastructure.Serial;

public static class ModbusRegisterAddress
{
    public static bool TryParse(string? value, out ushort registerAddress)
    {
        registerAddress = 0;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value.Trim();
        if (normalized.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return ushort.TryParse(normalized[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out registerAddress);
        }

        if (IsFourDigitHexAddress(normalized))
        {
            return ushort.TryParse(normalized, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out registerAddress);
        }

        return ushort.TryParse(normalized, NumberStyles.Integer, CultureInfo.InvariantCulture, out registerAddress);
    }

    public static string NormalizeForDisplay(string value)
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

        return IsFourDigitHexAddress(normalized)
            ? $"0x{normalized.ToUpperInvariant()}"
            : normalized;
    }

    private static bool IsFourDigitHexAddress(string value)
    {
        return value.Length == 4 && value.All(Uri.IsHexDigit);
    }
}
