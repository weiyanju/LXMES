using System.Threading;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VfdControl.Application.Abstractions;
using VfdControl.App;
using VfdControl.App.Views;
using VfdControl.App.Views.Admin;
using VfdControl.App.Views.Engineering;
using VfdControl.App.Views.Operator;
using VfdControl.App.Views.Traceability;
using VfdControl.Infrastructure.Serial;
using VfdControl.Infrastructure.Simulation;
using VfdControl.Presentation.Operator;
using VfdControl.Presentation.Shell;

namespace VfdControl.App.Tests;

public class MainWindowDialogTests
{
    [Fact]
    public Task Preparing_secondary_workspace_keeps_operator_console_hosted()
    {
        return RunStaAsync(async () =>
        {
            using var host = DependencyInjection.CreateHostBuilder([]).Build();
            var window = host.Services.GetRequiredService<MainWindow>();

            window.IsOperatorConsoleHosted.Should().BeTrue();

            var traceabilityWorkspace = await window.PrepareDialogWorkspaceAsync("Traceability");

            traceabilityWorkspace.Should().NotBeNull();
            window.IsOperatorConsoleHosted.Should().BeTrue();
        });
    }

    [Fact]
    public Task Administration_workspace_uses_full_width_station_management()
    {
        return RunStaAsync(async () =>
        {
            using var host = DependencyInjection.CreateHostBuilder([]).Build();
            var window = host.Services.GetRequiredService<MainWindow>();

            var workspace = await window.PrepareDialogWorkspaceAsync("Administration");

            workspace.Should().NotBeNull();
            workspace!.ColumnDefinitions.Should().ContainSingle();
            workspace.Children.OfType<StationConfigView>().Should().ContainSingle();
            workspace.Children.OfType<BarcodeRuleView>().Should().BeEmpty();
        });
    }

    [Fact]
    public Task Engineering_workspace_uses_compact_plan_sidebar()
    {
        return RunStaAsync(async () =>
        {
            using var host = DependencyInjection.CreateHostBuilder([]).Build();
            var window = host.Services.GetRequiredService<MainWindow>();

            var workspace = await window.PrepareDialogWorkspaceAsync("Engineering");
            var planListView = host.Services.GetRequiredService<PlanListView>();

            workspace.Should().NotBeNull();
            workspace!.ColumnDefinitions[0].Width.Value.Should().Be(280);
            workspace.ColumnDefinitions[1].Width.Value.Should().Be(14);
            ((ListBox)planListView.FindName("PlanListBox")).HorizontalContentAlignment
                .Should().Be(HorizontalAlignment.Stretch);
        });
    }

    [Fact]
    public Task Module_dialog_chrome_does_not_render_duplicate_module_header()
    {
        return RunStaAsync(() =>
        {
            using var host = DependencyInjection.CreateHostBuilder([]).Build();
            var window = host.Services.GetRequiredService<MainWindow>();

            foreach (var viewKey in new[] { "Engineering", "Administration", "Traceability" })
            {
                var workspace = new Grid();
                var chrome = window.CreateDialogChromeForTest(viewKey, workspace);

                FindVisualChildren<TextBlock>(chrome)
                    .Select(textBlock => textBlock.Text)
                    .Should().NotContain(["\u5DE5\u7A0B\u7EF4\u62A4", "\u7CFB\u7EDF\u7BA1\u7406", "\u8FFD\u6EAF\u67E5\u8BE2", "\u8F85\u52A9\u6A21\u5757"]);
                chrome.BorderThickness.Should().Be(new Thickness(0));
                chrome.Padding.Should().Be(new Thickness(0));
                chrome.Child = null;
            }

            return Task.CompletedTask;
        });
    }

    [Fact]
    public Task Employee_login_window_exposes_simulated_scan_entry()
    {
        return RunStaAsync(() =>
        {
            using var host = DependencyInjection.CreateHostBuilder([]).Build();
            var window = host.Services.GetRequiredService<EmployeeLoginWindow>();

            window.FindName("SimulatedEmployeeCodeInput").Should().NotBeNull();
            window.FindName("SimulateScanButton").Should().NotBeNull();
            window.FindName("ExitButton").Should().BeNull();
            ((TextBlock)window.FindName("LastScanText")).Text.Should().Be("\u7B49\u5F85\u626B\u7801");

            return Task.CompletedTask;
        });
    }

    [Fact]
    public Task Operator_console_vfd_barcode_input_allows_keyboard_entry_during_testing()
    {
        return RunStaAsync(() =>
        {
            using var host = DependencyInjection.CreateHostBuilder([]).Build();
            var view = host.Services.GetRequiredService<OperatorConsoleView>();
            var window = new Window
            {
                Width = 1400,
                Height = 900,
                Content = view
            };

            try
            {
                window.Show();
                window.UpdateLayout();

                var input = (TextBox)view.FindName("VfdBarcodeInput");
                var submitButton = (Button)view.FindName("SubmitVfdBarcodeButton");

                input.Should().NotBeNull();
                input.IsReadOnly.Should().BeFalse();
                input.InputBindings.OfType<KeyBinding>()
                    .Any(binding => binding.Key == Key.Enter && binding.Command != null)
                    .Should().BeTrue();
                submitButton.Should().NotBeNull();
                submitButton.Command.Should().NotBeNull();
                submitButton.Content.Should().Be("\u5F55\u5165\u6761\u7801");
            }
            finally
            {
                window.Close();
            }

            return Task.CompletedTask;
        });
    }

    [Fact]
    public Task Operator_console_exposes_change_plan_button()
    {
        return RunStaAsync(() =>
        {
            using var host = DependencyInjection.CreateHostBuilder([]).Build();
            var view = host.Services.GetRequiredService<OperatorConsoleView>();
            var window = new Window
            {
                Width = 1400,
                Height = 900,
                Content = view
            };

            try
            {
                window.Show();
                window.UpdateLayout();

                var button = (Button)view.FindName("ChangePlanButton");
                button.Should().NotBeNull();
                button.Command.Should().NotBeNull();
                button.Content.Should().Be("\u66F4\u6362\u65B9\u6848");
            }
            finally
            {
                window.Close();
            }

            return Task.CompletedTask;
        });
    }

