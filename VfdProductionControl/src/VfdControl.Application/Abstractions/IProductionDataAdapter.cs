using VfdControl.Application.Execution;

namespace VfdControl.Application.Abstractions;

public interface IProductionDataAdapter
{
    Task<OperatorInfo?> GetOperatorAsync(string employeeCode, CancellationToken ct);

    Task<DeviceBarcodeInfo?> GetDeviceBarcodeAsync(string barcode, CancellationToken ct);

    Task PublishSessionResultAsync(SessionResultMessage message, CancellationToken ct);

    Task PublishDeviceRunResultAsync(DeviceRunResultMessage message, CancellationToken ct);
}
