using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using VfdControl.App.Views.Admin;
using VfdControl.App.Views.Engineering;
using VfdControl.App.Views.Operator;
using VfdControl.App.Views.Traceability;
using VfdControl.Presentation.Admin;
using VfdControl.Presentation.Engineering;
using VfdControl.Presentation.Shell;
using VfdControl.Presentation.Traceability;

namespace VfdControl.App.Views;

public partial class MainWindow : Window
{
    private readonly MainShellViewModel _viewModel;
    private readonly OperatorConsoleView _operatorConsoleView;
    private readonly PlanListViewModel _planListViewModel;
    private readonly WorkflowEditorViewModel _workflowEditorViewModel;
    private readonly StationConfigViewModel _stationConfigViewModel;
    private readonly ExecutionHistoryViewModel _executionHistoryViewModel;
    private readonly Grid _engineeringWorkspace;
    private readonly Grid _adminWorkspace;
    private readonly Grid _traceabilityWorkspace;
    private bool _engineeringLoaded;
    private bool _adminLoaded;
    private bool _traceabilityLoaded;

    internal bool IsOperatorConsoleHosted => ReferenceEquals(WorkspaceHost.Content, _operatorConsoleView);

    public MainWindow(
        MainShellViewModel viewModel,
        OperatorConsoleView operatorConsoleView,
        PlanListViewModel planListViewModel,
        WorkflowEditorViewModel workflowEditorViewModel,
        PlanListView planListView,
        WorkflowEditorView workflowEditorView,
        StationConfigViewModel stationConfigViewModel,
        StationConfigView stationConfigView,
        ExecutionHistoryViewModel executionHistoryViewModel,
        ExecutionHistoryView executionHistoryView,
        DeviceRunTraceView deviceRunTraceView)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _operatorConsoleView = operatorConsoleView;
        _planListViewModel = planListViewModel;
        _workflowEditorViewModel = workflowEditorViewModel;
        _stationConfigViewModel = stationConfigViewModel;
        _executionHistoryViewModel = executionHistoryViewModel;
        _engineeringWorkspace = CreateEngineeringWorkspace(planListView, workflowEditorView);
        _adminWorkspace = CreateAdminWorkspace(stationConfigView);
        _traceabilityWorkspace = CreateTraceabilityWorkspace(executionHistoryView, deviceRunTraceView);

        DataContext = viewModel;
        WorkspaceHost.Content = operatorConsoleView;

