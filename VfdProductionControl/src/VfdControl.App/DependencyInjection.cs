using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VfdControl.Application.Abstractions;
using VfdControl.Application.Admin;
using VfdControl.Application.Engineering;
using VfdControl.Application.Execution;
using VfdControl.Application.Operator;
using VfdControl.Application.Traceability;
using VfdControl.App.Services;
using VfdControl.App.Views;
using VfdControl.App.Views.Admin;
using VfdControl.App.Views.Engineering;
using VfdControl.App.Views.Operator;
using VfdControl.App.Views.Traceability;
using VfdControl.Infrastructure.InMemory;
using VfdControl.Infrastructure.Seed;
using VfdControl.Infrastructure.Serial;
using VfdControl.Infrastructure.Simulation;
using VfdControl.Presentation.Admin;
using VfdControl.Presentation.Engineering;
using VfdControl.Presentation.Operator;
using VfdControl.Presentation.Shell;
using VfdControl.Presentation.Traceability;

namespace VfdControl.App;

public static class DependencyInjection
{
    public static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddJsonFile(ScannerSettingsService.SettingsPath, optional: true, reloadOnChange: false);
            })
            .ConfigureServices((context, services) =>
            {
                var demoData = DemoDataSeeder.Create();
                var scannerOptions = CreateScannerOptions(context.Configuration);

                services.AddSingleton(demoData);
                services.AddSingleton<IStationRepository>(_ => new InMemoryStationRepository(demoData.Stations));
                services.AddSingleton<IProcessPlanRepository>(_ => new InMemoryProcessPlanRepository(demoData.ProcessPlans));
                services.AddSingleton<ITraceRepository, InMemoryTraceRepository>();

                services.AddSingleton(_ => SimulationScenarioLoader.CreateDefault(demoData.Stations.SelectMany(station => station.Slots)));
                services.AddSingleton<StationSerialEndpointResolver>();
                if (IsSerialMode(context.Configuration["AppMode"]))
                {
                    services.AddSingleton<IDeviceCommandClient>(provider =>
                    {
                        var resolver = provider.GetRequiredService<StationSerialEndpointResolver>();
                        var deviceModelCatalog = provider.GetRequiredService<DeviceModelCatalog>();
                        return new ModbusRtuCommandClient(
                            resolver.Resolve,
                            registerAddressResolver: address => ResolveRegisterAddress(deviceModelCatalog, address));
                    });
                }
                else
                {
                    services.AddSingleton<IDeviceCommandClient, SimulatedDeviceCommandClient>();
                }
                services.AddSingleton(scannerOptions);
                services.AddSingleton<ScannerSettingsService>();
                services.AddSingleton<ISerialPortCatalog, ScannerSettingsSerialPortCatalog>();
                services.AddSingleton<StationConfigurationChangeNotifier>();
                services.AddSingleton<IBarcodeInputService, SerialBarcodeInputService>();
                services.AddSingleton<IWorkflowEngine, WorkflowEngine>();
                services.AddSingleton<SlotExecutionStateStore>();
                services.AddSingleton<ISlotScheduler, SlotScheduler>();

                services.AddTransient<OperatorSessionService>();
                services.AddTransient<PlanSelectionService>();
                services.AddTransient<SlotSelectionService>();
                services.AddTransient<ProductionRunService>();
                services.AddTransient<RunStatusQueryService>();
                services.AddTransient<StationConfigurationService>();
                services.AddSingleton<BarcodeRuleService>();
                services.AddSingleton<WorkflowDefinitionService>();
                services.AddSingleton<DeviceModelCatalog>();
                services.AddTransient<ProcessPlanService>();
                services.AddTransient<TraceabilityQueryService>();

                services.AddSingleton<OperatorConsoleViewModel>();
                services.AddSingleton<OperatorConsoleView>();
                services.AddSingleton<PlanListViewModel>();
                services.AddSingleton<WorkflowEditorViewModel>();
                services.AddSingleton<StationConfigViewModel>();
                services.AddSingleton<BarcodeRuleViewModel>();
                services.AddSingleton<DeviceRunTraceViewModel>();
                services.AddSingleton<ExecutionHistoryViewModel>();
                services.AddSingleton<PlanListView>();
                services.AddSingleton<WorkflowEditorView>();
                services.AddSingleton<StationConfigView>();
                services.AddSingleton<BarcodeRuleView>();
                services.AddSingleton<ExecutionHistoryView>();
                services.AddSingleton<DeviceRunTraceView>();
                services.AddTransient<EmployeeLoginWindow>();
                services.AddTransient<ScannerSettingsWindow>();
                services.AddTransient<Func<ScannerSettingsWindow>>(provider => () => provider.GetRequiredService<ScannerSettingsWindow>());
                services.AddSingleton(_ => new MainShellViewModel(context.Configuration["AppMode"]));
                services.AddSingleton<MainWindow>();
            });
    }

    private static bool IsSerialMode(string? appMode)
    {
        return string.Equals(appMode, "Serial", StringComparison.OrdinalIgnoreCase)
            || string.Equals(appMode, "ModbusRtu", StringComparison.OrdinalIgnoreCase)
            || string.Equals(appMode, "Production", StringComparison.OrdinalIgnoreCase);
    }

    private static string? ResolveRegisterAddress(DeviceModelCatalog deviceModelCatalog, DeviceAddress address)
    {
        var logicalKey = address.EndpointName.Contains(':', StringComparison.Ordinal)
            ? address.EndpointName
            : $"{address.Source}:{address.EndpointName}";

        return deviceModelCatalog.LogicalPoints
            .FirstOrDefault(point => point.LogicalKey.Equals(logicalKey, StringComparison.OrdinalIgnoreCase))
            ?.RegisterAddress;
    }

    private static SerialBarcodeInputOptions CreateScannerOptions(IConfiguration configuration)
    {
        var section = configuration.GetSection("Scanner");
        return new SerialBarcodeInputOptions
        {
            Enabled = ParseBool(section["Enabled"], defaultValue: false),
            PortName = string.IsNullOrWhiteSpace(section["PortName"]) ? "COM3" : section["PortName"]!,
            BaudRate = ParseInt(section["BaudRate"], defaultValue: 9600),
            NewLine = string.IsNullOrEmpty(section["NewLine"]) ? "\r\n" : section["NewLine"]!
        };
    }

    private static bool ParseBool(string? value, bool defaultValue)
    {
        return bool.TryParse(value, out var parsed) ? parsed : defaultValue;
    }

    private static int ParseInt(string? value, int defaultValue)
    {
        return int.TryParse(value, out var parsed) ? parsed : defaultValue;
    }
}
