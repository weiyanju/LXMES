# UI Layout Optimization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rework the WPF UI into a top-navigation industrial workstation layout with readable Chinese copy, consistent module templates, and an operator console that emphasizes the current next action.

**Architecture:** Keep the existing WPF + MVVM layering. Presentation ViewModels expose small layout/state properties for XAML, while App XAML owns layout, styles, and visible copy. No domain, workflow, persistence, simulation, or serial behavior changes are part of this plan.

**Tech Stack:** .NET 10, WPF XAML, CommunityToolkit.Mvvm, xUnit, FluentAssertions.

---

## File Structure

- `src/VfdControl.Presentation/Shell/MainShellViewModel.cs`: add module subtitles/prompts for the two-level top shell.
- `src/VfdControl.Presentation/Shell/NavigationItem.cs`: keep module identity and selected state, add optional short label only if needed.
- `src/VfdControl.Presentation/Operator/OperatorConsoleViewModel.cs`: add flow-step display properties and boolean state helpers for current-action panels.
- `src/VfdControl.Presentation/Operator/SlotCardViewModel.cs`: ensure status text and default barcode text are Chinese and state-complete.
- `src/VfdControl.App/Views/MainWindow.xaml`: replace left sidebar with global top navigation and module header, centralize common WPF styles.
- `src/VfdControl.App/Views/Operator/OperatorConsoleView.xaml`: implement flow progress, slot board, current action panel, and running detail layout.
- `src/VfdControl.App/Views/Operator/SlotCardView.xaml`: repair Chinese copy and make slot status readable.
- `src/VfdControl.App/Views/Engineering/*.xaml`: align engineering layout and repair Chinese copy.
- `src/VfdControl.App/Views/Admin/*.xaml`: align administration layout and repair Chinese copy.
- `src/VfdControl.App/Views/Traceability/*.xaml`: align traceability layout and repair Chinese copy.
- `tests/VfdControl.Presentation.Tests/Shell/MainShellViewModelTests.cs`: verify module title/prompt changes with navigation.
- `tests/VfdControl.Presentation.Tests/Operator/OperatorConsoleLayoutStateTests.cs`: verify current-action visibility and flow step labels.

---

### Task 1: Shell Navigation and Module Header

**Files:**
- Modify: `src/VfdControl.Presentation/Shell/MainShellViewModel.cs`
- Test: `tests/VfdControl.Presentation.Tests/Shell/MainShellViewModelTests.cs`

- [ ] **Step 1: Write failing shell ViewModel tests**

Create `tests/VfdControl.Presentation.Tests/Shell/MainShellViewModelTests.cs`:

```csharp
using FluentAssertions;
using VfdControl.Presentation.Shell;

namespace VfdControl.Presentation.Tests.Shell;

public class MainShellViewModelTests
{
    [Fact]
    public void Starts_on_operator_console_with_module_prompt()
    {
        var viewModel = new MainShellViewModel();

        viewModel.CurrentViewTitle.Should().Be("操作员控制台");
        viewModel.CurrentModulePrompt.Should().Be("扫描员工码开始生产会话");
        viewModel.AppModeDisplay.Should().Be("模拟模式");
        viewModel.ReadinessDisplay.Should().Be("就绪");
    }

    [Fact]
    public void Navigation_updates_title_prompt_and_selected_item()
    {
        var viewModel = new MainShellViewModel();

        viewModel.NavigateCommand.Execute("Traceability");

        viewModel.CurrentViewTitle.Should().Be("追溯查询");
        viewModel.CurrentModulePrompt.Should().Be("查询历史运行、步骤、测量和命令记录");
        viewModel.NavigationItems.Single(item => item.ViewKey == "Traceability").IsSelected.Should().BeTrue();
        viewModel.NavigationItems.Single(item => item.ViewKey == "OperatorConsole").IsSelected.Should().BeFalse();
    }
}
```

- [ ] **Step 2: Run shell tests and verify red**

Run:

```powershell
dotnet test tests\VfdControl.Presentation.Tests\VfdControl.Presentation.Tests.csproj --filter MainShellViewModelTests
```

