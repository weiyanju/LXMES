# VfdProductionControl 项目架构设计

日期：2026-05-28

## 1. 架构目标

VfdProductionControl 是面向流水线工位的变频器测试方案执行与监控平台。系统第一阶段采用 WPF 桌面客户端 + 中央 SQL Server 的部署方式，优先实现模拟设备可试运行，后续再接入真实串口、变频器、监控仪表和外部生产系统。

架构目标：

- 支持多工位、多槽位。
- 每个槽位绑定一台变频器和一组监控仪表。
- 每个槽位有独立串口配置，槽位之间可并行执行。
- 槽位内部访问变频器和仪表时串行执行。
- 技工端流程简单稳定，工程师端配置能力清晰可扩展。
- 流程引擎、调度器、通信、数据库和 UI 解耦。
- 第一阶段可用模拟设备验证完整业务闭环。

## 2. 解决方案结构

建议解决方案结构如下：

```text
VfdProductionControl/
├─ VfdProductionControl.sln
├─ README.md
├─ ARCHITECTURE.md
├─ GIT_WORKFLOW.md
├─ docs/
│  ├─ requirements/
│  ├─ plans/
│  └─ diagrams/
├─ src/
│  ├─ VfdControl.App/                 # WPF 启动项目与窗口资源
│  ├─ VfdControl.Presentation/        # View、ViewModel、UI 状态
│  ├─ VfdControl.Application/         # 用例服务、流程编排、接口定义
│  ├─ VfdControl.Domain/              # 领域模型、值对象、领域规则
│  ├─ VfdControl.Infrastructure/      # SQL、串口、模拟器、外部适配实现
│  └─ VfdControl.Contracts/           # DTO、跨层契约、枚举
└─ tests/
   ├─ VfdControl.Domain.Tests/
   ├─ VfdControl.Application.Tests/
   ├─ VfdControl.Infrastructure.Tests/
   └─ VfdControl.Presentation.Tests/
```

说明：

- `VfdControl.App` 只负责启动、依赖注入、主题、主窗口。
- `Presentation` 负责 WPF 页面和 ViewModel，不写串口和数据库细节。
- `Application` 是业务用例层，承载技工流程、工程配置流程、调度执行流程。
- `Domain` 是核心业务模型，不能依赖 WPF、SQL Server、串口或文件系统。
- `Infrastructure` 实现数据库、串口、模拟设备、外部系统适配。
- `Contracts` 放跨层共享的 DTO 和轻量枚举，避免 UI 直接依赖数据库实体。

## 3. 依赖方向

依赖方向必须保持单向：

```text
App
 ├─ Presentation
 ├─ Application
 └─ Infrastructure

Presentation -> Application -> Domain
Application  -> Contracts
Infrastructure -> Application interfaces
Infrastructure -> Domain
Infrastructure -> Contracts
Domain -> no project dependency
```

原则：

- UI 只能调用 Application 提供的用例服务。
- Application 定义接口，Infrastructure 提供实现。
- Domain 不知道数据库、串口、WPF、日志框架。
- Infrastructure 可以依赖第三方库，例如 Dapper、Microsoft.Data.SqlClient、System.IO.Ports。
- ViewModel 不直接 new 仓储、不直接访问串口、不直接写 SQL。

## 4. 模块划分

### 4.1 OperatorConsole

职责：

- 扫描员工码。
- 选择测试方案。
- 选择参与槽位。
- 按槽位顺序录入变频器条码。
- 开始、暂停、继续、停止会话。
- 显示槽位卡片、表格详情、小结论和总结论。
- 处理单槽位异常和人工确认。

主要 Application 服务：

- `OperatorSessionService`
- `SlotSelectionService`
- `ProductionRunService`
- `RunStatusQueryService`

### 4.2 Engineering

职责：

- 维护测试方案。
- 维护方案版本。
- 配置步骤、读写指令、判定规则和失败策略。
- 配置变频器数据与仪表数据的对照比对。
- 执行模拟验证。
- 提供工程调试入口。

主要 Application 服务：

- `ProcessPlanService`
- `PlanVersionService`
- `WorkflowDefinitionService`
- `SimulationValidationService`
- `EngineeringDiagnosticsService`

### 4.3 Administration

职责：

- 维护工程师和管理员账号。
- 维护技工员工码资料。
- 维护工位和槽位。
- 配置槽位串口。
- 配置槽位绑定的仪表和点位。
- 配置扫码规则。
- 配置外部系统适配参数。

主要 Application 服务：

- `UserAdministrationService`
- `OperatorAdministrationService`
- `StationConfigurationService`
- `BarcodeRuleService`
- `IntegrationConfigurationService`

### 4.4 WorkflowEngine

职责：

- 读取方案版本定义。
- 按步骤顺序执行流程。
- 执行启动、停止、读取、写入、延时、确认和判定。
- 生成小结论。
- 根据失败策略决定继续、警告、失败、停止槽位或暂停全部。
- 保存步骤执行上下文。

