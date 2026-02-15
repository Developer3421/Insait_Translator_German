import { useState, useEffect, useCallback, useRef, KeyboardEvent } from 'react';
import { checkHealth, translate, speakMp3 } from './services/translatorService';
import './App.css';

// ===== LOCALIZATION SYSTEM =====
type UILanguage = 'uk' | 'en' | 'de' | 'ru' | 'tr';

interface LocalizedStrings {
  // Header
  appTitle: string;
  connected: string;
  disconnected: string;
  
  // Toolbar
  clear: string;
  reconnect: string;
  interfaceLanguage: string;
  
  // Main area
  mainTitle: string;
  sourceLanguage: string;
  sourcePlaceholder: string;
  translationPlaceholder: string;
  
  // Buttons
  paste: string;
  pasteGerman: string;
  listen: string;
  copy: string;
  downloadMp3: string;
  translate: string;
  
  // Status messages
  ready: string;
  backendConnected: string;
  backendUnavailable: string;
  translating: string;
  translated: string;
  enterTextToTranslate: string;
  noTextToSpeak: string;
  generatingAudio: string;
  playing: string;
  noTextToCopy: string;
  copiedToClipboard: string;
  copyError: string;
  pastedFromClipboard: string;
  pasteError: string;
  noTextToSave: string;
  generatingMp3: string;
  mp3Downloaded: string;
  cleared: string;
  
  // Other
  characters: string;
  translateShortcut: string;
  error: string;
}