    [Fact]
    public Task Operator_console_moves_actions_to_top_and_uses_full_width_slot_board()
    {
        return RunStaAsync(() =>
        {
            using var host = DependencyInjection.CreateHostBuilder([]).Build();
            var view = host.Services.GetRequiredService<OperatorConsoleView>();
            var window = new Window
            {
                Width = 1600,
                Height = 900,
                Content = view
            };

            try
            {
                window.Show();
                window.UpdateLayout();

                view.FindName("OperatorTopActionBar").Should().NotBeNull();
                view.FindName("OperatorCurrentActionPanel").Should().BeNull();
                view.FindName("OperatorSlotBoard").Should().NotBeNull();
                view.FindName("OperatorMainContentGrid").Should().BeOfType<Grid>()
                    .Which.ColumnDefinitions.Should().ContainSingle();
                view.FindName("PlanSelectorFlow").Should().BeOfType<ComboBox>();
                view.FindName("SubmitVfdBarcodeButton").Should().BeOfType<Button>();
                view.FindName("TopStartRunButton").Should().BeOfType<Button>();
                view.FindName("TopStopRunButton").Should().BeOfType<Button>();
                view.FindName("ChangePlanButton").Should().BeOfType<Button>();
            }
            finally
            {
                window.Close();
            }

            return Task.CompletedTask;
        });
    }

    [Fact]
    public Task Operator_console_uses_compact_header_and_four_column_slot_grid()
    {
        return RunStaAsync(() =>
        {
            using var host = DependencyInjection.CreateHostBuilder([]).Build();
            var window = host.Services.GetRequiredService<MainWindow>();
            var view = host.Services.GetRequiredService<OperatorConsoleView>();

            try
            {
                window.Show();
                window.UpdateLayout();

                var rootGrid = (Grid)window.Content;
                rootGrid.RowDefinitions[0].Height.Value.Should().BeLessThanOrEqualTo(56);
                rootGrid.RowDefinitions[1].Height.Value.Should().Be(0);

                var slotItemsControl = (ItemsControl)view.FindName("SlotCardsItems");
                var itemsHost = FindVisualChild<UniformGrid>(slotItemsControl);
                itemsHost.Should().NotBeNull();
                itemsHost!.Columns.Should().Be(4);

                ((double)view.FindResource("SlotCardMinWidth")).Should().BeLessThan(356);
                ((double)view.FindResource("SlotCardMinHeight")).Should().BeGreaterThan(400);

                var slotButton = FindVisualChildren<Button>(slotItemsControl)
                    .First(button => ReferenceEquals(button.Command, ((OperatorConsoleViewModel)view.DataContext!).ToggleSlotCommand));
                slotButton.HorizontalAlignment.Should().Be(HorizontalAlignment.Stretch);
                slotButton.VerticalAlignment.Should().Be(VerticalAlignment.Stretch);
                slotButton.HorizontalContentAlignment.Should().Be(HorizontalAlignment.Stretch);
                slotButton.VerticalContentAlignment.Should().Be(VerticalAlignment.Stretch);
                slotButton.Style.Should().BeSameAs(view.FindResource("SlotCardButtonStyle"));
              }
              finally
              {
                  window.Close();
              }

            return Task.CompletedTask;
        });
    }

    [Fact]
    public Task Operator_console_keeps_instruction_log_area_visible_when_empty()
    {
        return RunStaAsync(() =>
        {
            using var host = DependencyInjection.CreateHostBuilder([]).Build();
            var window = host.Services.GetRequiredService<MainWindow>();
            var view = host.Services.GetRequiredService<OperatorConsoleView>();

            try
            {
                window.Show();
                window.UpdateLayout();

                view.FindName("InstructionLogTitle").Should().BeOfType<TextBlock>()
                    .Which.Text.Should().Be("指令日志");

                var instructionLogGrid = view.FindName("InstructionLogGrid").Should().BeOfType<DataGrid>().Subject;
                instructionLogGrid.MinHeight.Should().BeGreaterThanOrEqualTo(160);
                instructionLogGrid.MaxHeight.Should().BeLessThanOrEqualTo(260);

                var emptyHint = view.FindName("InstructionLogEmptyHint").Should().BeOfType<TextBlock>().Subject;
                emptyHint.Text.Should().Contain("暂无指令记录");
            }
            finally
            {
                window.Close();
            }

            return Task.CompletedTask;
        });
    }

    [Fact]
    public Task Workflow_editor_view_renders_without_xaml_resource_errors()
    {
        return RunStaAsync(() =>
        {
            using var host = DependencyInjection.CreateHostBuilder([]).Build();
            var view = host.Services.GetRequiredService<WorkflowEditorView>();
            var window = new Window
            {
                Width = 1200,
                Height = 760,
                Content = view
            };

            try
            {
                window.Show();
                window.UpdateLayout();
            }
            finally
            {
                window.Close();
            }

            return Task.CompletedTask;
        });
    }

