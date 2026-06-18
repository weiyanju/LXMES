using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace VfdControl.Presentation.Shell;

public sealed partial class MainShellViewModel : ObservableObject
{
    private static readonly IReadOnlyDictionary<string, string> ModulePrompts = new Dictionary<string, string>
    {
        ["OperatorConsole"] = "主控制台常驻，辅助模块弹窗打开",
        ["Engineering"] = "工程维护已打开，主控制台保持运行",
        ["Administration"] = "系统管理已打开，主控制台保持运行",
        ["Traceability"] = "追溯查询已打开，主控制台保持运行"
    };

    private static readonly IReadOnlyDictionary<string, string> ModuleTaskTitles = new Dictionary<string, string>
    {
        ["OperatorConsole"] = "操作员控制台",
        ["Engineering"] = "工程维护",
        ["Administration"] = "系统管理",
        ["Traceability"] = "追溯查询"
    };

    private static readonly IReadOnlyDictionary<string, string> ModuleTaskHints = new Dictionary<string, string>
    {
        ["OperatorConsole"] = "扫描员工码，开始生产会话",
        ["Engineering"] = "维护工艺方案、步骤、规则和失败策略",
        ["Administration"] = "配置工位、槽位、串口、仪表和条码规则",
        ["Traceability"] = "查询历史运行、步骤、测量、比较和命令记录"
    };

    [ObservableProperty]
    private string _currentViewKey = "OperatorConsole";

    [ObservableProperty]
    private string _currentViewTitle = "操作员控制台";

    [ObservableProperty]
    private string _currentModulePrompt = ModulePrompts["OperatorConsole"];

    [ObservableProperty]
    private string _moduleTaskTitle = ModuleTaskTitles["OperatorConsole"];

    [ObservableProperty]
    private string _moduleTaskHint = ModuleTaskHints["OperatorConsole"];

    [ObservableProperty]
    private string _moduleStatusSummary = "模拟模式 · 设备就绪 · 工位待命";

    [ObservableProperty]
    private string? _requestedDialogKey;

    public MainShellViewModel(string? appMode = null)
    {
        AppModeDisplay = IsSerialMode(appMode) ? "RTU \u4E32\u53E3" : "SIM \u6A21\u62DF";
        NavigationItems =
        [
            new NavigationItem("OperatorConsole", "操作员控制台"),
            new NavigationItem("Engineering", "工程维护"),
            new NavigationItem("Administration", "系统管理"),
            new NavigationItem("Traceability", "追溯查询")
        ];

        ClearNavigationSelection();
        SelectNavigationItem("OperatorConsole");
    }

    public ObservableCollection<NavigationItem> NavigationItems { get; }

    public string AppModeDisplay { get; }

    public string ReadinessDisplay => "就绪";

    [RelayCommand]
    private void Navigate(string viewKey)
    {
        if (string.IsNullOrWhiteSpace(viewKey))
        {
            return;
        }

        if (viewKey == "OperatorConsole")
        {
            RequestedDialogKey = null;
            CurrentModulePrompt = ModulePrompts["OperatorConsole"];
            ModuleTaskTitle = ModuleTaskTitles["OperatorConsole"];
            ModuleTaskHint = ModuleTaskHints["OperatorConsole"];
            ModuleStatusSummary = "模拟模式 · 设备就绪 · 工位待命";
            ClearNavigationSelection();
            SelectNavigationItem("OperatorConsole");
            return;
        }

        RequestedDialogKey = viewKey;
        CurrentViewKey = "OperatorConsole";
        CurrentViewTitle = "操作员控制台";
        ClearNavigationSelection();
        SelectNavigationItem("OperatorConsole");
        CurrentModulePrompt = ModulePrompts.TryGetValue(viewKey, out var prompt)
            ? prompt
            : ModulePrompts["OperatorConsole"];
        ModuleTaskTitle = ModuleTaskTitles.TryGetValue(viewKey, out var title)
            ? title
            : ModuleTaskTitles["OperatorConsole"];
        ModuleTaskHint = ModuleTaskHints.TryGetValue(viewKey, out var hint)
            ? hint
            : ModuleTaskHints["OperatorConsole"];
        ModuleStatusSummary = "辅助窗口 · 主控制台保持运行";
    }

    private void ClearNavigationSelection()
    {
        foreach (var item in NavigationItems)
        {
            item.IsSelected = false;
        }
    }

    private void SelectNavigationItem(string viewKey)
    {
        var item = NavigationItems.SingleOrDefault(item => item.ViewKey == viewKey);
        if (item is not null)
        {
            item.IsSelected = true;
        }
    }

    private static bool IsSerialMode(string? appMode)
    {
        return string.Equals(appMode, "Serial", StringComparison.OrdinalIgnoreCase)
            || string.Equals(appMode, "ModbusRtu", StringComparison.OrdinalIgnoreCase)
            || string.Equals(appMode, "Production", StringComparison.OrdinalIgnoreCase);
    }
}