Expected: compile fails because `CurrentModulePrompt`, `AppModeDisplay`, or `ReadinessDisplay` does not exist.

- [ ] **Step 3: Implement shell properties**

Update `src/VfdControl.Presentation/Shell/MainShellViewModel.cs` so it keeps a map:

```csharp
private static readonly IReadOnlyDictionary<string, string> ModulePrompts = new Dictionary<string, string>
{
    ["OperatorConsole"] = "扫描员工码开始生产会话",
    ["Engineering"] = "维护工艺方案和步骤版本",
    ["Administration"] = "配置工位、槽位、串口和条码规则",
    ["Traceability"] = "查询历史运行、步骤、测量和命令记录"
};

[ObservableProperty]
private string _currentModulePrompt = ModulePrompts["OperatorConsole"];

public string AppModeDisplay => "模拟模式";

public string ReadinessDisplay => "就绪";
```

Inside `SelectNavigationItem`, set:

```csharp
CurrentModulePrompt = ModulePrompts.TryGetValue(viewKey, out var prompt)
    ? prompt
    : "请选择工作模块";
```

- [ ] **Step 4: Verify shell tests pass**

Run:

```powershell
dotnet test tests\VfdControl.Presentation.Tests\VfdControl.Presentation.Tests.csproj --filter MainShellViewModelTests
```

Expected: `MainShellViewModelTests` pass.

- [ ] **Step 5: Commit shell ViewModel**

```powershell
git add src\VfdControl.Presentation\Shell tests\VfdControl.Presentation.Tests\Shell
git commit -m "feat: add shell module header state"
```

---

### Task 2: Operator Current-Action State

**Files:**
- Modify: `src/VfdControl.Presentation/Operator/OperatorConsoleViewModel.cs`
- Modify: `src/VfdControl.Presentation/Operator/SlotCardViewModel.cs`
- Test: `tests/VfdControl.Presentation.Tests/Operator/OperatorConsoleLayoutStateTests.cs`

- [ ] **Step 1: Write failing operator layout-state tests**

Create `tests/VfdControl.Presentation.Tests/Operator/OperatorConsoleLayoutStateTests.cs`:

