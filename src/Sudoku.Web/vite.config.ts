import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

// https://vite.dev/config/
export default defineConfig({
  plugins: [vue()],
  server: {
    proxy: process.env.SUDOKU_API_PROXY_TARGET
      ? {
          '/api': {
            changeOrigin: true,
            secure: false,
            target: process.env.SUDOKU_API_PROXY_TARGET,
          },
        }
      : undefined,
  },
})
