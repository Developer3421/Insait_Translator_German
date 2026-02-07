using System;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Insait_Translator_Deutsch.Localization;
using Insait_Translator_Deutsch.Services;

namespace Insait_Translator_Deutsch.Views;

public partial class SettingsWindow : Window
{
    private bool _isPasswordVisible;
    private readonly TranslationService _translationService;

    public SettingsWindow()
    {
        InitializeComponent();
        _translationService = new TranslationService();
        
        LoadSettings();
        UpdateUILanguage();
    }


    private void LoadSettings()
    {
        var settings = SettingsService.Instance;
        
        if (this.FindControl<CheckBox>("UseGoogleApiCheckBox") is CheckBox useGoogleApi)
            useGoogleApi.IsChecked = settings.UseGoogleApi;
        
        if (this.FindControl<TextBox>("ApiKeyTextBox") is TextBox apiKey)
            apiKey.Text = settings.GoogleApiKey;
    }

    private void UpdateUILanguage()
    {
        var strings = LocalizationManager.Instance.Strings;
        Title = strings.SettingsTitle;
        
        // Title bar
        if (this.FindControl<TextBlock>("TitleBarText") is TextBlock titleBarText)
            titleBarText.Text = strings.SettingsHeader;
        
        // Header
        if (this.FindControl<TextBlock>("HeaderText") is TextBlock headerText)
            headerText.Text = strings.SettingsHeader;
        if (this.FindControl<TextBlock>("SubheaderText") is TextBlock subheaderText)
            subheaderText.Text = strings.SettingsSubheader;
        
        // API Section
        if (this.FindControl<TextBlock>("ApiSectionTitle") is TextBlock apiSectionTitle)
            apiSectionTitle.Text = strings.GoogleGeminiApiSection;
        if (this.FindControl<TextBlock>("ApiSectionDescription") is TextBlock apiSectionDesc)
            apiSectionDesc.Text = strings.GoogleGeminiApiDescription;
        if (this.FindControl<TextBlock>("UseGoogleApiText") is TextBlock useGoogleApiText)
            useGoogleApiText.Text = strings.UseGoogleGeminiApi;
        if (this.FindControl<TextBlock>("ApiKeyLabel") is TextBlock apiKeyLabel)
            apiKeyLabel.Text = strings.ApiKey;
        if (this.FindControl<TextBox>("ApiKeyTextBox") is TextBox apiKeyTextBox)
            apiKeyTextBox.Watermark = strings.ApiKeyPlaceholder;
        if (this.FindControl<TextBlock>("TestKeyText") is TextBlock testKeyText)
            testKeyText.Text = strings.TestKey;
        if (this.FindControl<TextBlock>("GetKeyText") is TextBlock getKeyText)
            getKeyText.Text = strings.GetKey;
        if (this.FindControl<TextBlock>("DeleteKeyText") is TextBlock deleteKeyText)
            deleteKeyText.Text = strings.DeleteKey;
        
        // Provider Info
        if (this.FindControl<TextBlock>("AboutProvidersText") is TextBlock aboutProviders)
            aboutProviders.Text = strings.AboutProviders;
        
        // Provider descriptions - using TextBlocks now
        if (this.FindControl<TextBlock>("ProviderMyMemoryText") is TextBlock providerMyMemory)
            providerMyMemory.Text = strings.ProviderMyMemory;
        if (this.FindControl<TextBlock>("ProviderMyMemoryDescText") is TextBlock providerMyMemoryDesc)
            providerMyMemoryDesc.Text = strings.ProviderMyMemoryDesc;
        if (this.FindControl<TextBlock>("ProviderGoogleTranslateText") is TextBlock providerGoogleTranslate)
            providerGoogleTranslate.Text = strings.ProviderGoogleTranslate;
        if (this.FindControl<TextBlock>("ProviderGoogleTranslateDescText") is TextBlock providerGoogleTranslateDesc)
            providerGoogleTranslateDesc.Text = strings.ProviderGoogleTranslateDesc;
        if (this.FindControl<TextBlock>("ProviderGoogleGeminiText") is TextBlock providerGoogleGemini)
            providerGoogleGemini.Text = strings.ProviderGoogleGemini;
        if (this.FindControl<TextBlock>("ProviderGoogleGeminiDescText") is TextBlock providerGoogleGeminiDesc)
            providerGoogleGeminiDesc.Text = strings.ProviderGoogleGeminiDesc;
        
        // Footer buttons
        if (this.FindControl<TextBlock>("CancelButtonText") is TextBlock cancelText)
            cancelText.Text = strings.Cancel;
        if (this.FindControl<TextBlock>("SaveButtonText") is TextBlock saveText)
            saveText.Text = strings.Save;
    }


