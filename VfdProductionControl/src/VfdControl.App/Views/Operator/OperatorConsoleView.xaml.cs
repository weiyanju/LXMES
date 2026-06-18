using System.Windows.Controls;
using VfdControl.Application.Abstractions;
using VfdControl.Application.Execution;
using VfdControl.Presentation.Operator;

namespace VfdControl.App.Views.Operator;

public partial class OperatorConsoleView : UserControl
{
    private readonly OperatorConsoleViewModel _viewModel;
    private readonly IBarcodeInputService _barcodeInputService;
    private bool _isLoaded;
    private bool _isScannerSubscribed;

    public OperatorConsoleView(
        OperatorConsoleViewModel viewModel,
        IBarcodeInputService barcodeInputService)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _barcodeInputService = barcodeInputService;
        DataContext = viewModel;
        Unloaded += OnUnloaded;
    }

    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (_isLoaded)
        {
            SubscribeScanner();
            return;
        }

        _isLoaded = true;
        await _viewModel.InitializeAsync();
        SubscribeScanner();
        try
        {
            await _barcodeInputService.StartAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            _viewModel.StatusMessage = $"扫码枪未连接或串口配置错误：{ex.Message}";
        }
    }

    private void SubscribeScanner()
    {
        if (_isScannerSubscribed)
        {
            return;
        }

        _barcodeInputService.BarcodeScanned += OnBarcodeScanned;
        _isScannerSubscribed = true;
    }

    private void OnBarcodeScanned(object? sender, BarcodeScannedEventArgs e)
    {
        _ = Dispatcher.InvokeAsync(async () =>
        {
            await _viewModel.ApplyScannedTextAsync(e.Text);
        });
    }

    private void OnUnloaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (!_isScannerSubscribed)
        {
            return;
        }

        _barcodeInputService.BarcodeScanned -= OnBarcodeScanned;
        _isScannerSubscribed = false;
    }
}
