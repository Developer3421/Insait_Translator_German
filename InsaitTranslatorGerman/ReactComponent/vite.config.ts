/// <reference types="node" />
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig(() => {
  const host = process.env.INSAIT_UI_HOST ?? '0.0.0.0'
  // React UI (Vite dev server)
  const port = Number(process.env.INSAIT_UI_PORT ?? '4200')
  // Native backend (HttpListener) – loopback only
  const backendUrl = process.env.INSAIT_BACKEND_URL ?? 'http://127.0.0.1:4201'

  return {
    plugins: [react()],
    base: './',
    build: {
      outDir: 'dist',
      emptyOutDir: true,
    },
    server: {
      // Avalonia Desktop expects a fixed port during DEBUG proxying.
      port,
      strictPort: true,
      // Bind to all network interfaces to allow access from other devices on the network
      host,
      proxy: {
        '/api': {
          // Native backend host runs on 4201 in Desktop.
          target: backendUrl,
          changeOrigin: true,
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