```csharp
using FluentAssertions;
using VfdControl.Application.Abstractions;
using VfdControl.Application.Execution;
using VfdControl.Application.Operator;
using VfdControl.Domain.Enums;
using VfdControl.Domain.Plans;
using VfdControl.Domain.Stations;
using VfdControl.Domain.ValueObjects;
using VfdControl.Presentation.Operator;

namespace VfdControl.Presentation.Tests.Operator;

public class OperatorConsoleLayoutStateTests
{
    [Fact]
    public async Task Current_action_flags_follow_operator_flow()
    {
        var viewModel = CreateViewModel();
        await viewModel.InitializeAsync();

        viewModel.FlowStepDisplay.Should().Be("1 / 7");
        viewModel.IsEmployeeActionVisible.Should().BeTrue();
        viewModel.EmployeeCodeInput = "EMP0001";
        await viewModel.ScanEmployeeCommand.ExecuteAsync(null);

        viewModel.FlowStepDisplay.Should().Be("2 / 7");
        viewModel.IsPlanActionVisible.Should().BeTrue();
        viewModel.SelectPlanCommand.Execute(viewModel.AvailablePlans[0]);

        viewModel.FlowStepDisplay.Should().Be("3 / 7");
        viewModel.IsSlotActionVisible.Should().BeTrue();
    }

    [Fact]
    public void Slot_card_defaults_use_readable_chinese_copy()
    {
        var slot = SlotCardViewModel.CreateDemo(1);

        slot.StatusText.Should().Be("待选择");
        slot.BarcodeDisplay.Should().Be("未绑定条码");
    }

    private static OperatorConsoleViewModel CreateViewModel()
    {
        var station = new Station(Guid.NewGuid(), "演示工位");
        station.AddSlot(new StationSlot(
            Guid.NewGuid(),
            new SlotNumber(1),
            new SlotCommunicationConfig(new SerialPortName("COM1"), new ModbusAddress(1), 9600)));
        var plan = new ProcessPlan(Guid.NewGuid(), "VFD 生产测试演示方案");
        plan.AddVersion(new ProcessPlanVersion(Guid.NewGuid(), 1, isExecutable: true));

        return new OperatorConsoleViewModel(
            new OperatorSessionService(),
            new PlanSelectionService(new FakeProcessPlanRepository([plan])),
            new SlotSelectionService(),
            new ProductionRunService(new NoOpSlotScheduler()),
            new FakeStationRepository([station]));
    }

    private sealed class FakeStationRepository : IStationRepository
    {
        private readonly IReadOnlyList<Station> _stations;
        public FakeStationRepository(IReadOnlyList<Station> stations) => _stations = stations;
        public Task<IReadOnlyList<Station>> ListAsync(CancellationToken ct) => Task.FromResult(_stations);
        public Task<Station?> GetAsync(Guid stationId, CancellationToken ct) => Task.FromResult(_stations.SingleOrDefault(station => station.Id == stationId));
        public Task SaveAsync(Station station, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeProcessPlanRepository : IProcessPlanRepository
    {
        private readonly IReadOnlyList<ProcessPlan> _plans;
        public FakeProcessPlanRepository(IReadOnlyList<ProcessPlan> plans) => _plans = plans;
        public Task<IReadOnlyList<ProcessPlan>> ListAsync(CancellationToken ct) => Task.FromResult(_plans);
        public Task<IReadOnlyList<ProcessPlanVersion>> ListExecutableVersionsAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<ProcessPlanVersion>>(_plans.SelectMany(plan => plan.Versions).Where(version => version.IsExecutable).ToList());
        public Task<ProcessPlan?> GetAsync(Guid planId, CancellationToken ct) => Task.FromResult(_plans.SingleOrDefault(plan => plan.Id == planId));
        public Task SaveAsync(ProcessPlan plan, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class NoOpSlotScheduler : ISlotScheduler
    {
        public Task<StationSessionResult> RunAsync(StationSessionContext context, CancellationToken cancellationToken) =>
            Task.FromResult(new StationSessionResult(context.SessionId, Conclusion.Pass, []));
        public Task PauseAsync(Guid sessionId) => Task.CompletedTask;
        public Task ResumeAsync(Guid sessionId) => Task.CompletedTask;
        public Task StopSlotAsync(Guid sessionId, Guid slotRunId) => Task.CompletedTask;
        public Task StopSessionAsync(Guid sessionId) => Task.CompletedTask;
    }
}
```

- [ ] **Step 2: Run operator layout tests and verify red**

Run:

```powershell
dotnet test tests\VfdControl.Presentation.Tests\VfdControl.Presentation.Tests.csproj --filter OperatorConsoleLayoutStateTests
```

Expected: compile fails because `FlowStepDisplay`, `IsEmployeeActionVisible`, `IsPlanActionVisible`, `IsSlotActionVisible`, or `BarcodeDisplay` does not exist.

- [ ] **Step 3: Add flow display properties**

In `OperatorConsoleViewModel`, add computed properties:

```csharp
public string FlowStepDisplay => State switch
{
    OperatorConsoleState.WaitingEmployeeCode => "1 / 7",
    OperatorConsoleState.SelectingPlan => "2 / 7",
    OperatorConsoleState.SelectingSlots => "3 / 7",
    OperatorConsoleState.ScanningBarcodes => "4 / 7",
    OperatorConsoleState.ConfirmingStart => "5 / 7",
    OperatorConsoleState.Running => "6 / 7",
    OperatorConsoleState.Completed => "7 / 7",
    _ => "1 / 7"
};

public string FlowStepName => State switch
{
    OperatorConsoleState.WaitingEmployeeCode => "扫描员工码",
    OperatorConsoleState.SelectingPlan => "选择方案",
    OperatorConsoleState.SelectingSlots => "选择槽位",
    OperatorConsoleState.ScanningBarcodes => "绑定条码",
    OperatorConsoleState.ConfirmingStart => "确认启动",
    OperatorConsoleState.Running => "运行中",
    OperatorConsoleState.Completed => "已完成",
    _ => "准备"
};

public bool IsEmployeeActionVisible => State == OperatorConsoleState.WaitingEmployeeCode;
public bool IsPlanActionVisible => State == OperatorConsoleState.SelectingPlan;
public bool IsSlotActionVisible => State == OperatorConsoleState.SelectingSlots;
public bool IsBarcodeActionVisible => State == OperatorConsoleState.ScanningBarcodes;
public bool IsStartActionVisible => State == OperatorConsoleState.ConfirmingStart;
public bool IsRunningActionVisible => State == OperatorConsoleState.Running;
public bool IsCompletedActionVisible => State == OperatorConsoleState.Completed;
```

