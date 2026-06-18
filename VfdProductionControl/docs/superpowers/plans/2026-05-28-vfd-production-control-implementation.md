# VfdProductionControl Phase 1 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a clean WPF + .NET Phase 1 system that can run a simulated production test flow: operator scans employee code, selects a plan and slots, scans VFD barcodes, executes multi-slot simulated tests, displays conclusions, and persists trace data.

**Architecture:** Use a layered solution: WPF App/Presentation -> Application -> Domain, with Infrastructure implementing Application interfaces. Phase 1 uses simulated devices/instruments first, but preserves interfaces for real serial Modbus, SQL Server persistence, barcode input, and external production integration.

**Tech Stack:** .NET 10.0-windows, WPF, CommunityToolkit.Mvvm, Microsoft.Extensions.Hosting, Microsoft.Extensions.DependencyInjection, xUnit, FluentAssertions, Dapper, Microsoft.Data.SqlClient, System.IO.Ports, Serilog.

---

## Reference Documents

- `ARCHITECTURE.md`
- `superpowers/specs/2026-05-28-vfd-production-control-design.md`
- `docs/superpowers/plans/2026-05-28-vfd-production-control-implementation.md`

## File Structure

Create this structure:

```text
src/
  VfdControl.App/
  VfdControl.Presentation/
  VfdControl.Application/
  VfdControl.Domain/
  VfdControl.Infrastructure/
  VfdControl.Contracts/
tests/
  VfdControl.Domain.Tests/
  VfdControl.Application.Tests/
  VfdControl.Infrastructure.Tests/
  VfdControl.Presentation.Tests/
```

Ownership:

- `VfdControl.Domain`: entities, value objects, enums, rules that do not depend on WPF, SQL, serial ports, or logging.
- `VfdControl.Application`: use-case services, orchestration interfaces, workflow engine, slot scheduler, result objects.
- `VfdControl.Contracts`: DTOs shared across UI/application/infrastructure boundaries.
- `VfdControl.Infrastructure`: simulated device client, SQL repositories, barcode services, serial transport adapter skeleton, external adapter skeleton.
- `VfdControl.Presentation`: ViewModels, UI state models, WPF Views.
- `VfdControl.App`: startup, Host, DI registration, appsettings, main shell.

---

### Task 1: Scaffold Solution and Project References

**Files:**
- Create: `VfdProductionControl.sln`
- Create: `src/VfdControl.Domain/VfdControl.Domain.csproj`
- Create: `src/VfdControl.Contracts/VfdControl.Contracts.csproj`
- Create: `src/VfdControl.Application/VfdControl.Application.csproj`
- Create: `src/VfdControl.Infrastructure/VfdControl.Infrastructure.csproj`
- Create: `src/VfdControl.Presentation/VfdControl.Presentation.csproj`
- Create: `src/VfdControl.App/VfdControl.App.csproj`
- Create: `tests/VfdControl.Domain.Tests/VfdControl.Domain.Tests.csproj`
- Create: `tests/VfdControl.Application.Tests/VfdControl.Application.Tests.csproj`
- Create: `tests/VfdControl.Infrastructure.Tests/VfdControl.Infrastructure.Tests.csproj`
- Create: `tests/VfdControl.Presentation.Tests/VfdControl.Presentation.Tests.csproj`
- Create: `.gitignore`
- Create: `README.md`

- [ ] **Step 1: Create solution and source projects**

Run:

```powershell
dotnet new sln -n VfdProductionControl
dotnet new classlib -n VfdControl.Domain -o src\VfdControl.Domain
dotnet new classlib -n VfdControl.Contracts -o src\VfdControl.Contracts
dotnet new classlib -n VfdControl.Application -o src\VfdControl.Application
dotnet new classlib -n VfdControl.Infrastructure -o src\VfdControl.Infrastructure
dotnet new classlib -n VfdControl.Presentation -o src\VfdControl.Presentation
dotnet new wpf -n VfdControl.App -o src\VfdControl.App
```

Expected: all projects are created without errors.

- [ ] **Step 2: Create test projects**

Run:

```powershell
dotnet new xunit -n VfdControl.Domain.Tests -o tests\VfdControl.Domain.Tests
dotnet new xunit -n VfdControl.Application.Tests -o tests\VfdControl.Application.Tests
dotnet new xunit -n VfdControl.Infrastructure.Tests -o tests\VfdControl.Infrastructure.Tests
dotnet new xunit -n VfdControl.Presentation.Tests -o tests\VfdControl.Presentation.Tests
```

Expected: all test projects are created without errors.

- [ ] **Step 3: Add projects to solution**

Run:

```powershell
dotnet sln add src\VfdControl.Domain\VfdControl.Domain.csproj
dotnet sln add src\VfdControl.Contracts\VfdControl.Contracts.csproj
dotnet sln add src\VfdControl.Application\VfdControl.Application.csproj
dotnet sln add src\VfdControl.Infrastructure\VfdControl.Infrastructure.csproj
dotnet sln add src\VfdControl.Presentation\VfdControl.Presentation.csproj
dotnet sln add src\VfdControl.App\VfdControl.App.csproj
dotnet sln add tests\VfdControl.Domain.Tests\VfdControl.Domain.Tests.csproj
dotnet sln add tests\VfdControl.Application.Tests\VfdControl.Application.Tests.csproj
dotnet sln add tests\VfdControl.Infrastructure.Tests\VfdControl.Infrastructure.Tests.csproj
dotnet sln add tests\VfdControl.Presentation.Tests\VfdControl.Presentation.Tests.csproj
```

Expected: each project is added to the solution.

- [ ] **Step 4: Add project references**

Run:

