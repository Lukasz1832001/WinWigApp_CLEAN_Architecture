import { defineConfig } from 'vite'
import path from 'path'
import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'

function winwigAssetResolver() {
    return {
        name: 'winwig-asset-resolver',
        resolveId(id) {
            if (id.startsWith('winwig:asset/')) {
                const filename = id.replace('winwig:asset/', '')
                return path.resolve(__dirname, 'src/assets', filename)
            }
        },
    }
}

export default defineConfig({
    plugins: [
        winwigAssetResolver(),
        react(),
        tailwindcss(),
    ],
    resolve: {
        alias: {
            '@': path.resolve(__dirname, './src'),
        },
    },
    server: {
        port: 5173, // Standardowy port Vite
        proxy: {
            // Przekierowuje zapytania /api na port backendu ASP.NET
            '^/api': {
                target: 'https://localhost:7054',
                secure: false
            }
        }
    },
    assetsInclude: ['**/*.svg', '**/*.csv'],
})