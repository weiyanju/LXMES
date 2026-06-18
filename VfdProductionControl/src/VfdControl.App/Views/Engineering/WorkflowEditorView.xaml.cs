using System.Windows.Controls;
using System.Windows;
using VfdControl.Presentation.Engineering;

namespace VfdControl.App.Views.Engineering;

public partial class WorkflowEditorView : UserControl
{
    private const double NarrowLayoutThreshold = 1080;

    public WorkflowEditorView(WorkflowEditorViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void OpenAddStepDialog(object sender, RoutedEventArgs args)
    {
        var dialog = new WorkflowAddStepWindow
        {
            Owner = Window.GetWindow(this),
            DataContext = DataContext
        };

        dialog.ShowDialog();
    }

    private void WorkflowEditorRootSizeChanged(object sender, SizeChangedEventArgs args)
    {
        ApplyResponsiveLayout(args.NewSize.Width);
    }

    private void ApplyResponsiveLayout(double width)
    {
        if (width < NarrowLayoutThreshold)
        {
            UseStackedLayout();
            return;
        }

        UseSideBySideLayout();
    }

    private void UseSideBySideLayout()
    {
        Grid.SetRow(WorkflowHeaderActions, 0);
        Grid.SetColumn(WorkflowHeaderActions, 1);
        Grid.SetColumnSpan(WorkflowHeaderActions, 1);
        WorkflowHeaderActions.Margin = new Thickness(0);

        Grid.SetRow(WorkflowStepGrid, 0);
        Grid.SetColumn(WorkflowStepGrid, 0);
        Grid.SetColumnSpan(WorkflowStepGrid, 1);

        Grid.SetRow(StepPropertyPanel, 0);
        Grid.SetColumn(StepPropertyPanel, 2);
        Grid.SetColumnSpan(StepPropertyPanel, 1);

        StepPropertyGapRow.Height = new GridLength(0);
        StepPropertyGapColumn.Width = new GridLength(18);
        StepPropertyColumn.Width = new GridLength(420);
    }

    private void UseStackedLayout()
    {
        Grid.SetRow(WorkflowHeaderActions, 1);
        Grid.SetColumn(WorkflowHeaderActions, 0);
        Grid.SetColumnSpan(WorkflowHeaderActions, 2);
        WorkflowHeaderActions.Margin = new Thickness(0, 10, 0, 0);

        Grid.SetRow(WorkflowStepGrid, 0);
        Grid.SetColumn(WorkflowStepGrid, 0);
        Grid.SetColumnSpan(WorkflowStepGrid, 3);

        Grid.SetRow(StepPropertyPanel, 2);
        Grid.SetColumn(StepPropertyPanel, 0);
        Grid.SetColumnSpan(StepPropertyPanel, 3);

        StepPropertyGapRow.Height = new GridLength(14);
        StepPropertyGapColumn.Width = new GridLength(0);
        StepPropertyColumn.Width = new GridLength(1, GridUnitType.Star);
    }
}