```powershell
dotnet add src\VfdControl.Application\VfdControl.Application.csproj reference src\VfdControl.Domain\VfdControl.Domain.csproj
dotnet add src\VfdControl.Application\VfdControl.Application.csproj reference src\VfdControl.Contracts\VfdControl.Contracts.csproj
dotnet add src\VfdControl.Infrastructure\VfdControl.Infrastructure.csproj reference src\VfdControl.Application\VfdControl.Application.csproj
dotnet add src\VfdControl.Infrastructure\VfdControl.Infrastructure.csproj reference src\VfdControl.Domain\VfdControl.Domain.csproj
dotnet add src\VfdControl.Infrastructure\VfdControl.Infrastructure.csproj reference src\VfdControl.Contracts\VfdControl.Contracts.csproj
dotnet add src\VfdControl.Presentation\VfdControl.Presentation.csproj reference src\VfdControl.Application\VfdControl.Application.csproj
dotnet add src\VfdControl.Presentation\VfdControl.Presentation.csproj reference src\VfdControl.Contracts\VfdControl.Contracts.csproj
dotnet add src\VfdControl.App\VfdControl.App.csproj reference src\VfdControl.Presentation\VfdControl.Presentation.csproj
dotnet add src\VfdControl.App\VfdControl.App.csproj reference src\VfdControl.Infrastructure\VfdControl.Infrastructure.csproj
dotnet add src\VfdControl.App\VfdControl.App.csproj reference src\VfdControl.Application\VfdControl.Application.csproj
dotnet add tests\VfdControl.Domain.Tests\VfdControl.Domain.Tests.csproj reference src\VfdControl.Domain\VfdControl.Domain.csproj
dotnet add tests\VfdControl.Application.Tests\VfdControl.Application.Tests.csproj reference src\VfdControl.Application\VfdControl.Application.csproj
dotnet add tests\VfdControl.Application.Tests\VfdControl.Application.Tests.csproj reference src\VfdControl.Domain\VfdControl.Domain.csproj
dotnet add tests\VfdControl.Infrastructure.Tests\VfdControl.Infrastructure.Tests.csproj reference src\VfdControl.Infrastructure\VfdControl.Infrastructure.csproj
dotnet add tests\VfdControl.Presentation.Tests\VfdControl.Presentation.Tests.csproj reference src\VfdControl.Presentation\VfdControl.Presentation.csproj
```

Expected: dependency direction matches `ARCHITECTURE.md`.

- [ ] **Step 5: Add required NuGet packages**

Run:

```powershell
dotnet add src\VfdControl.Presentation\VfdControl.Presentation.csproj package CommunityToolkit.Mvvm
dotnet add src\VfdControl.App\VfdControl.App.csproj package Microsoft.Extensions.Hosting
dotnet add src\VfdControl.App\VfdControl.App.csproj package Microsoft.Extensions.DependencyInjection
dotnet add src\VfdControl.App\VfdControl.App.csproj package Microsoft.Extensions.Configuration.Json
dotnet add src\VfdControl.App\VfdControl.App.csproj package Microsoft.Extensions.Configuration.EnvironmentVariables
dotnet add src\VfdControl.App\VfdControl.App.csproj package Serilog.Extensions.Hosting
dotnet add src\VfdControl.App\VfdControl.App.csproj package Serilog.Sinks.File
dotnet add src\VfdControl.Infrastructure\VfdControl.Infrastructure.csproj package Dapper
dotnet add src\VfdControl.Infrastructure\VfdControl.Infrastructure.csproj package Microsoft.Data.SqlClient
dotnet add src\VfdControl.Infrastructure\VfdControl.Infrastructure.csproj package System.IO.Ports
dotnet add tests\VfdControl.Domain.Tests\VfdControl.Domain.Tests.csproj package FluentAssertions
dotnet add tests\VfdControl.Application.Tests\VfdControl.Application.Tests.csproj package FluentAssertions
dotnet add tests\VfdControl.Infrastructure.Tests\VfdControl.Infrastructure.Tests.csproj package FluentAssertions
dotnet add tests\VfdControl.Presentation.Tests\VfdControl.Presentation.Tests.csproj package FluentAssertions
```

Expected: packages restore successfully.

- [ ] **Step 6: Create `.gitignore`**

Add:

```gitignore
bin/
obj/
.vs/
*.user
*.suo
*.log
logs/
appsettings.Development.json
*.db
TestResults/
.superpowers/
```

- [ ] **Step 7: Create initial README**

Add:

```markdown
# VfdProductionControl

流水线变频器测试方案执行与监控平台。

Phase 1 focuses on a simulated production run:

- operator employee-code scan
- plan selection
- station slot selection
- VFD barcode binding
- simulated multi-slot execution
- measurement comparison
- traceability records
```

- [ ] **Step 8: Build and test**

Run:

```powershell
dotnet build
dotnet test
```

Expected: build succeeds and all default tests pass.

- [ ] **Step 9: Commit**

```powershell
git add .
git commit -m "chore: scaffold VfdProductionControl solution"
```

---

### Task 2: Add Domain Enums, Value Objects, and Rule Tests

**Files:**
- Create: `src/VfdControl.Domain/Common/Result.cs`
- Create: `src/VfdControl.Domain/Common/DomainError.cs`
- Create: `src/VfdControl.Domain/Enums/Conclusion.cs`
- Create: `src/VfdControl.Domain/Enums/FailureAction.cs`
- Create: `src/VfdControl.Domain/Enums/RunStatuses.cs`
- Create: `src/VfdControl.Domain/Enums/MeasurementEnums.cs`
- Create: `src/VfdControl.Domain/ValueObjects/Barcode.cs`
- Create: `src/VfdControl.Domain/ValueObjects/EmployeeCode.cs`
- Create: `src/VfdControl.Domain/ValueObjects/SlotNumber.cs`
- Create: `src/VfdControl.Domain/ValueObjects/SerialPortName.cs`
- Create: `src/VfdControl.Domain/ValueObjects/ModbusAddress.cs`
- Create: `src/VfdControl.Domain/ValueObjects/MeasurementValue.cs`
- Create: `src/VfdControl.Domain/ValueObjects/Tolerance.cs`
- Test: `tests/VfdControl.Domain.Tests/ValueObjects/ToleranceTests.cs`
- Test: `tests/VfdControl.Domain.Tests/ValueObjects/BarcodeTests.cs`

- [ ] **Step 1: Write Tolerance tests**

Create `tests/VfdControl.Domain.Tests/ValueObjects/ToleranceTests.cs`:

```csharp
using FluentAssertions;
using VfdControl.Domain.Enums;
using VfdControl.Domain.ValueObjects;

namespace VfdControl.Domain.Tests.ValueObjects;

public class ToleranceTests
{
    [Fact]
    public void AbsoluteTolerance_accepts_difference_within_limit()
    {
        var tolerance = Tolerance.Absolute(2.0);
        tolerance.IsWithin(220.0, 218.2).Should().BeTrue();
    }

    [Fact]
    public void AbsoluteTolerance_rejects_difference_outside_limit()
    {
        var tolerance = Tolerance.Absolute(1.0);
        tolerance.IsWithin(220.0, 218.2).Should().BeFalse();
    }

    [Fact]
    public void PercentTolerance_uses_reference_value_as_basis()
    {
        var tolerance = Tolerance.Percent(1.0);
        tolerance.IsWithin(primaryValue: 220.0, referenceValue: 218.0).Should().BeTrue();
    }

    [Fact]
    public void PercentTolerance_rejects_large_percent_difference()
    {
        var tolerance = Tolerance.Percent(0.5);
        tolerance.IsWithin(primaryValue: 220.0, referenceValue: 218.0).Should().BeFalse();
    }
}
```