    [Fact]
    public Task Workflow_editor_dialogs_render_without_xaml_resource_errors()
    {
        return RunStaAsync(() =>
        {
            using var host = DependencyInjection.CreateHostBuilder([]).Build();
            var view = host.Services.GetRequiredService<WorkflowEditorView>();
            var addStepWindow = new WorkflowAddStepWindow
            {
                Width = 520,
                Height = 420,
                DataContext = view.DataContext
            };

            try
            {
                addStepWindow.Show();
                addStepWindow.UpdateLayout();

                var expectedBackground = ((SolidColorBrush)addStepWindow.FindResource("IndustrialBackgroundBrush")).Color;
                ((SolidColorBrush)addStepWindow.Background).Color.Should().Be(expectedBackground);

                addStepWindow.FindName("AddStepLogicalPointSelector").Should().BeOfType<ComboBox>();
                addStepWindow.FindName("AddStepNameInput").Should().BeOfType<TextBox>();
                addStepWindow.FindName("AddStepCompareButton").Should().BeOfType<Button>();
                addStepWindow.FindName("AddStepParameterInput").Should().BeNull();
                addStepWindow.FindName("AddStepWriteCommandSelector").Should().BeNull();
                FindVisualChildren<TextBlock>(addStepWindow)
                    .Select(textBlock => textBlock.Text)
                    .Should().Contain("\u6DFB\u52A0\u6B65\u9AA4")
                    .And.NotContain([
                        "\u4ECE\u6A21\u677F\u6DFB\u52A0\u6B65\u9AA4",
                        "\u547D\u4EE4\u6A21\u677F",
                        "\u6A21\u677F\u547D\u4EE4",
                        "\u6A21\u677F\u76EE\u6807"
                    ]);
                FindVisualChildren<Button>(addStepWindow)
                    .Select(button => button.Content)
                    .Should().Contain("\u6DFB\u52A0\u6B65\u9AA4")
                    .And.NotContain("\u6DFB\u52A0\u6A21\u677F\u6B65\u9AA4");
            }
            finally
            {
                addStepWindow.Close();
            }

            return Task.CompletedTask;
        });
    }

    [Fact]
    public Task Workflow_editor_uses_shared_design_system_styles()
    {
        return RunStaAsync(() =>
        {
            using var host = DependencyInjection.CreateHostBuilder([]).Build();
            var view = host.Services.GetRequiredService<WorkflowEditorView>();
            var window = new Window
            {
                Width = 1200,
                Height = 760,
                Content = view
            };

            try
            {
                window.Show();
                window.UpdateLayout();

                ((Style)view.FindResource("EngineeringStepButtonStyle")).BasedOn
                    .Should().BeSameAs(view.FindResource("IndustrialCommandButtonStyle"));
                ((Style)view.FindResource("EngineeringSmallButtonStyle")).BasedOn
                    .Should().BeSameAs(view.FindResource("IndustrialCommandButtonStyle"));
                ((Style)view.FindResource("EditorTextBoxStyle")).BasedOn
                    .Should().BeSameAs(view.FindResource("IndustrialTextBoxStyle"));
                ((Style)view.FindResource("EditorComboBoxStyle")).BasedOn
                    .Should().BeSameAs(view.FindResource("IndustrialComboBoxStyle"));
                view.FindName("WorkflowCommandStrip").Should().BeNull();
                view.FindName("WorkflowStepToolbar").Should().BeNull();
                view.FindName("AddWorkflowStepButton").Should().NotBeNull();
                view.FindName("AddWorkflowStepFromTemplateButton").Should().BeNull();
                view.FindName("ManageCommandTemplatesButton").Should().BeNull();
                view.FindName("ValidateWorkflowButton").Should().NotBeNull();
                view.FindName("PublishPlanButton").Should().NotBeNull();
                view.FindName("WorkflowStatusMessageText").Should().BeNull();
                view.FindName("CommandTemplateSelector").Should().BeNull();
                view.FindName("TemplateManagerPanel").Should().BeNull();
                view.FindName("AddCustomCommandTemplateButton").Should().BeNull();
                view.FindName("RemoveSelectedCommandTemplateButton").Should().BeNull();
                ((ColumnDefinition)view.FindName("StepPropertyColumn")).Width.Value.Should().Be(420);
                view.FindName("StepTypeSelector").Should().BeNull();
                view.FindName("StepLogicalPointSelector").Should().BeOfType<ComboBox>();
                view.FindName("StepPointActionSelector").Should().BeNull();
                view.FindName("StepTargetPointSelector").Should().BeNull();
                view.FindName("StepPropertyPanel").Should().NotBeNull();
                ((DataGrid)view.FindName("WorkflowStepGrid")).Columns
                    .Select(column => column.Header)
                    .Should().NotContain(["\u64CD\u4F5C", "\u542F\u7528"]);
                view.FindName("StepRowMoreActionsButton").Should().BeNull();
                FindVisualChildren<Button>(view)
                    .Select(button => button.Content)
                    .Should().NotContain([
                        "\u542F\u52A8 VFD",
                        "\u8BFB VFD \u7535\u538B",
                        "\u8BFB\u53D6\u4EEA\u8868\u7535\u538B",
                        "\u590D\u5236",
                        "\u4E0A\u79FB",
                        "\u4E0B\u79FB",
                        "\u5220\u9664",
                        "\u53D1\u5E03\u4E3A\u65B0\u7248\u672C"
                    ]);
                FindVisualChildren<TextBlock>(view)
                    .Select(textBlock => textBlock.Text)
                    .Should().NotContain(text => text.StartsWith("\u5DF2\u52A0\u8F7D", StringComparison.Ordinal));

                var addStepButton = (Button)view.FindName("AddWorkflowStepButton");
                addStepButton.Content.Should().BeOfType<StackPanel>()
                    .Which.Children.OfType<TextBlock>().Select(textBlock => textBlock.Text)
                    .Should().Contain(["\uE710", "\u6DFB\u52A0\u6B65\u9AA4"]);
                var validateButton = (Button)view.FindName("ValidateWorkflowButton");
                validateButton.Content.Should().BeOfType<StackPanel>()
                    .Which.Children.OfType<TextBlock>().Select(textBlock => textBlock.Text)
                    .Should().Contain(["\uE930", "\u6821\u9A8C\u65B9\u6848"]);

                var publishButton = (Button)view.FindName("PublishPlanButton");
                publishButton.Content.Should().BeOfType<StackPanel>()
                    .Which.Children.OfType<TextBlock>().Select(textBlock => textBlock.Text)
                    .Should().Contain(["\uE898", "\u53D1\u5E03\u65B9\u6848"]);
                publishButton.Style.Should().BeSameAs(view.FindResource("IndustrialPrimaryCommandButtonStyle"));
                GetRenderedTextBrush(publishButton).Color
                    .Should().Be(((SolidColorBrush)view.FindResource("IndustrialInverseTextBrush")).Color);
                GetRenderedTextRenderingMode(publishButton).Should().Be(TextRenderingMode.Grayscale);
                GetEffectiveOpacity(GetRenderedTextElement(publishButton), publishButton).Should().Be(1.0);
                ((ScrollViewer)view.FindName("WorkflowEditorContentScroll")).HorizontalScrollBarVisibility
                    .Should().Be(ScrollBarVisibility.Disabled);
                view.FindName("WorkflowEditorRoot").Should().BeOfType<DockPanel>()
                    .Which.MinWidth.Should().Be(0);
                view.FindName("WorkflowHeaderActions").Should().BeAssignableTo<Panel>();
                view.FindName("WorkflowEditorMainGrid").Should().BeOfType<Grid>();
                Grid.GetColumn((UIElement)view.FindName("StepPropertyPanel")).Should().Be(2);
                Grid.GetRow((UIElement)view.FindName("StepPropertyPanel")).Should().Be(0);
                ((ColumnDefinition)view.FindName("WorkflowStepColumn")).MinWidth.Should().BeLessThan(720);

                var actionButtons = new[]
                {
                    ("StepCopyButton", "\uE8C8", "\u590D\u5236\u6B65\u9AA4"),
                    ("StepMoveUpButton", "\uE74A", "\u4E0A\u79FB\u6B65\u9AA4"),
                    ("StepMoveDownButton", "\uE74B", "\u4E0B\u79FB\u6B65\u9AA4"),
                    ("StepDeleteButton", "\uE74D", "\u5220\u9664\u6B65\u9AA4")
                };
                foreach (var (buttonName, icon, automationName) in actionButtons)
                {
                    var actionButton = (Button)view.FindName(buttonName);
                    actionButton.Content.Should().Be(icon);
                    actionButton.MinWidth.Should().BeLessThanOrEqualTo(34);
                    actionButton.MinHeight.Should().BeLessThanOrEqualTo(32);
                    actionButton.FontFamily.Source.Should().Be("Segoe Fluent Icons");
                    actionButton.GetValue(AutomationProperties.NameProperty).Should().Be(automationName);
                }
            }
            finally
            {
                window.Close();
            }

            return Task.CompletedTask;
        });
    }

