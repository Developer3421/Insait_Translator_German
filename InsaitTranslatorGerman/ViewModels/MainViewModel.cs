using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using InsaitTranslatorGerman.Localization;
using InsaitTranslatorGerman.Services;

namespace InsaitTranslatorGerman.ViewModels;

public class MainViewModel : ViewModelBase, IDisposable
{
    private readonly TranslationService _translationService;
    private readonly TextToSpeechService _ttsService;
    private readonly IPlatformServices _platform;

    private string _statusText = string.Empty;
    private string _currentProviderName = "Auto";
    private bool _isTranslating;
    private bool _isSpeaking;

    private readonly ObservableCollection<WorkspaceTab> _workspaces = new();
    public ReadOnlyObservableCollection<WorkspaceTab> Workspaces { get; }

    // Temporary workspace used when all tabs are closed - not persisted
    private WorkspaceTab _temporaryWorkspace = new() { Id = -1, Title = "Temporary" };
    
    private WorkspaceTab? _activeWorkspace;
    public WorkspaceTab? ActiveWorkspace
    {
        get => _activeWorkspace;
        private set
        {
            if (_activeWorkspace == value) return;
            _activeWorkspace = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanCloseWorkspace));
            OnPropertyChanged(nameof(IsTemporaryWorkspace));
            OnPropertyChanged(nameof(HasWorkspaces));
        }
    }

    private int _selectedWorkspaceIndex = -1;
    public int SelectedWorkspaceIndex
    {
        get => _selectedWorkspaceIndex;
        set
        {
            // Allow -1 when no workspaces (temporary workspace mode)
            if (_workspaces.Count == 0)
            {
                if (_selectedWorkspaceIndex != -1)
                {
                    _selectedWorkspaceIndex = -1;
                    OnPropertyChanged();
                    ActiveWorkspace = _temporaryWorkspace;
                    ApplyWorkspaceToFields(ActiveWorkspace);
                }
                return;
            }
            
            if (value < 0 || value >= _workspaces.Count)
                return;
            if (_selectedWorkspaceIndex == value) return;
            
            _selectedWorkspaceIndex = value;
            OnPropertyChanged();
            ActiveWorkspace = _workspaces[value];
            ApplyWorkspaceToFields(ActiveWorkspace);
            SaveWorkspaces();
        }
    }

    public bool CanAddWorkspace => _workspaces.Count < 3;
    public bool CanCloseWorkspace => _workspaces.Count > 0;
    
    /// <summary>
    /// True when there are no saved workspaces and showing temporary workspace.
    /// </summary>
    public bool IsTemporaryWorkspace => _workspaces.Count == 0;
    
    /// <summary>
    /// True when there are actual saved workspaces.
    /// </summary>
    public bool HasWorkspaces => _workspaces.Count > 0;

    // Commands
    public ICommand AddWorkspaceCommand { get; }
    public ICommand CloseActiveWorkspaceCommand { get; }
    public ICommand SelectWorkspaceCommand { get; }
    public ICommand CloseWorkspaceCommand { get; }
    public ICommand TranslateCommand { get; }
    public ICommand SelectTranslateTabCommand { get; }

    // Replace global SelectedTab with per-workspace
    public MainTab SelectedTab
    {
        get => ActiveWorkspace?.SelectedTab ?? MainTab.Translate;
        set
        {
            if (ActiveWorkspace == null) return;
            if (ActiveWorkspace.SelectedTab == value) return;
            
            ActiveWorkspace.SelectedTab = value;
            OnPropertyChanged();
            SaveWorkspaces();
        }
    }

    // UkrainianText / GermanText now proxy to active workspace so bindings keep working
    public string UkrainianText
    {
        get => ActiveWorkspace?.UkrainianText ?? string.Empty;
        set
        {
            if (ActiveWorkspace == null) return;
            if (ActiveWorkspace.UkrainianText == value) return;
            
            ActiveWorkspace.UkrainianText = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(UkrainianCharCount));
            OnPropertyChanged(nameof(UkrainianCharCountDisplay));
            SaveWorkspaces();
        }
    }

    public string GermanText
    {
        get => ActiveWorkspace?.GermanText ?? string.Empty;
        set
        {
            if (ActiveWorkspace == null) return;
            if (ActiveWorkspace.GermanText == value) return;
            
            ActiveWorkspace.GermanText = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(GermanCharCount));
            OnPropertyChanged(nameof(GermanCharCountDisplay));
            SaveWorkspaces();
        }
    }

    public string StatusText
    {
        get => _statusText;
        set => SetField(ref _statusText, value);
    }

    public string CurrentProviderName
    {
        get => _currentProviderName;
        set => SetField(ref _currentProviderName, value);
    }

    public bool IsTranslating
    {
        get => _isTranslating;
        set => SetField(ref _isTranslating, value);
    }

    public bool IsSpeaking
    {
        get => _isSpeaking;
        set => SetField(ref _isSpeaking, value);
    }

    public int UkrainianCharCount => UkrainianText.Length;
    public int GermanCharCount => GermanText.Length;
    
    // Localized character count display strings
    public string UkrainianCharCountDisplay
    {
        get
        {
            var strings = LocalizationManager.Instance.Strings;
            return $"{strings.Ukrainian}: {string.Format(strings.CharactersFormat, UkrainianCharCount)}";
        }
    }
    
    public string GermanCharCountDisplay
    {
        get
        {
            var strings = LocalizationManager.Instance.Strings;
            return $"{strings.German}: {string.Format(strings.CharactersFormat, GermanCharCount)}";
        }
    }

    public MainViewModel()
        : this(AppServices.Current, new TranslationService(), new TextToSpeechService())
    {
    }

    public MainViewModel(IPlatformServices platformServices)
        : this(platformServices, new TranslationService(), new TextToSpeechService())
    {
    }

    public MainViewModel(IPlatformServices platformServices, TranslationService translationService, TextToSpeechService ttsService)
    {
        _platform = platformServices;

        Workspaces = new ReadOnlyObservableCollection<WorkspaceTab>(_workspaces);

        _translationService = translationService;
        _ttsService = ttsService;

        // Initialize with localized status
        _statusText = LocalizationManager.Instance.Strings.Ready;
        
        // Initialize temporary workspace (not persisted, used when all tabs are closed)
        var strings = LocalizationManager.Instance.Strings;
        _temporaryWorkspace = new WorkspaceTab { Id = -1, Title = strings.TemporaryWorkspace };
        
        // Subscribe to language changes
        LocalizationManager.Instance.LanguageChanged += OnLanguageChanged;

        // Load persisted workspaces (max 3)
        LoadWorkspaces();

        // Initialize commands
        SelectWorkspaceCommand = new RelayCommand<int>(SelectWorkspace);
        AddWorkspaceCommand = new RelayCommand(AddWorkspace, () => CanAddWorkspace);
        CloseActiveWorkspaceCommand = new RelayCommand(CloseActiveWorkspace, () => CanCloseWorkspace);
        CloseWorkspaceCommand = new RelayCommand<int>(CloseWorkspace, _ => CanCloseWorkspace);
        TranslateCommand = new AsyncRelayCommand(TranslateAsync, () => !string.IsNullOrWhiteSpace(UkrainianText) && !IsTranslating);
        SelectTranslateTabCommand = new RelayCommand(() => { SelectedTab = MainTab.Translate; });
    }
    
    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        // Update character count displays when language changes
        OnPropertyChanged(nameof(UkrainianCharCountDisplay));
        OnPropertyChanged(nameof(GermanCharCountDisplay));
        
        // Update temporary workspace title
        var strings = LocalizationManager.Instance.Strings;
        _temporaryWorkspace.Title = strings.TemporaryWorkspace;
        
        // If status is "Ready", update it to the new language
        if (IsReadyStatus(_statusText))
        {
            StatusText = strings.Ready;
        }
    }
    
    private bool IsReadyStatus(string status)
    {
        return status == "Готовий" ||
               status == "Ready" ||
               status == "Bereit" ||
               status == "Готов" ||
               status == "Hazır";
    }

    private void LoadWorkspaces()
    {
        var (tabs, selectedIndex) = WorkspacePersistence.Load();

        if (tabs.Count == 0)
        {
            // No saved workspaces - show temporary workspace (not persisted)
            _selectedWorkspaceIndex = -1;
            _activeWorkspace = _temporaryWorkspace;
        }
        else
        {
            foreach (var t in tabs.Take(3))
                _workspaces.Add(t);
                
            _selectedWorkspaceIndex = Math.Clamp(selectedIndex, 0, _workspaces.Count - 1);
            _activeWorkspace = _workspaces[_selectedWorkspaceIndex];
        }
        
        ApplyWorkspaceToFields(_activeWorkspace);
        OnPropertyChanged(nameof(IsTemporaryWorkspace));
        OnPropertyChanged(nameof(HasWorkspaces));
    }

    private void SaveWorkspaces()
    {
        try
        {
            WorkspacePersistence.Save(_workspaces, SelectedWorkspaceIndex);
        }
        catch
        {
            // ignore persistence errors
        }

        OnPropertyChanged(nameof(CanAddWorkspace));
        OnPropertyChanged(nameof(CanCloseWorkspace));
        OnPropertyChanged(nameof(IsTemporaryWorkspace));
        OnPropertyChanged(nameof(HasWorkspaces));
        
        // Update command states
        (AddWorkspaceCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (CloseActiveWorkspaceCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (CloseWorkspaceCommand as RelayCommand<int>)?.RaiseCanExecuteChanged();
    }

    private void ApplyWorkspaceToFields(WorkspaceTab? ws)
    {
        if (ws == null) return;

        OnPropertyChanged(nameof(UkrainianText));
        OnPropertyChanged(nameof(GermanText));
        OnPropertyChanged(nameof(SelectedTab));
        OnPropertyChanged(nameof(UkrainianCharCount));
        OnPropertyChanged(nameof(GermanCharCount));
        OnPropertyChanged(nameof(UkrainianCharCountDisplay));
        OnPropertyChanged(nameof(GermanCharCountDisplay));
    }

    private void SelectWorkspace(int index)
    {
        if (index < 0 || index >= _workspaces.Count) return;
        SelectedWorkspaceIndex = index;
    }

    private void AddWorkspace()
    {
        if (!CanAddWorkspace) return;

        var strings = LocalizationManager.Instance.Strings;
        var nextId = _workspaces.Count == 0 ? 0 : _workspaces.Max(w => w.Id) + 1;
        var title = $"{strings.Workspace} {_workspaces.Count + 1}";

        _workspaces.Add(new WorkspaceTab { Id = nextId, Title = title });
        SelectedWorkspaceIndex = _workspaces.Count - 1;

        SaveWorkspaces();
    }

    private void CloseActiveWorkspace()
    {
        if (!CanCloseWorkspace) return;
        
        // If in temporary workspace mode, there's nothing to close
        if (IsTemporaryWorkspace) return;
        
        if (ActiveWorkspace == null) return;

        var index = _workspaces.IndexOf(ActiveWorkspace);
        if (index < 0) return;

        CloseWorkspace(index);
    }

    private void CloseWorkspace(int index)
    {
        if (!CanCloseWorkspace || index < 0 || index >= _workspaces.Count) return;

        _workspaces.RemoveAt(index);

        if (_workspaces.Count == 0)
        {
            // Switch to temporary workspace - content will not be persisted
            _selectedWorkspaceIndex = -1;
            _activeWorkspace = _temporaryWorkspace;
            // Reset temporary workspace content
            _temporaryWorkspace.UkrainianText = string.Empty;
            _temporaryWorkspace.GermanText = string.Empty;
        }
        else
        {
            // Select the previous tab, or stay at current if it's still valid
            int newIndex;
            if (_selectedWorkspaceIndex >= _workspaces.Count)
                newIndex = _workspaces.Count - 1;
            else if (_selectedWorkspaceIndex > index)
                newIndex = _selectedWorkspaceIndex - 1;
            else if (_selectedWorkspaceIndex == index)
                newIndex = Math.Max(0, index - 1);
            else
                newIndex = _selectedWorkspaceIndex;

            _selectedWorkspaceIndex = newIndex;
            _activeWorkspace = _workspaces[newIndex];
        }
        
        OnPropertyChanged(nameof(SelectedWorkspaceIndex));
        OnPropertyChanged(nameof(ActiveWorkspace));
        ApplyWorkspaceToFields(_activeWorkspace);
        SaveWorkspaces();
    }

    public async Task TranslateAsync()
    {
        var strings = LocalizationManager.Instance.Strings;
        
        if (string.IsNullOrWhiteSpace(UkrainianText))
        {
            StatusText = strings.EnterTextToTranslate;
            return;
        }

        IsTranslating = true;
        (TranslateCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        StatusText = strings.Translating;

        try
        {
            var result = await _translationService.TranslateWithDetailsAsync(UkrainianText);
            GermanText = result.Text;
            CurrentProviderName = result.ProviderName;
            
            if (result.WasFallback)
            {
                StatusText = $"{strings.Translated} {string.Format(strings.MyMemoryExhaustedFallback, "MyMemory", result.ProviderName)}";
            }
            else
            {
                StatusText = strings.Translated;
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
        finally
        {
            IsTranslating = false;
            (TranslateCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        }
    }

    /// <summary>
    /// Translates text specifically using Google Gemini API
    /// </summary>
    public async Task TranslateWithGoogleApiAsync()
    {
        var strings = LocalizationManager.Instance.Strings;
        
        if (string.IsNullOrWhiteSpace(UkrainianText))
        {
            StatusText = strings.EnterTextToTranslate;
            return;
        }

        var settings = SettingsService.Instance;
        if (string.IsNullOrEmpty(settings.GoogleApiKey))
        {
            StatusText = strings.GoogleApiKeyNotConfigured;
            return;
        }

        IsTranslating = true;
        StatusText = strings.TranslatingViaGoogleApi;

        try
        {
            var originalUseGoogleApi = settings.UseGoogleApi;
            settings.UseGoogleApi = true;
            
            try
            {
                var result = await _translationService.TranslateWithDetailsAsync(UkrainianText);
                GermanText = result.Text;
                CurrentProviderName = "Google Gemini API";
                StatusText = strings.TranslatedViaGoogleApi;
            }
            finally
            {
                settings.UseGoogleApi = originalUseGoogleApi;
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Gemini API Error: {ex.Message}";
        }
        finally
        {
            IsTranslating = false;
        }
    }

    public async Task SpeakGermanAsync()
    {
        var strings = LocalizationManager.Instance.Strings;
        
        if (string.IsNullOrWhiteSpace(GermanText))
        {
            StatusText = strings.NoTextToSpeak;
            return;
        }

        if (IsSpeaking)
            return;

        IsSpeaking = true;

        try
        {
            await _ttsService.SpeakAsync(GermanText, status => StatusText = status);
        }
        catch (Exception ex)
        {
            StatusText = $"TTS Error: {ex.Message}";
        }
        finally
        {
            IsSpeaking = false;
        }
    }

    public async Task SaveToMp3Async(string filePath)
    {
        var strings = LocalizationManager.Instance.Strings;
        
        if (string.IsNullOrWhiteSpace(GermanText))
        {
            StatusText = strings.NoTextToSpeak;
            return;
        }

        try
        {
            await _ttsService.SaveToMp3Async(GermanText, filePath, status => StatusText = status);
        }
        catch (Exception ex)
        {
            StatusText = $"MP3 Error: {ex.Message}";
        }
    }

    public async Task InitializeTtsAsync()
    {
        try
        {
            await _ttsService.InitializeAsync(status => StatusText = status);
        }
        catch (Exception ex)
        {
            StatusText = $"TTS Init Error: {ex.Message}";
        }
    }

    public void OpenMyMemoryInBrowser()
    {
        _platform.OpenUrl.Open(new Uri("https://mymemory.translated.net/"));
        var strings = LocalizationManager.Instance.Strings;
        _platform.Notifications.Notify(strings.WindowTitle, "MyMemory");
    }

    public void Dispose()
    {
        LocalizationManager.Instance.LanguageChanged -= OnLanguageChanged;
        SaveWorkspaces();
        _translationService.Dispose();
        _ttsService.Dispose();
    }
}

// Simple RelayCommand implementation
public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    public void Execute(object? parameter) => _execute();

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

public class RelayCommand<T> : ICommand
{
    private readonly Action<T> _execute;
    private readonly Func<T, bool>? _canExecute;

    public RelayCommand(Action<T> execute, Func<T, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter)
    {
        if (parameter is T typedParam)
            return _canExecute?.Invoke(typedParam) ?? true;
        return _canExecute?.Invoke(default!) ?? true;
    }

    public void Execute(object? parameter)
    {
        if (parameter is T typedParam)
            _execute(typedParam);
        else if (parameter is int intParam && typeof(T) == typeof(int))
            _execute((T)(object)intParam);
    }

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

public class AsyncRelayCommand : ICommand
{
    private readonly Func<Task> _execute;
    private readonly Func<bool>? _canExecute;
    private bool _isExecuting;

    public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => !_isExecuting && (_canExecute?.Invoke() ?? true);

    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter)) return;

        _isExecuting = true;
        RaiseCanExecuteChanged();

        try
        {
            await _execute();
        }
        finally
        {
            _isExecuting = false;
            RaiseCanExecuteChanged();
        }
    }

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
