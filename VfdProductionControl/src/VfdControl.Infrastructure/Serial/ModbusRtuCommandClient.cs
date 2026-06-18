using System.Globalization;
using System.Text;
using VfdControl.Application.Abstractions;
using VfdControl.Application.Execution;
using VfdControl.Domain.ValueObjects;

namespace VfdControl.Infrastructure.Serial;

public sealed class ModbusRtuCommandClient : IDeviceCommandClient
{
    private readonly Func<DeviceAddress, SerialDeviceEndpoint?> _endpointResolver;
    private readonly Func<SerialDeviceEndpoint, ISerialTransport> _transportFactory;
    private readonly Func<DeviceAddress, string?> _registerAddressResolver;
    private readonly TimeSpan _timeout;

    public ModbusRtuCommandClient(
        Func<DeviceAddress, SerialDeviceEndpoint?> endpointResolver,
        Func<SerialDeviceEndpoint, ISerialTransport>? transportFactory = null,
        Func<DeviceAddress, string?>? registerAddressResolver = null,
        TimeSpan? timeout = null)
    {
        _endpointResolver = endpointResolver;
        _transportFactory = transportFactory ?? (endpoint => new SerialPortTransport(endpoint.PortName, endpoint.BaudRate));
        _registerAddressResolver = registerAddressResolver ?? (_ => null);
        _timeout = timeout ?? TimeSpan.FromSeconds(1);
    }

