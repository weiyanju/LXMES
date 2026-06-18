using System.Text;

namespace VfdControl.Infrastructure.Serial;

public sealed class SerialBarcodeFrameBuffer
{
    private readonly StringBuilder _pending = new();

    public IReadOnlyList<string> Append(string chunk)
    {
        if (string.IsNullOrEmpty(chunk))
        {
            return [];
        }

        var scans = new List<string>();
        foreach (var character in chunk)
        {
            if (character is '\r' or '\n')
            {
                Flush(scans);
                continue;
            }

            _pending.Append(character);
        }

        return scans;
    }

    private void Flush(List<string> scans)
    {
        var scan = _pending.ToString().Trim();
        _pending.Clear();
        if (!string.IsNullOrWhiteSpace(scan))
        {
            scans.Add(scan);
        }
    }
}
