using FluentAssertions;
using VfdControl.Presentation.Shell;

namespace VfdControl.Presentation.Tests.Shell;

public class MainShellViewModelTests
{
    [Fact]
    public void Starts_on_operator_console_with_module_prompt()
    {
        var viewModel = new MainShellViewModel();

        viewModel.CurrentViewKey.Should().Be("OperatorConsole");
        viewModel.CurrentViewTitle.Should().Be("操作员控制台");
        viewModel.CurrentModulePrompt.Should().Be("主控制台常驻，辅助模块弹窗打开");
        viewModel.AppModeDisplay.Should().Be("SIM 模拟");
        viewModel.ReadinessDisplay.Should().Be("就绪");
        viewModel.ModuleTaskTitle.Should().Be("操作员控制台");
        viewModel.ModuleTaskHint.Should().Be("扫描员工码，开始生产会话");
        viewModel.ModuleStatusSummary.Should().Be("模拟模式 · 设备就绪 · 工位待命");
        viewModel.NavigationItems.Should().Contain(item => item.ViewKey == "OperatorConsole" && item.IsSelected);
        viewModel.NavigationItems.Single(item => item.ViewKey == "Engineering").IsSelected.Should().BeFalse();
    }

    [Fact]
    public void Module_commands_request_dialogs_without_leaving_operator_console()
    {
        var viewModel = new MainShellViewModel();

        viewModel.NavigateCommand.Execute("Traceability");

        viewModel.CurrentViewKey.Should().Be("OperatorConsole");
        viewModel.CurrentViewTitle.Should().Be("操作员控制台");
        viewModel.RequestedDialogKey.Should().Be("Traceability");
        viewModel.CurrentModulePrompt.Should().Be("追溯查询已打开，主控制台保持运行");
        viewModel.ModuleTaskTitle.Should().Be("追溯查询");
        viewModel.ModuleTaskHint.Should().Be("查询历史运行、步骤、测量、比较和命令记录");
        viewModel.ModuleStatusSummary.Should().Be("辅助窗口 · 主控制台保持运行");
        viewModel.NavigationItems.Should().Contain(item => item.ViewKey == "OperatorConsole");
        viewModel.NavigationItems.Single(item => item.ViewKey == "Traceability").IsSelected.Should().BeFalse();
    }

    [Fact]
    public void Operator_console_command_clears_dialog_request()
    {
        var viewModel = new MainShellViewModel();

        viewModel.NavigateCommand.Execute("Engineering");
        viewModel.NavigateCommand.Execute("OperatorConsole");

        viewModel.RequestedDialogKey.Should().BeNull();
        viewModel.CurrentModulePrompt.Should().Be("主控制台常驻，辅助模块弹窗打开");
        viewModel.ModuleTaskTitle.Should().Be("操作员控制台");
        viewModel.ModuleTaskHint.Should().Be("扫描员工码，开始生产会话");
    }
}