- [ ] **Step 2: Write Barcode tests**

Create `tests/VfdControl.Domain.Tests/ValueObjects/BarcodeTests.cs`:

```csharp
using FluentAssertions;
using VfdControl.Domain.ValueObjects;

namespace VfdControl.Domain.Tests.ValueObjects;

public class BarcodeTests
{
    [Fact]
    public void EmployeeCode_accepts_default_test_format()
    {
        EmployeeCode.TryCreate("EMP0001").IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void VfdBarcode_accepts_default_test_format()
    {
        Barcode.TryCreateVfd("VFD202605280001").IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void VfdBarcode_rejects_empty_text()
    {
        Barcode.TryCreateVfd("").IsSuccess.Should().BeFalse();
    }
}
```

- [ ] **Step 3: Run tests and verify they fail**

Run:

```powershell
dotnet test tests\VfdControl.Domain.Tests\VfdControl.Domain.Tests.csproj
```

Expected: fail because domain types do not exist.

- [ ] **Step 4: Implement enums**

Create enum files:

```csharp
namespace VfdControl.Domain.Enums;

public enum Conclusion { None, Pass, Fail, Warning }
public enum FailureAction { ContinueAndMarkFail, ContinueAsWarning, StopSlotImmediately, PauseAllSlots, RetryThenStop, RequireOperatorConfirm }
public enum ToleranceType { Absolute, Percent }
public enum MeasurementSource { Vfd, Instrument }
public enum DataType { Number, String, Boolean, Enum, BitField }
public enum SessionStatus { NotStarted, Running, Paused, Completed, Stopped, Faulted }
public enum SlotRunStatus { Empty, WaitingBarcode, Queued, Running, Passed, Failed, Warning, PendingAction, Stopped, Removed }
public enum StepRunStatus { Pending, Running, Passed, Warning, Failed, Skipped, WaitingOperator, Retried }
```

Place them in the enum files listed above.

- [ ] **Step 5: Implement result and value objects**

Implement:

```csharp
namespace VfdControl.Domain.Common;

public sealed record DomainError(string Code, string Message);

public sealed class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public DomainError? Error { get; }

    private Result(bool isSuccess, T? value, DomainError? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(string code, string message) => new(false, default, new DomainError(code, message));
}
```

Implement `Tolerance`:

```csharp
using VfdControl.Domain.Enums;

namespace VfdControl.Domain.ValueObjects;

public sealed record Tolerance(ToleranceType Type, double Value)
{
    public static Tolerance Absolute(double value) => new(ToleranceType.Absolute, value);
    public static Tolerance Percent(double value) => new(ToleranceType.Percent, value);

    public bool IsWithin(double primaryValue, double referenceValue)
    {
        var difference = Math.Abs(primaryValue - referenceValue);
        return Type switch
        {
            ToleranceType.Absolute => difference <= Value,
            ToleranceType.Percent => referenceValue == 0
                ? difference == 0
                : difference / Math.Abs(referenceValue) * 100.0 <= Value,
            _ => false
        };
    }
}
```

Implement barcode objects:

```csharp
using System.Text.RegularExpressions;
using VfdControl.Domain.Common;

namespace VfdControl.Domain.ValueObjects;

public sealed record EmployeeCode(string Value)
{
    private static readonly Regex DefaultPattern = new("^EMP\\d{4,8}$", RegexOptions.Compiled);

    public static Result<EmployeeCode> TryCreate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<EmployeeCode>.Failure("EmployeeCode.Empty", "Employee code is required.");

        var normalized = value.Trim().ToUpperInvariant();
        return DefaultPattern.IsMatch(normalized)
            ? Result<EmployeeCode>.Success(new EmployeeCode(normalized))
            : Result<EmployeeCode>.Failure("EmployeeCode.Invalid", "Employee code must match EMP plus 4-8 digits.");
    }
}

public sealed record Barcode(string Value)
{
    private static readonly Regex DefaultVfdPattern = new("^VFD[A-Z0-9]{8,20}$", RegexOptions.Compiled);

    public static Result<Barcode> TryCreateVfd(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<Barcode>.Failure("Barcode.Empty", "Barcode is required.");

        var normalized = value.Trim().ToUpperInvariant();
        return DefaultVfdPattern.IsMatch(normalized)
            ? Result<Barcode>.Success(new Barcode(normalized))
            : Result<Barcode>.Failure("Barcode.Invalid", "VFD barcode must match VFD plus 8-20 uppercase letters or digits.");
    }
}
```

- [ ] **Step 6: Run tests**

Run:

```powershell
dotnet test tests\VfdControl.Domain.Tests\VfdControl.Domain.Tests.csproj
```

Expected: all domain tests pass.

- [ ] **Step 7: Commit**

```powershell
git add src\VfdControl.Domain tests\VfdControl.Domain.Tests
git commit -m "feat: add core domain value objects"
```

---

### Task 3: Add Domain Entities for Station, Plan, and Runs

**Files:**
- Create: `src/VfdControl.Domain/Stations/Station.cs`
- Create: `src/VfdControl.Domain/Stations/StationSlot.cs`
- Create: `src/VfdControl.Domain/Stations/SlotCommunicationConfig.cs`
- Create: `src/VfdControl.Domain/Stations/SlotInstrument.cs`
- Create: `src/VfdControl.Domain/Stations/InstrumentPoint.cs`
- Create: `src/VfdControl.Domain/Plans/ProcessPlan.cs`
- Create: `src/VfdControl.Domain/Plans/ProcessPlanVersion.cs`
- Create: `src/VfdControl.Domain/Plans/ProcessStep.cs`
- Create: `src/VfdControl.Domain/Plans/StepCommand.cs`
- Create: `src/VfdControl.Domain/Plans/StepRule.cs`
- Create: `src/VfdControl.Domain/Plans/StepFailurePolicy.cs`
- Create: `src/VfdControl.Domain/Runs/StationSession.cs`
- Create: `src/VfdControl.Domain/Runs/DeviceRun.cs`
- Create: `src/VfdControl.Domain/Runs/StepRun.cs`
- Create: `src/VfdControl.Domain/Runs/MeasurementResult.cs`
- Create: `src/VfdControl.Domain/Runs/ComparisonResult.cs`
- Test: `tests/VfdControl.Domain.Tests/Runs/ConclusionAggregationTests.cs`

