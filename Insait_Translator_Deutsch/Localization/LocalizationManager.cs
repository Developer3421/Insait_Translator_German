using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Insait_Translator_Deutsch.Services;

namespace Insait_Translator_Deutsch.Localization;

public class LocalizationManager : INotifyPropertyChanged
{
    private static LocalizationManager? _instance;
    public static LocalizationManager Instance => _instance ??= new LocalizationManager();

    private string _currentLanguage;

    public event PropertyChangedEventHandler? PropertyChanged;

    public LocalizationManager()
    {
        // Load saved language from settings database
        _currentLanguage = SettingsService.Instance.InterfaceLanguage;
    }

    public string CurrentLanguage
    {
        get => _currentLanguage;
        set
        {
            if (_currentLanguage != value)
            {
                _currentLanguage = value;
                // Save to settings database
                SettingsService.Instance.InterfaceLanguage = value;
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
        { "de", "Deutsch" },
        { "ru", "Русский" },
        { "tr", "Türkçe" }
    };

    private LocalizedStrings GetStrings(string languageCode)
    {
        return languageCode switch
        {
            "en" => EnglishStrings,
            "de" => GermanStrings,
            "ru" => RussianStrings,
            "tr" => TurkishStrings,
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

        // Tabs
        TabTranslate = "Переклад",
        TabHistory = "Історія",
        TabSettings = "Налаштування",
        
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
        Cancel = "Скасувати",
        
        // About window
        AboutWindowTitle = "ℹ️ Про додаток",
        AboutAppName = "🌍 Insait Перекладач → Deutsch",
        AboutVersion = "Версія 1.0.0",
        AboutDescriptionTitle = "Універсальний Перекладач на Deutsch",
        AboutDescription = "Цей додаток дозволяє легко перекладати тексти з будь-якої мови на німецьку та озвучувати переклад.",
        AboutFeaturesTitle = "Основні можливості:",
        AboutFeature1 = "• Переклад тексту з будь-якої мови на німецьку",
        AboutFeature2 = "• Озвучення перекладу (Text-to-Speech)",
        AboutFeature3 = "• Збереження перекладу як текст або MP3",
        AboutFeature4 = "• Робота з файлами та буфером обміну",
        AboutTechnologies = "Технології:",
        AboutCopyright = "© 2026 Insait. Всі права захищено.",
        
        // Workspaces
        Workspace = "Робоча область",
        AddWorkspaceTooltip = "Додати нову робочу область",
        CloseWorkspaceTooltip = "Закрити вкладку",
        
        // Settings
        Settings = "Налаштування",
        SettingsTooltip = "Налаштування перекладача",
        SettingsTitle = "Налаштування",
        TranslateWithGoogleTooltip = "Перекласти через Google Gemini API",
        SettingsHeader = "⚙️ Налаштування",
        SettingsSubheader = "Налаштуйте параметри перекладача",
        GoogleGeminiApiSection = "Google Gemini API",
        GoogleGeminiApiDescription = "Google Gemini забезпечує якісний AI-переклад. Отримайте API ключ на aistudio.google.com",
        UseGoogleGeminiApi = "Використовувати Google Gemini API",
        ApiKey = "API Ключ:",
        ApiKeyPlaceholder = "Введіть ваш Google Gemini API ключ...",
        TestKey = "Перевірити ключ",
        GetKey = "Отримати ключ",
        DeleteKey = "Видалити ключ",
        KeyDeleted = "API ключ видалено",
        EnterApiKey = "Введіть API ключ",
        Checking = "Перевірка...",
        AboutProviders = "ℹ️ Про провайдери перекладу",
        ProviderMyMemory = "MyMemory",
        ProviderMyMemoryDesc = "— безкоштовний, ліміт 10,000 символів/день",
        ProviderGoogleTranslate = "Google Translate",
        ProviderGoogleTranslateDesc = "— резервний при вичерпанні MyMemory",
        ProviderGoogleGemini = "Google Gemini API",
        ProviderGoogleGeminiDesc = "— якісний AI-переклад, потребує API ключ (безкоштовний рівень)",
        Save = "Зберегти",
        
        // Google API status messages
        GoogleApiKeyNotConfigured = "⚠️ Google API ключ не налаштовано. Відкрийте Налаштування.",
        TranslatingViaGoogleApi = "Переклад через Google Cloud API...",
        TranslatedViaGoogleApi = "✓ Перекладено через Google Cloud API",
        MyMemoryExhaustedFallback = "({0} вичерпано → {1})",
        
        // TTS messages
        TtsAvailableOnlyInNative = "TTS доступний лише в нативному додатку",
        Mp3GeneratedInNative = "MP3 генерується в нативному додатку",
        TtsPlatformNotSupported = "Piper TTS наразі підтримується лише на Windows",
        TtsDownloadingPiper = "Завантаження Piper TTS (~15 MB)...",
        TtsDownloadingPiperProgress = "Завантаження Piper TTS... {0}%",
        TtsExtractingPiper = "Розпаковка Piper...",
        TtsDownloadingVoiceModel = "Завантаження німецької моделі голосу (~65 MB)...",
        TtsDownloadingVoiceModelProgress = "Завантаження моделі голосу... {0}%",
        TtsDownloadingModelConfig = "Завантаження конфігурації моделі...",
        TtsPiperNotFound = "Piper executable не знайдено: {0}",
        TtsReady = "TTS готовий",
        TtsInitError = "Помилка ініціалізації TTS: {0}",
        TtsNotInitialized = "TTS не ініціалізовано",
        TtsAudioFileNotCreated = "Piper не створив аудіо файл",
        TtsGeneratingSpeech = "Генерація мовлення...",
        TtsEmptyAudioError = "Помилка: порожнє аудіо",
        TtsPlaying = "Відтворення...",
        TtsTextCannotBeEmpty = "Текст не може бути порожнім",
        TtsGeneratingAudio = "Генерація аудіо з тексту...",
        TtsCouldNotGenerateAudio = "Не вдалося згенерувати аудіо",
        TtsConvertingToMp3 = "Конвертація в MP3...",
        TtsSavedAs = "Збережено: {0}"
    };
    #endregion

    #region English Strings
    private static readonly LocalizedStrings EnglishStrings = new()
    {
        // Window
        WindowTitle = "Insait Translator Deutsch",
        TitleBarText = "🌍 Insait Translator 🇩🇪",

        // Tabs
        TabTranslate = "Translate",
        TabHistory = "History",
        TabSettings = "Settings",
        
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
        Cancel = "Cancel",
        
        // About window
        AboutWindowTitle = "ℹ️ About",
        AboutAppName = "🌍 Insait Translator → Deutsch",
        AboutVersion = "Version 1.0.0",
        AboutDescriptionTitle = "Universal Translator to German",
        AboutDescription = "This app allows you to easily translate text from any language to German and listen to the translation.",
        AboutFeaturesTitle = "Key features:",
        AboutFeature1 = "• Translate text from any language to German",
        AboutFeature2 = "• Text-to-Speech playback",
        AboutFeature3 = "• Save translation as text or MP3",
        AboutFeature4 = "• Work with files and clipboard",
        AboutTechnologies = "Technologies:",
        AboutCopyright = "© 2026 Insait. All rights reserved.",
        
        // Workspaces
        Workspace = "Workspace",
        AddWorkspaceTooltip = "Add new workspace",
        CloseWorkspaceTooltip = "Close tab",
        
        // Settings
        Settings = "Settings",
        SettingsTooltip = "Translator settings",
        SettingsTitle = "Settings",
        TranslateWithGoogleTooltip = "Translate with Google Gemini API",
        SettingsHeader = "⚙️ Settings",
        SettingsSubheader = "Configure translator options",
        GoogleGeminiApiSection = "Google Gemini API",
        GoogleGeminiApiDescription = "Google Gemini provides high-quality AI translation. Get your API key at aistudio.google.com",
        UseGoogleGeminiApi = "Use Google Gemini API",
        ApiKey = "API Key:",
        ApiKeyPlaceholder = "Enter your Google Gemini API key...",
        TestKey = "Test key",
        GetKey = "Get key",
        DeleteKey = "Delete key",
        KeyDeleted = "API key deleted",
        EnterApiKey = "Enter API key",
        Checking = "Checking...",
        AboutProviders = "ℹ️ About translation providers",
        ProviderMyMemory = "MyMemory",
        ProviderMyMemoryDesc = "— free, limit 10,000 characters/day",
        ProviderGoogleTranslate = "Google Translate",
        ProviderGoogleTranslateDesc = "— fallback when MyMemory is exhausted",
        ProviderGoogleGemini = "Google Gemini API",
        ProviderGoogleGeminiDesc = "— high-quality AI translation, requires API key (free tier available)",
        Save = "Save",
        
        // Google API status messages
        GoogleApiKeyNotConfigured = "⚠️ Google API key not configured. Open Settings.",
        TranslatingViaGoogleApi = "Translating via Google Cloud API...",
        TranslatedViaGoogleApi = "✓ Translated via Google Cloud API",
        MyMemoryExhaustedFallback = "({0} exhausted → {1})",
        
        // TTS messages
        TtsAvailableOnlyInNative = "TTS is only available in the native app",
        Mp3GeneratedInNative = "MP3 is generated in the native app",
        TtsPlatformNotSupported = "Piper TTS is currently only supported on Windows",
        TtsDownloadingPiper = "Downloading Piper TTS (~15 MB)...",
        TtsDownloadingPiperProgress = "Downloading Piper TTS... {0}%",
        TtsExtractingPiper = "Extracting Piper...",
        TtsDownloadingVoiceModel = "Downloading German voice model (~65 MB)...",
        TtsDownloadingVoiceModelProgress = "Downloading voice model... {0}%",
        TtsDownloadingModelConfig = "Downloading model configuration...",
        TtsPiperNotFound = "Piper executable not found: {0}",
        TtsReady = "TTS ready",
        TtsInitError = "TTS initialization error: {0}",
        TtsNotInitialized = "TTS not initialized",
        TtsAudioFileNotCreated = "Piper did not create audio file",
        TtsGeneratingSpeech = "Generating speech...",
        TtsEmptyAudioError = "Error: empty audio",
        TtsPlaying = "Playing...",
        TtsTextCannotBeEmpty = "Text cannot be empty",
        TtsGeneratingAudio = "Generating audio from text...",
        TtsCouldNotGenerateAudio = "Could not generate audio",
        TtsConvertingToMp3 = "Converting to MP3...",
        TtsSavedAs = "Saved: {0}"
    };
    #endregion

    #region German Strings
    private static readonly LocalizedStrings GermanStrings = new()
    {
        // Window
        WindowTitle = "Insait Übersetzer",
        TitleBarText = "Insait Übersetzer → Deutsch",

        // Tabs
        TabTranslate = "Übersetzen",
        TabHistory = "Verlauf",
        TabSettings = "Einstellungen",
        
        // Toolbar
        Open = "Öffnen",
        Clear = "Löschen",
        About = "Über",
        Language = "Sprache",
        
        // Main area
        AppTitle = "Übersetzung in Deutsch",
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
        Cancel = "Abbrechen",
        
        // About window
        AboutWindowTitle = "ℹ️ Über",
        AboutAppName = "🌍 Insait Übersetzer → Deutsch",
        AboutVersion = "Version 1.0.0",
        AboutDescriptionTitle = "Universeller Übersetzer in Deutsch",
        AboutDescription = "Diese App ermöglicht es Ihnen, Texte aus jeder Sprache in Deutsch zu übersetzen und die Übersetzung anzuhören.",
        AboutFeaturesTitle = "Hauptfunktionen:",
        AboutFeature1 = "• Text aus jeder Sprache in Deutsch übersetzen",
        AboutFeature2 = "• Sprachausgabe (Text-to-Speech)",
        AboutFeature3 = "• Übersetzung als Text oder MP3 speichern",
        AboutFeature4 = "• Arbeit mit Dateien und Zwischenablage",
        AboutTechnologies = "Technologien:",
        AboutCopyright = "© 2026 Insait. Alle Rechte vorbehalten.",
        
        // Workspaces
        Workspace = "Arbeitsbereich",
        AddWorkspaceTooltip = "Neuen Arbeitsbereich hinzufügen",
        CloseWorkspaceTooltip = "Tab schließen",
        
        // Settings
        Settings = "Einstellungen",
        SettingsTooltip = "Übersetzer-Einstellungen",
        SettingsTitle = "Einstellungen",
        TranslateWithGoogleTooltip = "Mit Google Gemini API übersetzen",
        SettingsHeader = "⚙️ Einstellungen",
        SettingsSubheader = "Übersetzer-Optionen konfigurieren",
        GoogleGeminiApiSection = "Google Gemini API",
        GoogleGeminiApiDescription = "Google Gemini bietet hochwertige KI-Übersetzung. Holen Sie sich Ihren API-Schlüssel auf aistudio.google.com",
        UseGoogleGeminiApi = "Google Gemini API verwenden",
        ApiKey = "API-Schlüssel:",
        ApiKeyPlaceholder = "Geben Sie Ihren Google Gemini API-Schlüssel ein...",
        TestKey = "Schlüssel testen",
        GetKey = "Schlüssel holen",
        DeleteKey = "Schlüssel löschen",
        KeyDeleted = "API-Schlüssel gelöscht",
        EnterApiKey = "API-Schlüssel eingeben",
        Checking = "Überprüfung...",
        AboutProviders = "ℹ️ Über Übersetzungsanbieter",
        ProviderMyMemory = "MyMemory",
        ProviderMyMemoryDesc = "— kostenlos, Limit 10.000 Zeichen/Tag",
        ProviderGoogleTranslate = "Google Translate",
        ProviderGoogleTranslateDesc = "— Fallback bei erschöpftem MyMemory",
        ProviderGoogleGemini = "Google Gemini API",
        ProviderGoogleGeminiDesc = "— hochwertige KI-Übersetzung, erfordert API-Schlüssel (kostenloses Kontingent verfügbar)",
        Save = "Speichern",
        
        // Google API status messages
        GoogleApiKeyNotConfigured = "⚠️ Google API-Schlüssel nicht konfiguriert. Öffnen Sie die Einstellungen.",
        TranslatingViaGoogleApi = "Übersetzung über Google Cloud API...",
        TranslatedViaGoogleApi = "✓ Über Google Cloud API übersetzt",
        MyMemoryExhaustedFallback = "({0} erschöpft → {1})",
        
        // TTS messages
        TtsAvailableOnlyInNative = "TTS ist nur in der nativen App verfügbar",
        Mp3GeneratedInNative = "MP3 wird in der nativen App generiert",
        TtsPlatformNotSupported = "Piper TTS wird derzeit nur unter Windows unterstützt",
        TtsDownloadingPiper = "Piper TTS wird heruntergeladen (~15 MB)...",
        TtsDownloadingPiperProgress = "Piper TTS wird heruntergeladen... {0}%",
        TtsExtractingPiper = "Piper wird entpackt...",
        TtsDownloadingVoiceModel = "Deutsches Sprachmodell wird heruntergeladen (~65 MB)...",
        TtsDownloadingVoiceModelProgress = "Sprachmodell wird heruntergeladen... {0}%",
        TtsDownloadingModelConfig = "Modellkonfiguration wird heruntergeladen...",
        TtsPiperNotFound = "Piper-Executable nicht gefunden: {0}",
        TtsReady = "TTS bereit",
        TtsInitError = "TTS-Initialisierungsfehler: {0}",
        TtsNotInitialized = "TTS nicht initialisiert",
        TtsAudioFileNotCreated = "Piper hat keine Audiodatei erstellt",
        TtsGeneratingSpeech = "Sprache wird generiert...",
        TtsEmptyAudioError = "Fehler: leeres Audio",
        TtsPlaying = "Wiedergabe...",
        TtsTextCannotBeEmpty = "Text darf nicht leer sein",
        TtsGeneratingAudio = "Audio aus Text wird generiert...",
        TtsCouldNotGenerateAudio = "Audio konnte nicht generiert werden",
        TtsConvertingToMp3 = "Konvertierung in MP3...",
        TtsSavedAs = "Gespeichert: {0}"
    };
    #endregion

    #region Russian Strings
    private static readonly LocalizedStrings RussianStrings = new()
    {
        // Window
        WindowTitle = "Insait Переводчик Deutsch",
        TitleBarText = "🌍 Insait Переводчик 🇩🇪",

        // Tabs
        TabTranslate = "Перевод",
        TabHistory = "История",
        TabSettings = "Настройки",
        
        // Toolbar
        Open = "Открыть",
        Clear = "Очистить",
        About = "О программе",
        Language = "Язык",
        
        // Main area
        AppTitle = "Перевод на немецкий",
        Ukrainian = "Язык текста",
        German = "Deutsch",
        EnterTextPlaceholder = "Введите текст для перевода...",
        TranslationPlaceholder = "Перевод на немецкий появится здесь...",
        
        // Tooltips
        OpenFileTooltip = "Открыть текстовый файл",
        ClearTooltip = "Очистить всё",
        AboutTooltip = "О программе",
        LanguageTooltip = "Выбрать язык интерфейса",
        TranslateTooltip = "Перевести",
        SpeakUkrainianTooltip = "Прослушать исходный текст",
        SpeakGermanTooltip = "Прослушать на немецком",
        PasteTooltip = "Вставить из буфера",
        CopyTooltip = "Копировать перевод",
        SaveTextTooltip = "Сохранить как текстовый файл",
        SaveMp3Tooltip = "Сохранить как MP3",
        
        // Status bar
        CharactersFormat = "{0} символов",
        TranslateShortcut = "Перевести",
        SaveShortcut = "Сохранить",
        
        // Statuses
        Ready = "Готово",
        Translating = "Перевод...",
        Translated = "Переведено",
        Cleared = "Очищено",
        FileLoaded = "Файл загружен",
        PastedFromClipboard = "Вставлено из буфера",
        CopiedToClipboard = "Скопировано в буфер",
        SavedAsText = "Сохранено как текстовый файл",
        EnterTextToTranslate = "Введите текст для перевода",
        NoTextToSpeak = "Нет текста для озвучивания",
        UkrainianSpeechNotSupported = "Озвучивание исходного текста пока не поддерживается",
        
        // Dialogs
        OpenTextFileTitle = "Открыть текстовый файл",
        SaveTranslationTitle = "Сохранить перевод",
        SaveAsMp3Title = "Сохранить как MP3",
        TextFiles = "Текстовые файлы",
        AllFiles = "Все файлы",
        TextFile = "Текстовый файл",
        Mp3Audio = "MP3 аудио",
        
        // Language dialog
        SelectLanguageTitle = "Выбор языка",
        SelectLanguageDescription = "Выберите язык интерфейса:",
        Apply = "Применить",
        Cancel = "Отмена",
        
        // About window
        AboutWindowTitle = "ℹ️ О программе",
        AboutAppName = "🌍 Insait Переводчик → Deutsch",
        AboutVersion = "Версия 1.0.0",
        AboutDescriptionTitle = "Универсальный переводчик на немецкий",
        AboutDescription = "Это приложение позволяет легко переводить тексты с любого языка на немецкий и озвучивать перевод.",
        AboutFeaturesTitle = "Основные возможности:",
        AboutFeature1 = "• Перевод текста с любого языка на немецкий",
        AboutFeature2 = "• Озвучивание перевода (Text-to-Speech)",
        AboutFeature3 = "• Сохранение перевода как текст или MP3",
        AboutFeature4 = "• Работа с файлами и буфером обмена",
        AboutTechnologies = "Технологии:",
        AboutCopyright = "© 2026 Insait. Все права защищены.",
        
        // Workspaces
        Workspace = "Рабочая область",
        AddWorkspaceTooltip = "Добавить новую рабочую область",
        CloseWorkspaceTooltip = "Закрыть вкладку",
        
        // Settings
        Settings = "Настройки",
        SettingsTooltip = "Настройки переводчика",
        SettingsTitle = "Настройки",
        TranslateWithGoogleTooltip = "Перевести через Google Gemini API",
        SettingsHeader = "⚙️ Настройки",
        SettingsSubheader = "Настройте параметры переводчика",
        GoogleGeminiApiSection = "Google Gemini API",
        GoogleGeminiApiDescription = "Google Gemini обеспечивает качественный AI-перевод. Получите API ключ на aistudio.google.com",
        UseGoogleGeminiApi = "Использовать Google Gemini API",
        ApiKey = "API Ключ:",
        ApiKeyPlaceholder = "Введите ваш Google Gemini API ключ...",
        TestKey = "Проверить ключ",
        GetKey = "Получить ключ",
        DeleteKey = "Удалить ключ",
        KeyDeleted = "API ключ удалён",
        EnterApiKey = "Введите API ключ",
        Checking = "Проверка...",
        AboutProviders = "ℹ️ О провайдерах перевода",
        ProviderMyMemory = "MyMemory",
        ProviderMyMemoryDesc = "— бесплатный, лимит 10,000 символов/день",
        ProviderGoogleTranslate = "Google Translate",
        ProviderGoogleTranslateDesc = "— резервный при исчерпании MyMemory",
        ProviderGoogleGemini = "Google Gemini API",
        ProviderGoogleGeminiDesc = "— качественный AI-перевод, требует API ключ (бесплатный уровень)",
        Save = "Сохранить",
        
        // Google API status messages
        GoogleApiKeyNotConfigured = "⚠️ Google API ключ не настроен. Откройте Настройки.",
        TranslatingViaGoogleApi = "Перевод через Google Cloud API...",
        TranslatedViaGoogleApi = "✓ Переведено через Google Cloud API",
        MyMemoryExhaustedFallback = "({0} исчерпан → {1})",
        
        // TTS messages
        TtsAvailableOnlyInNative = "TTS доступен только в нативном приложении",
        Mp3GeneratedInNative = "MP3 генерируется в нативном приложении",
        TtsPlatformNotSupported = "Piper TTS пока поддерживается только на Windows",
        TtsDownloadingPiper = "Загрузка Piper TTS (~15 MB)...",
        TtsDownloadingPiperProgress = "Загрузка Piper TTS... {0}%",
        TtsExtractingPiper = "Распаковка Piper...",
        TtsDownloadingVoiceModel = "Загрузка немецкой голосовой модели (~65 MB)...",
        TtsDownloadingVoiceModelProgress = "Загрузка голосовой модели... {0}%",
        TtsDownloadingModelConfig = "Загрузка конфигурации модели...",
        TtsPiperNotFound = "Piper executable не найден: {0}",
        TtsReady = "TTS готов",
        TtsInitError = "Ошибка инициализации TTS: {0}",
        TtsNotInitialized = "TTS не инициализирован",
        TtsAudioFileNotCreated = "Piper не создал аудио файл",
        TtsGeneratingSpeech = "Генерация речи...",
        TtsEmptyAudioError = "Ошибка: пустое аудио",
        TtsPlaying = "Воспроизведение...",
        TtsTextCannotBeEmpty = "Текст не может быть пустым",
        TtsGeneratingAudio = "Генерация аудио из текста...",
        TtsCouldNotGenerateAudio = "Не удалось сгенерировать аудио",
        TtsConvertingToMp3 = "Конвертация в MP3...",
        TtsSavedAs = "Сохранено: {0}"
    };
    #endregion

    #region Turkish Strings
    private static readonly LocalizedStrings TurkishStrings = new()
    {
        // Window
        WindowTitle = "Insait Çevirmen Deutsch",
        TitleBarText = "🌍 Insait Çevirmen 🇩🇪",

        // Tabs
        TabTranslate = "Çeviri",
        TabHistory = "Geçmiş",
        TabSettings = "Ayarlar",
        
        // Toolbar
        Open = "Aç",
        Clear = "Temizle",
        About = "Hakkında",
        Language = "Dil",
        
        // Main area
        AppTitle = "Almancaya Çevir",
        Ukrainian = "Metin Dili",
        German = "Almanca",
        EnterTextPlaceholder = "Çevrilecek metni girin...",
        TranslationPlaceholder = "Almanca çeviri burada görünecek...",
        
        // Tooltips
        OpenFileTooltip = "Metin dosyası aç",
        ClearTooltip = "Tümünü temizle",
        AboutTooltip = "Uygulama hakkında",
        LanguageTooltip = "Arayüz dilini seç",
        TranslateTooltip = "Çevir",
        SpeakUkrainianTooltip = "Kaynak metni dinle",
        SpeakGermanTooltip = "Almanca dinle",
        PasteTooltip = "Panodan yapıştır",
        CopyTooltip = "Çeviriyi kopyala",
        SaveTextTooltip = "Metin dosyası olarak kaydet",
        SaveMp3Tooltip = "MP3 olarak kaydet",
        
        // Status bar
        CharactersFormat = "{0} karakter",
        TranslateShortcut = "Çevir",
        SaveShortcut = "Kaydet",
        
        // Statuses
        Ready = "Hazır",
        Translating = "Çevriliyor...",
        Translated = "Çevrildi",
        Cleared = "Temizlendi",
        FileLoaded = "Dosya yüklendi",
        PastedFromClipboard = "Panodan yapıştırıldı",
        CopiedToClipboard = "Panoya kopyalandı",
        SavedAsText = "Metin dosyası olarak kaydedildi",
        EnterTextToTranslate = "Çevrilecek metni girin",
        NoTextToSpeak = "Seslendirilecek metin yok",
        UkrainianSpeechNotSupported = "Kaynak metin seslendirme henüz desteklenmiyor",
        
        // Dialogs
        OpenTextFileTitle = "Metin dosyası aç",
        SaveTranslationTitle = "Çeviriyi kaydet",
        SaveAsMp3Title = "MP3 olarak kaydet",
        TextFiles = "Metin dosyaları",
        AllFiles = "Tüm dosyalar",
        TextFile = "Metin dosyası",
        Mp3Audio = "MP3 ses",
        
        // Language dialog
        SelectLanguageTitle = "Dil Seçimi",
        SelectLanguageDescription = "Arayüz dilini seçin:",
        Apply = "Uygula",
        Cancel = "İptal",
        
        // About window
        AboutWindowTitle = "ℹ️ Hakkında",
        AboutAppName = "🌍 Insait Çevirmen → Deutsch",
        AboutVersion = "Sürüm 1.0.0",
        AboutDescriptionTitle = "Evrensel Almanca Çevirmen",
        AboutDescription = "Bu uygulama, herhangi bir dilden Almancaya kolayca metin çevirmenizi ve çeviriyi dinlemenizi sağlar.",
        AboutFeaturesTitle = "Temel özellikler:",
        AboutFeature1 = "• Herhangi bir dilden Almancaya metin çevirisi",
        AboutFeature2 = "• Metin okuma (Text-to-Speech)",
        AboutFeature3 = "• Çeviriyi metin veya MP3 olarak kaydetme",
        AboutFeature4 = "• Dosyalar ve pano ile çalışma",
        AboutTechnologies = "Teknolojiler:",
        AboutCopyright = "© 2026 Insait. Tüm hakları saklıdır.",
        
        // Workspaces
        Workspace = "Çalışma Alanı",
        AddWorkspaceTooltip = "Yeni çalışma alanı ekle",
        CloseWorkspaceTooltip = "Sekmeyi kapat",
        
        // Settings
        Settings = "Ayarlar",
        SettingsTooltip = "Çevirmen ayarları",
        SettingsTitle = "Ayarlar",
        TranslateWithGoogleTooltip = "Google Gemini API ile çevir",
        SettingsHeader = "⚙️ Ayarlar",
        SettingsSubheader = "Çevirmen seçeneklerini yapılandırın",
        GoogleGeminiApiSection = "Google Gemini API",
        GoogleGeminiApiDescription = "Google Gemini yüksek kaliteli AI çevirisi sağlar. API anahtarınızı aistudio.google.com adresinden alın",
        UseGoogleGeminiApi = "Google Gemini API Kullan",
        ApiKey = "API Anahtarı:",
        ApiKeyPlaceholder = "Google Gemini API anahtarınızı girin...",
        TestKey = "Anahtarı test et",
        GetKey = "Anahtar al",
        DeleteKey = "Anahtarı sil",
        KeyDeleted = "API anahtarı silindi",
        EnterApiKey = "API anahtarını girin",
        Checking = "Kontrol ediliyor...",
        AboutProviders = "ℹ️ Çeviri sağlayıcıları hakkında",
        ProviderMyMemory = "MyMemory",
        ProviderMyMemoryDesc = "— ücretsiz, günde 10.000 karakter limiti",
        ProviderGoogleTranslate = "Google Translate",
        ProviderGoogleTranslateDesc = "— MyMemory tükendiğinde yedek",
        ProviderGoogleGemini = "Google Gemini API",
        ProviderGoogleGeminiDesc = "— yüksek kaliteli AI çevirisi, API anahtarı gerektirir (ücretsiz katman mevcut)",
        Save = "Kaydet",
        
        // Google API status messages
        GoogleApiKeyNotConfigured = "⚠️ Google API anahtarı yapılandırılmadı. Ayarları açın.",
        TranslatingViaGoogleApi = "Google Cloud API ile çevriliyor...",
        TranslatedViaGoogleApi = "✓ Google Cloud API ile çevrildi",
        MyMemoryExhaustedFallback = "({0} tükendi → {1})",
        
        // TTS messages
        TtsAvailableOnlyInNative = "TTS yalnızca yerel uygulamada kullanılabilir",
        Mp3GeneratedInNative = "MP3 yerel uygulamada oluşturuluyor",
        TtsPlatformNotSupported = "Piper TTS şu anda yalnızca Windows'ta desteklenmektedir",
        TtsDownloadingPiper = "Piper TTS indiriliyor (~15 MB)...",
        TtsDownloadingPiperProgress = "Piper TTS indiriliyor... {0}%",
        TtsExtractingPiper = "Piper açılıyor...",
        TtsDownloadingVoiceModel = "Almanca ses modeli indiriliyor (~65 MB)...",
        TtsDownloadingVoiceModelProgress = "Ses modeli indiriliyor... {0}%",
        TtsDownloadingModelConfig = "Model yapılandırması indiriliyor...",
        TtsPiperNotFound = "Piper çalıştırılabilir dosyası bulunamadı: {0}",
        TtsReady = "TTS hazır",
        TtsInitError = "TTS başlatma hatası: {0}",
        TtsNotInitialized = "TTS başlatılmadı",
        TtsAudioFileNotCreated = "Piper ses dosyası oluşturmadı",
        TtsGeneratingSpeech = "Konuşma oluşturuluyor...",
        TtsEmptyAudioError = "Hata: boş ses",
        TtsPlaying = "Oynatılıyor...",
        TtsTextCannotBeEmpty = "Metin boş olamaz",
        TtsGeneratingAudio = "Metinden ses oluşturuluyor...",
        TtsCouldNotGenerateAudio = "Ses oluşturulamadı",
        TtsConvertingToMp3 = "MP3'e dönüştürülüyor...",
        TtsSavedAs = "Kaydedildi: {0}"
    };
    #endregion
}

public class LocalizedStrings
{
    // Window
    public string WindowTitle { get; init; } = "";
    public string TitleBarText { get; init; } = "";

    // Tabs
    public string TabTranslate { get; init; } = "";
    public string TabHistory { get; init; } = "";
    public string TabSettings { get; init; } = "";

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
    
    // About window
    public string AboutWindowTitle { get; init; } = "";
    public string AboutAppName { get; init; } = "";
    public string AboutVersion { get; init; } = "";
    public string AboutDescriptionTitle { get; init; } = "";
    public string AboutDescription { get; init; } = "";
    public string AboutFeaturesTitle { get; init; } = "";
    public string AboutFeature1 { get; init; } = "";
    public string AboutFeature2 { get; init; } = "";
    public string AboutFeature3 { get; init; } = "";
    public string AboutFeature4 { get; init; } = "";
    public string AboutTechnologies { get; init; } = "";
    public string AboutCopyright { get; init; } = "";
    
    // Workspaces
    public string Workspace { get; init; } = "";
    public string AddWorkspaceTooltip { get; init; } = "";
    public string CloseWorkspaceTooltip { get; init; } = "";
    
    // Settings
    public string Settings { get; init; } = "";
    public string SettingsTooltip { get; init; } = "";
    public string SettingsTitle { get; init; } = "";
    public string TranslateWithGoogleTooltip { get; init; } = "";
    public string SettingsHeader { get; init; } = "";
    public string SettingsSubheader { get; init; } = "";
    public string GoogleGeminiApiSection { get; init; } = "";
    public string GoogleGeminiApiDescription { get; init; } = "";
    public string UseGoogleGeminiApi { get; init; } = "";
    public string ApiKey { get; init; } = "";
    public string ApiKeyPlaceholder { get; init; } = "";
    public string TestKey { get; init; } = "";
    public string GetKey { get; init; } = "";
    public string DeleteKey { get; init; } = "";
    public string KeyDeleted { get; init; } = "";
    public string EnterApiKey { get; init; } = "";
    public string Checking { get; init; } = "";
    public string AboutProviders { get; init; } = "";
    public string ProviderMyMemory { get; init; } = "";
    public string ProviderMyMemoryDesc { get; init; } = "";
    public string ProviderGoogleTranslate { get; init; } = "";
    public string ProviderGoogleTranslateDesc { get; init; } = "";
    public string ProviderGoogleGemini { get; init; } = "";
    public string ProviderGoogleGeminiDesc { get; init; } = "";
    public string Save { get; init; } = "";
    
    // Google API status messages
    public string GoogleApiKeyNotConfigured { get; init; } = "";
    public string TranslatingViaGoogleApi { get; init; } = "";
    public string TranslatedViaGoogleApi { get; init; } = "";
    public string MyMemoryExhaustedFallback { get; init; } = "";
    
    // TTS messages
    public string TtsAvailableOnlyInNative { get; init; } = "";
    public string Mp3GeneratedInNative { get; init; } = "";
    public string TtsPlatformNotSupported { get; init; } = "";
    public string TtsDownloadingPiper { get; init; } = "";
    public string TtsDownloadingPiperProgress { get; init; } = "";
    public string TtsExtractingPiper { get; init; } = "";
    public string TtsDownloadingVoiceModel { get; init; } = "";
    public string TtsDownloadingVoiceModelProgress { get; init; } = "";
    public string TtsDownloadingModelConfig { get; init; } = "";
    public string TtsPiperNotFound { get; init; } = "";
    public string TtsReady { get; init; } = "";
    public string TtsInitError { get; init; } = "";
    public string TtsNotInitialized { get; init; } = "";
    public string TtsAudioFileNotCreated { get; init; } = "";
    public string TtsGeneratingSpeech { get; init; } = "";
    public string TtsEmptyAudioError { get; init; } = "";
    public string TtsPlaying { get; init; } = "";
    public string TtsTextCannotBeEmpty { get; init; } = "";
    public string TtsGeneratingAudio { get; init; } = "";
    public string TtsCouldNotGenerateAudio { get; init; } = "";
    public string TtsConvertingToMp3 { get; init; } = "";
    public string TtsSavedAs { get; init; } = "";
}
