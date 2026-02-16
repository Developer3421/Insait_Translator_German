# Privacy Policy (Insait Translator → German)

**Last updated:** 2026‑02‑15

This policy explains what data the app processes, what is stored on your device, and what is sent over the network.

## Summary
- The app **does not send any of your texts or settings to the developer’s servers**.
- Your data is stored **locally on your device**.
- Workspace/tab content is stored in a local database.
- Sensitive settings (for example, an API key) are stored in a local database and **encrypted with AES‑256**; the encryption key is protected by **Windows DPAPI (CurrentUser)**.

## What data the app stores locally
### 1) Workspace / tab content
To provide “workspace tabs” and restore them after restart, the app can store locally:
- workspace/tab titles
- selected tab state
- the text you entered (source text)
- the translated text (result)

This data is stored **only on your device** in a local LiteDB database under your Windows user profile (Local AppData).

### 2) Settings and configuration
The app stores settings such as (examples):
- UI language
- feature toggles
- provider preferences

### 3) API keys (optional)
If you enable **Google Gemini API** and enter an API key, the key is stored **locally**:
- in the settings database
- **encrypted using AES‑256**
- the AES key is derived/stored using **Windows DPAPI (user-level protection)**, so it can’t be decrypted by other Windows users on the same machine.

**Important:** The Gemini API key is **optional**. The app can still translate using other online providers without a Gemini key (subject to their limits).

## What data is sent over the network (translation providers)
The app will send data to third‑party services **only when you use features that require the Internet**:
- Online translation providers (e.g., MyMemory)
- Fallback translation via **GTranslate** (a client library)
- If enabled by you: **Google Gemini API**

**When you click “Translate”, your text is sent to the selected provider** (or to a fallback provider). This is necessary to get a translation.

### Provider limits / quotas
Third‑party providers may enforce limits (for example, per‑request character limits or daily quotas). If a provider rejects a request due to quota/limits, the app may:
- temporarily fall back to another provider (if available), or
- show an error if translation can’t be completed.

## What data is NOT sent
- The app does **not** upload your workspace/tab database to the developer.
- The app does **not** upload your settings database to the developer.
- The app does **not** send any analytics/telemetry to the developer’s servers.

## Local-only services
If you enable the optional Web UI, it is served **locally** on your device (loopback), for example:
- `http://127.0.0.1:4200`

This local UI is **not** hosted by the developer and is not intended to be accessible from other devices.

## Data retention and deletion
- You can delete your stored data by removing the app’s local data directory (Local AppData for your Windows user), or by uninstalling the app.
- If a local database becomes corrupted, the app may recreate it (which can reset stored settings/workspaces).

## Security notes
- Encryption is used to protect sensitive settings (like API keys) at rest.
- No method can guarantee absolute security on a compromised device; keep your OS account secure.

## Contact
If you have privacy questions or requests regarding your data, please contact us:
- **Email:** insait.privacy@proton.me
- **Website:** https://insait-apps.github.io/translator-german/

We will respond to your inquiry within 30 days.
