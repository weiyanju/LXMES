using System.Windows.Controls;
using VfdControl.Presentation.Traceability;

namespace VfdControl.App.Views.Traceability;

public partial class DeviceRunTraceView : UserControl
{
    public DeviceRunTraceView(DeviceRunTraceViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
