# PiperTTS - Вбудовані ресурси (Embedded Resources)

## Опис змін

Починаючи з версії 1.1, PiperTTS та німецька модель голосу Thorsten включені в додаток як **вбудовані ресурси** (Embedded Resources). Це забезпечує:

✅ **Повну офлайн роботу** - не потрібен інтернет для ініціалізації TTS  
✅ **Сумісність з Microsoft Store** - немає зовнішніх завантажень  
✅ **Швидку ініціалізацію** - копіювання з ресурсів замість завантаження  
✅ **Надійність** - немає залежності від доступності GitHub/HuggingFace  

---

## Структура файлів

### Вбудовані ресурси (Embedded)
Розташовані в проекті `Insait_Translator_Deutsch.Desktop`:

```
Insait_Translator_Deutsch.Desktop/
  PiperTTS/
    piper/
      piper/
        espeak-ng-data/    # Фонетичні дані
        espeak-ng.dll
        libtashkeel_model.ort
        onnxruntime.dll
        onnxruntime_providers_shared.dll
        piper.exe          # Основний виконуваний файл
        piper_phonemize.dll
    models/
      de_DE-thorsten-high.onnx       # Модель голосу (~65 MB)
      de_DE-thorsten-high.onnx.json  # Конфігурація моделі
```

### Runtime розташування (AppData)
Під час першого запуску файли копіюються в:

```
%LOCALAPPDATA%\InsaitTranslator\
  piper/
    piper/
      [всі файли Piper TTS]
  models/
    de_DE-thorsten-high.onnx
    de_DE-thorsten-high.onnx.json
  temp/
    temp_*.wav    # Тимчасові WAV файли (автоматично видаляються)
    temp_*.mp3    # Тимчасові MP3 файли (автоматично видаляються)
```

---

## Роль папки `temp`

Папка **`%LOCALAPPDATA%\InsaitTranslator\temp`** використовується для:

1. **Тимчасові WAV файли** - створюються під час синтезу мовлення через Piper
2. **Проміжні аудіо файли** - використовуються при конвертації в MP3
3. **Автоматичне очищення** - файли старші 24 годин видаляються методом `AppDataPaths.CleanupTempFiles()`

### Життєвий цикл тимчасового файлу:
```
1. SynthesizeToWavAsync() створює temp_GUID.wav
2. Piper записує синтезоване аудіо
3. Метод зчитує дані
4. Файл видаляється в блоці finally
```

---

## Технічні деталі

### Як працює ExtractEmbeddedPiperResourcesAsync()

1. Отримує список всіх вбудованих ресурсів через `Assembly.GetManifestResourceNames()`
2. Шукає ресурси з префіксом `PiperTTS.piper.`
3. Витягує відносний шлях з імені ресурсу
4. Копіює файл до `AppDataPaths.PiperDirectory`

### Приклад імені ресурсу:
```
Insait_Translator_Deutsch.Desktop.PiperTTS.piper.piper.exe
                                  ^^^^^^^^^^^^^^^^^^^^^^^^
                                  PiperTTS.piper.piper.exe
                                             ^^^^^^^^
                                             piper/piper.exe (результат)
```

### Як працює ExtractEmbeddedModelResourcesAsync()

1. Шукає ресурси з префіксом `PiperTTS.models.`
2. Коректно обробляє `.onnx` та `.onnx.json` файли
3. Копіює файли до `AppDataPaths.ModelsDirectory`

---

## Оновлені рядки локалізації

Видалено згадки про "завантаження" (downloading), оновлено на "ініціалізація" та "розпаковка":

### Українська (UK)
- `TtsDownloadingPiper` → "Ініціалізація Piper TTS..."
- `TtsExtractingPiper` → "Розпаковка Piper із вбудованих ресурсів..."
- `TtsDownloadingVoiceModel` → "Ініціалізація німецької моделі голосу..."