- [ ] **Step 1: Write conclusion aggregation tests**

Create `tests/VfdControl.Domain.Tests/Runs/ConclusionAggregationTests.cs`:

```csharp
using FluentAssertions;
using VfdControl.Domain.Enums;
using VfdControl.Domain.Runs;

namespace VfdControl.Domain.Tests.Runs;

public class ConclusionAggregationTests
{
    [Fact]
    public void DeviceRun_passes_when_all_affecting_steps_pass()
    {
        var run = DeviceRun.CreateForTest();
        run.AddStepConclusion(Conclusion.Pass, affectsFinalConclusion: true);
        run.AddStepConclusion(Conclusion.Pass, affectsFinalConclusion: true);

        run.CalculateFinalConclusion().Should().Be(Conclusion.Pass);
    }

    [Fact]
    public void DeviceRun_fails_when_any_affecting_step_fails()
    {
        var run = DeviceRun.CreateForTest();
        run.AddStepConclusion(Conclusion.Pass, affectsFinalConclusion: true);
        run.AddStepConclusion(Conclusion.Fail, affectsFinalConclusion: true);

        run.CalculateFinalConclusion().Should().Be(Conclusion.Fail);
    }

    [Fact]
    public void DeviceRun_warns_when_only_non_affecting_warning_exists()
    {
        var run = DeviceRun.CreateForTest();
        run.AddStepConclusion(Conclusion.Pass, affectsFinalConclusion: true);
        run.AddStepConclusion(Conclusion.Warning, affectsFinalConclusion: false);

        run.CalculateFinalConclusion().Should().Be(Conclusion.Warning);
    }
}
```

- [ ] **Step 2: Run tests and verify they fail**

Run:

```powershell
dotnet test tests\VfdControl.Domain.Tests\VfdControl.Domain.Tests.csproj
```

Expected: fail because run entities do not exist.

- [ ] **Step 3: Implement minimal run aggregation model**

Create `DeviceRun`:

```csharp
using VfdControl.Domain.Enums;

namespace VfdControl.Domain.Runs;

public sealed class DeviceRun
{
    private readonly List<(Conclusion Conclusion, bool AffectsFinalConclusion)> _stepConclusions = [];

    public Guid Id { get; } = Guid.NewGuid();
    public Conclusion FinalConclusion { get; private set; } = Conclusion.None;

    public static DeviceRun CreateForTest() => new();

    public void AddStepConclusion(Conclusion conclusion, bool affectsFinalConclusion)
    {
        _stepConclusions.Add((conclusion, affectsFinalConclusion));
    }

    public Conclusion CalculateFinalConclusion()
    {
        if (_stepConclusions.Any(x => x.AffectsFinalConclusion && x.Conclusion == Conclusion.Fail))
            return FinalConclusion = Conclusion.Fail;

        if (_stepConclusions.Any(x => x.Conclusion == Conclusion.Warning))
            return FinalConclusion = Conclusion.Warning;

        if (_stepConclusions.Count > 0 && _stepConclusions.All(x => x.Conclusion is Conclusion.Pass or Conclusion.Warning))
            return FinalConclusion = Conclusion.Pass;

        return FinalConclusion = Conclusion.None;
    }
}
```

Then add the other entity classes as simple records/classes with required properties. Keep behavior small until later tasks need it.

- [ ] **Step 4: Run tests**

Run:

```powershell
dotnet test tests\VfdControl.Domain.Tests\VfdControl.Domain.Tests.csproj
```

Expected: all domain tests pass.

- [ ] **Step 5: Commit**

```powershell
git add src\VfdControl.Domain tests\VfdControl.Domain.Tests
git commit -m "feat: add station plan and run domain models"
```

---

### Task 4: Add Measurement and String Rule Evaluation

**Files:**
- Create: `src/VfdControl.Domain/Rules/NumericRangeRule.cs`
- Create: `src/VfdControl.Domain/Rules/MeasurementComparisonRule.cs`
- Create: `src/VfdControl.Domain/Rules/StringMatchRule.cs`
- Create: `src/VfdControl.Domain/Rules/RuleEvaluationResult.cs`
- Test: `tests/VfdControl.Domain.Tests/Rules/NumericRuleTests.cs`
- Test: `tests/VfdControl.Domain.Tests/Rules/MeasurementComparisonRuleTests.cs`
- Test: `tests/VfdControl.Domain.Tests/Rules/StringMatchRuleTests.cs`

- [ ] **Step 1: Write numeric range tests**

Create tests for:

- value inside lower/upper range returns `Pass`
- value below lower returns `Fail`
- value above upper returns `Fail`

- [ ] **Step 2: Write comparison tests**

Create tests for:

- VFD voltage 220 and instrument voltage 219 with absolute tolerance 2 returns `Pass`
- VFD voltage 225 and instrument voltage 219 with absolute tolerance 2 returns `Fail`
- VFD voltage and instrument voltage within percent tolerance returns `Pass`

- [ ] **Step 3: Write string match tests**

Create tests for:

- exact match passes
- allowed list passes
- regex match passes
- mismatch fails

- [ ] **Step 4: Run tests and verify they fail**

Run:

```powershell
dotnet test tests\VfdControl.Domain.Tests\VfdControl.Domain.Tests.csproj
```

Expected: fail because rules are not implemented.

- [ ] **Step 5: Implement rule classes**

Implement each rule as a small immutable class returning:

```csharp
using VfdControl.Domain.Enums;

namespace VfdControl.Domain.Rules;

public sealed record RuleEvaluationResult(
    Conclusion Conclusion,
    string Message,
    bool AffectsFinalConclusion);
```

Rule behavior:

- `NumericRangeRule` compares converted numeric values against nullable lower/upper limits.
- `MeasurementComparisonRule` compares primary VFD value against reference instrument value using `Tolerance`.
- `StringMatchRule` supports exact, allowed list, contains, prefix, suffix, regex, and case sensitivity.

- [ ] **Step 6: Run tests**

Run:

```powershell
dotnet test tests\VfdControl.Domain.Tests\VfdControl.Domain.Tests.csproj
```

Expected: all domain rule tests pass.

- [ ] **Step 7: Commit**

```powershell
git add src\VfdControl.Domain tests\VfdControl.Domain.Tests
git commit -m "feat: add domain rule evaluation"
```

