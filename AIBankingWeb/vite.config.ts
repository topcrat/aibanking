import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

const apiTarget = process.env.services__aibanking__https__0
  ?? process.env.services__aibanking__http__0
  ?? 'https://localhost:7164';

const agentTarget = process.env.services__aiagent__https__0
  ?? process.env.services__aiagent__http__0
  ?? 'https://localhost:7165';

export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    port: parseInt(process.env.PORT ?? '5173'),
    proxy: {
      '/api/agent': {
        target: agentTarget,
        changeOrigin: true,
        secure: false,
      },
      '/api': {
        target: apiTarget,
        changeOrigin: true,
        secure: false,
      },
    },
  },
})