const translations: Record<UILanguage, LocalizedStrings> = {
  uk: {
    appTitle: 'Insait Перекладач → German',
    connected: '● Підключено',
    disconnected: '○ Відключено',
    
    clear: 'Очистити',
    reconnect: 'Перепідключити',
    interfaceLanguage: '🌐 Мова',
    
    mainTitle: 'Переклад на німецьку',
    sourceLanguage: 'Мова тексту:',
    sourcePlaceholder: 'Введіть текст для перекладу...',
    translationPlaceholder: 'Переклад німецькою з\'явиться тут...',
    
    paste: 'Вставити з буфера',
    pasteGerman: 'Вставити текст для озвучення',
    listen: 'Прослухати',
    copy: 'Копіювати',
    downloadMp3: 'Завантажити MP3',
    translate: 'Перекласти',
    
    ready: 'Готово',
    backendConnected: 'Бекенд підключено ✓',
    backendUnavailable: '⚠️ Бекенд недоступний - запустіть нативний додаток',
    translating: 'Переклад...',
    translated: 'Перекладено ✓',
    enterTextToTranslate: 'Введіть текст для перекладу',
    noTextToSpeak: 'Немає тексту для озвучення',
    generatingAudio: 'Генерація аудіо...',
    playing: 'Відтворення...',
    noTextToCopy: 'Немає тексту для копіювання',
    copiedToClipboard: 'Скопійовано в буфер обміну ✓',
    copyError: 'Помилка копіювання',
    pastedFromClipboard: 'Вставлено з буфера обміну ✓',
    pasteError: 'Помилка вставки',
    noTextToSave: 'Немає тексту для збереження',
    generatingMp3: 'Генерація MP3...',
    mp3Downloaded: 'MP3 завантажено ✓',
    cleared: 'Очищено',
    
    characters: 'символів',
    translateShortcut: 'Перекласти',
    error: 'Помилка',
  },
  en: {
    appTitle: 'Insait Translator → German',
    connected: '● Connected',
    disconnected: '○ Disconnected',
    
    clear: 'Clear',
    reconnect: 'Reconnect',
    interfaceLanguage: '🌐 Language',
    
    mainTitle: 'Translate to German',
    sourceLanguage: 'Source language:',
    sourcePlaceholder: 'Enter text to translate...',
    translationPlaceholder: 'German translation will appear here...',
    
    paste: 'Paste from clipboard',
    pasteGerman: 'Paste text to listen',
    listen: 'Listen',
    copy: 'Copy',
    downloadMp3: 'Download MP3',
    translate: 'Translate',
    
    ready: 'Ready',
    backendConnected: 'Backend connected ✓',
    backendUnavailable: '⚠️ Backend unavailable - run the native app',
    translating: 'Translating...',
    translated: 'Translated ✓',
    enterTextToTranslate: 'Enter text to translate',
    noTextToSpeak: 'No text to speak',
    generatingAudio: 'Generating audio...',
    playing: 'Playing...',
    noTextToCopy: 'No text to copy',
    copiedToClipboard: 'Copied to clipboard ✓',
    copyError: 'Copy error',
    pastedFromClipboard: 'Pasted from clipboard ✓',
    pasteError: 'Paste error',
    noTextToSave: 'No text to save',
    generatingMp3: 'Generating MP3...',
    mp3Downloaded: 'MP3 downloaded ✓',
    cleared: 'Cleared',
    
    characters: 'characters',
    translateShortcut: 'Translate',
    error: 'Error',
  },
  de: {
    appTitle: 'Insait Übersetzer → Deutsch',
    connected: '● Verbunden',
    disconnected: '○ Getrennt',
    
    clear: 'Löschen',
    reconnect: 'Neu verbinden',
    interfaceLanguage: 'Sprache',
    
    mainTitle: 'Übersetzung in Deutsch',
    sourceLanguage: 'Ausgangssprache:',
    sourcePlaceholder: 'Text zum Übersetzen eingeben...',
    translationPlaceholder: 'Die deutsche Übersetzung wird hier angezeigt...',
    
    paste: 'Aus Zwischenablage einfügen',
    pasteGerman: 'Text zum Anhören einfügen',
    listen: 'Anhören',
    copy: 'Kopieren',
    downloadMp3: 'MP3 herunterladen',
    translate: 'Übersetzen',
    
    ready: 'Bereit',
    backendConnected: 'Backend verbunden ✓',
    backendUnavailable: '⚠️ Backend nicht verfügbar - Native App starten',
    translating: 'Übersetze...',
    translated: 'Übersetzt ✓',
    enterTextToTranslate: 'Text zum Übersetzen eingeben',
    noTextToSpeak: 'Kein Text zum Vorlesen',
    generatingAudio: 'Audio wird generiert...',
    playing: 'Wiedergabe...',
    noTextToCopy: 'Kein Text zum Kopieren',
    copiedToClipboard: 'In Zwischenablage kopiert ✓',
    copyError: 'Kopierfehler',
    pastedFromClipboard: 'Aus Zwischenablage eingefügt ✓',
    pasteError: 'Einfügefehler',
    noTextToSave: 'Kein Text zum Speichern',
    generatingMp3: 'MP3 wird generiert...',
    mp3Downloaded: 'MP3 heruntergeladen ✓',
    cleared: 'Gelöscht',
    
    characters: 'Zeichen',
    translateShortcut: 'Übersetzen',
    error: 'Fehler',
  },
  ru: {
    appTitle: 'Insait Переводчик → German',
    connected: '● Подключено',
    disconnected: '○ Отключено',
    
    clear: 'Очистить',
    reconnect: 'Переподключить',
    interfaceLanguage: '🌐 Язык',
    
    mainTitle: 'Перевод на немецкий',
    sourceLanguage: 'Язык текста:',
    sourcePlaceholder: 'Введите текст для перевода...',
    translationPlaceholder: 'Перевод на немецкий появится здесь...',
    
    paste: 'Вставить из буфера',
    pasteGerman: 'Вставить текст для озвучивания',
    listen: 'Прослушать',
    copy: 'Копировать',
    downloadMp3: 'Скачать MP3',
    translate: 'Перевести',
    
    ready: 'Готово',
    backendConnected: 'Бэкенд подключен ✓',
    backendUnavailable: '⚠️ Бэкенд недоступен - запустите нативное приложение',
    translating: 'Перевод...',
    translated: 'Переведено ✓',
    enterTextToTranslate: 'Введите текст для перевода',
    noTextToSpeak: 'Нет текста для озвучивания',
    generatingAudio: 'Генерация аудио...',
    playing: 'Воспроизведение...',
    noTextToCopy: 'Нет текста для копирования',
    copiedToClipboard: 'Скопировано в буфер обмена ✓',
    copyError: 'Ошибка копирования',
    pastedFromClipboard: 'Вставлено из буфера обмена ✓',
    pasteError: 'Ошибка вставки',
    noTextToSave: 'Нет текста для сохранения',
    generatingMp3: 'Генерация MP3...',
    mp3Downloaded: 'MP3 скачан ✓',
    cleared: 'Очищено',
    
    characters: 'символов',
    translateShortcut: 'Перевести',
    error: 'Ошибка',
  },
  tr: {
    appTitle: 'Insait Çevirmen → German',
    connected: '● Bağlı',
    disconnected: '○ Bağlantı kesildi',
    
    clear: 'Temizle',
    reconnect: 'Yeniden bağlan',
    interfaceLanguage: '🌐 Dil',
    
    mainTitle: 'Almancaya çeviri',
    sourceLanguage: 'Kaynak dil:',
    sourcePlaceholder: 'Çevrilecek metni girin...',
    translationPlaceholder: 'Almanca çeviri burada görünecek...',
    
    paste: 'Panodan yapıştır',
    pasteGerman: 'Dinlemek için metin yapıştır',
    listen: 'Dinle',
    copy: 'Kopyala',
    downloadMp3: 'MP3 indir',
    translate: 'Çevir',
    
    ready: 'Hazır',
    backendConnected: 'Backend bağlı ✓',
    backendUnavailable: '⚠️ Backend kullanılamıyor - yerel uygulamayı çalıştırın',
    translating: 'Çevriliyor...',
    translated: 'Çevrildi ✓',
    enterTextToTranslate: 'Çevrilecek metni girin',
    noTextToSpeak: 'Seslendirilecek metin yok',
    generatingAudio: 'Ses oluşturuluyor...',
    playing: 'Oynatılıyor...',
    noTextToCopy: 'Kopyalanacak metin yok',
    copiedToClipboard: 'Panoya kopyalandı ✓',
    copyError: 'Kopyalama hatası',
    pastedFromClipboard: 'Panodan yapıştırıldı ✓',
    pasteError: 'Yapıştırma hatası',
    noTextToSave: 'Kaydedilecek metin yok',
    generatingMp3: 'MP3 oluşturuluyor...',
    mp3Downloaded: 'MP3 indirildi ✓',
    cleared: 'Temizlendi',
    
    characters: 'karakter',
    translateShortcut: 'Çevir',
    error: 'Hata',
  },
};


