using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Insait_Translator_Deutsch.Localization;

public class LocalizationManager : INotifyPropertyChanged
{
    private static LocalizationManager? _instance;
    public static LocalizationManager Instance => _instance ??= new LocalizationManager();

    private string _currentLanguage = "uk"; // Default to Ukrainian

    public event PropertyChangedEventHandler? PropertyChanged;

    public string CurrentLanguage
    {
        get => _currentLanguage;
        set
        {
            if (_currentLanguage != value)
            {
                _currentLanguage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Strings));
                LanguageChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public event EventHandler? LanguageChanged;

    public LocalizedStrings Strings => GetStrings(CurrentLanguage);

    public static readonly Dictionary<string, string> AvailableLanguages = new()
    {
        { "uk", "Українська" },
        { "en", "English" },
        { "de", "Deutsch" }
    };

    private LocalizedStrings GetStrings(string languageCode)
    {
        return languageCode switch
        {
            "en" => EnglishStrings,
            "de" => GermanStrings,
            _ => UkrainianStrings
        };
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #region Ukrainian Strings
    private static readonly LocalizedStrings UkrainianStrings = new()
    {
        // Window
        WindowTitle = "Insait Перекладач Deutsch",
        TitleBarText = "🌍 Insait Перекладач 🇩🇪",
        
        // Toolbar
        Open = "Відкрити",
        Clear = "Очистити",
        About = "Про додаток",
        Language = "Мова",
        
        // Main area
        AppTitle = "Переклад на німецьку",
        Ukrainian = "Мова тексту",
        German = "Deutsch",
        EnterTextPlaceholder = "Введіть текст для перекладу...",
        TranslationPlaceholder = "Переклад німецькою з'явиться тут...",
        
        // Tooltips
        OpenFileTooltip = "Відкрити текстовий файл",
        ClearTooltip = "Очистити все",
        AboutTooltip = "Про додаток",
        LanguageTooltip = "Вибрати мову інтерфейсу",
        TranslateTooltip = "Перекласти",
        SpeakUkrainianTooltip = "Прослухати вихідний текст",
        SpeakGermanTooltip = "Прослухати німецькою",
        PasteTooltip = "Вставити з буфера",
        CopyTooltip = "Копіювати переклад",
        SaveTextTooltip = "Зберегти як текстовий файл",
        SaveMp3Tooltip = "Зберегти як MP3",
        
        // Status bar
        CharactersFormat = "{0} символів",
        TranslateShortcut = "Перекласти",
        SaveShortcut = "Зберегти",
        
        // Statuses
        Ready = "Готово",
        Translating = "Переклад...",
        Translated = "Перекладено",
        Cleared = "Очищено",
        FileLoaded = "Файл завантажено",
        PastedFromClipboard = "Вставлено з буфера",
        CopiedToClipboard = "Скопійовано в буфер",
        SavedAsText = "Збережено як текстовий файл",
        EnterTextToTranslate = "Введіть текст для перекладу",
        NoTextToSpeak = "Немає тексту для озвучення",
        UkrainianSpeechNotSupported = "Озвучення вихідного тексту поки не підтримується",
        
        // Dialogs
        OpenTextFileTitle = "Відкрити текстовий файл",
        SaveTranslationTitle = "Зберегти переклад",
        SaveAsMp3Title = "Зберегти як MP3",
        TextFiles = "Текстові файли",
        AllFiles = "Всі файли",
        TextFile = "Текстовий файл",
        Mp3Audio = "MP3 аудіо",
        
        // Language dialog
        SelectLanguageTitle = "Вибір мови",
        SelectLanguageDescription = "Виберіть мову інтерфейсу:",
        Apply = "Застосувати",
        Cancel = "Скасувати"
    };
    #endregion

    #region English Strings
    private static readonly LocalizedStrings EnglishStrings = new()
    {
        // Window
        WindowTitle = "Insait Translator Deutsch",
        TitleBarText = "🌍 Insait Translator 🇩🇪",
        
        // Toolbar
        Open = "Open",
        Clear = "Clear",
        About = "About",
        Language = "Language",
        
        // Main area
        AppTitle = "Translate to German",
        Ukrainian = "Source Language",
        German = "German",
        EnterTextPlaceholder = "Enter text to translate...",
        TranslationPlaceholder = "German translation will appear here...",
        
        // Tooltips
        OpenFileTooltip = "Open text file",
        ClearTooltip = "Clear all",
        AboutTooltip = "About application",
        LanguageTooltip = "Select interface language",
        TranslateTooltip = "Translate",
        SpeakUkrainianTooltip = "Listen to source text",
        SpeakGermanTooltip = "Listen in German",
        PasteTooltip = "Paste from clipboard",
        CopyTooltip = "Copy translation",
        SaveTextTooltip = "Save as text file",
        SaveMp3Tooltip = "Save as MP3",
        
        // Status bar
        CharactersFormat = "{0} characters",
        TranslateShortcut = "Translate",
        SaveShortcut = "Save",
        
        // Statuses
        Ready = "Ready",
        Translating = "Translating...",
        Translated = "Translated",
        Cleared = "Cleared",
        FileLoaded = "File loaded",
        PastedFromClipboard = "Pasted from clipboard",
        CopiedToClipboard = "Copied to clipboard",
        SavedAsText = "Saved as text file",
        EnterTextToTranslate = "Enter text to translate",
        NoTextToSpeak = "No text to speak",
        UkrainianSpeechNotSupported = "Source text speech is not yet supported",
        
        // Dialogs
        OpenTextFileTitle = "Open text file",
        SaveTranslationTitle = "Save translation",
        SaveAsMp3Title = "Save as MP3",
        TextFiles = "Text files",
        AllFiles = "All files",
        TextFile = "Text file",
        Mp3Audio = "MP3 audio",
        
        // Language dialog
        SelectLanguageTitle = "Language Selection",
        SelectLanguageDescription = "Select interface language:",
        Apply = "Apply",
        Cancel = "Cancel"
    };
    #endregion

    #region German Strings
    private static readonly LocalizedStrings GermanStrings = new()
    {
        // Window
        WindowTitle = "Insait Übersetzer",
        TitleBarText = "Insait Übersetzer → Deutsch",
        
        // Toolbar
        Open = "Öffnen",
        Clear = "Löschen",
        About = "Über",
        Language = "Sprache",
        
        // Main area
        AppTitle = "Übersetzung ins Deutsche",
        Ukrainian = "Ausgangssprache",
        German = "Deutsch",
        EnterTextPlaceholder = "Text zum Übersetzen eingeben...",
        TranslationPlaceholder = "Die deutsche Übersetzung wird hier angezeigt...",
        
        // Tooltips
        OpenFileTooltip = "Textdatei öffnen",
        ClearTooltip = "Alles löschen",
        AboutTooltip = "Über die Anwendung",
        LanguageTooltip = "Oberflächensprache wählen",
        TranslateTooltip = "Übersetzen",
        SpeakUkrainianTooltip = "Ausgangstext anhören",
        SpeakGermanTooltip = "Auf Deutsch anhören",
        PasteTooltip = "Aus Zwischenablage einfügen",
        CopyTooltip = "Übersetzung kopieren",
        SaveTextTooltip = "Als Textdatei speichern",
        SaveMp3Tooltip = "Als MP3 speichern",
        
        // Status bar
        CharactersFormat = "{0} Zeichen",
        TranslateShortcut = "Übersetzen",
        SaveShortcut = "Speichern",
        
        // Statuses
        Ready = "Bereit",
        Translating = "Übersetze...",
        Translated = "Übersetzt",
        Cleared = "Gelöscht",
        FileLoaded = "Datei geladen",
        PastedFromClipboard = "Aus Zwischenablage eingefügt",
        CopiedToClipboard = "In Zwischenablage kopiert",
        SavedAsText = "Als Textdatei gespeichert",
        EnterTextToTranslate = "Text zum Übersetzen eing geben",
        NoTextToSpeak = "Kein Text zum Vorlesen",
        UkrainianSpeechNotSupported = "Ausgangstext-Sprache wird noch nicht unterstützt",
        
        // Dialogs
        OpenTextFileTitle = "Textdatei öffnen",
        SaveTranslationTitle = "Übersetzung speichern",
        SaveAsMp3Title = "Als MP3 speichern",
        TextFiles = "Textdateien",
        AllFiles = "Alle Dateien",
        TextFile = "Textdatei",
        Mp3Audio = "MP3-Audio",
        
        // Language dialog
        SelectLanguageTitle = "Sprachauswahl",
        SelectLanguageDescription = "Oberflächensprache wählen:",
        Apply = "Anwenden",
        Cancel = "Abbrechen"
    };
    #endregion
}

public class LocalizedStrings
{
    // Window
    public string WindowTitle { get; init; } = "";
    public string TitleBarText { get; init; } = "";
    
    // Toolbar
    public string Open { get; init; } = "";
    public string Clear { get; init; } = "";
    public string About { get; init; } = "";
    public string Language { get; init; } = "";
    
    // Main area
    public string AppTitle { get; init; } = "";
    public string Ukrainian { get; init; } = "";
    public string German { get; init; } = "";
    public string EnterTextPlaceholder { get; init; } = "";
    public string TranslationPlaceholder { get; init; } = "";
    
    // Tooltips
    public string OpenFileTooltip { get; init; } = "";
    public string ClearTooltip { get; init; } = "";
    public string AboutTooltip { get; init; } = "";
    public string LanguageTooltip { get; init; } = "";
    public string TranslateTooltip { get; init; } = "";
    public string SpeakUkrainianTooltip { get; init; } = "";
    public string SpeakGermanTooltip { get; init; } = "";
    public string PasteTooltip { get; init; } = "";
    public string CopyTooltip { get; init; } = "";
    public string SaveTextTooltip { get; init; } = "";
    public string SaveMp3Tooltip { get; init; } = "";
    
    // Status bar
    public string CharactersFormat { get; init; } = "";
    public string TranslateShortcut { get; init; } = "";
    public string SaveShortcut { get; init; } = "";
    
    // Statuses
    public string Ready { get; init; } = "";
    public string Translating { get; init; } = "";
    public string Translated { get; init; } = "";
    public string Cleared { get; init; } = "";
    public string FileLoaded { get; init; } = "";
    public string PastedFromClipboard { get; init; } = "";
    public string CopiedToClipboard { get; init; } = "";
    public string SavedAsText { get; init; } = "";
    public string EnterTextToTranslate { get; init; } = "";
    public string NoTextToSpeak { get; init; } = "";
    public string UkrainianSpeechNotSupported { get; init; } = "";
    
    // Dialogs
    public string OpenTextFileTitle { get; init; } = "";
    public string SaveTranslationTitle { get; init; } = "";
    public string SaveAsMp3Title { get; init; } = "";
    public string TextFiles { get; init; } = "";
    public string AllFiles { get; init; } = "";
    public string TextFile { get; init; } = "";
    public string Mp3Audio { get; init; } = "";
    
    // Language dialog
    public string SelectLanguageTitle { get; init; } = "";
    public string SelectLanguageDescription { get; init; } = "";
    public string Apply { get; init; } = "";
    public string Cancel { get; init; } = "";
}
