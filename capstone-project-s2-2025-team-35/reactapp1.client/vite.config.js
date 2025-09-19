import { fileURLToPath, URL } from 'node:url'
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
    plugins: [react()],
    resolve: {
        alias: { '@': fileURLToPath(new URL('./src', import.meta.url)) },
    },
    server: {
        port: 5173,            // fixed
        https: false,          // <- important: no HTTPS
        proxy: {
            '/api': {
                target: 'http://localhost:5089', // <- your API
                changeOrigin: true,
                secure: false,
            },
        },
    },
})
