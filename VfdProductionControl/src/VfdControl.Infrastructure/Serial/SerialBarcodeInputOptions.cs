namespace VfdControl.Infrastructure.Serial;

public sealed class SerialBarcodeInputOptions
{
    public bool Enabled { get; set; }

    public string PortName { get; set; } = "COM3";

    public int BaudRate { get; set; } = 9600;

    public string NewLine { get; set; } = "\r\n";
}
