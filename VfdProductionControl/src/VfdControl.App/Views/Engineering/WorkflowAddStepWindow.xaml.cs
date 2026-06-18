using System.Windows;
using VfdControl.Presentation.Engineering;

namespace VfdControl.App.Views.Engineering;

public partial class WorkflowAddStepWindow : Window
{
    public WorkflowAddStepWindow()
    {
        InitializeComponent();
    }

    private void AddDelayStep(object sender, RoutedEventArgs args)
    {
        if (DataContext is WorkflowEditorViewModel viewModel)
        {
            viewModel.AddDelayStepCommand.Execute(null);
        }

        DialogResult = true;
    }

    private void AddCompareMeasurementStep(object sender, RoutedEventArgs args)
    {
        if (DataContext is WorkflowEditorViewModel viewModel)
        {
            viewModel.AddCompareMeasurementStepCommand.Execute(null);
        }

        DialogResult = true;
    }

    private void AddLogicalPointStep(object sender, RoutedEventArgs args)
    {
        if (DataContext is WorkflowEditorViewModel viewModel)
        {
            var stepCount = viewModel.Steps.Count;
            viewModel.AddDraftLogicalPointStepCommand.Execute(null);
            if (viewModel.Steps.Count > stepCount)
            {
                DialogResult = true;
            }
        }
    }

    private void Cancel(object sender, RoutedEventArgs args)
    {
        DialogResult = false;
    }
}
