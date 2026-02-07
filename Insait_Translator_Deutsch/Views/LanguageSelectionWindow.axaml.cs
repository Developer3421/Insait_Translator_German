using Avalonia.Controls;
using Avalonia.Interactivity;
using Insait_Translator_Deutsch.Localization;
using System.Collections.Generic;
using System.Linq;

namespace Insait_Translator_Deutsch.Views;

public partial class LanguageSelectionWindow : Window
{
    private string _selectedLanguageCode;

    public LanguageSelectionWindow()
    {
        InitializeComponent();
        
        _selectedLanguageCode = LocalizationManager.Instance.CurrentLanguage;
        
        InitializeLanguageComboBox();
        UpdateUILanguage();
        
        // Subscribe to language changes for live preview
        LocalizationManager.Instance.LanguageChanged += (_, _) => UpdateUILanguage();
    }

    private void InitializeLanguageComboBox()
    {
        var languages = LocalizationManager.AvailableLanguages
            .Select(kvp => new LanguageItem(kvp.Key, kvp.Value))
            .ToList();

        LanguageComboBox.ItemsSource = languages;
        LanguageComboBox.DisplayMemberBinding = new Avalonia.Data.Binding("DisplayName");
        
        var currentItem = languages.FirstOrDefault(l => l.Code == _selectedLanguageCode);
        LanguageComboBox.SelectedItem = currentItem;
    }

    private void UpdateUILanguage()
    {
        var strings = LocalizationManager.Instance.Strings;
        
        Title = strings.SelectLanguageTitle;
        
        // Title bar
        if (this.FindControl<TextBlock>("TitleBarText") is TextBlock titleBarText)
            titleBarText.Text = $"🌐 {strings.Language}";
        
        TitleText.Text = strings.SelectLanguageTitle;
        DescriptionText.Text = strings.SelectLanguageDescription;
        ApplyButton.Content = strings.Apply;
        CancelButton.Content = strings.Cancel;
    }

    private void ApplyButton_Click(object? sender, RoutedEventArgs e)
    {
        if (LanguageComboBox.SelectedItem is LanguageItem selectedItem)
        {
            LocalizationManager.Instance.CurrentLanguage = selectedItem.Code;
        }
        Close(true);
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}

public record LanguageItem(string Code, string DisplayName);