        viewModel.PropertyChanged += OnShellPropertyChanged;
        planListViewModel.PropertyChanged += OnPlanListPropertyChanged;
    }

    private static Grid CreateEngineeringWorkspace(PlanListView planListView, WorkflowEditorView workflowEditorView)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(280) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        Grid.SetColumn(planListView, 0);
        Grid.SetColumn(workflowEditorView, 2);
        grid.Children.Add(planListView);
        grid.Children.Add(workflowEditorView);
        return grid;
    }

    private static Grid CreateAdminWorkspace(StationConfigView stationConfigView)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        Grid.SetColumn(stationConfigView, 0);
        grid.Children.Add(stationConfigView);
        return grid;
    }

    private static Grid CreateTraceabilityWorkspace(ExecutionHistoryView executionHistoryView, DeviceRunTraceView deviceRunTraceView)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(430) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(18) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        Grid.SetColumn(executionHistoryView, 0);
        Grid.SetColumn(deviceRunTraceView, 2);
        grid.Children.Add(executionHistoryView);
        grid.Children.Add(deviceRunTraceView);
        return grid;
    }

    private async void OnShellPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(MainShellViewModel.RequestedDialogKey))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_viewModel.RequestedDialogKey))
        {
            return;
        }

        await ShowModuleDialogAsync(_viewModel.RequestedDialogKey);
        _viewModel.NavigateCommand.Execute("OperatorConsole");
    }

    private async Task ShowModuleDialogAsync(string viewKey)
    {
        var workspace = await PrepareDialogWorkspaceAsync(viewKey);
        if (workspace is null)
        {
            return;
        }

        ShowWorkspaceDialog(viewKey, workspace);
    }

    internal async Task<Grid?> PrepareDialogWorkspaceAsync(string viewKey)
    {
        if (viewKey == "Engineering")
        {
            if (!_engineeringLoaded)
            {
                _engineeringLoaded = true;
                await _planListViewModel.LoadAsync();
                if (_planListViewModel.SelectedPlan is not null)
                {
                    await _workflowEditorViewModel.LoadPlanAsync(_planListViewModel.SelectedPlan.Plan);
                }
            }

            return _engineeringWorkspace;
        }

        if (viewKey == "Administration")
        {
            if (!_adminLoaded)
            {
                _adminLoaded = true;
                await _stationConfigViewModel.LoadAsync();
            }

            return _adminWorkspace;
        }

        if (viewKey == "Traceability")
        {
            if (!_traceabilityLoaded)
            {
                _traceabilityLoaded = true;
                await _executionHistoryViewModel.LoadAsync();
            }

            return _traceabilityWorkspace;
        }

        return null;
    }

    private static string TitleFor(string viewKey)
    {
        return viewKey switch
        {
            "Engineering" => "工程维护",
            "Administration" => "系统管理",
            "Traceability" => "追溯查询",
            _ => "辅助模块"
        };
    }

    private void ShowWorkspaceDialog(string viewKey, Grid workspace)
    {
        var title = TitleFor(viewKey);
        var parent = workspace.Parent as ContentControl;
        if (parent is not null)
        {
            parent.Content = null;
        }
        else if (workspace.Parent is Panel panel)
        {
            panel.Children.Remove(workspace);
        }

        var chrome = CreateDialogChrome(title, workspace, showHeader: false);
        var dialog = new Window
        {
            Title = title,
            Owner = this,
            Width = Math.Max(980, ActualWidth * 0.86),
            Height = Math.Max(620, ActualHeight * 0.82),
            MinWidth = 900,
            MinHeight = 560,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = (System.Windows.Media.Brush)FindResource("IndustrialBackgroundBrush"),
            FontFamily = FontFamily,
            Content = chrome
        };

        try
        {
            dialog.ShowDialog();
        }
        finally
        {
            chrome.Child = null;
            dialog.Content = null;
        }
    }

    internal Border CreateDialogChromeForTest(string viewKey, Grid workspace)
    {
        return CreateDialogChrome(TitleFor(viewKey), workspace, showHeader: false);
    }

    private Border CreateDialogChrome(string title, Grid workspace, bool showHeader)
    {
        var content = new DockPanel();
        if (showHeader)
        {
            var header = new Grid { Margin = new Thickness(0, 0, 0, 14) };
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var titleBlock = new TextBlock
        {
            Text = title,
            FontSize = 20,
            FontWeight = FontWeights.SemiBold,
            Foreground = (System.Windows.Media.Brush)FindResource("IndustrialTextBrush"),
            VerticalAlignment = VerticalAlignment.Center
        };

        var modePill = new Border
        {
            Padding = new Thickness(10, 6, 10, 6),
            Background = (System.Windows.Media.Brush)FindResource("IndustrialSelectionBrush"),
            BorderBrush = (System.Windows.Media.Brush)FindResource("IndustrialAccentBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Child = new TextBlock
            {
                Text = "辅助模块",
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = (System.Windows.Media.Brush)FindResource("IndustrialAccentStrongBrush")
            }
        };

        Grid.SetColumn(modePill, 1);
        header.Children.Add(titleBlock);
        header.Children.Add(modePill);

            DockPanel.SetDock(header, Dock.Top);
            content.Children.Add(header);
        }

        content.Children.Add(workspace);

        if (!showHeader)
        {
            return new Border
            {
                Margin = new Thickness(14),
                Padding = new Thickness(0),
                BorderThickness = new Thickness(0),
                Child = content
            };
        }

        return new Border
        {
            Margin = new Thickness(14),
            Padding = new Thickness(14),
            Background = (System.Windows.Media.Brush)FindResource("IndustrialSurfaceBrush"),
            BorderBrush = (System.Windows.Media.Brush)FindResource("IndustrialBorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Child = content
        };
    }

    private async void OnPlanListPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PlanListViewModel.SelectedPlan) && _planListViewModel.SelectedPlan is not null)
        {
            await _workflowEditorViewModel.LoadPlanAsync(_planListViewModel.SelectedPlan.Plan);
        }
    }
}