---

### Task 5: Add Application Interfaces and In-Memory Test Doubles

**Files:**
- Create: `src/VfdControl.Application/Abstractions/IWorkflowEngine.cs`
- Create: `src/VfdControl.Application/Abstractions/ISlotScheduler.cs`
- Create: `src/VfdControl.Application/Abstractions/IDeviceCommandClient.cs`
- Create: `src/VfdControl.Application/Abstractions/IBarcodeInputService.cs`
- Create: `src/VfdControl.Application/Abstractions/ITraceRepository.cs`
- Create: `src/VfdControl.Application/Abstractions/IProductionDataAdapter.cs`
- Create: `src/VfdControl.Application/Common/AppResult.cs`
- Create: `src/VfdControl.Application/Execution/Contexts.cs`
- Create: `src/VfdControl.Application/Execution/ExecutionResults.cs`
- Test: `tests/VfdControl.Application.Tests/TestDoubles/FakeDeviceCommandClient.cs`
- Test: `tests/VfdControl.Application.Tests/TestDoubles/InMemoryTraceRepository.cs`

- [ ] **Step 1: Define Application result object**

Create `AppResult` and `AppResult<T>` with success/failure, message, and optional error code.

- [ ] **Step 2: Define communication interface**

Use signatures:

```csharp
public interface IDeviceCommandClient
{
    Task<CommandResult<MeasurementValue>> ReadMeasurementAsync(DeviceAddress address, ReadCommand command, CancellationToken ct);
    Task<CommandResult<string>> ReadStringAsync(DeviceAddress address, ReadStringCommand command, CancellationToken ct);
    Task<CommandResult> WriteAsync(DeviceAddress address, WriteCommand command, CancellationToken ct);
}
```

- [ ] **Step 3: Define workflow and scheduler interfaces**

Use the signatures from `ARCHITECTURE.md`.

- [ ] **Step 4: Add test doubles**

Create fake command client with configurable responses:

- measurement values by `(slotId, source, pointName)`
- string values by `(slotId, source, pointName)`
- write command log
- optional failure injection

Create in-memory trace repository storing sessions, runs, steps, and command traces in lists.

- [ ] **Step 5: Build**

Run:

```powershell
dotnet build
```

Expected: build succeeds.

- [ ] **Step 6: Commit**

```powershell
git add src\VfdControl.Application tests\VfdControl.Application.Tests
git commit -m "feat: add application interfaces"
```

---

### Task 6: Implement Workflow Engine with Simulated Step Execution

**Files:**
- Create: `src/VfdControl.Application/Execution/WorkflowEngine.cs`
- Create: `src/VfdControl.Application/Execution/StepExecutors/StartStepExecutor.cs`
- Create: `src/VfdControl.Application/Execution/StepExecutors/StopStepExecutor.cs`
- Create: `src/VfdControl.Application/Execution/StepExecutors/DelayStepExecutor.cs`
- Create: `src/VfdControl.Application/Execution/StepExecutors/ReadMeasurementStepExecutor.cs`
- Create: `src/VfdControl.Application/Execution/StepExecutors/CompareMeasurementStepExecutor.cs`
- Create: `src/VfdControl.Application/Execution/StepExecutors/ReadStringStepExecutor.cs`
- Test: `tests/VfdControl.Application.Tests/Execution/WorkflowEngineTests.cs`

- [ ] **Step 1: Write workflow tests**

Test cases:

- software start writes start command before reads
- delay step is executed in order
- VFD and instrument voltage are read and compared
- failed comparison with `ContinueAndMarkFail` continues later steps but final result is fail
- failed string comparison with `StopSlotImmediately` writes stop command and ends slot
- normal completion writes stop command

- [ ] **Step 2: Run tests and verify they fail**

Run:

```powershell
dotnet test tests\VfdControl.Application.Tests\VfdControl.Application.Tests.csproj
```

Expected: fail because workflow engine is not implemented.

- [ ] **Step 3: Implement workflow engine**

Implement behavior:

- iterate steps in order
- call correct step executor
- write trace for each step
- apply failure strategy
- aggregate conclusions
- always attempt stop in normal completion or slot termination when configured

- [ ] **Step 4: Run application tests**

Run:

```powershell
dotnet test tests\VfdControl.Application.Tests\VfdControl.Application.Tests.csproj
```

Expected: workflow tests pass.

- [ ] **Step 5: Commit**

```powershell
git add src\VfdControl.Application tests\VfdControl.Application.Tests
git commit -m "feat: implement workflow engine"
```

---

### Task 7: Implement Slot Scheduler for Multi-Slot Parallel Execution

**Files:**
- Create: `src/VfdControl.Application/Execution/SlotScheduler.cs`
- Create: `src/VfdControl.Application/Execution/SlotExecutionStateStore.cs`
- Test: `tests/VfdControl.Application.Tests/Execution/SlotSchedulerTests.cs`

- [ ] **Step 1: Write scheduler tests**

Test cases:

- selected slots start independent executions
- slot A failure does not stop slot B when policy is single-slot
- `PauseAllSlots` policy pauses all executions
- stop session requests stop for all running slots
- single slot serializes its own command calls

- [ ] **Step 2: Run tests and verify they fail**

Run:

```powershell
dotnet test tests\VfdControl.Application.Tests\VfdControl.Application.Tests.csproj
```

Expected: fail because scheduler is not implemented.

- [ ] **Step 3: Implement scheduler**

Implementation rules:

- create one task per selected slot
- use per-slot cancellation token
- keep session-level cancellation token
- expose pause/resume state
- use workflow engine per slot
- write session result when all slots finish or session stops

- [ ] **Step 4: Run tests**

Run:

```powershell
dotnet test tests\VfdControl.Application.Tests\VfdControl.Application.Tests.csproj
```

Expected: scheduler tests pass.

- [ ] **Step 5: Commit**

```powershell
git add src\VfdControl.Application tests\VfdControl.Application.Tests
git commit -m "feat: add multi-slot scheduler"
```

---

### Task 8: Implement Simulated Device and Instrument Infrastructure

**Files:**
- Create: `src/VfdControl.Infrastructure/Simulation/SimulatedDeviceCommandClient.cs`
- Create: `src/VfdControl.Infrastructure/Simulation/SimulatedSlotState.cs`
- Create: `src/VfdControl.Infrastructure/Simulation/SimulationScenario.cs`
- Create: `src/VfdControl.Infrastructure/Simulation/SimulationScenarioLoader.cs`
- Create: `src/VfdControl.App/appsettings.example.json`
- Test: `tests/VfdControl.Infrastructure.Tests/Simulation/SimulatedDeviceCommandClientTests.cs`

