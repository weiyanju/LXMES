using FluentAssertions;
using VfdControl.Application.Execution;
using VfdControl.Domain.Enums;
using VfdControl.Infrastructure.Serial;

namespace VfdControl.Infrastructure.Tests.Serial;

public class ModbusRtuCommandClientTests
{
    [Fact]
    public async Task Write_result_includes_modbus_request_and_response_frames()
    {
        var endpoint = new SerialDeviceEndpoint("COM6", 1, 9600);
        var response = ModbusCrc.Append([0x01, 0x06, 0x20, 0x00, 0x00, 0x01]);
        var transport = new FakeSerialTransport(response);
        var client = new ModbusRtuCommandClient(_ => endpoint, _ => transport);

        var result = await client.WriteAsync(
            new DeviceAddress(Guid.NewGuid(), MeasurementSource.Vfd, "0x2000"),
            new WriteCommand("Start", "1"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.RequestJson.Should().Contain("01 06 20 00 00 01");
        result.ResponseJson.Should().Contain("01 06 20 00 00 01");
        transport.Request.Should().Equal(0x01, 0x06, 0x20, 0x00, 0x00, 0x01, 0x43, 0xCA);
    }

    [Fact]
    public async Task Write_resolves_logical_point_to_modbus_register()
    {
        var endpoint = new SerialDeviceEndpoint("COM6", 1, 9600);
        var response = ModbusCrc.Append([0x01, 0x06, 0x20, 0x00, 0x00, 0x01]);
        var transport = new FakeSerialTransport(response);
        var client = new ModbusRtuCommandClient(
            _ => endpoint,
            _ => transport,
            registerAddressResolver: address => address.EndpointName == "Vfd:Control" ? "0x2000" : null);

        var result = await client.WriteAsync(
            new DeviceAddress(Guid.NewGuid(), MeasurementSource.Vfd, "Vfd:Control"),
            new WriteCommand("Start", "1"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        transport.Request.Should().Equal(0x01, 0x06, 0x20, 0x00, 0x00, 0x01, 0x43, 0xCA);
    }

    [Fact]
    public async Task Write_fails_when_value_is_missing_instead_of_defaulting_to_start()
    {
        var endpoint = new SerialDeviceEndpoint("COM6", 1, 9600);
        var response = ModbusCrc.Append([0x01, 0x06, 0x20, 0x00, 0x00, 0x01]);
        var transport = new FakeSerialTransport(response);
        var client = new ModbusRtuCommandClient(_ => endpoint, _ => transport);

        var result = await client.WriteAsync(
            new DeviceAddress(Guid.NewGuid(), MeasurementSource.Vfd, "0x2000"),
            new WriteCommand("Stop"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("Serial.WriteValueMissing");
        transport.Request.Should().BeEmpty();
    }

    [Fact]
    public async Task Write_fails_when_response_does_not_echo_request()
    {
        var endpoint = new SerialDeviceEndpoint("COM6", 1, 9600);
        var response = ModbusCrc.Append([0x01, 0x06, 0x20, 0x00, 0x00, 0x02]);
        var transport = new FakeSerialTransport(response);
        var client = new ModbusRtuCommandClient(_ => endpoint, _ => transport);

        var result = await client.WriteAsync(
            new DeviceAddress(Guid.NewGuid(), MeasurementSource.Vfd, "0x2000"),
            new WriteCommand("Start", "1"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("Serial.ResponseMismatch");
    }

    [Fact]
    public async Task Read_failure_reports_modbus_exception_response()
    {
        var endpoint = new SerialDeviceEndpoint("COM6", 1, 9600);
        var response = ModbusCrc.Append([0x01, 0x83, 0x02]);
        var transport = new FakeSerialTransport(response);
        var client = new ModbusRtuCommandClient(_ => endpoint, _ => transport);

        var result = await client.ReadMeasurementAsync(
            new DeviceAddress(Guid.NewGuid(), MeasurementSource.Vfd, "40030"),
            new ReadCommand("Voltage"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("Serial.ModbusException");
        result.Message.Should().Contain("0x02");
    }

    [Fact]
    public async Task Write_failure_reports_transport_exception_instead_of_throwing()
    {
        var endpoint = new SerialDeviceEndpoint("COM6", 1, 9600);
        var client = new ModbusRtuCommandClient(_ => endpoint, _ => new ThrowingSerialTransport());

        var result = await client.WriteAsync(
            new DeviceAddress(Guid.NewGuid(), MeasurementSource.Vfd, "0x2000"),
            new WriteCommand("Start", "1"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("Serial.TransportError");
        result.Message.Should().Contain("serial failure");
    }

    private sealed class FakeSerialTransport : ISerialTransport
    {
        private readonly byte[] _response;

        public FakeSerialTransport(byte[] response)
        {
            _response = response;
        }

        public byte[] Request { get; private set; } = [];

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public Task<byte[]> TransactAsync(byte[] request, TimeSpan timeout, CancellationToken ct)
        {
            Request = request;
            return Task.FromResult(_response);
        }
    }

    private sealed class ThrowingSerialTransport : ISerialTransport
    {
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public Task<byte[]> TransactAsync(byte[] request, TimeSpan timeout, CancellationToken ct)
        {
            throw new IOException("serial failure");
        }
    }
}
