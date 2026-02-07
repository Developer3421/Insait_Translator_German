using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Insait_Translator_Deutsch.Localization;

namespace Insait_Translator_Deutsch.Views;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        
        // Enable window dragging on title bar
        var titleBar = this.FindControl<Grid>("TitleBar");
        if (titleBar != null)
        {
            titleBar.PointerPressed += TitleBar_PointerPressed;
        }
        
        UpdateUILanguage();
    }

    private void UpdateUILanguage()
    {
        var strings = LocalizationManager.Instance.Strings;
        Title = strings.AboutWindowTitle;
        
        // Title bar
        if (this.FindControl<TextBlock>("TitleBarText") is TextBlock titleBarText)
            titleBarText.Text = strings.AboutWindowTitle;
        
        // App name and version
        if (this.FindControl<TextBlock>("AppNameText") is TextBlock appNameText)
            appNameText.Text = strings.AboutAppName;
        if (this.FindControl<TextBlock>("VersionText") is TextBlock versionText)
            versionText.Text = strings.AboutVersion;
        
        // Description
        if (this.FindControl<TextBlock>("DescriptionTitleText") is TextBlock descTitleText)
            descTitleText.Text = strings.AboutDescriptionTitle;
        if (this.FindControl<TextBlock>("DescriptionText") is TextBlock descText)
            descText.Text = strings.AboutDescription;
        if (this.FindControl<TextBlock>("FeaturesTitleText") is TextBlock featuresTitleText)
            featuresTitleText.Text = strings.AboutFeaturesTitle;
        if (this.FindControl<TextBlock>("Feature1Text") is TextBlock feature1Text)
            feature1Text.Text = strings.AboutFeature1;
        if (this.FindControl<TextBlock>("Feature2Text") is TextBlock feature2Text)
            feature2Text.Text = strings.AboutFeature2;
        if (this.FindControl<TextBlock>("Feature3Text") is TextBlock feature3Text)
            feature3Text.Text = strings.AboutFeature3;
        if (this.FindControl<TextBlock>("Feature4Text") is TextBlock feature4Text)
            feature4Text.Text = strings.AboutFeature4;
        
        // Technologies and copyright
        if (this.FindControl<TextBlock>("TechnologiesText") is TextBlock techText)
            techText.Text = strings.AboutTechnologies;
        if (this.FindControl<TextBlock>("CopyrightText") is TextBlock copyrightText)
            copyrightText.Text = strings.AboutCopyright;
    }

    private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private void CloseButton_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OkButton_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}