    [Fact]
    public Task Workflow_editor_reflows_for_narrow_windows()
    {
        return RunStaAsync(() =>
        {
            using var host = DependencyInjection.CreateHostBuilder([]).Build();
            var view = host.Services.GetRequiredService<WorkflowEditorView>();
            var window = new Window
            {
                Width = 860,
                Height = 760,
                Content = view
            };

            try
            {
                window.Show();
                window.UpdateLayout();

                var headerActions = (Panel)view.FindName("WorkflowHeaderActions");
                Grid.GetRow(headerActions).Should().Be(1);
                Grid.GetColumn(headerActions).Should().Be(0);
                Grid.GetColumnSpan(headerActions).Should().Be(2);

                var propertyPanel = (Border)view.FindName("StepPropertyPanel");
                Grid.GetRow(propertyPanel).Should().Be(2);
                Grid.GetColumn(propertyPanel).Should().Be(0);
                Grid.GetColumnSpan(propertyPanel).Should().Be(3);

                ((ScrollViewer)view.FindName("WorkflowEditorContentScroll")).HorizontalScrollBarVisibility
                    .Should().Be(ScrollBarVisibility.Disabled);
                ((DataGrid)view.FindName("WorkflowStepGrid")).Columns
                    .Select(column => column.Header)
                    .Should().NotContain("\u542F\u7528");
                ((ColumnDefinition)view.FindName("StepPropertyColumn")).Width.GridUnitType.Should().Be(GridUnitType.Star);
            }
            finally
            {
                window.Close();
            }

            return Task.CompletedTask;
        });
    }

    [Fact]
    public Task Combo_boxes_use_lightweight_industrial_dropdown_style()
    {
        return RunStaAsync(() =>
        {
            using var host = DependencyInjection.CreateHostBuilder([]).Build();
            var view = host.Services.GetRequiredService<WorkflowEditorView>();
            var window = new Window
            {
                Width = 1200,
                Height = 760,
                Content = view
            };

            try
            {
                window.Show();
                window.UpdateLayout();

                var industrialStyle = (Style)view.FindResource("IndustrialComboBoxStyle");
                industrialStyle.Setters.OfType<Setter>()
                    .Single(setter => setter.Property == Control.MinHeightProperty)
                    .Value.Should().Be(40.0);
                industrialStyle.Setters.OfType<Setter>()
                    .Single(setter => setter.Property == Control.BackgroundProperty)
                    .Value.Should().BeSameAs(view.FindResource("IndustrialRaisedBrush"));
                industrialStyle.Setters.OfType<Setter>()
                    .Single(setter => setter.Property == Control.FocusVisualStyleProperty)
                    .Value.Should().BeNull();

                var workflowCombos = FindVisualChildren<ComboBox>(view);
                workflowCombos.Should().NotBeEmpty();
                workflowCombos.Should().OnlyContain(combo =>
                    StyleInheritsFrom(combo.Style, industrialStyle)
                    || StyleInheritsFrom((Style)combo.FindResource(typeof(ComboBox)), industrialStyle));
            }
            finally
            {
                window.Close();
            }

            return Task.CompletedTask;
        });
    }