核心接口：

```csharp
public interface IWorkflowEngine
{
    Task<DeviceRunResult> ExecuteAsync(DeviceRunContext context, CancellationToken cancellationToken);
}
```

### 4.5 SlotScheduler

职责：

- 管理一个工位会话中的多个槽位。
- 为每个槽位启动独立执行任务。
- 处理暂停、继续、停止。
- 传播系统级异常。
- 保证单槽位内部串行访问通信通道。
- 允许不同槽位之间并行执行。

核心接口：

```csharp
public interface ISlotScheduler
{
    Task<StationSessionResult> RunAsync(StationSessionContext context, CancellationToken cancellationToken);
    Task PauseAsync(Guid sessionId);
    Task ResumeAsync(Guid sessionId);
    Task StopSlotAsync(Guid sessionId, Guid slotRunId);
    Task StopSessionAsync(Guid sessionId);
}
```

### 4.6 Communication

职责：

- 提供设备读写抽象。
- 支持模拟设备。
- 后续支持真实 Modbus RTU 串口。
- 记录请求、响应、耗时和异常。
- 支持键盘口扫码枪和串口扫码枪。

核心接口：

```csharp
public interface IDeviceCommandClient
{
    Task<CommandResult<MeasurementValue>> ReadMeasurementAsync(DeviceAddress address, ReadCommand command, CancellationToken ct);
    Task<CommandResult<string>> ReadStringAsync(DeviceAddress address, ReadStringCommand command, CancellationToken ct);
    Task<CommandResult> WriteAsync(DeviceAddress address, WriteCommand command, CancellationToken ct);
}

public interface IBarcodeInputService
{
    event EventHandler<BarcodeScannedEventArgs> BarcodeScanned;
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync();
}
```

### 4.7 Traceability

职责：

- 保存生产会话。
- 保存槽位执行记录。
- 保存步骤记录。
- 保存测量结果、比对结果、小结论、总结论。
- 保存请求/响应报文。
- 提供历史查询。

核心接口：

```csharp
public interface ITraceRepository
{
    Task SaveSessionStartedAsync(StationSessionSnapshot session, CancellationToken ct);
    Task SaveDeviceRunAsync(DeviceRunSnapshot run, CancellationToken ct);
    Task SaveStepRunAsync(StepRunSnapshot step, CancellationToken ct);
    Task SaveCommandTraceAsync(CommandTraceSnapshot trace, CancellationToken ct);
    Task<IReadOnlyList<DeviceRunSummary>> QueryDeviceRunsAsync(DeviceRunQuery query, CancellationToken ct);
}
```

### 4.8 ExternalIntegration

职责：

- 定义与外部生产系统的数据边界。
- 第一阶段使用模拟适配器。
- 后续根据真实数据库表结构实现 SQL 适配器。

核心接口：

```csharp
public interface IProductionDataAdapter
{
    Task<OperatorInfo?> GetOperatorAsync(string employeeCode, CancellationToken ct);
    Task<DeviceBarcodeInfo?> GetDeviceBarcodeAsync(string barcode, CancellationToken ct);
    Task PublishSessionResultAsync(SessionResultMessage message, CancellationToken ct);
    Task PublishDeviceRunResultAsync(DeviceRunResultMessage message, CancellationToken ct);
}
```

## 5. 领域模型

核心领域对象：

- `Station`
- `StationSlot`
- `SlotCommunicationConfig`
- `SlotInstrument`
- `InstrumentPoint`
- `ProcessPlan`
- `ProcessPlanVersion`
- `ProcessStep`
- `StepCommand`
- `StepRule`
- `StepFailurePolicy`
- `StationSession`
- `DeviceRun`
- `StepRun`
- `MeasurementResult`
- `ComparisonResult`
- `CommandTrace`

核心值对象：

- `Barcode`
- `EmployeeCode`
- `SlotNumber`
- `SerialPortName`
- `ModbusSlaveAddress`
- `RegisterAddress`
- `MeasurementValue`
- `Tolerance`
- `PlanVersionNumber`

核心枚举：

- `SessionStatus`
- `SlotRunStatus`
- `StepRunStatus`
- `Conclusion`
- `Severity`
- `FailureAction`
- `MeasurementSource`
- `ToleranceType`
- `DataType`

## 6. 核心数据流

### 6.1 技工执行数据流

```text
员工码扫码
 -> OperatorSessionService 验证员工
 -> 技工选择方案
 -> 技工选择槽位
 -> 按槽位顺序扫描变频器条码
 -> ProductionRunService 创建 StationSession
 -> SlotScheduler 为每个槽位创建 DeviceRun
 -> WorkflowEngine 执行步骤
 -> DeviceCommandClient 读写变频器和仪表
 -> TraceRepository 保存步骤、测量、比对、报文
 -> Presentation 更新卡片和表格状态
```

