using CommunityToolkit.Mvvm.ComponentModel;

namespace VfdControl.Presentation.Shell;

public sealed partial class NavigationItem : ObservableObject
{
    [ObservableProperty]
    private bool _isSelected;

    public NavigationItem(string viewKey, string title)
    {
        ViewKey = viewKey;
        Title = title;
    }

    public string ViewKey { get; }

    public string Title { get; }
}
