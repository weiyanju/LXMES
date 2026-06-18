using System.Collections.Concurrent;
using System.IO.Ports;

namespace VfdControl.Infrastructure.Serial;

public sealed class SerialPortTransport : ISerialTransport
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> PortLocks = new(StringComparer.OrdinalIgnoreCase);
    private readonly SerialPort _port;
    private readonly SemaphoreSlim _portLock;
    private bool _disposed;

    public SerialPortTransport(string portName, int baudRate, TimeSpan? readTimeout = null, TimeSpan? writeTimeout = null)
    {
        _port = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One)
        {
            ReadTimeout = ToMilliseconds(readTimeout ?? TimeSpan.FromSeconds(1)),
            WriteTimeout = ToMilliseconds(writeTimeout ?? TimeSpan.FromSeconds(1))
        };
        _portLock = PortLocks.GetOrAdd(portName, _ => new SemaphoreSlim(1, 1));
    }

    public bool IsOpen => _port.IsOpen;

    public string PortName => _port.PortName;

    public Task OpenAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (!_port.IsOpen)
        {
            _port.Open();
        }

        return Task.CompletedTask;
    }

    public Task CloseAsync()
    {
        if (_port.IsOpen)
        {
            _port.Close();
        }

        return Task.CompletedTask;
    }

    public async Task<byte[]> TransactAsync(byte[] request, TimeSpan timeout, CancellationToken ct)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        await _portLock.WaitAsync(ct);
        try
        {
            await OpenAsync(ct);
            _port.DiscardInBuffer();
            _port.DiscardOutBuffer();

            await _port.BaseStream.WriteAsync(request, ct);
            await _port.BaseStream.FlushAsync(ct);

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(timeout);

            var response = new List<byte>();
            var buffer = new byte[256];
            while (!timeoutCts.IsCancellationRequested)
            {
                try
                {
                    var read = await _port.BaseStream.ReadAsync(buffer, timeoutCts.Token);
                    if (read > 0)
                    {
                        response.AddRange(buffer.AsSpan(0, read).ToArray());
                        if (ModbusRtuResponseFramer.IsComplete(request, response.ToArray()))
                        {
                            break;
                        }
                    }
                }
                catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                {
                    break;
                }
            }

            return response.ToArray();
        }
        finally
        {
            _portLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        await CloseAsync();
        _port.Dispose();
        _disposed = true;
    }

    private static int ToMilliseconds(TimeSpan value)
    {
        return Math.Max(1, (int)value.TotalMilliseconds);
    }
}