    private void ShowKey_Click(object? sender, RoutedEventArgs e)
    {
        if (this.FindControl<TextBox>("ApiKeyTextBox") is TextBox apiKeyBox &&
            this.FindControl<TextBlock>("ShowKeyIcon") is TextBlock icon)
        {
            _isPasswordVisible = !_isPasswordVisible;
            apiKeyBox.PasswordChar = _isPasswordVisible ? '\0' : '●';
            icon.Text = _isPasswordVisible ? "🙈" : "👁️";
        }
    }

    private async void TestKey_Click(object? sender, RoutedEventArgs e)
    {
        var apiKeyBox = this.FindControl<TextBox>("ApiKeyTextBox");
        var statusText = this.FindControl<TextBlock>("StatusText");
        var strings = LocalizationManager.Instance.Strings;
        
        if (apiKeyBox == null || statusText == null) return;

        var apiKey = apiKeyBox.Text;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            statusText.Text = $"⚠️ {strings.EnterApiKey}";
            statusText.Foreground = new SolidColorBrush(Color.Parse("#FF8C00"));
            return;
        }

        statusText.Text = $"🔄 {strings.Checking}";
        statusText.Foreground = new SolidColorBrush(Color.Parse("#606060"));

        try
        {
            var (success, message) = await _translationService.TestGoogleApiKeyAsync(apiKey);
            
            if (success)
            {
                statusText.Text = $"✅ {message}";
                statusText.Foreground = new SolidColorBrush(Color.Parse("#22C55E"));
            }
            else
            {
                statusText.Text = $"❌ {message}";
                statusText.Foreground = new SolidColorBrush(Color.Parse("#EF4444"));
            }
        }
        catch (Exception ex)
        {
            statusText.Text = $"❌ {ex.Message}";
            statusText.Foreground = new SolidColorBrush(Color.Parse("#EF4444"));
        }
    }

    private void GetKey_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://aistudio.google.com/apikey",
                UseShellExecute = true
            });
        }
        catch
        {
            // Fallback if browser can't be opened
        }
    }

    private void DeleteKey_Click(object? sender, RoutedEventArgs e)
    {
        var apiKeyBox = this.FindControl<TextBox>("ApiKeyTextBox");
        var useGoogleApiCheckBox = this.FindControl<CheckBox>("UseGoogleApiCheckBox");
        var statusText = this.FindControl<TextBlock>("StatusText");
        var strings = LocalizationManager.Instance.Strings;
        
        // Clear the text box
        if (apiKeyBox != null)
            apiKeyBox.Text = string.Empty;
        
        // Uncheck the Google API option
        if (useGoogleApiCheckBox != null)
            useGoogleApiCheckBox.IsChecked = false;
        
        // Delete from database
        var settings = SettingsService.Instance;
        settings.GoogleApiKey = string.Empty;
        settings.UseGoogleApi = false;
        
        // Show confirmation
        if (statusText != null)
        {
            statusText.Text = $"✅ {strings.KeyDeleted}";
            statusText.Foreground = new SolidColorBrush(Color.Parse("#22C55E"));
        }
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Save_Click(object? sender, RoutedEventArgs e)
    {
        var settings = SettingsService.Instance;
        
        if (this.FindControl<CheckBox>("UseGoogleApiCheckBox") is CheckBox useGoogleApi)
            settings.UseGoogleApi = useGoogleApi.IsChecked ?? false;
        
        if (this.FindControl<TextBox>("ApiKeyTextBox") is TextBox apiKey)
            settings.GoogleApiKey = apiKey.Text ?? string.Empty;
        
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _translationService.Dispose();
    }
}

