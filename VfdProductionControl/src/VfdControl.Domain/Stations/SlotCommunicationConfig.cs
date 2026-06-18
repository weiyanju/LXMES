using VfdControl.Domain.ValueObjects;

namespace VfdControl.Domain.Stations;

public sealed record SlotCommunicationConfig(
    SerialPortName? PortName,
    ModbusAddress VfdAddress,
    ModbusAddress VoltageMeterAddress,
    ModbusAddress CurrentMeterAddress,
    int BaudRate)
{
    public SlotCommunicationConfig(SerialPortName portName, ModbusAddress deviceAddress, int baudRate)
        : this(
            portName,
            deviceAddress,
            new ModbusAddress((byte)Math.Min(deviceAddress.Value + 10, byte.MaxValue)),
            new ModbusAddress((byte)Math.Min(deviceAddress.Value + 20, byte.MaxValue)),
            baudRate)
    {
    }

    public ModbusAddress DeviceAddress => VfdAddress;

    public bool HasPort => PortName is not null;

    public string PortDisplay => PortName?.Value ?? "\u65E0";
}