    public async Task<CommandResult<MeasurementValue>> ReadMeasurementAsync(DeviceAddress address, ReadCommand command, CancellationToken ct)
    {
        if (!TryCreateReadFrame(address, command.RegisterAddress, quantity: 1, out var frame, out var endpoint, out var error))
        {
            return CommandResult<MeasurementValue>.Failure(error, "Serial.InvalidRead");
        }

        var requestTrace = FrameTrace(endpoint, frame);
        byte[] response;
        try
        {
            response = await TransactAsync(endpoint, frame, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return CommandResult<MeasurementValue>.Failure(TransportErrorMessage(ex), "Serial.TransportError", requestTrace);
        }

        var responseTrace = FrameTrace(endpoint, response);
        if (!ModbusCrc.IsValid(response))
        {
            return CommandResult<MeasurementValue>.Failure("Modbus response CRC is invalid.", "Serial.CrcInvalid", requestTrace, responseTrace);
        }

        if (TryCreateModbusExceptionFailure(response, requestTrace, responseTrace, out var exceptionFailure))
        {
            return CommandResult<MeasurementValue>.Failure(exceptionFailure.Message, exceptionFailure.ErrorCode, requestTrace, responseTrace);
        }

        if (response.Length != response[2] + 5 || response[0] != endpoint.ModbusAddress || response[1] != 0x03 || response[2] < 2)
        {
            return CommandResult<MeasurementValue>.Failure("Modbus response did not contain a holding-register value.", "Serial.ResponseInvalid", requestTrace, responseTrace);
        }

        var raw = response[3] << 8 | response[4];
        return CommandResult<MeasurementValue>.Success(new MeasurementValue(raw, string.Empty, address.Source), requestJson: requestTrace, responseJson: responseTrace);
    }

    public async Task<CommandResult<string>> ReadStringAsync(DeviceAddress address, ReadStringCommand command, CancellationToken ct)
    {
        if (!TryCreateReadFrame(address, command.RegisterAddress, quantity: 8, out var frame, out var endpoint, out var error))
        {
            return CommandResult<string>.Failure(error, "Serial.InvalidRead");
        }

        var requestTrace = FrameTrace(endpoint, frame);
        byte[] response;
        try
        {
            response = await TransactAsync(endpoint, frame, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return CommandResult<string>.Failure(TransportErrorMessage(ex), "Serial.TransportError", requestTrace);
        }

        var responseTrace = FrameTrace(endpoint, response);
        if (!ModbusCrc.IsValid(response))
        {
            return CommandResult<string>.Failure("Modbus response CRC is invalid.", "Serial.CrcInvalid", requestTrace, responseTrace);
        }

        if (TryCreateModbusExceptionFailure(response, requestTrace, responseTrace, out var exceptionFailure))
        {
            return CommandResult<string>.Failure(exceptionFailure.Message, exceptionFailure.ErrorCode, requestTrace, responseTrace);
        }

        if (response.Length < 5 || response[0] != endpoint.ModbusAddress || response[1] != 0x03)
        {
            return CommandResult<string>.Failure("Modbus response did not contain string data.", "Serial.ResponseInvalid", requestTrace, responseTrace);
        }

        var byteCount = response[2];
        if (response.Length < byteCount + 5)
        {
            return CommandResult<string>.Failure("Modbus string response is truncated.", "Serial.ResponseInvalid", requestTrace, responseTrace);
        }

        var value = Encoding.ASCII.GetString(response, 3, byteCount).TrimEnd('\0', ' ');
        return CommandResult<string>.Success(value, requestJson: requestTrace, responseJson: responseTrace);
    }

    public async Task<CommandResult> WriteAsync(DeviceAddress address, WriteCommand command, CancellationToken ct)
    {
        if (!TryResolve(address, out var endpoint, out var error))
        {
            return CommandResult.Failure(error, "Serial.EndpointMissing");
        }

        var registerAddressText = ResolveRegisterAddress(address);
        if (!ModbusRegisterAddress.TryParse(registerAddressText, out var registerAddress))
        {
            return CommandResult.Failure("Write endpoint must provide a numeric Modbus register address.", "Serial.RegisterInvalid");
        }

        if (!TryParseUShort(command.Value, out var parsedValue))
        {
            return CommandResult.Failure("Write command must provide a numeric 16-bit value.", "Serial.WriteValueMissing");
        }

        var value = parsedValue;
        var payload = new byte[]
        {
            endpoint.ModbusAddress,
            0x06,
            (byte)(registerAddress >> 8),
            (byte)(registerAddress & 0xFF),
            (byte)(value >> 8),
            (byte)(value & 0xFF)
        };

        var frame = ModbusCrc.Append(payload);
        var requestTrace = FrameTrace(endpoint, frame);
        byte[] response;
        try
        {
            response = await TransactAsync(endpoint, frame, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return CommandResult.Failure(TransportErrorMessage(ex), "Serial.TransportError", requestTrace);
        }

        var responseTrace = FrameTrace(endpoint, response);
        if (!ModbusCrc.IsValid(response))
        {
            return CommandResult.Failure("Modbus write response CRC is invalid.", "Serial.CrcInvalid", requestTrace, responseTrace);
        }

        if (TryCreateModbusExceptionFailure(response, requestTrace, responseTrace, out var exceptionFailure))
        {
            return CommandResult.Failure(exceptionFailure.Message, exceptionFailure.ErrorCode, requestTrace, responseTrace);
        }

        if (response.Length != 8 || !response.AsSpan(0, 6).SequenceEqual(payload))
        {
            return CommandResult.Failure("Modbus write response did not echo the request.", "Serial.ResponseMismatch", requestTrace, responseTrace);
        }

        return CommandResult.Success("Modbus write completed.", requestTrace, responseTrace);
    }

    private async Task<byte[]> TransactAsync(SerialDeviceEndpoint endpoint, byte[] frame, CancellationToken ct)
    {
        await using var transport = _transportFactory(endpoint);
        return await transport.TransactAsync(frame, _timeout, ct);
    }

    private static string FrameTrace(SerialDeviceEndpoint endpoint, byte[] frame)
    {
        return $$"""{"port":"{{endpoint.PortName}}","slave":{{endpoint.ModbusAddress}},"hex":"{{ToHex(frame)}}"}""";
    }

    private static string ToHex(byte[] frame)
    {
        return frame.Length == 0
            ? ""
            : string.Join(" ", frame.Select(value => value.ToString("X2", CultureInfo.InvariantCulture)));
    }

    private static string TransportErrorMessage(Exception exception)
    {
        return $"Serial transport failed: {exception.Message}";
    }

    private static bool TryCreateModbusExceptionFailure(
        byte[] response,
        string requestTrace,
        string responseTrace,
        out CommandResult failure)
    {
        failure = CommandResult.Success(requestJson: requestTrace, responseJson: responseTrace);
        if (response.Length != 5 || (response[1] & 0x80) == 0)
        {
            return false;
        }

        failure = CommandResult.Failure(
            $"Modbus exception response 0x{response[2]:X2}.",
            "Serial.ModbusException",
            requestTrace,
            responseTrace);
        return true;
    }

    private bool TryCreateReadFrame(
        DeviceAddress address,
        string? registerAddressText,
        ushort quantity,
        out byte[] frame,
        out SerialDeviceEndpoint endpoint,
        out string error)
    {
        frame = [];
        endpoint = default!;
        if (!TryResolve(address, out endpoint, out error))
        {
            return false;
        }

        if (!ModbusRegisterAddress.TryParse(registerAddressText ?? ResolveRegisterAddress(address), out var registerAddress))
        {
            error = "Read endpoint must provide a numeric Modbus register address.";
            return false;
        }

        var payload = new byte[]
        {
            endpoint.ModbusAddress,
            0x03,
            (byte)(registerAddress >> 8),
            (byte)(registerAddress & 0xFF),
            (byte)(quantity >> 8),
            (byte)(quantity & 0xFF)
        };

        frame = ModbusCrc.Append(payload);
        return true;
    }

    private string ResolveRegisterAddress(DeviceAddress address)
    {
        return _registerAddressResolver(address) ?? address.EndpointName;
    }

    private bool TryResolve(DeviceAddress address, out SerialDeviceEndpoint endpoint, out string error)
    {
        var resolved = _endpointResolver(address);
        if (resolved is null)
        {
            endpoint = default!;
            error = "No serial endpoint is configured for this device address.";
            return false;
        }

        endpoint = resolved;
        error = string.Empty;
        return true;
    }

    private static bool TryParseUShort(string? value, out ushort parsed)
    {
        parsed = 0;
        return !string.IsNullOrWhiteSpace(value) &&
               ushort.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed);
    }
}

public sealed record SerialDeviceEndpoint(
    string PortName,
    byte ModbusAddress,
    int BaudRate = 9600);
