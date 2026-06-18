using System.Windows;
using VfdControl.App.Services;
using VfdControl.Infrastructure.Serial;

namespace VfdControl.App.Views.Operator;

public partial class ScannerSettingsWindow : Window
{
    private static readonly int[] SupportedBaudRates = [9600, 19200, 38400, 57600, 115200];

    private readonly ScannerSettingsService _settingsService;

    public ScannerSettingsWindow(ScannerSettingsService settingsService)
    {
        InitializeComponent();
        _settingsService = settingsService;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        BaudRateComboBox.ItemsSource = SupportedBaudRates;
        LoadPorts();
        LoadCurrentSettings();
    }

    private void LoadPorts()
    {
        var ports = _settingsService.ListPorts();
        PortComboBox.ItemsSource = ports;
        if (ports.Count == 0)
        {
            StatusText.Text = "\u672A\u68C0\u6D4B\u5230\u53EF\u7528 COM \u53E3\uFF0C\u7CFB\u7EDF\u626B\u7801\u67AA\u6682\u4E0D\u53EF\u8FDE\u63A5\u3002";
        }
    }

    private void LoadCurrentSettings()
    {
        var options = _settingsService.Current;
        BaudRateComboBox.SelectedItem = SupportedBaudRates.Contains(options.BaudRate)
            ? options.BaudRate
            : 9600;

        if (!string.IsNullOrWhiteSpace(options.PortName))
        {
            PortComboBox.SelectedValue = options.PortName;
            if (PortComboBox.SelectedItem is null)
            {
                PortComboBox.Text = options.PortName;
            }
        }
    }

    private void OnRefreshPortsClick(object sender, RoutedEventArgs e)
    {
        LoadPorts();
        StatusText.Text = "\u5DF2\u5237\u65B0 COM \u53E3\u5217\u8868\u3002";
    }

    private async void OnTestConnectionClick(object sender, RoutedEventArgs e)
    {
        if (!TryReadInputs(out var portName, out var baudRate))
        {
            return;
        }

        try
        {
            await _settingsService.TestConnectionAsync(portName, baudRate);
            StatusText.Text = $"{portName} \u5DF2\u6253\u5F00\uFF0C\u53EF\u4FDD\u5B58\u5E76\u8FDE\u63A5\u7CFB\u7EDF\u626B\u7801\u67AA\u3002";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"\u8FDE\u63A5\u5931\u8D25\uFF1A{ex.Message}";
        }
    }

    private async void OnSaveClick(object sender, RoutedEventArgs e)
    {
        if (!TryReadInputs(out var portName, out var baudRate))
        {
            return;
        }

        try
        {
            await _settingsService.TestConnectionAsync(portName, baudRate);
        }
        catch (Exception ex)
        {
            StatusText.Text = $"\u4FDD\u5B58\u5931\u8D25\uFF0C\u4E32\u53E3\u65E0\u6CD5\u8FDE\u63A5\uFF1A{ex.Message}";
            return;
        }

        await _settingsService.SaveAsync(new SerialBarcodeInputOptions
        {
            Enabled = true,
            PortName = portName,
            BaudRate = baudRate,
            NewLine = "\r\n"
        });

        DialogResult = true;
        Close();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private bool TryReadInputs(out string portName, out int baudRate)
    {
        portName = (PortComboBox.SelectedValue as string ?? PortComboBox.Text).Trim();
        if (string.IsNullOrWhiteSpace(portName))
        {
            baudRate = 0;
            StatusText.Text = "\u8BF7\u5148\u9009\u62E9\u7CFB\u7EDF\u626B\u7801\u67AA\u6240\u5728 COM \u53E3\u3002";
            return false;
        }

        if (BaudRateComboBox.SelectedItem is not int selectedBaudRate)
        {
            baudRate = 0;
            StatusText.Text = "\u8BF7\u9009\u62E9\u6709\u6548\u6CE2\u7279\u7387\u3002";
            return false;
        }

        baudRate = selectedBaudRate;
        return true;
    }
}
