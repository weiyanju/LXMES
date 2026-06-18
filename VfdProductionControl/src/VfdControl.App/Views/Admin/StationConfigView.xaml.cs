using System.Windows;
using System.Windows.Controls;
using VfdControl.Presentation.Admin;

namespace VfdControl.App.Views.Admin;

public partial class StationConfigView : UserControl
{
    public StationConfigView(StationConfigViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void ConfirmDeleteSlot(object sender, RoutedEventArgs args)
    {
        if (DataContext is not StationConfigViewModel viewModel
            || sender is not Button { DataContext: StationSlotRowViewModel slot })
        {
            return;
        }

        var result = MessageBox.Show(
            Window.GetWindow(this),
            $"确定删除 {slot.DisplayName} 吗？",
            "删除槽位",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (result == MessageBoxResult.Yes && viewModel.DeleteSlotCommand.CanExecute(slot))
        {
            viewModel.DeleteSlotCommand.Execute(slot);
        }
    }
}