- [ ] **Step 1: Write simulation tests**

Test cases:

- writing start command marks slot as running
- writing stop command marks slot as stopped
- reading VFD voltage returns configured value
- reading instrument voltage returns configured value
- configured timeout returns failed command result
- configured string returns model/version/serial text

- [ ] **Step 2: Run tests and verify they fail**

Run:

```powershell
dotnet test tests\VfdControl.Infrastructure.Tests\VfdControl.Infrastructure.Tests.csproj
```

Expected: fail because simulation infrastructure is missing.

- [ ] **Step 3: Implement simulation client**

Implement `SimulatedDeviceCommandClient` against in-memory `SimulationScenario`.

Scenario must support:

- slots
- VFD measurement points
- instrument measurement points
- string points
- failure injection

- [ ] **Step 4: Add example appsettings**

Add a safe example config with no passwords:

```json
{
  "AppMode": "Simulation",
  "Database": {
    "ConnectionString": "Server=.;Database=VfdProductionControl;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Simulation": {
    "DefaultSlots": 4
  }
}
```

- [ ] **Step 5: Run infrastructure tests**

Run:

```powershell
dotnet test tests\VfdControl.Infrastructure.Tests\VfdControl.Infrastructure.Tests.csproj
```

Expected: simulation tests pass.

- [ ] **Step 6: Commit**

```powershell
git add src\VfdControl.Infrastructure src\VfdControl.App\appsettings.example.json tests\VfdControl.Infrastructure.Tests
git commit -m "feat: add simulated device infrastructure"
```

---

### Task 9: Implement In-Memory Repositories and Seed Demo Data

**Files:**
- Create: `src/VfdControl.Application/Abstractions/IStationRepository.cs`
- Create: `src/VfdControl.Application/Abstractions/IProcessPlanRepository.cs`
- Create: `src/VfdControl.Infrastructure/InMemory/InMemoryStationRepository.cs`
- Create: `src/VfdControl.Infrastructure/InMemory/InMemoryProcessPlanRepository.cs`
- Create: `src/VfdControl.Infrastructure/InMemory/InMemoryTraceRepository.cs`
- Create: `src/VfdControl.Infrastructure/Seed/DemoDataSeeder.cs`
- Test: `tests/VfdControl.Infrastructure.Tests/InMemory/DemoDataSeederTests.cs`

- [ ] **Step 1: Write seed tests**

Verify demo data contains:

- one station
- at least four slots
- each slot has communication config
- each slot has VFD plus at least voltage instrument point
- one executable plan version with start, delay, read/compare, stop steps

- [ ] **Step 2: Run tests and verify they fail**

Run:

```powershell
dotnet test tests\VfdControl.Infrastructure.Tests\VfdControl.Infrastructure.Tests.csproj
```

Expected: fail because repositories and seed data are missing.

- [ ] **Step 3: Implement in-memory repositories**

Use lists/dictionaries and deterministic IDs.

- [ ] **Step 4: Implement demo data**

Create a demo plan:

1. start VFD
2. confirm running
3. delay 5 seconds
4. read VFD voltage
5. read instrument voltage
6. compare absolute tolerance
7. compare percent tolerance
8. stop VFD
9. confirm stopped

- [ ] **Step 5: Run tests**

Run:

```powershell
dotnet test tests\VfdControl.Infrastructure.Tests\VfdControl.Infrastructure.Tests.csproj
```

Expected: demo seed tests pass.

- [ ] **Step 6: Commit**

```powershell
git add src\VfdControl.Application src\VfdControl.Infrastructure tests\VfdControl.Infrastructure.Tests
git commit -m "feat: add in-memory repositories and demo data"
```

---

### Task 10: Implement Operator Production Use Cases

**Files:**
- Create: `src/VfdControl.Application/Operator/OperatorSessionService.cs`
- Create: `src/VfdControl.Application/Operator/PlanSelectionService.cs`
- Create: `src/VfdControl.Application/Operator/SlotSelectionService.cs`
- Create: `src/VfdControl.Application/Operator/ProductionRunService.cs`
- Create: `src/VfdControl.Application/Operator/RunStatusQueryService.cs`
- Test: `tests/VfdControl.Application.Tests/Operator/OperatorUseCaseTests.cs`

- [ ] **Step 1: Write use-case tests**

Test cases:

- employee code creates operator session
- invalid employee code is rejected
- executable plans are returned
- selected slots determine barcode scan order
- scanned VFD barcodes bind to selected slots in order
- production run starts scheduler with selected plan and slots

- [ ] **Step 2: Run tests and verify they fail**

Run:

```powershell
dotnet test tests\VfdControl.Application.Tests\VfdControl.Application.Tests.csproj
```

Expected: fail because services are missing.

- [ ] **Step 3: Implement services**

Keep these services thin:

- validate inputs
- call repositories
- build execution context
- call scheduler
- return result DTOs

- [ ] **Step 4: Run tests**

Run:

```powershell
dotnet test tests\VfdControl.Application.Tests\VfdControl.Application.Tests.csproj
```

Expected: operator use-case tests pass.

- [ ] **Step 5: Commit**

```powershell
git add src\VfdControl.Application tests\VfdControl.Application.Tests
git commit -m "feat: add operator production use cases"
```

---

### Task 11: Implement WPF App Shell and Dependency Injection

**Files:**
- Modify: `src/VfdControl.App/App.xaml`
- Modify: `src/VfdControl.App/App.xaml.cs`
- Create: `src/VfdControl.App/DependencyInjection.cs`
- Create: `src/VfdControl.App/Views/MainWindow.xaml`
- Create: `src/VfdControl.App/Views/MainWindow.xaml.cs`
- Create: `src/VfdControl.Presentation/Shell/MainShellViewModel.cs`
- Create: `src/VfdControl.Presentation/Shell/NavigationItem.cs`
- Test: `tests/VfdControl.Presentation.Tests/Shell/MainShellViewModelTests.cs`

- [ ] **Step 1: Write shell ViewModel tests**

Verify:

- default area is operator console
- engineer/admin areas are not selected by default
- navigation command changes current view key

- [ ] **Step 2: Implement shell ViewModel**

Use `CommunityToolkit.Mvvm`.

- [ ] **Step 3: Configure Host and DI**

Register:

