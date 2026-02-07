export interface TranslateResponse {
  translation: string;
}

export interface HealthResponse {
  ok: boolean;
}

// В режимі розробки Vite проксіює /api/* до бекенду на 5050.
// В продакшені (embedded) всі запити йдуть на той самий хост.
const baseUrl = '';

/**
 * Check if the native backend is available
 */
export async function checkHealth(): Promise<boolean> {
  try {
    console.log('[TranslatorService] Checking backend health...');
    const response = await fetch(`${baseUrl}/api/health`, {
      // Додаємо таймаут через AbortController
      signal: AbortSignal.timeout(5000)
    });
    console.log('[TranslatorService] Health response status:', response.status);
    if (!response.ok) return false;
    const data: HealthResponse = await response.json();
    console.log('[TranslatorService] Health response data:', data);
    return data.ok;
  } catch (error) {
    console.error('[TranslatorService] Health check failed:', error);
    return false;
  }
}

/**
 * Translate text from Ukrainian to German
 */
export async function translate(
  text: string,
  sourceLang = 'uk',
  targetLang = 'de'
): Promise<string> {
  console.log('[TranslatorService] Sending translate request...');
  
  const response = await fetch(`${baseUrl}/api/translate`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({
      Text: text,
      SourceLang: sourceLang,
      TargetLang: targetLang,
    }),
  });

  console.log('[TranslatorService] Translate response status:', response.status);

  if (!response.ok) {
    let errorDetails = response.statusText;
    try {
      const errorJson = await response.json();
      console.error('[TranslatorService] Translation error details:', errorJson);
      errorDetails = errorJson.error || errorJson.details || errorDetails;
    } catch {
      // Ignore JSON parse errors
    }
    throw new Error(`Translation failed: ${errorDetails}`);
  }

  const data: TranslateResponse = await response.json();
  console.log('[TranslatorService] Translation successful');
  return data.translation;
}

/**
 * Get MP3 audio for text-to-speech
 */
export async function speakMp3(text: string): Promise<Blob> {
  console.log('[TranslatorService] Sending TTS request...');
  
  const response = await fetch(`${baseUrl}/api/speak-mp3`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({
      Text: text,
    }),
  });

  console.log('[TranslatorService] TTS response status:', response.status);

  if (!response.ok) {
    let errorDetails = response.statusText;
    try {
      const errorJson = await response.json();
      console.error('[TranslatorService] TTS error details:', errorJson);
      errorDetails = errorJson.error || errorJson.details || errorDetails;
    } catch {
      // Ignore JSON parse errors
    }
    throw new Error(`TTS failed: ${errorDetails}`);
  }

  console.log('[TranslatorService] TTS successful');
  return response.blob();
}

