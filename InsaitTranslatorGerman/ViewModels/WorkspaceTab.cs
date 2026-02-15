using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace InsaitTranslatorGerman.ViewModels;

/// <summary>
/// Browser-like workspace tab (max 3). Each workspace has its own state (text fields + selected inner tab).
/// </summary>
public class WorkspaceTab : INotifyPropertyChanged
{
    private string _title = "Workspace";
    private MainTab _selectedTab = MainTab.Translate;
    private string _ukrainianText = string.Empty;
    private string _germanText = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public int Id { get; init; }

    public string Title
    {
        get => _title;
        set
        {
            if (_title == value) return;
            _title = value;
            OnPropertyChanged();
        }
    }

    public MainTab SelectedTab
    {
        get => _selectedTab;
        set
        {
            if (_selectedTab == value) return;
            _selectedTab = value;
            OnPropertyChanged();
        }
    }

    public string UkrainianText
    {
        get => _ukrainianText;
        set
        {
            if (_ukrainianText == value) return;
            _ukrainianText = value;
            OnPropertyChanged();
        }
    }

    public string GermanText
    {
        get => _germanText;
        set
        {
            if (_germanText == value) return;
            _germanText = value;
            OnPropertyChanged();
        }
    }
}
