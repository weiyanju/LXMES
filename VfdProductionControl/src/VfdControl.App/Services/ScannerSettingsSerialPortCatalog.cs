using VfdControl.Presentation.Admin;

namespace VfdControl.App.Services;

public sealed class ScannerSettingsSerialPortCatalog : ISerialPortCatalog
{
    private readonly ScannerSettingsService _scannerSettingsService;

    public ScannerSettingsSerialPortCatalog(ScannerSettingsService scannerSettingsService)
    {
        _scannerSettingsService = scannerSettingsService;
    }

    public IReadOnlyList<string> ListPortNames()
    {
        return _scannerSettingsService
            .ListPorts()
            .Select(port => port.PortName)
            .ToList();
    }
}