Add a partial state-change hook:

```csharp
partial void OnStateChanged(OperatorConsoleState value)
{
    OnPropertyChanged(nameof(FlowStepDisplay));
    OnPropertyChanged(nameof(FlowStepName));
    OnPropertyChanged(nameof(IsEmployeeActionVisible));
    OnPropertyChanged(nameof(IsPlanActionVisible));
    OnPropertyChanged(nameof(IsSlotActionVisible));
    OnPropertyChanged(nameof(IsBarcodeActionVisible));
    OnPropertyChanged(nameof(IsStartActionVisible));
    OnPropertyChanged(nameof(IsRunningActionVisible));
    OnPropertyChanged(nameof(IsCompletedActionVisible));
}
```

- [ ] **Step 4: Add slot card display property**

In `SlotCardViewModel`, add:

```csharp
public string BarcodeDisplay => string.IsNullOrWhiteSpace(Barcode) ? "未绑定条码" : Barcode;
```

Call `OnPropertyChanged(nameof(BarcodeDisplay));` wherever `Barcode` changes. If `Barcode` is an `[ObservableProperty]`, use:

```csharp
partial void OnBarcodeChanged(string value)
{
    OnPropertyChanged(nameof(BarcodeDisplay));
}
```

- [ ] **Step 5: Verify operator layout tests pass**

Run:

```powershell
dotnet test tests\VfdControl.Presentation.Tests\VfdControl.Presentation.Tests.csproj --filter OperatorConsoleLayoutStateTests
```

Expected: tests pass.

- [ ] **Step 6: Commit operator state**

```powershell
git add src\VfdControl.Presentation\Operator tests\VfdControl.Presentation.Tests\Operator
git commit -m "feat: add operator layout state"
```

---

### Task 3: Top Navigation Shell XAML

**Files:**
- Modify: `src/VfdControl.App/Views/MainWindow.xaml`

- [ ] **Step 1: Replace left-sidebar shell with top shell**

Rewrite `MainWindow.xaml` root layout as:

```xml
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition Height="58" />
        <RowDefinition Height="74" />
        <RowDefinition Height="*" />
    </Grid.RowDefinitions>

    <Border Grid.Row="0" Background="#E7EEEB" BorderBrush="#CBD8D3" BorderThickness="0,0,0,1">
        <Grid Margin="24,0">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="Auto" />
                <ColumnDefinition Width="24" />
                <ColumnDefinition Width="*" />
                <ColumnDefinition Width="Auto" />
            </Grid.ColumnDefinitions>
            <TextBlock Text="VFD 生产控制" VerticalAlignment="Center" FontSize="20" FontWeight="SemiBold" Foreground="#172522" />
            <ItemsControl Grid.Column="2" ItemsSource="{Binding NavigationItems}" VerticalAlignment="Center">
                <ItemsControl.ItemsPanel>
                    <ItemsPanelTemplate>
                        <StackPanel Orientation="Horizontal" />
                    </ItemsPanelTemplate>
                </ItemsControl.ItemsPanel>
                <ItemsControl.ItemTemplate>
                    <DataTemplate>
                        <Button Content="{Binding Title}"
                                Command="{Binding DataContext.NavigateCommand, RelativeSource={RelativeSource AncestorType=Window}}"
                                CommandParameter="{Binding ViewKey}"
                                Style="{StaticResource TopNavigationButtonStyle}" />
                    </DataTemplate>
                </ItemsControl.ItemTemplate>
            </ItemsControl>
            <StackPanel Grid.Column="3" Orientation="Horizontal" VerticalAlignment="Center">
                <Border Style="{StaticResource StatusChipStyle}">
                    <TextBlock Text="{Binding AppModeDisplay}" />
                </Border>
                <Border Margin="8,0,0,0" Style="{StaticResource ReadyChipStyle}">
                    <TextBlock Text="{Binding ReadinessDisplay}" />
                </Border>
            </StackPanel>
        </Grid>
    </Border>

    <Border Grid.Row="1" Background="#FAFCFB" BorderBrush="#DDE6E2" BorderThickness="0,0,0,1">
        <StackPanel Margin="28,0" VerticalAlignment="Center">
            <TextBlock Text="{Binding CurrentViewTitle}" FontSize="20" FontWeight="SemiBold" Foreground="#172522" />
            <TextBlock Text="{Binding CurrentModulePrompt}" Margin="0,5,0,0" FontSize="13" Foreground="#60716C" />
        </StackPanel>
    </Border>

    <ContentControl x:Name="WorkspaceHost" Grid.Row="2" Margin="24" />
</Grid>
```

