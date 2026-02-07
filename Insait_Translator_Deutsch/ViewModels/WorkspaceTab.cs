using ReactiveUI;

namespace Insait_Translator_Deutsch.ViewModels;

/// <summary>
/// Browser-like workspace tab (max 3). Each workspace has its own state (text fields + selected inner tab).
/// </summary>
public class WorkspaceTab : ReactiveObject
{
    private string _title = "Workspace";
    private MainTab _selectedTab = MainTab.Translate;
    private string _ukrainianText = string.Empty;
    private string _germanText = string.Empty;

    public int Id { get; init; }

    public string Title
    {
        get => _title;
        set => this.RaiseAndSetIfChanged(ref _title, value);
    }

    public MainTab SelectedTab
    {
        get => _selectedTab;
        set => this.RaiseAndSetIfChanged(ref _selectedTab, value);
    }

    public string UkrainianText
    {
        get => _ukrainianText;
        set => this.RaiseAndSetIfChanged(ref _ukrainianText, value);
    }

    public string GermanText
    {
        get => _germanText;
        set => this.RaiseAndSetIfChanged(ref _germanText, value);
    }
}