    [Fact]
    public Task Secondary_module_views_use_flat_workspace_roots()
    {
        return RunStaAsync(() =>
        {
            using var host = DependencyInjection.CreateHostBuilder([]).Build();
            UserControl[] views =
            [
                host.Services.GetRequiredService<PlanListView>(),
                host.Services.GetRequiredService<WorkflowEditorView>(),
                host.Services.GetRequiredService<ExecutionHistoryView>(),
                host.Services.GetRequiredService<DeviceRunTraceView>()
            ];

            foreach (var view in views)
            {
                view.Content.Should().NotBeOfType<Border>();
            }

            return Task.CompletedTask;
        });
    }

    [Fact]
    public Task Station_config_view_uses_system_management_workbench_layout()
    {
        return RunStaAsync(() =>
        {
            using var host = DependencyInjection.CreateHostBuilder([]).Build();
            var view = host.Services.GetRequiredService<StationConfigView>();
            var window = new Window
            {
                Width = 1200,
                Height = 760,
                Content = view
            };

            try
            {
                window.Show();
                window.UpdateLayout();

                view.Content.Should().BeOfType<Grid>();
                view.FindName("AdminNavigationRail").Should().NotBeNull();
                view.FindName("AdminConfigurationWorkspace").Should().BeOfType<Grid>();
                view.FindName("AdminMaintenanceSummary").Should().BeNull();
                view.FindName("AdminStatusStrip").Should().BeNull();
                view.FindName("SlotPortEditor").Should().BeNull();
                view.FindName("SlotInstrumentPanel").Should().BeNull();
                view.FindName("SlotCommunicationContent").Should().BeOfType<Grid>();
                ((Grid)view.FindName("SlotCommunicationContent")).MaxWidth.Should().Be(double.PositiveInfinity);
                ((Grid)view.FindName("SlotCommunicationContent")).HorizontalAlignment.Should().Be(HorizontalAlignment.Stretch);
                view.FindName("AddSlotButton").Should().NotBeNull();
                view.FindName("RefreshSlotPortsButton").Should().NotBeNull();
                view.FindName("SaveAllSlotsButton").Should().NotBeNull();
                view.FindName("SelectedDeviceModelPointPanel").Should().BeOfType<Border>();
                view.FindName("AddLogicalPointButton").Should().NotBeNull();
                view.FindName("DeleteLogicalPointButton").Should().NotBeNull();
                view.FindName("LegacyDeviceModelPointStack").Should().BeNull();
                view.FindName("LogicalPointGrid").Should().BeNull();
                view.FindName("LogicalPointConfigTab").Should().BeNull();
                ((Border)view.FindName("AdminNavigationRail")).BorderThickness.Should().Be(new Thickness(0));
                ((DataGrid)view.FindName("DeviceModelGrid")).MaxHeight.Should().Be(double.PositiveInfinity);
                ((DataGrid)view.FindName("SelectedDeviceModelPointGrid")).MaxHeight.Should().Be(double.PositiveInfinity);
                ((DataGrid)view.FindName("SelectedDeviceModelPointGrid")).IsReadOnly.Should().BeFalse();
                ((DataGrid)view.FindName("SelectedDeviceModelPointGrid")).Columns
                    .Select(column => column.Header)
                    .Should().Contain("\u5BC4\u5B58\u5668\u5730\u5740");
                ((DataGrid)view.FindName("SlotConfigGrid")).MaxHeight.Should().Be(438);
                ((DataGrid)view.FindName("SlotConfigGrid")).MaxWidth.Should().Be(double.PositiveInfinity);
                ((DataGrid)view.FindName("SlotConfigGrid")).HorizontalAlignment.Should().Be(HorizontalAlignment.Stretch);
                ((DataGrid)view.FindName("SlotConfigGrid")).VerticalAlignment.Should().Be(VerticalAlignment.Top);
                ((DataGrid)view.FindName("SlotConfigGrid")).HorizontalScrollBarVisibility.Should().Be(ScrollBarVisibility.Auto);
                ((DataGrid)view.FindName("SlotConfigGrid")).CanUserResizeColumns.Should().BeFalse();
                ((DataGrid)view.FindName("SlotConfigGrid")).RowHeight.Should().Be(40);
                ((DataGrid)view.FindName("SlotConfigGrid")).Columns
                    .Select(column => column.Header)
                    .Should().Contain([
                        "\u69FD\u4F4D\u540D\u79F0",
                        "\u4E32\u53E3",
                        "\u53D8\u9891\u5668\u5730\u5740",
                        "\u7535\u538B\u8868\u5730\u5740",
                        "\u7535\u6D41\u8868\u5730\u5740",
                        "\u72B6\u6001",
                        "\u64CD\u4F5C"
                    ]);
                FindVisualChildren<TextBlock>(view)
                    .Select(textBlock => textBlock.Text)
                    .Should().Contain([
                        "\u69FD\u4F4D\u901A\u4FE1\u914D\u7F6E",
                        "\u914D\u7F6E\u6BCF\u4E2A\u69FD\u4F4D\u7684\u4E32\u53E3\u3001\u6CE2\u7279\u7387\u548C Modbus \u5730\u5740\u3002"
                    ]);
                ((DataGrid)view.FindName("SlotConfigGrid")).Columns
                    .Select(column => column.Header)
                    .Should().NotContain("\u7F16\u53F7");
                var slotOperationColumn = ((DataGrid)view.FindName("SlotConfigGrid")).Columns
                    .OfType<DataGridTemplateColumn>()
                    .Single(column => Equals(column.Header, "\u64CD\u4F5C"));
                var slotOperationTemplate = (StackPanel)slotOperationColumn.CellTemplate.LoadContent();
                slotOperationTemplate.Children.OfType<Button>()
                    .Should().ContainSingle(button => Equals(button.GetValue(AutomationProperties.NameProperty), "\u5220\u9664\u69FD\u4F4D"))
                    .Which.Should().Match<Button>(button => button.MinHeight <= 32 && button.MinWidth <= 34);
                var statusColumn = ((DataGrid)view.FindName("SlotConfigGrid")).Columns
                    .OfType<DataGridTemplateColumn>()
                    .Single(column => Equals(column.Header, "\u72B6\u6001"));
                statusColumn.Width.UnitType.Should().Be(DataGridLengthUnitType.Pixel);
                statusColumn.Width.Value.Should().Be(56);
                statusColumn.CanUserResize.Should().BeFalse();
                var statusTemplate = statusColumn.CellTemplate.LoadContent().Should().BeOfType<StackPanel>().Subject;
                statusTemplate.VerticalAlignment.Should().Be(VerticalAlignment.Center);
                statusTemplate.Children.OfType<Ellipse>().Should().ContainSingle()
                    .Which.VerticalAlignment.Should().Be(VerticalAlignment.Center);
                statusTemplate.Children.OfType<TextBlock>().Should().ContainSingle()
                    .Which.VerticalAlignment.Should().Be(VerticalAlignment.Center);
                ((DataGrid)view.FindName("SlotConfigGrid")).Columns
                    .Where(column => Equals(column.Header, "\u53D8\u9891\u5668\u5730\u5740")
                        || Equals(column.Header, "\u7535\u538B\u8868\u5730\u5740")
                        || Equals(column.Header, "\u7535\u6D41\u8868\u5730\u5740"))
                    .Should().OnlyContain(column => column.Width.UnitType == DataGridLengthUnitType.Pixel
                        && column.Width.Value == 130
                        && ((DataGridTextColumn)column).ElementStyle == view.FindResource("AdminCenteredCellTextStyle"));
                ((Style)view.FindResource("AdminIconDeleteRowButtonStyle")).Setters
                    .OfType<Setter>()
                    .Should().Contain(setter => setter.Property == Control.ForegroundProperty
                        && ReferenceEquals(setter.Value, view.FindResource("IndustrialTextMutedBrush")))
                    .And.Contain(setter => setter.Property == Control.BackgroundProperty
                        && ((SolidColorBrush)setter.Value).Color == Colors.Transparent);
                ((Style)view.FindResource("AdminSectionListBoxItemStyle")).Setters
                    .OfType<Setter>()
                    .Should().Contain(setter => setter.Property == Control.FocusVisualStyleProperty && setter.Value == null);
                foreach (var buttonName in new[] { "RefreshSlotPortsButton", "SaveAllSlotsButton", "AddSlotButton" })
                {
                    var buttonContent = ((Button)view.FindName(buttonName)).Content.Should().BeOfType<StackPanel>().Subject;
                    buttonContent.VerticalAlignment.Should().Be(VerticalAlignment.Center);
                    buttonContent.Children.OfType<TextBlock>().Should().OnlyContain(text => text.VerticalAlignment == VerticalAlignment.Center);
                }
                ((DataGrid)view.FindName("DeviceModelGrid")).IsReadOnly.Should().BeFalse();
            }
            finally
            {
                window.Close();
            }

            return Task.CompletedTask;
        });
    }

