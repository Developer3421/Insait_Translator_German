# Insait Translator: German

A privacy-first Windows desktop translator focused on translating **any language to German**, with optional German Text-to-Speech.

## Overview
Insait Translator: German is a Windows desktop app built with Avalonia. It translates text using online providers (with automatic fallback) and stores your workspaces/settings locally.

The app **does not send your texts or settings to the developer’s servers**. Network requests are made **only** to the translation provider(s) you choose to use.

## Features
- Translate **any language: German**
- Provider system with fallback:
  - **MyMemory** (free API with limits)
  - Fallback to **Google Translate** via **GTranslate**
  - Optional: **Google Gemini API** (requires your API key)
- Workspace tabs (restore your texts after restarting the app)
- German Text-to-Speech (TTS) via **Piper**
  - Playback
  - MP3 export

## Tech stack
- **.NET 10** + **Avalonia 11**
- **ReactiveUI**
- Translation: MyMemory API, **GTranslate**, optional **Google Gemini API**
- Local storage: **LiteDB**
- Security: **AES-256** encryption + **Windows DPAPI (CurrentUser)** protection for the encryption key
- Audio/TTS: **Piper TTS**, **NAudio**, **LibVLCSharp**

## Privacy
- No developer telemetry/analytics.
- Data is stored locally under your Windows profile (Local AppData).
- Sensitive settings (e.g., API key) are encrypted at rest.
- Your text is sent over the network only when you use an online translation provider.

See: `PRIVACY_POLICY.md`

## Project structure
- `Views/`, `ViewModels/` — Avalonia UI (MVVM)
- `Services/TranslationService.cs` — translation providers + fallback
- `Services/SettingsService.cs` — encrypted local settings
- `Services/WorkspaceDatabaseService.cs` — workspace persistence
- `Services/NativeBackend/` — local backend services (if used by the app)
- `ReactComponent/` — optional React UI (source included in repo)
- `PiperTTS/` — bundled Piper runtime and voice model(s)

## License / usage
This repository uses a **split license** approach:

- **Source code**: you may use, modify, and redistribute the code in this repository.
- **UI/window style and visual assets** (images/icons/styles): **not** included in the permission above. You may not reuse the app’s window style/theme or bundled assets outside this project without explicit permission.

See `LICENSE.md` for the full text.
