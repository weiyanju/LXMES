using VfdControl.Application.Common;
using VfdControl.Domain.ValueObjects;

namespace VfdControl.Application.Operator;

public sealed class OperatorSessionService
{
    public AppResult<OperatorSession> StartSession(string employeeCode)
    {
        var code = EmployeeCode.TryCreate(employeeCode);
        if (!code.IsSuccess || code.Value is null)
        {
            return AppResult<OperatorSession>.Failure(code.Error?.Message ?? "Invalid employee code.", code.Error?.Code);
        }

        return AppResult<OperatorSession>.Success(new OperatorSession(Guid.NewGuid(), code.Value, DateTimeOffset.UtcNow));
    }
}

public sealed record OperatorSession(
    Guid Id,
    EmployeeCode EmployeeCode,
    DateTimeOffset StartedAt);
