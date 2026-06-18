using VfdControl.Application.Execution;

namespace VfdControl.Application.Abstractions;

public interface IBarcodeInputService
{
    event EventHandler<BarcodeScannedEventArgs>? BarcodeScanned;

    Task StartAsync(CancellationToken cancellationToken);

    Task StopAsync();
}