### 6.2 工程师配置数据流

```text
工程师登录
 -> 创建或复制方案
 -> 编辑步骤、指令、规则、失败策略
 -> 保存为新 ProcessPlanVersion
 -> 模拟验证
 -> 标记为可执行
 -> 技工端方案列表可见
```

### 6.3 槽位通信数据流

```text
WorkflowEngine 请求读取
 -> SlotExecutionContext 获取槽位通信配置
 -> DeviceCommandClient 选择模拟或真实通信实现
 -> 同槽位通道加锁串行访问
 -> 读取变频器或仪表
 -> 转换为 MeasurementValue 或字符串
 -> 生成 CommandTrace
 -> 返回给 WorkflowEngine 判定
```

## 7. 并发模型

第一阶段采用多槽位并行、单槽位串行。

规则：

- 一个工位会话可包含多个选中槽位。
- 每个槽位一个独立执行任务。
- 每个槽位有自己的 `SemaphoreSlim` 或等价串行队列，保证该槽位内 Modbus 请求不并发。
- 不同槽位使用不同串口，可并行访问。
- 单槽位设备失败只影响该槽位。
- 系统级异常或配置要求 `PauseAllSlots` 时，调度器暂停全部槽位。

## 8. 数据库存储策略

数据库采用中央 SQL Server。

结构化字段用于常用查询：

- 员工码。
- 工位。
- 槽位。
- 串口配置摘要。
- 变频器条码。
- 方案和版本。
- 开始时间。
- 结束时间。
- 总结论。
- 步骤编号。
- 小结论。
- 变频器读数。
- 仪表读数。
- 差值。
- 偏差百分比。
- 异常类型。

JSON 明细用于追溯扩展：

- 原始请求/响应。
- 多寄存器原始值。
- 换算参数。
- 判定规则快照。
- 步骤配置快照。
- 仪表配置快照。
- 重试过程。
- 诊断上下文。

建议所有执行记录都保存配置快照，避免工程师修改方案后影响历史解释。

## 9. 配置策略

配置分三类：

- 系统配置：数据库连接、模拟/真实模式、日志级别。
- 工位配置：工位、槽位、串口参数、仪表清单。
- 业务配置：方案、版本、步骤、规则、失败策略。

敏感配置要求：

- 真实数据库密码不得写入公开仓库。
- 本地开发可使用 `appsettings.Development.json` 或用户机密。
- 仓库中只保留 `appsettings.example.json`。

## 10. UI 架构

WPF 采用 MVVM。

建议 ViewModel 只做：

- 页面状态。
- 命令转发。
- 输入校验。
- 调用 Application 服务。
- 将服务结果转换为 UI 状态。

ViewModel 不做：

- 串口通信。
- SQL 查询。
- 流程引擎规则判断。
- 长事务调度。

主要 UI 区域：

- 技工操作台。
- 工程师维护区。
- 管理员配置区。
- 历史追溯查询区。

## 11. 错误处理策略

错误分层：

- 领域错误：规则不满足、数据无效。
- 应用错误：会话状态不允许、方案版本不可执行。
- 通信错误：超时、CRC 错误、设备无响应。
- 数据错误：数据库连接失败、保存失败。
- 系统错误：未处理异常。

处理原则：

- 可预期业务失败返回结果对象，不用异常驱动正常流程。
- 不可恢复系统异常记录日志并进入安全状态。
- 单槽位错误优先隔离到单槽位。
- 停止指令作为收尾流程尽量执行，并记录是否成功。

## 12. 测试策略

优先测试 Application 和 Domain。

单元测试重点：

- `Tolerance` 绝对误差和百分比误差。
- 数值上下限判定。
- 变频器读数与仪表读数比对。
- 字符串规则判定。
- 失败策略选择。
- 总结论汇总。

应用层测试重点：

- 技工会话创建。
- 槽位选择和条码绑定。
- 多槽位并行执行。
- 单槽位失败不影响其他槽位。
- `PauseAllSlots` 策略暂停全部槽位。
- 停止指令收尾执行。

基础设施测试重点：

- 模拟设备读写。
- 模拟仪表读数。
- 请求/响应追踪保存。
- 仓储读写。

UI 测试重点：

- ViewModel 状态转换。
- 命令可用性。
- 卡片颜色和结论映射。

## 13. 第一阶段实现顺序建议

1. 创建解决方案和项目结构。
2. 建立 Domain 模型和值对象。
3. 建立 Application 接口和结果对象。
4. 实现模拟通信。
5. 实现方案定义和简单内存方案仓储。
6. 实现流程引擎。
7. 实现槽位调度器。
8. 实现追溯仓储。
9. 实现技工端操作台。
10. 实现工程师方案配置基础页面。
11. 实现管理员工位和槽位配置。
12. 接入 SQL Server 持久化。
13. 完成第一阶段演示流程。

正式实施前，应基于本架构文档再写详细实施计划。

