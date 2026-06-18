namespace VfdControl.Infrastructure.Serial;

public static class ModbusCrc
{
    public static ushort Compute(ReadOnlySpan<byte> frame)
    {
        ushort crc = 0xFFFF;

        foreach (var value in frame)
        {
            crc ^= value;
            for (var bit = 0; bit < 8; bit++)
            {
                var hasCarry = (crc & 0x0001) != 0;
                crc >>= 1;
                if (hasCarry)
                {
                    crc ^= 0xA001;
                }
            }
        }

        return crc;
    }

    public static byte[] Append(ReadOnlySpan<byte> frame)
    {
        var crc = Compute(frame);
        var result = new byte[frame.Length + 2];
        frame.CopyTo(result);
        result[^2] = (byte)(crc & 0xFF);
        result[^1] = (byte)(crc >> 8);
        return result;
    }

    public static bool IsValid(ReadOnlySpan<byte> frame)
    {
        if (frame.Length < 3)
        {
            return false;
        }

        var payload = frame[..^2];
        var expected = Compute(payload);
        var actual = (ushort)(frame[^2] | frame[^1] << 8);
        return expected == actual;
    }
}