    [Fact]
    public Task Workbench_surfaces_use_responsive_layout_constraints()
    {
        return RunStaAsync(() =>
        {
            using var host = DependencyInjection.CreateHostBuilder([]).Build();
            var workflowView = host.Services.GetRequiredService<WorkflowEditorView>();
            var operatorView = host.Services.GetRequiredService<OperatorConsoleView>();
            var historyView = host.Services.GetRequiredService<ExecutionHistoryView>();

            var window = new Window
            {
                Width = 1280,
                Height = 760,
                Content = workflowView
            };

            try
            {
                window.Show();
                window.UpdateLayout();

                workflowView.FindName("WorkflowEditorContentScroll").Should().BeOfType<ScrollViewer>()
                    .Which.HorizontalScrollBarVisibility.Should().Be(ScrollBarVisibility.Disabled);
                workflowView.FindName("StepPropertyContentScroll").Should().BeOfType<ScrollViewer>()
                    .Which.VerticalScrollBarVisibility.Should().Be(ScrollBarVisibility.Auto);
                ((DataGrid)workflowView.FindName("WorkflowStepGrid")).HorizontalScrollBarVisibility.Should().Be(ScrollBarVisibility.Auto);

                window.Content = operatorView;
                window.UpdateLayout();

                operatorView.FindName("OperatorTopActionScroll").Should().BeOfType<ScrollViewer>()
                    .Which.HorizontalScrollBarVisibility.Should().Be(ScrollBarVisibility.Auto);
                ((DataGrid)operatorView.FindName("InstructionLogGrid")).HorizontalScrollBarVisibility.Should().Be(ScrollBarVisibility.Auto);

                window.Content = historyView;
                window.UpdateLayout();

                historyView.FindName("HistoryFilterScroll").Should().BeOfType<ScrollViewer>()
                    .Which.HorizontalScrollBarVisibility.Should().Be(ScrollBarVisibility.Auto);
            }
            finally
            {
                window.Close();
            }

            return Task.CompletedTask;
        });
    }

    [Fact]
    public Task Secondary_windows_can_resize_without_clipping_content()
    {
        return RunStaAsync(() =>
        {
            using var host = DependencyInjection.CreateHostBuilder([]).Build();
            var workflowView = host.Services.GetRequiredService<WorkflowEditorView>();
            var addStepWindow = new WorkflowAddStepWindow { DataContext = workflowView.DataContext };
            var employeeLoginWindow = host.Services.GetRequiredService<EmployeeLoginWindow>();
            var scannerWindow = host.Services.GetRequiredService<ScannerSettingsWindow>();

            try
            {
                addStepWindow.ResizeMode.Should().Be(ResizeMode.CanResize);
                addStepWindow.MinWidth.Should().BeGreaterThanOrEqualTo(520);
                addStepWindow.MinHeight.Should().BeGreaterThanOrEqualTo(340);
                addStepWindow.FindName("AddStepContentScroll").Should().BeOfType<ScrollViewer>()
                    .Which.VerticalScrollBarVisibility.Should().Be(ScrollBarVisibility.Auto);

                employeeLoginWindow.FindName("EmployeeLoginContentScroll").Should().BeOfType<ScrollViewer>()
                    .Which.VerticalScrollBarVisibility.Should().Be(ScrollBarVisibility.Auto);

                scannerWindow.FindName("ScannerSettingsContentScroll").Should().BeOfType<ScrollViewer>()
                    .Which.VerticalScrollBarVisibility.Should().Be(ScrollBarVisibility.Auto);
            }
            finally
            {
                addStepWindow.Close();
                employeeLoginWindow.Close();
                scannerWindow.Close();
            }

            return Task.CompletedTask;
        });
    }

