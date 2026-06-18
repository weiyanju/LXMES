using System.IO;
using System.IO.Ports;
using System.Text.Json;
using VfdControl.Infrastructure.Serial;

namespace VfdControl.App.Services;

public sealed class ScannerSettingsService
{
    private readonly SerialBarcodeInputOptions _options;

    public ScannerSettingsService(SerialBarcodeInputOptions options)
    {
        _options = options;
    }

    public static string SettingsPath => Path.Combine(AppContext.BaseDirectory, "scanner-settings.json");

    public SerialBarcodeInputOptions Current => _options;

    public IReadOnlyList<ScannerPortOption> ListPorts()
    {
        return SerialPort.GetPortNames()
            .Order(StringComparer.OrdinalIgnoreCase)
            .Select(port => new ScannerPortOption(port, port))
            .ToList();
    }

    public async Task SaveAsync(SerialBarcodeInputOptions options, CancellationToken cancellationToken = default)
    {
        _options.Enabled = options.Enabled;
        _options.PortName = options.PortName;
        _options.BaudRate = options.BaudRate;
        _options.NewLine = options.NewLine;

        var payload = new ScannerSettingsFile(new ScannerSettingsSection(
            _options.Enabled,
            _options.PortName,
            _options.BaudRate,
            _options.NewLine));

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(SettingsPath, json, cancellationToken);
    }

    public Task TestConnectionAsync(string portName, int baudRate, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var port = new SerialPort(portName, baudRate);
        port.Open();
        port.Close();
        return Task.CompletedTask;
    }

    private sealed record ScannerSettingsFile(ScannerSettingsSection Scanner);

    private sealed record ScannerSettingsSection(
        bool Enabled,
        string PortName,
        int BaudRate,
        string NewLine);
}

public sealed record ScannerPortOption(string PortName, string DisplayName);
