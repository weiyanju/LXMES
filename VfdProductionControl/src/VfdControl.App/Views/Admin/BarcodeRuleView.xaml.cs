using System.Windows.Controls;
using VfdControl.Presentation.Admin;

namespace VfdControl.App.Views.Admin;

public partial class BarcodeRuleView : UserControl
{
    public BarcodeRuleView(BarcodeRuleViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
