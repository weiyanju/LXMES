using System.Windows.Controls;
using VfdControl.Presentation.Engineering;

namespace VfdControl.App.Views.Engineering;

public partial class PlanListView : UserControl
{
    public PlanListView(PlanListViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