### English (EN)
- `TtsDownloadingPiper` → "Initializing Piper TTS..."
- `TtsExtractingPiper` → "Extracting Piper from embedded resources..."
- `TtsDownloadingVoiceModel` → "Initializing German voice model..."

### Deutsch (DE)
- `TtsDownloadingPiper` → "Piper TTS wird initialisiert..."
- `TtsExtractingPiper` → "Piper wird aus eingebetteten Ressourcen entpackt..."
- `TtsDownloadingVoiceModel` → "Deutsches Sprachmodell wird initialisiert..."

### Русский (RU)
- `TtsDownloadingPiper` → "Инициализация Piper TTS..."
- `TtsExtractingPiper` → "Распаковка Piper из встроенных ресурсов..."
- `TtsDownloadingVoiceModel` → "Инициализация немецкой голосовой модели..."

### Türkçe (TR)
- `TtsDownloadingPiper` → "Piper TTS başlatılıyor..."
- `TtsExtractingPiper` → "Piper gömülü kaynaklardan çıkarılıyor..."
- `TtsDownloadingVoiceModel` → "Almanca ses modeli başlatılıyor..."

---

## Переваги для Microsoft Store

### ✅ Що виправлено:
- ❌ **Було**: Завантаження з GitHub/HuggingFace при першому запуску
- ✅ **Стало**: Копіювання з вбудованих ресурсів (офлайн)

### ✅ Сумісність:
- Немає потреби в мережевих дозволах для TTS
- Повна офлайн функціональність після встановлення
- Швидша ініціалізація (немає мережевих затримок)

### ✅ Розмір додатку:
- Piper TTS: ~15 MB
- Модель Thorsten: ~65 MB
- **Загальний додатковий розмір**: ~80 MB

---

## Як оновити Piper TTS або модель

1. Завантажити нову версію Piper з [GitHub Releases](https://github.com/rhasspy/piper/releases)
2. Замінити файли в `Insait_Translator_Deutsch.Desktop/PiperTTS/piper/piper/`
3. Для оновлення моделі - завантажити з [Piper Voices](https://huggingface.co/rhasspy/piper-voices)
4. Замінити файли в `Insait_Translator_Deutsch.Desktop/PiperTTS/models/`
5. Перебілдити проект

---

## Видалені залежності

Видалено з `TextToSpeechService.cs`:
- ❌ `System.IO.Compression` (використовувався для ZipFile.ExtractToDirectory)
- ❌ `System.Net.Http` (використовувався для завантаження)
- ❌ `HttpClient` екземпляр
- ❌ URL константи: `PIPER_WINDOWS_URL`, `MODEL_URL`, `MODEL_CONFIG_URL`

---

## Файли змінено

1. **Insait_Translator_Deutsch.Desktop.csproj**
   - Додано `<EmbeddedResource>` для PiperTTS та моделей
   
2. **TextToSpeechService.cs**
   - Видалено код завантаження
   - Додано `ExtractEmbeddedPiperResourcesAsync()`
   - Додано `ExtractEmbeddedModelResourcesAsync()`
   - Додано `ExtractRelativePathFromResourceName()`
   
3. **AppDataPaths.cs**
   - Оновлено коментарі для `TempDirectory`
   
4. **LocalizationManager.cs**
   - Оновлено рядки TTS для всіх 5 мов

---

## Тестування

Перед публікацією в Microsoft Store необхідно перевірити:

1. ✅ Чистий запуск (видалити `%LOCALAPPDATA%\InsaitTranslator`)
2. ✅ Перша ініціалізація TTS (перевірити копіювання файлів)
3. ✅ Синтез мовлення (перевірити роботу Piper)
4. ✅ Експорт в MP3 (перевірити конвертацію)
5. ✅ Повторний запуск (перевірити що файли не копіюються повторно)

---

**Версія документа**: 1.0  
**Дата**: 11 лютого 2026  
**Автор**: Insait Translator Team

