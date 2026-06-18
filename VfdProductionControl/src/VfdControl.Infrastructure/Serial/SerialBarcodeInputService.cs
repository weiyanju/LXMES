using System.IO.Ports;
using VfdControl.Application.Abstractions;
using VfdControl.Application.Execution;

namespace VfdControl.Infrastructure.Serial;

public sealed class SerialBarcodeInputService : IBarcodeInputService
{
    private readonly SerialBarcodeInputOptions _options;
    private readonly SerialBarcodeFrameBuffer _frameBuffer = new();
    private readonly object _syncRoot = new();
    private SerialPort? _port;

    public SerialBarcodeInputService(SerialBarcodeInputOptions options)
    {
        _options = options;
    }

    public event EventHandler<BarcodeScannedEventArgs>? BarcodeScanned;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_options.Enabled)
        {
            throw new InvalidOperationException("\u4E32\u53E3\u626B\u7801\u67AA\u672A\u542F\u7528\uFF0C\u5F53\u524D\u53EF\u4F7F\u7528\u6A21\u62DF\u5165\u53E3\u3002");
        }

        if (string.IsNullOrWhiteSpace(_options.PortName))
        {
            throw new InvalidOperationException("\u4E32\u53E3\u626B\u7801\u67AA\u7AEF\u53E3\u672A\u914D\u7F6E\u3002");
        }

        lock (_syncRoot)
        {
            if (_port is { IsOpen: true })
            {
                return Task.CompletedTask;
            }

            _port = new SerialPort(_options.PortName, _options.BaudRate, Parity.None, 8, StopBits.One)
            {
                NewLine = string.IsNullOrEmpty(_options.NewLine) ? "\r\n" : _options.NewLine
            };
            _port.DataReceived += OnDataReceived;
            _port.Open();
        }

        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        lock (_syncRoot)
        {
            if (_port is null)
            {
                return Task.CompletedTask;
            }

            _port.DataReceived -= OnDataReceived;
            if (_port.IsOpen)
            {
                _port.Close();
            }

            _port.Dispose();
            _port = null;
        }

        return Task.CompletedTask;
    }

    private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        string chunk;
        lock (_syncRoot)
        {
            if (_port is null)
            {
                return;
            }

            chunk = _port.ReadExisting();
        }

        foreach (var scan in _frameBuffer.Append(chunk))
        {
            BarcodeScanned?.Invoke(this, new BarcodeScannedEventArgs(scan, DateTimeOffset.UtcNow));
        }
    }
}