- application services
- workflow engine
- slot scheduler
- in-memory repositories
- simulation client
- trace repository
- ViewModels
- MainWindow

- [ ] **Step 4: Build**

Run:

```powershell
dotnet build
```

Expected: build succeeds.

- [ ] **Step 5: Run app smoke test**

Run:

```powershell
dotnet run --project src\VfdControl.App\VfdControl.App.csproj
```

Expected: WPF window opens with operator console shell.

- [ ] **Step 6: Commit**

```powershell
git add src\VfdControl.App src\VfdControl.Presentation tests\VfdControl.Presentation.Tests
git commit -m "feat: add WPF app shell"
```

---

### Task 12: Implement Operator Console UI

**Files:**
- Create: `src/VfdControl.Presentation/Operator/OperatorConsoleViewModel.cs`
- Create: `src/VfdControl.Presentation/Operator/SlotCardViewModel.cs`
- Create: `src/VfdControl.Presentation/Operator/DeviceRunTableRowViewModel.cs`
- Create: `src/VfdControl.App/Views/Operator/OperatorConsoleView.xaml`
- Create: `src/VfdControl.App/Views/Operator/SlotCardView.xaml`
- Test: `tests/VfdControl.Presentation.Tests/Operator/OperatorConsoleViewModelTests.cs`

- [ ] **Step 1: Write ViewModel tests**

Verify:

- employee scan moves to plan selection state
- plan selection enables slot selection
- selected slots define barcode prompts
- barcodes bind to slot cards in order
- start command calls production service
- slot card color maps Pass green, Fail red, Warning yellow, Running neutral

- [ ] **Step 2: Implement ViewModels**

Operator console states:

- `WaitingEmployeeCode`
- `SelectingPlan`
- `SelectingSlots`
- `ScanningBarcodes`
- `ConfirmingStart`
- `Running`
- `Completed`

- [ ] **Step 3: Implement XAML**

Layout:

- top status bar: employee, plan, station, database/simulation status
- left workflow panel: current prompt and actions
- main card board: one card per selected slot
- lower table: details rows

- [ ] **Step 4: Run tests**

Run:

```powershell
dotnet test tests\VfdControl.Presentation.Tests\VfdControl.Presentation.Tests.csproj
```

Expected: presentation tests pass.

- [ ] **Step 5: Run app**

Run:

```powershell
dotnet run --project src\VfdControl.App\VfdControl.App.csproj
```

Expected: operator can complete demo scan flow using keyboard input.

- [ ] **Step 6: Commit**

```powershell
git add src\VfdControl.Presentation src\VfdControl.App tests\VfdControl.Presentation.Tests
git commit -m "feat: add operator console UI"
```

---

### Task 13: Implement Engineering Plan Configuration MVP

**Files:**
- Create: `src/VfdControl.Application/Engineering/ProcessPlanService.cs`
- Create: `src/VfdControl.Application/Engineering/WorkflowDefinitionService.cs`
- Create: `src/VfdControl.Presentation/Engineering/PlanListViewModel.cs`
- Create: `src/VfdControl.Presentation/Engineering/WorkflowEditorViewModel.cs`
- Create: `src/VfdControl.App/Views/Engineering/PlanListView.xaml`
- Create: `src/VfdControl.App/Views/Engineering/WorkflowEditorView.xaml`
- Test: `tests/VfdControl.Application.Tests/Engineering/ProcessPlanServiceTests.cs`
- Test: `tests/VfdControl.Presentation.Tests/Engineering/WorkflowEditorViewModelTests.cs`

- [ ] **Step 1: Write service tests**

Verify:

- creating plan works
- saving plan creates new version
- executable version appears in operator plan list
- editing version does not mutate historical version

- [ ] **Step 2: Implement services**

Support MVP:

- create plan
- create version from draft
- mark version executable
- clone version

- [ ] **Step 3: Implement editor ViewModels and XAML**

MVP editor supports:

- list steps
- add start step
- add delay step
- add read VFD measurement
- add read instrument measurement
- add compare measurement
- add stop step
- configure failure action

- [ ] **Step 4: Run tests and app**

Run:

```powershell
dotnet test
dotnet run --project src\VfdControl.App\VfdControl.App.csproj
```

Expected: can edit a simple demo plan in engineering area.

- [ ] **Step 5: Commit**

```powershell
git add src\VfdControl.Application src\VfdControl.Presentation src\VfdControl.App tests
git commit -m "feat: add engineering plan configuration MVP"
```

---

### Task 14: Implement Administration Configuration MVP

**Files:**
- Create: `src/VfdControl.Application/Admin/StationConfigurationService.cs`
- Create: `src/VfdControl.Application/Admin/BarcodeRuleService.cs`
- Create: `src/VfdControl.Presentation/Admin/StationConfigViewModel.cs`
- Create: `src/VfdControl.Presentation/Admin/BarcodeRuleViewModel.cs`
- Create: `src/VfdControl.App/Views/Admin/StationConfigView.xaml`
- Create: `src/VfdControl.App/Views/Admin/BarcodeRuleView.xaml`
- Test: `tests/VfdControl.Application.Tests/Admin/StationConfigurationServiceTests.cs`

- [ ] **Step 1: Write admin tests**

Verify:

- station can contain multiple slots
- slot serial port can be changed without changing slot identity
- slot can contain multiple instruments
- barcode rules validate default EMP and VFD formats

- [ ] **Step 2: Implement admin services**

Use in-memory repository first.

- [ ] **Step 3: Implement admin UI**

MVP supports:

- station name
- slot list
- COM port edit
- instrument list per slot
- instrument point edit
- default barcode rule display

- [ ] **Step 4: Run tests**

Run:

```powershell
dotnet test
```

Expected: all tests pass.

- [ ] **Step 5: Commit**

```powershell
git add src\VfdControl.Application src\VfdControl.Presentation src\VfdControl.App tests
git commit -m "feat: add administration configuration MVP"
```

---

### Task 15: Add SQL Server Persistence Skeleton

**Files:**
- Create: `src/VfdControl.Infrastructure/Sql/SqlConnectionFactory.cs`
- Create: `src/VfdControl.Infrastructure/Sql/DatabaseInitializer.cs`
- Create: `src/VfdControl.Infrastructure/Sql/SqlTraceRepository.cs`
- Create: `src/VfdControl.Infrastructure/Sql/SqlStationRepository.cs`
- Create: `src/VfdControl.Infrastructure/Sql/SqlProcessPlanRepository.cs`
- Create: `src/VfdControl.Infrastructure/Sql/schema.sql`
- Test: `tests/VfdControl.Infrastructure.Tests/Sql/SchemaScriptTests.cs`