- [ ] **Step 2: Add common styles in `Window.Resources`**

Add or update these styles:

```xml
<Style x:Key="TopNavigationButtonStyle" TargetType="Button">
    <Setter Property="Margin" Value="0,0,8,0" />
    <Setter Property="Padding" Value="14,8" />
    <Setter Property="Background" Value="#EEF3F1" />
    <Setter Property="BorderBrush" Value="#D7E1DD" />
    <Setter Property="Foreground" Value="#243431" />
    <Setter Property="FontWeight" Value="SemiBold" />
    <Setter Property="Cursor" Value="Hand" />
    <Style.Triggers>
        <DataTrigger Binding="{Binding IsSelected}" Value="True">
            <Setter Property="Background" Value="#D9ECE5" />
            <Setter Property="BorderBrush" Value="#58A884" />
            <Setter Property="Foreground" Value="#163B30" />
        </DataTrigger>
    </Style.Triggers>
</Style>

<Style x:Key="StatusChipStyle" TargetType="Border">
    <Setter Property="Padding" Value="10,5" />
    <Setter Property="Background" Value="#EEF3F1" />
    <Setter Property="BorderBrush" Value="#DDE6E2" />
    <Setter Property="BorderThickness" Value="1" />
</Style>

<Style x:Key="ReadyChipStyle" TargetType="Border" BasedOn="{StaticResource StatusChipStyle}">
    <Setter Property="Background" Value="#E7F5EC" />
    <Setter Property="BorderBrush" Value="#87C59E" />
</Style>
```

- [ ] **Step 3: Build to validate XAML**

Run:

```powershell
dotnet build VfdProductionControl.sln
```

Expected: build succeeds.

- [ ] **Step 4: Commit shell XAML**

```powershell
git add src\VfdControl.App\Views\MainWindow.xaml
git commit -m "feat: move shell navigation to top"
```

---

### Task 4: Operator Console Layout XAML

**Files:**
- Modify: `src/VfdControl.App/Views/Operator/OperatorConsoleView.xaml`
- Modify: `src/VfdControl.App/Views/Operator/SlotCardView.xaml`

- [ ] **Step 1: Add boolean-to-visibility resource**

In `OperatorConsoleView.xaml`, add:

```xml
<BooleanToVisibilityConverter x:Key="BoolToVisibilityConverter" />
```

- [ ] **Step 2: Replace operator layout with progress, board, action panel**

Use this structure:

