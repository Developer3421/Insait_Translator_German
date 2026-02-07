# React Translator Component

Цей React компонент дублює функціонал Angular перекладача.

## Функціонал

- 🔄 Переклад українського тексту на німецьку мову
- 🔊 Озвучення німецького тексту (TTS)
- 📋 Копіювання/вставка тексту
- 💾 Завантаження MP3 файлу з озвученням
- ⌨️ Гаряча клавіша `Ctrl+Enter` для перекладу

## Структура проекту

```
ReactComponent/
├── index.html          # HTML точка входу
├── package.json        # NPM залежності
├── tsconfig.json       # TypeScript конфігурація
├── vite.config.ts      # Vite конфігурація
└── src/
    ├── main.tsx        # React точка входу
    ├── App.tsx         # Головний компонент
    ├── App.css         # Стилі компонента
    ├── index.css       # Глобальні стилі
    └── services/
        └── translatorService.ts  # API сервіс
```

## Команди

```bash
# Встановлення залежностей
npm install

# Запуск в режимі розробки
npm run dev

# Збірка для продакшену
npm run build

# Попередній перегляд збірки
npm run preview
```

## API Endpoints

Компонент використовує ті ж API ендпоінти, що й Angular версія:

- `GET /api/health` - Перевірка доступності бекенду
- `POST /api/translate` - Переклад тексту
- `POST /api/speak-mp3` - Генерація MP3 аудіо

## Інтеграція з Avalonia Desktop

Збудований React додаток автоматично включається як embedded resources в Desktop проект.
Для збірки:

```bash
cd ReactComponent
npm run build
```

Результат буде в папці `dist/` і буде включено в .NET збірку.