- [ ] **Step 1: Write schema script test**

Test that `schema.sql` contains required table names:

- `Stations`
- `StationSlots`
- `SlotInstruments`
- `ProcessPlans`
- `ProcessPlanVersions`
- `StationSessions`
- `DeviceRuns`
- `StepRuns`
- `MeasurementResults`
- `ComparisonResults`
- `CommandTraces`

- [ ] **Step 2: Create schema**

Add tables with identity primary keys, foreign keys where practical, and JSON columns as `NVARCHAR(MAX)`.

- [ ] **Step 3: Implement connection factory**

Read connection string from configuration. Do not hardcode passwords.

- [ ] **Step 4: Implement trace repository first**

Persist:

- session start/end
- device run
- step run
- measurement/comparison results
- command trace

- [ ] **Step 5: Build and run tests**

Run:

```powershell
dotnet build
dotnet test
```

Expected: build succeeds and schema tests pass. Integration tests that need a real SQL Server may be skipped unless configured.

- [ ] **Step 6: Commit**

```powershell
git add src\VfdControl.Infrastructure tests\VfdControl.Infrastructure.Tests
git commit -m "feat: add SQL persistence skeleton"
```

---

### Task 16: Add Traceability Query UI

**Files:**
- Create: `src/VfdControl.Application/Traceability/TraceabilityQueryService.cs`
- Create: `src/VfdControl.Presentation/Traceability/ExecutionHistoryViewModel.cs`
- Create: `src/VfdControl.Presentation/Traceability/DeviceRunTraceViewModel.cs`
- Create: `src/VfdControl.App/Views/Traceability/ExecutionHistoryView.xaml`
- Create: `src/VfdControl.App/Views/Traceability/DeviceRunTraceView.xaml`
- Test: `tests/VfdControl.Application.Tests/Traceability/TraceabilityQueryServiceTests.cs`

- [ ] **Step 1: Write query service tests**

Verify:

- query by date range returns sessions
- query by barcode returns device run
- device run details include steps
- step details include measurement and comparison data
- command traces include request/response JSON

- [ ] **Step 2: Implement query service**

Use `ITraceRepository` query methods.

- [ ] **Step 3: Implement query UI**

MVP supports:

- filter by date range
- filter by barcode
- filter by conclusion
- open device run detail
- view step and command trace details

- [ ] **Step 4: Run tests and app**

Run:

```powershell
dotnet test
dotnet run --project src\VfdControl.App\VfdControl.App.csproj
```

Expected: demo execution can be queried after completion.

- [ ] **Step 5: Commit**

```powershell
git add src\VfdControl.Application src\VfdControl.Presentation src\VfdControl.App tests
git commit -m "feat: add traceability query UI"
```

---

### Task 17: Add Real Serial Modbus Adapter Skeleton

**Files:**
- Create: `src/VfdControl.Infrastructure/Serial/SerialPortTransport.cs`
- Create: `src/VfdControl.Infrastructure/Serial/ModbusCrc.cs`
- Create: `src/VfdControl.Infrastructure/Serial/ModbusRtuCommandClient.cs`
- Test: `tests/VfdControl.Infrastructure.Tests/Serial/ModbusCrcTests.cs`

- [ ] **Step 1: Write CRC tests**

Use a known Modbus CRC frame and expected CRC bytes.

- [ ] **Step 2: Implement CRC utility**

Implement:

- `Compute`
- `Append`
- `IsValid`

- [ ] **Step 3: Implement serial transport adapter skeleton**

Support:

- open/close
- transact async
- per-port semaphore
- timeout

Keep it behind DI and disabled unless app mode is `RealSerial`.

- [ ] **Step 4: Build and test**

Run:

```powershell
dotnet test tests\VfdControl.Infrastructure.Tests\VfdControl.Infrastructure.Tests.csproj
dotnet build
```

Expected: CRC tests pass; real serial is compiled but not required for demo.

- [ ] **Step 5: Commit**

```powershell
git add src\VfdControl.Infrastructure tests\VfdControl.Infrastructure.Tests
git commit -m "feat: add serial Modbus adapter skeleton"
```

---

### Task 18: End-to-End Demo Validation

**Files:**
- Create: `docs/demo/phase-1-demo-script.md`
- Create: `docs/demo/phase-1-acceptance-checklist.md`
- Modify: `README.md`

- [ ] **Step 1: Write demo script**

Include:

1. start app
2. scan `EMP0001`
3. choose demo plan
4. choose slots 1, 2, 3
5. scan `VFD202605280001`, `VFD202605280002`, `VFD202605280003`
6. confirm start
7. observe parallel execution
8. inspect green/red/yellow cards
9. open traceability query
10. verify measurements and command traces

- [ ] **Step 2: Write acceptance checklist**

Checklist:

- operator session starts
- slots bind in selected order
- simulation starts VFDs
- voltage comparison generates small conclusions
- final conclusion aggregates correctly
- failed slot does not stop other slots unless configured
- stop command is attempted at completion or failure
- trace data is queryable

- [ ] **Step 3: Update README**

Add:

- prerequisites
- build command
- test command
- run command
- demo script link

- [ ] **Step 4: Run full verification**

Run:

```powershell
dotnet build
dotnet test
dotnet run --project src\VfdControl.App\VfdControl.App.csproj
```

Expected:

- build succeeds
- tests pass
- app opens
- demo script can be completed in simulation mode

- [ ] **Step 5: Commit**

```powershell
git add README.md docs
git commit -m "docs: add phase 1 demo validation"
```

---

## Self-Review Checklist

Before executing this plan:

- [ ] Confirm requirements document is approved.
- [ ] Confirm `ARCHITECTURE.md` is approved.
- [ ] Confirm solution target framework version available on this machine.
- [ ] Confirm whether SQL persistence must run in Phase 1 demo or can remain skeleton plus in-memory runtime.
- [ ] Confirm final project name remains `VfdProductionControl`.

Coverage mapping:

- Operator flow: Tasks 10, 12, 18.
- Engineer plan configuration: Task 13.
- Admin station/slot config: Task 14.
- Workflow engine: Task 6.
- Multi-slot parallel execution: Task 7.
- Simulated VFD/instruments: Task 8.
- Traceability: Tasks 15, 16.
- Real serial adapter skeleton: Task 17.
- Tests: Tasks 2-18 include test steps.