    [Fact]
    public Task Barcode_rule_view_uses_system_management_summary_layout()
    {
        return RunStaAsync(() =>
        {
            using var host = DependencyInjection.CreateHostBuilder([]).Build();
            var view = host.Services.GetRequiredService<BarcodeRuleView>();
            var window = new Window
            {
                Width = 360,
                Height = 760,
                Content = view
            };

            try
            {
                window.Show();
                window.UpdateLayout();

                view.FindName("BarcodeRuleStatusPanel").Should().NotBeNull();
                view.FindName("EmployeeRuleCard").Should().NotBeNull();
                view.FindName("VfdRuleCard").Should().NotBeNull();
            }
            finally
            {
                window.Close();
            }

            return Task.CompletedTask;
        });
    }

    [Fact]
    public Task Traceability_history_filter_uses_shared_design_system_styles()
    {
        return RunStaAsync(() =>
        {
            using var host = DependencyInjection.CreateHostBuilder([]).Build();
            var view = host.Services.GetRequiredService<ExecutionHistoryView>();
            var window = new Window
            {
                Width = 980,
                Height = 720,
                Content = view
            };

            try
            {
                window.Show();
                window.UpdateLayout();

                ((DatePicker)view.FindName("FromDatePicker")).Style
                    .Should().BeSameAs(view.FindResource("IndustrialDatePickerStyle"));
                ((DatePicker)view.FindName("ToDatePicker")).Style
                    .Should().BeSameAs(view.FindResource("IndustrialDatePickerStyle"));
                ((TextBox)view.FindName("BarcodeFilterInput")).Style
                    .Should().BeSameAs(view.FindResource("IndustrialTextBoxStyle"));
                ((ComboBox)view.FindName("ConclusionFilterCombo")).Style
                    .Should().BeSameAs(view.FindResource("IndustrialComboBoxStyle"));
            }
            finally
            {
                window.Close();
            }

            return Task.CompletedTask;
        });
    }

    [Fact]
    public Task Disabled_primary_command_button_uses_neutral_disabled_state()
    {
        return RunStaAsync(() =>
        {
            using var host = DependencyInjection.CreateHostBuilder([]).Build();
            var view = host.Services.GetRequiredService<WorkflowEditorView>();
            var window = new Window
            {
                Width = 280,
                Height = 120
            };

            var button = new Button
            {
                Content = "\u53D1\u5E03\u4E3A\u65B0\u7248\u672C",
                IsEnabled = false,
                Style = (Style)view.FindResource("IndustrialPrimaryCommandButtonStyle")
            };
            window.Content = button;

            try
            {
                window.Show();
                window.UpdateLayout();

                ((SolidColorBrush)button.Background).Color
                    .Should().Be(((SolidColorBrush)view.FindResource("IndustrialDisabledControlBrush")).Color);
                ((SolidColorBrush)button.BorderBrush).Color
                    .Should().Be(((SolidColorBrush)view.FindResource("IndustrialDisabledBorderBrush")).Color);
                ((SolidColorBrush)button.Foreground).Color
                    .Should().Be(((SolidColorBrush)view.FindResource("IndustrialDisabledTextBrush")).Color);
                GetRenderedTextBrush(button).Color
                    .Should().Be(((SolidColorBrush)view.FindResource("IndustrialDisabledTextBrush")).Color);
                button.Opacity.Should().Be(1.0);
            }
            finally
            {
                window.Close();
            }

            return Task.CompletedTask;
        });
    }

    [Fact]
    public Task Enabled_primary_command_button_renders_inverse_text_on_accent_surface()
    {
        return RunStaAsync(() =>
        {
            using var host = DependencyInjection.CreateHostBuilder([]).Build();
            var view = host.Services.GetRequiredService<WorkflowEditorView>();
            var window = new Window
            {
                Width = 280,
                Height = 120
            };

            var button = new Button
            {
                Content = "\u53D1\u5E03\u4E3A\u65B0\u7248\u672C",
                Style = (Style)view.FindResource("IndustrialPrimaryCommandButtonStyle")
            };
            window.Content = button;

            try
            {
                window.Show();
                window.UpdateLayout();

                ((SolidColorBrush)button.Background).Color
                    .Should().Be(((SolidColorBrush)view.FindResource("IndustrialAccentBrush")).Color);
                ((SolidColorBrush)button.Foreground).Color
                    .Should().Be(((SolidColorBrush)view.FindResource("IndustrialInverseTextBrush")).Color);
                GetRenderedTextBrush(button).Color
                    .Should().Be(((SolidColorBrush)view.FindResource("IndustrialInverseTextBrush")).Color);
                GetRenderedTextRenderingMode(button).Should().Be(TextRenderingMode.Grayscale);
                GetEffectiveOpacity(GetRenderedTextElement(button), button).Should().Be(1.0);
            }
            finally
            {
                window.Close();
            }

            return Task.CompletedTask;
        });
    }

