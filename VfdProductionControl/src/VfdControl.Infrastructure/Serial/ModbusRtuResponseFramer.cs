namespace VfdControl.Infrastructure.Serial;

public static class ModbusRtuResponseFramer
{
    public static bool IsComplete(ReadOnlySpan<byte> request, ReadOnlySpan<byte> response)
    {
        if (!TryGetExpectedLength(request, response, out var expectedLength))
        {
            return false;
        }

        return response.Length >= expectedLength;
    }

    public static bool TryGetExpectedLength(ReadOnlySpan<byte> request, ReadOnlySpan<byte> response, out int expectedLength)
    {
        expectedLength = 0;
        if (request.Length < 2 || response.Length < 2)
        {
            return false;
        }

        var functionCode = request[1];
        var responseFunctionCode = response[1];
        if (responseFunctionCode == (functionCode | 0x80))
        {
            expectedLength = 5;
            return true;
        }

        if (responseFunctionCode != functionCode)
        {
            return false;
        }

        expectedLength = functionCode switch
        {
            0x03 => response.Length >= 3 ? response[2] + 5 : 0,
            0x06 => 8,
            _ => 0
        };

        return expectedLength > 0;
    }
}
