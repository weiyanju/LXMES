using System.Windows;
using VfdControl.Application.Abstractions;
using VfdControl.Application.Execution;
using VfdControl.App.Services;
using VfdControl.Domain.ValueObjects;
using VfdControl.Presentation.Operator;

namespace VfdControl.App.Views.Operator;

public partial class EmployeeLoginWindow : Window
{
    private readonly OperatorConsoleViewModel _viewModel;
    private readonly IBarcodeInputService _barcodeInputService;
    private readonly ScannerSettingsService _settingsService;
    private readonly Func<ScannerSettingsWindow> _settingsWindowFactory;
    private bool _isAuthenticated;

    public EmployeeLoginWindow(
        OperatorConsoleViewModel viewModel,
        IBarcodeInputService barcodeInputService,
        ScannerSettingsService settingsService,
        Func<ScannerSettingsWindow> settingsWindowFactory)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _barcodeInputService = barcodeInputService;
        _settingsService = settingsService;
        _settingsWindowFactory = settingsWindowFactory;
        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.InitializeAsync();
        _barcodeInputService.BarcodeScanned += OnBarcodeScanned;
        RefreshScannerConfigText();
        await StartScannerAsync();
    }

    private async Task StartScannerAsync()
    {
        try
        {
            await _barcodeInputService.StartAsync(CancellationToken.None);
            StatusText.Text = "\u7CFB\u7EDF\u626B\u7801\u67AA\u5DF2\u8FDE\u63A5\uFF0C\u8BF7\u626B\u63CF\u5458\u5DE5\u7801\u3002";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"\u7CFB\u7EDF\u626B\u7801\u67AA\u672A\u8FDE\u63A5\u6216\u4E32\u53E3\u914D\u7F6E\u9519\u8BEF\uFF1A{ex.Message}";
        }
    }

    private void RefreshScannerConfigText()
    {
        var options = _settingsService.Current;
        ScannerConfigText.Text = options.Enabled
            ? $"\u5DF2\u914D\u7F6E\u7CFB\u7EDF\u626B\u7801\u67AA\uFF1A{options.PortName}\uFF0C{options.BaudRate} bps"
            : "\u672A\u8FDE\u63A5\u7CFB\u7EDF\u626B\u7801\u67AA\uFF0C\u53EF\u4F7F\u7528\u6A21\u62DF\u5165\u53E3\u3002";
    }

    private void OnBarcodeScanned(object? sender, BarcodeScannedEventArgs e)
    {
        _ = Dispatcher.InvokeAsync(async () =>
        {
            await ApplyEmployeeScanAsync(e.Text);
        });
    }

    private async Task ApplyEmployeeScanAsync(string employeeCode)
    {
        var normalized = employeeCode.Trim().ToUpperInvariant();
        if (!EmployeeCode.TryCreate(normalized).IsSuccess)
        {
            LastScanText.Text = "\u7B49\u5F85\u626B\u7801";
            StatusText.Text = "\u5DF2\u5FFD\u7565\u65E0\u6548\u626B\u7801\u6570\u636E\uFF0C\u8BF7\u626B\u63CF\u5458\u5DE5\u7801\u3002";
            return;
        }

        LastScanText.Text = normalized;
        await _viewModel.ApplyScannedTextAsync(normalized);
        StatusText.Text = _viewModel.StatusMessage;

        if (_viewModel.State != OperatorConsoleState.WaitingEmployeeCode)
        {
            _isAuthenticated = true;
            DialogResult = true;
            Close();
        }
    }

    private async void OnClosed(object? sender, EventArgs e)
    {
        _barcodeInputService.BarcodeScanned -= OnBarcodeScanned;
        if (!_isAuthenticated)
        {
            await _barcodeInputService.StopAsync();
        }
    }

    private async void OnSimulateScanClick(object sender, RoutedEventArgs e)
    {
        await ApplyEmployeeScanAsync(SimulatedEmployeeCodeInput.Text);
    }

    private async void OnScannerSettingsClick(object sender, RoutedEventArgs e)
    {
        var settingsWindow = _settingsWindowFactory();
        settingsWindow.Owner = this;
        if (settingsWindow.ShowDialog() == true)
        {
            await _barcodeInputService.StopAsync();
            RefreshScannerConfigText();
            await StartScannerAsync();
        }
    }

    public Task ApplyEmployeeScanForTestAsync(string employeeCode)
    {
        return ApplyEmployeeScanAsync(employeeCode);
    }
}