```xml
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto" />
        <RowDefinition Height="*" />
        <RowDefinition Height="170" />
    </Grid.RowDefinitions>

    <Border Padding="16" Background="#FAFCFB" BorderBrush="#DDE6E2" BorderThickness="1">
        <Grid>
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="180" />
                <ColumnDefinition Width="*" />
                <ColumnDefinition Width="220" />
                <ColumnDefinition Width="220" />
            </Grid.ColumnDefinitions>
            <TextBlock Text="{Binding FlowStepDisplay}" FontSize="20" FontWeight="SemiBold" Foreground="#172522" />
            <StackPanel Grid.Column="1">
                <TextBlock Text="{Binding FlowStepName}" FontSize="16" FontWeight="SemiBold" Foreground="#172522" />
                <TextBlock Text="{Binding CurrentPrompt}" Margin="0,4,0,0" Foreground="#60716C" TextWrapping="Wrap" />
            </StackPanel>
            <StackPanel Grid.Column="2">
                <TextBlock Text="员工 / 方案" FontSize="12" FontWeight="SemiBold" Foreground="#60716C" />
                <TextBlock Text="{Binding EmployeeDisplay}" Margin="0,4,0,0" Foreground="#172522" />
                <TextBlock Text="{Binding SelectedPlanDisplay}" Margin="0,4,0,0" Foreground="#334541" TextWrapping="Wrap" />
            </StackPanel>
            <StackPanel Grid.Column="3">
                <TextBlock Text="工位 / 状态" FontSize="12" FontWeight="SemiBold" Foreground="#60716C" />
                <TextBlock Text="{Binding StationDisplay}" Margin="0,4,0,0" Foreground="#172522" />
                <TextBlock Text="{Binding StatusMessage}" Margin="0,4,0,0" Foreground="#25613F" TextWrapping="Wrap" />
            </StackPanel>
        </Grid>
    </Border>

    <Grid Grid.Row="1" Margin="0,16,0,16">
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="*" />
            <ColumnDefinition Width="20" />
            <ColumnDefinition Width="340" />
        </Grid.ColumnDefinitions>
        <!-- left slot board, right current action panel -->
    </Grid>

    <Border Grid.Row="2" Padding="16" Background="#FAFCFB" BorderBrush="#DDE6E2" BorderThickness="1">
        <!-- running detail DataGrid -->
    </Border>
</Grid>
```

In the right action panel, add separate `StackPanel` sections with visibility bound to:

```xml
Visibility="{Binding IsEmployeeActionVisible, Converter={StaticResource BoolToVisibilityConverter}}"
Visibility="{Binding IsPlanActionVisible, Converter={StaticResource BoolToVisibilityConverter}}"
Visibility="{Binding IsSlotActionVisible, Converter={StaticResource BoolToVisibilityConverter}}"
Visibility="{Binding IsBarcodeActionVisible, Converter={StaticResource BoolToVisibilityConverter}}"
Visibility="{Binding IsStartActionVisible, Converter={StaticResource BoolToVisibilityConverter}}"
Visibility="{Binding IsRunningActionVisible, Converter={StaticResource BoolToVisibilityConverter}}"
Visibility="{Binding IsCompletedActionVisible, Converter={StaticResource BoolToVisibilityConverter}}"
```

Use readable Chinese labels:

- `扫描员工码`
- `选择测试方案`
- `选择槽位`
- `绑定 VFD 条码`
- `确认开始`
- `测试运行中`
- `测试完成`

- [ ] **Step 3: Update slot card XAML**

In `SlotCardView.xaml`, replace corrupted copy with:

```xml
<TextBlock Text="{Binding BarcodeDisplay}"
           Margin="0,6,0,0"
           FontSize="12"
           Foreground="#60716C"
           TextWrapping="Wrap" />
<CheckBox Grid.Row="2"
          Content="已选择"
          IsChecked="{Binding IsSelected, Mode=OneWay}"
          IsHitTestVisible="False"
          FontSize="12"
          Foreground="#334541" />
```

- [ ] **Step 4: Build to validate XAML**

Run:

```powershell
dotnet build VfdProductionControl.sln
```

Expected: build succeeds.

- [ ] **Step 5: Commit operator XAML**

```powershell
git add src\VfdControl.App\Views\Operator
git commit -m "feat: redesign operator console layout"
```

---

### Task 5: Engineering, Administration, and Traceability Copy/Layout Cleanup

