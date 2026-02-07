/// <reference types="node" />
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig(() => {
  const host = process.env.INSAIT_UI_HOST ?? '127.0.0.1'
  const port = Number(process.env.INSAIT_UI_PORT ?? '4200')
  const backendUrl = process.env.INSAIT_BACKEND_URL ?? 'http://127.0.0.1:5050'

  return {
    plugins: [react()],
    base: './',
    build: {
      outDir: 'dist',
      emptyOutDir: true,
    },
    server: {
      // Avalonia Desktop expects this port during DEBUG proxying.
      port,
      strictPort: true,
      // Bind explicitly so the .NET host polling always works.
      host,
      proxy: {
        '/api': {
          // Native backend host runs on 5050 in Desktop.
          target: backendUrl,
          changeOrigin: true,
          // Retry with localhost if 127.0.0.1 fails
          configure: (proxy) => {
            proxy.on('error', (err, _req, res) => {
              console.log('[Vite Proxy] Error:', err.message);
              if ('writeHead' in res && typeof res.writeHead === 'function') {
                res.writeHead(502, { 'Content-Type': 'application/json' });
                res.end(JSON.stringify({ error: 'Backend unavailable', message: err.message }));
              }
            });
          },
        },
      },
    },
  }
})