    [Fact]
    public Task Primary_command_buttons_across_app_render_text_at_full_opacity()
    {
        return RunStaAsync(() =>
        {
            using var host = DependencyInjection.CreateHostBuilder([]).Build();
            var cases = new (Window Window, string[] ButtonContents)[]
            {
                (new Window
                {
                    Width = 1400,
                    Height = 900,
                    Content = host.Services.GetRequiredService<OperatorConsoleView>()
                }, ["\u7B49\u5F85\u626B\u7801"]),
                (new Window
                {
                    Width = 360,
                    Height = 620,
                    Content = host.Services.GetRequiredService<PlanListView>()
                }, ["\u521B\u5EFA\u65B9\u6848"]),
                (host.Services.GetRequiredService<EmployeeLoginWindow>(), ["\u6A21\u62DF\u626B\u7801\u8FDB\u5165"])
            };

            try
            {
                foreach (var (window, buttonContents) in cases)
                {
                    window.Show();
                    window.UpdateLayout();
                    AssertButtonsTextIsFullOpacity(window, buttonContents);
                }
            }
            finally
            {
                foreach (var (window, _) in cases)
                {
                    window.Close();
                }
            }

            return Task.CompletedTask;
        });
    }

    [Fact]
    public async Task Scanner_input_is_disabled_by_default()
    {
        using var host = DependencyInjection.CreateHostBuilder([]).Build();
        var scanner = host.Services.GetRequiredService<IBarcodeInputService>();

        var act = () => scanner.StartAsync(CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*\u672A\u542F\u7528*");
    }

    [Fact]
    public Task Employee_login_window_ignores_invalid_scanner_noise()
    {
        return RunStaAsync(async () =>
        {
            using var host = DependencyInjection.CreateHostBuilder([]).Build();
            var window = host.Services.GetRequiredService<EmployeeLoginWindow>();

            await window.ApplyEmployeeScanForTestAsync("\u003F\u003F\u003F");

            ((TextBlock)window.FindName("LastScanText")).Text.Should().Be("\u7B49\u5F85\u626B\u7801");
        });
    }

    [Fact]
    public void Default_app_mode_uses_simulated_device_client()
    {
        using var host = DependencyInjection.CreateHostBuilder([]).Build();

        host.Services.GetRequiredService<IDeviceCommandClient>()
            .Should()
            .BeOfType<SimulatedDeviceCommandClient>();
        host.Services.GetRequiredService<MainShellViewModel>().AppModeDisplay
            .Should()
            .Be("SIM \u6A21\u62DF");
    }

    [Fact]
    public void Serial_app_mode_uses_modbus_rtu_device_client()
    {
        using var host = DependencyInjection.CreateHostBuilder(["--AppMode=Serial"]).Build();

        host.Services.GetRequiredService<IDeviceCommandClient>()
            .Should()
            .BeOfType<ModbusRtuCommandClient>();
        host.Services.GetRequiredService<MainShellViewModel>().AppModeDisplay
            .Should()
            .Be("RTU \u4E32\u53E3");
    }

    private static Task RunStaAsync(Func<Task> action)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(async () =>
        {
            try
            {
                await action();
                completion.SetResult();
            }
            catch (Exception ex)
            {
                completion.SetException(ex);
            }
            finally
            {
                Dispatcher.CurrentDispatcher.InvokeShutdown();
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return completion.Task;
    }

    private static SolidColorBrush GetRenderedTextBrush(DependencyObject parent)
    {
        return GetRenderedTextElement(parent) switch
        {
            TextBlock textBlock => (SolidColorBrush)textBlock.Foreground,
            AccessText accessText => (SolidColorBrush)accessText.Foreground,
            _ => throw new InvalidOperationException("Rendered button text was not found.")
        };
    }

    private static TextRenderingMode GetRenderedTextRenderingMode(DependencyObject parent)
    {
        return TextOptions.GetTextRenderingMode(GetRenderedTextElement(parent));
    }

    private static FrameworkElement GetRenderedTextElement(DependencyObject parent)
    {
        var textBlock = FindVisualChild<TextBlock>(parent);
        if (textBlock is not null)
        {
            return textBlock;
        }

        var accessText = FindVisualChild<AccessText>(parent);
        accessText.Should().NotBeNull();
        return accessText!;
    }

    private static double GetEffectiveOpacity(DependencyObject child, DependencyObject ancestor)
    {
        var opacity = 1.0;
        var current = child;

        while (!ReferenceEquals(current, ancestor))
        {
            if (current is UIElement element)
            {
                opacity *= element.Opacity;
            }

            current = VisualTreeHelper.GetParent(current);
            current.Should().NotBeNull();
        }

        if (ancestor is UIElement ancestorElement)
        {
            opacity *= ancestorElement.Opacity;
        }

        return opacity;
    }

    private static void AssertButtonsTextIsFullOpacity(DependencyObject root, IEnumerable<string> buttonContents)
    {
        foreach (var buttonContent in buttonContents)
        {
            var buttons = FindVisualChildren<Button>(root)
                .Where(candidate => Equals(candidate.Content, buttonContent))
                .ToList();

            buttons.Should().NotBeEmpty();
            foreach (var button in buttons)
            {
                GetEffectiveOpacity(GetRenderedTextElement(button), button).Should().Be(1.0);
            }
        }
    }

    private static bool StyleInheritsFrom(Style? style, Style expectedBase)
    {
        while (style is not null)
        {
            if (ReferenceEquals(style, expectedBase))
            {
                return true;
            }

            style = style.BasedOn;
        }

        return false;
    }

    private static T? FindVisualChild<T>(DependencyObject parent)
        where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (child is T typedChild)
            {
                return typedChild;
            }

            var descendant = FindVisualChild<T>(child);
            if (descendant is not null)
            {
                return descendant;
            }
        }

        return null;
    }

    private static IReadOnlyList<T> FindVisualChildren<T>(DependencyObject parent)
        where T : DependencyObject
    {
        var children = new List<T>();
        AddVisualChildren(parent, children);
        return children;
    }

    private static void AddVisualChildren<T>(DependencyObject parent, ICollection<T> matches)
        where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (child is T typedChild)
            {
                matches.Add(typedChild);
            }

            AddVisualChildren(child, matches);
        }
    }
}