**Files:**
- Modify: `src/VfdControl.App/Views/Engineering/PlanListView.xaml`
- Modify: `src/VfdControl.App/Views/Engineering/WorkflowEditorView.xaml`
- Modify: `src/VfdControl.App/Views/Admin/StationConfigView.xaml`
- Modify: `src/VfdControl.App/Views/Admin/BarcodeRuleView.xaml`
- Modify: `src/VfdControl.App/Views/Traceability/ExecutionHistoryView.xaml`
- Modify: `src/VfdControl.App/Views/Traceability/DeviceRunTraceView.xaml`

- [ ] **Step 1: Fix engineering visible Chinese copy**

Use these visible labels:

- `测试方案`
- `新方案名称`
- `创建方案`
- `添加步骤`
- `启动 VFD`
- `延时等待`
- `读 VFD 电压`
- `读仪表电压`
- `比对测量`
- `停止 VFD`
- `失败策略`
- `保存为新版本`
- DataGrid headers: `序号`, `步骤名称`, `命令类型`, `目标`, `参数`, `失败策略`

- [ ] **Step 2: Fix administration visible Chinese copy**

Use these visible labels:

- `更新串口`
- `当前槽位 COM`
- `波特率 {0}`
- `仪表与点位`
- `默认条码规则`

- [ ] **Step 3: Fix traceability visible Chinese copy**

Use these visible labels:

- `追溯查询`
- `按时间、条码和结论筛选历史运行记录`
- `开始日期`
- `结束日期`
- `VFD 条码`
- `结论`
- `设备运行记录`
- `查询`
- DataGrid headers: `条码`, `结论`, `开始时间`, `详情`, `员工`, `数量`
- Detail labels: `测量数据`, `比对结果`, `命令追踪`, `请求: {0}`, `响应: {0}`

- [ ] **Step 4: Align module templates**

Keep existing panes but adjust widths:

```csharp
// MainWindow.xaml.cs workspace creation target widths
Engineering: 300 | 18 | * or 300 | 18 | * | 18 | 320 if a right panel is introduced.
Administration: 300 | 18 | *.
Traceability: 430 | 18 | *.
```

Do not introduce new behavior in this task. Keep data bindings unchanged unless a binding references corrupted display text.

- [ ] **Step 5: Build to validate XAML**

Run:

```powershell
dotnet build VfdProductionControl.sln
```

Expected: build succeeds.

- [ ] **Step 6: Commit module cleanup**

```powershell
git add src\VfdControl.App\Views\Engineering src\VfdControl.App\Views\Admin src\VfdControl.App\Views\Traceability
git commit -m "fix: clean up module UI copy and layouts"
```

---

### Task 6: Full Verification and UI Smoke

**Files:**
- No planned code changes.

- [ ] **Step 1: Run full build**

Run:

```powershell
dotnet build VfdProductionControl.sln
```

Expected: 0 errors.

- [ ] **Step 2: Run full tests**

Run:

```powershell
dotnet test VfdProductionControl.sln
```

Expected: all tests pass.

- [ ] **Step 3: Run WPF smoke launch**

Run:

```powershell
dotnet run --project src\VfdControl.App\VfdControl.App.csproj
```

Expected: app launches and host logs `Application started`. Close `VfdControl.App` after confirming launch to avoid locked build outputs.

- [ ] **Step 4: Manual visual checklist**

Confirm:

- Top navigation is visible and selected state changes.
- Module header title and prompt update when switching modules.
- Operator console has progress, slot board, and one current action panel.
- Engineering, administration, and traceability visible Chinese copy is readable.
- Statuses include text labels.
- No large decorative gradients, dark neon styling, or marketing layout patterns are introduced.

- [ ] **Step 5: Commit verification note only if files changed**

If no files changed, do not commit. If a small copy/layout fix was needed during smoke validation:

```powershell
git add src\VfdControl.App src\VfdControl.Presentation tests
git commit -m "fix: polish UI layout verification issues"
```

---

## Self-Review

- Spec coverage: top navigation, module header, operator current-action layout, module templates, component rules, Chinese copy cleanup, status text, build/test/run verification are all mapped to tasks.
- Placeholder scan: no unresolved placeholder markers are intentionally left for implementers.
- Type consistency: new shell properties are introduced before shell XAML consumes them; new operator layout properties are introduced before operator XAML consumes them.
