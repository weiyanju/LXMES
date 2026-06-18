# VfdProductionControl

VfdProductionControl 是一个面向生产现场的 VFD 测试执行与监控平台。Phase 1 重点验证模拟生产闭环：员工码扫描、工艺方案选择、槽位选择、VFD 条码绑定、多槽位模拟执行、测量比对和追溯查询。

## 前置条件

- Windows
- .NET SDK 10.0
- Visual Studio 2026 或可运行 .NET/WPF 项目的等效环境

Phase 1 默认使用内存仓储和模拟设备，不需要真实 SQL Server、真实 COM 串口或外部生产系统。

## 构建

```powershell
dotnet build VfdProductionControl.sln
```

## 测试

```powershell
dotnet test VfdProductionControl.sln
```

## 启动

```powershell
dotnet run --project src\VfdControl.App\VfdControl.App.csproj
```

也可以在 Visual Studio 中打开 `VfdProductionControl.sln`，将 `VfdControl.App` 设置为启动项目后运行。

## Phase 1 演示

- [演示脚本](docs/demo/phase-1-demo-script.md)
- [验收清单](docs/demo/phase-1-acceptance-checklist.md)

建议按演示脚本完成一次端到端操作，再用验收清单逐项确认。
