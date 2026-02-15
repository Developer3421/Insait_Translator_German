using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Insait_Translator_Deutsch.Localization;

namespace Insait_Translator_Deutsch.Views;

public partial class UserAgreementWindow : Window
{
    public UserAgreementWindow()
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
        Title = strings.UserAgreementTitle;
        
        // Title bar
        if (this.FindControl<TextBlock>("TitleBarText") is TextBlock titleBarText)
            titleBarText.Text = strings.UserAgreementTitle;
        
        // Header
        if (this.FindControl<TextBlock>("HeaderText") is TextBlock headerText)
            headerText.Text = strings.UserAgreementTitle;
        if (this.FindControl<TextBlock>("SubheaderText") is TextBlock subheaderText)
            subheaderText.Text = strings.UserAgreementSubheader;
        
        // Section 1: Data Privacy
        if (this.FindControl<TextBlock>("Section1TitleText") is TextBlock section1Title)
            section1Title.Text = strings.UserAgreementPrivacyTitle;
        if (this.FindControl<TextBlock>("Section1Text") is TextBlock section1Text)
            section1Text.Text = strings.UserAgreementPrivacyText;
        
        // Section 2: Local Storage
        if (this.FindControl<TextBlock>("Section2TitleText") is TextBlock section2Title)
            section2Title.Text = strings.UserAgreementLocalStorageTitle;
        if (this.FindControl<TextBlock>("Section2Text") is TextBlock section2Text)
            section2Text.Text = strings.UserAgreementLocalStorageText;
        
        // Section 3: Translation Providers
        if (this.FindControl<TextBlock>("Section3TitleText") is TextBlock section3Title)
            section3Title.Text = strings.UserAgreementProvidersTitle;
        if (this.FindControl<TextBlock>("Section3Text") is TextBlock section3Text)
            section3Text.Text = strings.UserAgreementProvidersText;
        
        // Section 4: Responsibility
        if (this.FindControl<TextBlock>("Section4TitleText") is TextBlock section4Title)
            section4Title.Text = strings.UserAgreementResponsibilityTitle;
        if (this.FindControl<TextBlock>("Section4Text") is TextBlock section4Text)
            section4Text.Text = strings.UserAgreementResponsibilityText;
        
        // Section 5: Agreement
        if (this.FindControl<TextBlock>("Section5TitleText") is TextBlock section5Title)
            section5Title.Text = strings.UserAgreementConsentTitle;
        if (this.FindControl<TextBlock>("Section5Text") is TextBlock section5Text)
            section5Text.Text = strings.UserAgreementConsentText;
        
        // Accept button
        if (this.FindControl<TextBlock>("AcceptButtonText") is TextBlock acceptButtonText)
            acceptButtonText.Text = strings.UserAgreementAccept;
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

