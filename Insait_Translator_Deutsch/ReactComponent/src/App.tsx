import { useState, useEffect, useCallback, useRef, KeyboardEvent } from 'react';
import { checkHealth, translate, speakMp3 } from './services/translatorService';
import './App.css';

// ===== LOCALIZATION SYSTEM =====
type UILanguage = 'uk' | 'en' | 'de';

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
    appTitle: 'Insait Перекладач → Deutsch',
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
    appTitle: 'Insait Translator → Deutsch',
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
    
    mainTitle: 'Übersetzung ins Deutsche',
    sourceLanguage: 'Ausgangssprache:',
    sourcePlaceholder: 'Text zum Übersetzen eingeben...',
    translationPlaceholder: 'Die deutsche Übersetzung wird hier angezeigt...',
    
    paste: 'Aus Zwischenablage einfügen',
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
};

// ===== SOURCE LANGUAGES (translate FROM) =====
interface SourceLanguage {
  code: string;
  name: Record<UILanguage, string>;
}

const sourceLanguages: SourceLanguage[] = [
  { code: 'uk', name: { uk: 'Українська', en: 'Ukrainian', de: 'Ukrainisch' } },
  { code: 'en', name: { uk: 'Англійська', en: 'English', de: 'Englisch' } },
  { code: 'fr', name: { uk: 'Французька', en: 'French', de: 'Französisch' } },
  { code: 'es', name: { uk: 'Іспанська', en: 'Spanish', de: 'Spanisch' } },
  { code: 'it', name: { uk: 'Італійська', en: 'Italian', de: 'Italienisch' } },
  { code: 'pt', name: { uk: 'Португальська', en: 'Portuguese', de: 'Portugiesisch' } },
  { code: 'pl', name: { uk: 'Польська', en: 'Polish', de: 'Polnisch' } },
  { code: 'nl', name: { uk: 'Нідерландська', en: 'Dutch', de: 'Niederländisch' } },
  { code: 'ru', name: { uk: 'Російська', en: 'Russian', de: 'Russisch' } },
  { code: 'cs', name: { uk: 'Чеська', en: 'Czech', de: 'Tschechisch' } },
  { code: 'sk', name: { uk: 'Словацька', en: 'Slovak', de: 'Slowakisch' } },
  { code: 'hu', name: { uk: 'Угорська', en: 'Hungarian', de: 'Ungarisch' } },
  { code: 'ro', name: { uk: 'Румунська', en: 'Romanian', de: 'Rumänisch' } },
  { code: 'bg', name: { uk: 'Болгарська', en: 'Bulgarian', de: 'Bulgarisch' } },
  { code: 'hr', name: { uk: 'Хорватська', en: 'Croatian', de: 'Kroatisch' } },
  { code: 'sv', name: { uk: 'Шведська', en: 'Swedish', de: 'Schwedisch' } },
  { code: 'da', name: { uk: 'Данська', en: 'Danish', de: 'Dänisch' } },
  { code: 'fi', name: { uk: 'Фінська', en: 'Finnish', de: 'Finnisch' } },
  { code: 'el', name: { uk: 'Грецька', en: 'Greek', de: 'Griechisch' } },
  { code: 'tr', name: { uk: 'Турецька', en: 'Turkish', de: 'Türkisch' } },
  { code: 'ja', name: { uk: 'Японська', en: 'Japanese', de: 'Japanisch' } },
  { code: 'ko', name: { uk: 'Корейська', en: 'Korean', de: 'Koreanisch' } },
  { code: 'zh', name: { uk: 'Китайська', en: 'Chinese', de: 'Chinesisch' } },
  { code: 'ar', name: { uk: 'Арабська', en: 'Arabic', de: 'Arabisch' } },
];

// UI Languages for selection
const uiLanguages: { code: UILanguage; name: string }[] = [
  { code: 'uk', name: 'Українська' },
  { code: 'en', name: 'English' },
  { code: 'de', name: 'Deutsch' },
];

function App() {
  const [sourceText, setSourceText] = useState('');
  const [germanText, setGermanText] = useState('');
  const [statusText, setStatusText] = useState('');
  const [isTranslating, setIsTranslating] = useState(false);
  const [isSpeaking, setIsSpeaking] = useState(false);
  const [isBackendAvailable, setIsBackendAvailable] = useState(false);
  const [sourceLanguage, setSourceLanguage] = useState('uk');
  const [uiLanguage, setUILanguage] = useState<UILanguage>('uk');
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

  // Get current source language info
  const currentSourceLang = sourceLanguages.find(l => l.code === sourceLanguage) || sourceLanguages[0];

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
      const translation = await translate(text, sourceLanguage, 'de');
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
          <div className="panel-header">
            <select 
              className="language-select"
              value={sourceLanguage}
              onChange={(e) => setSourceLanguage(e.target.value)}
            >
              {sourceLanguages.map(lang => (
                <option key={lang.code} value={lang.code}>
                  {lang.name[uiLanguage]}
                </option>
              ))}
            </select>
          </div>
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
            <span className="panel-title">Deutsch</span>
          </div>
          <textarea
            className="text-area"
            value={germanText}
            placeholder={t.translationPlaceholder}
            readOnly
          />
          <div className="panel-actions">
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
              📋
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
          <span className="char-count">{currentSourceLang.name[uiLanguage]}: {sourceCharCount} {t.characters}</span>
          <span className="char-count">Deutsch: {germanCharCount} {t.characters}</span>
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

