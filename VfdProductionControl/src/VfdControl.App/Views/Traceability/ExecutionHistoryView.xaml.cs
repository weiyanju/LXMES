using System.Windows.Controls;
using VfdControl.Presentation.Traceability;

namespace VfdControl.App.Views.Traceability;

public partial class ExecutionHistoryView : UserControl
{
    public ExecutionHistoryView(ExecutionHistoryViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