// UI Languages for selection
const uiLanguages: { code: UILanguage; name: string }[] = [
  { code: 'uk', name: 'Українська' },
  { code: 'en', name: 'English' },
  { code: 'de', name: 'Deutsch' },
  { code: 'ru', name: 'Русский' },
  { code: 'tr', name: 'Türkçe' },
];

function App() {
  const [sourceText, setSourceText] = useState('');
  const [germanText, setGermanText] = useState('');
  const [statusText, setStatusText] = useState('');
  const [isTranslating, setIsTranslating] = useState(false);
  const [isSpeaking, setIsSpeaking] = useState(false);
  const [isBackendAvailable, setIsBackendAvailable] = useState(false);
  const [uiLanguage, setUILanguage] = useState<UILanguage>('en');
  const [showUILanguageMenu, setShowUILanguageMenu] = useState(false);

  const audioContextRef = useRef<AudioContext | null>(null);
  const audioSourceRef = useRef<AudioBufferSourceNode | null>(null);
  const uiLangMenuRef = useRef<HTMLDivElement>(null);

  // Get current translations
  const t = translations[uiLanguage];

  // Computed values
  const sourceCharCount = sourceText.length;
  const germanCharCount = germanText.length;
  const canTranslate = sourceText.trim().length > 0 && !isTranslating;


  const stopAudio = useCallback(() => {
    if (audioSourceRef.current) {
      try {
        audioSourceRef.current.stop();
      } catch { /* empty */ }
      audioSourceRef.current = null;
    }
    if (audioContextRef.current) {
      try {
        audioContextRef.current.close();
      } catch { /* empty */ }
      audioContextRef.current = null;
    }
    setIsSpeaking(false);
  }, []);

  const checkBackendHealth = useCallback(async () => {
    const isAvailable = await checkHealth();
    setIsBackendAvailable(isAvailable);
    if (isAvailable) {
      setStatusText(t.backendConnected);
    } else {
      setStatusText(t.backendUnavailable);
    }
    return isAvailable;
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // Initialize status text with translation
  useEffect(() => {
    setStatusText(t.ready);
  }, [t.ready]);

  // Update document title based on UI language
  useEffect(() => {
    document.title = t.appTitle;
  }, [t.appTitle]);

  // Close UI language menu when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (uiLangMenuRef.current && !uiLangMenuRef.current.contains(event.target as Node)) {
        setShowUILanguageMenu(false);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  // Періодична перевірка з'єднання
  useEffect(() => {
    checkBackendHealth();
    
    // Перевіряємо з'єднання кожні 5 секунд якщо відключено
    const interval = setInterval(async () => {
      const isAvailable = await checkBackendHealth();
      // Якщо підключено - перевіряємо рідше
      if (isAvailable) {
        clearInterval(interval);
        // Встановлюємо новий інтервал на 30 секунд
        const slowInterval = setInterval(() => checkBackendHealth(), 30000);
        return () => clearInterval(slowInterval);
      }
    }, 5000);

    return () => {
      clearInterval(interval);
      stopAudio();
    };
  }, [checkBackendHealth, stopAudio]);

  const handleTranslate = async () => {
    const text = sourceText.trim();
    if (!text) {
      setStatusText(t.enterTextToTranslate);
      return;
    }

    setIsTranslating(true);
    setStatusText(t.translating);

    try {
      const translation = await translate(text, 'auto', 'de');
      setGermanText(translation);
      setStatusText(t.translated);
    } catch (err) {
      console.error('Translation error:', err);
      setStatusText(`${t.error}: ${err instanceof Error ? err.message : 'Translation failed'}`);
    } finally {
      setIsTranslating(false);
    }
  };

  const playAudioBlob = async (blob: Blob) => {
    stopAudio();

    audioContextRef.current = new AudioContext();
    const arrayBuffer = await blob.arrayBuffer();
    const audioBuffer = await audioContextRef.current.decodeAudioData(arrayBuffer);

    audioSourceRef.current = audioContextRef.current.createBufferSource();
    audioSourceRef.current.buffer = audioBuffer;
    audioSourceRef.current.connect(audioContextRef.current.destination);

    audioSourceRef.current.onended = () => {
      setIsSpeaking(false);
      setStatusText(t.ready);
    };

    audioSourceRef.current.start();
  };

  const handleSpeakGerman = async () => {
    const text = germanText.trim();
    if (!text) {
      setStatusText(t.noTextToSpeak);
      return;
    }

    if (isSpeaking) {
      stopAudio();
      return;
    }

    setIsSpeaking(true);
    setStatusText(t.generatingAudio);

    try {
      const blob = await speakMp3(text);
      await playAudioBlob(blob);
      setStatusText(t.playing);
    } catch (err) {
      console.error('TTS error:', err);
      setStatusText(`TTS ${t.error}: ${err instanceof Error ? err.message : 'TTS failed'}`);
      setIsSpeaking(false);
    }
  };

  const handleClear = () => {
    setSourceText('');
    setGermanText('');
    setStatusText(t.cleared);
  };

  const handleCopyGerman = async () => {
    if (!germanText) {
      setStatusText(t.noTextToCopy);
      return;
    }

    try {
      await navigator.clipboard.writeText(germanText);
      setStatusText(t.copiedToClipboard);
    } catch {
      setStatusText(t.copyError);
    }
  };

  const handlePasteSource = async () => {
    try {
      const text = await navigator.clipboard.readText();
      setSourceText(text);
      setStatusText(t.pastedFromClipboard);
    } catch {
      setStatusText(t.pasteError);
    }
  };

  const handlePasteGerman = async () => {
    try {
      const text = await navigator.clipboard.readText();
      setGermanText(text);
      setStatusText(t.pastedFromClipboard);
    } catch {
      setStatusText(t.pasteError);
    }
  };

  const handleDownloadMp3 = async () => {
    const text = germanText.trim();
    if (!text) {
      setStatusText(t.noTextToSave);
      return;
    }

    setStatusText(t.generatingMp3);

    try {
      const blob = await speakMp3(text);
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `translation_${Date.now()}.mp3`;
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
      URL.revokeObjectURL(url);
      setStatusText(t.mp3Downloaded);
    } catch (err) {
      setStatusText(`${t.error}: ${err instanceof Error ? err.message : 'MP3 generation failed'}`);
    }
  };

  const handleKeyDown = (event: KeyboardEvent<HTMLTextAreaElement>) => {
    if (event.ctrlKey && event.key === 'Enter') {
      event.preventDefault();
      handleTranslate();
    }
  };

  const isError = statusText.includes(t.error) || statusText.includes('⚠️');

  return (
    <div className="app-container">
      {/* Header */}
      <header className="header">
        <div className="header-left">
          <h1 className="title">{t.appTitle}</h1>
        </div>
        <div className="header-right">
          {/* UI Language Selector */}
          <div className="ui-language-selector" ref={uiLangMenuRef}>
            <button 
              className="btn toolbar-btn ui-lang-btn"
              onClick={() => setShowUILanguageMenu(!showUILanguageMenu)}
            >
              <span className="btn-text">{t.interfaceLanguage}</span>
            </button>
            {showUILanguageMenu && (
              <div className="ui-language-menu">
                {uiLanguages.map(lang => (
                  <button
                    key={lang.code}
                    className={`ui-lang-option ${lang.code === uiLanguage ? 'active' : ''}`}
                    onClick={() => {
                      setUILanguage(lang.code);
                      setShowUILanguageMenu(false);
                    }}
                  >
                    <span className="lang-name">{lang.name}</span>
                  </button>
                ))}
              </div>
            )}
          </div>
          <div className={`connection-status ${isBackendAvailable ? 'connected' : ''}`}>
            {isBackendAvailable ? t.connected : t.disconnected}
          </div>
        </div>
      </header>

      {/* Toolbar */}
      <div className="toolbar">
        <div className="toolbar-left">
          <button className="btn toolbar-btn" onClick={handleClear}>
            <span className="btn-icon">🗑️</span>
            <span className="btn-text">{t.clear}</span>
          </button>
        </div>
        <h2 className="toolbar-title">{t.mainTitle}</h2>
        <div className="toolbar-right">
          <button className="btn toolbar-btn" onClick={checkBackendHealth}>
            <span className="btn-icon">🔄</span>
            <span className="btn-text">{t.reconnect}</span>
          </button>
        </div>
      </div>

      {/* Main Translation Area */}
      <div className="translation-area">
        {/* Source Input */}
        <div className="panel input-panel">
          <textarea
            className="text-area"
            value={sourceText}
            onChange={(e) => setSourceText(e.target.value)}
            onKeyDown={handleKeyDown}
            placeholder={t.sourcePlaceholder}
            disabled={isTranslating}
          />
          <div className="panel-actions">
            <button className="btn icon-btn" onClick={handlePasteSource} title={t.paste}>
              📋
            </button>
          </div>
        </div>

        {/* Center Controls */}
        <div className="center-controls">
          <button
            className="btn translate-btn"
            onClick={handleTranslate}
            disabled={!canTranslate}
            title={`${t.translate} (Ctrl+Enter)`}
          >
            <span className="translate-icon">🔄</span>
          </button>
          <div className="arrow">→</div>
        </div>

        {/* German Output */}
        <div className="panel output-panel">
          <div className="panel-header">
            <span className="panel-title">German</span>
          </div>
          <textarea
            className="text-area"
            value={germanText}
            onChange={(e) => setGermanText(e.target.value)}
            placeholder={t.translationPlaceholder}
          />
          <div className="panel-actions">
            <button
              className="btn icon-btn"
              onClick={handlePasteGerman}
              title={t.pasteGerman}
            >
              📋
            </button>
            <button
              className="btn icon-btn"
              onClick={handleSpeakGerman}
              disabled={!germanText}
              title={t.listen}
            >
              {isSpeaking ? '⏹️' : '🔊'}
            </button>
            <button
              className="btn icon-btn"
              onClick={handleCopyGerman}
              disabled={!germanText}
              title={t.copy}
            >
              📄
            </button>
            <button
              className="btn icon-btn"
              onClick={handleDownloadMp3}
              disabled={!germanText}
              title={t.downloadMp3}
            >
              💾
            </button>
          </div>
        </div>
      </div>

      {/* Status Bar */}
      <footer className="status-bar">
        <div className="char-counts">
          <span className="char-count">{sourceCharCount} / {germanCharCount} {t.characters}</span>
        </div>
        <div className="shortcuts">
          <span className="shortcut"><kbd>Ctrl+Enter</kbd> {t.translateShortcut}</span>
        </div>
        <div className={`status-text ${isError ? 'error' : ''}`}>
          {statusText}
        </div>
      </footer>

      {/* Loading Overlay */}
      {isTranslating && (
        <div className="loading-overlay">
          <div className="spinner"></div>
          <p>{t.translating}</p>
        </div>
      )}
    </div>
  );
}

export default App;

