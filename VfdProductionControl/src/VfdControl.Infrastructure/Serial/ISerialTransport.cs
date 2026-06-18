namespace VfdControl.Infrastructure.Serial;

public interface ISerialTransport : IAsyncDisposable
{
    Task<byte[]> TransactAsync(byte[] request, TimeSpan timeout, CancellationToken ct);
}
