import { defineConfig, devices } from '@playwright/test'

export default defineConfig({
  testDir: './tests',
  timeout: 30_000,
  reporter: [['list'], ['html', { open: 'never' }]],
  retries: process.env.CI ? 2 : 0,
  use: {
    baseURL: process.env.SUDOKU_E2E_BASE_URL ?? 'http://localhost:5173',
    trace: 'on-first-retry',
  },
  webServer: process.env.SUDOKU_E2E_BASE_URL
    ? undefined
    : {
        command: 'npm run dev --workspace @sudoku/web -- --host 127.0.0.1 --port 5173',
        cwd: '../..',
        reuseExistingServer: !process.env.CI,
        timeout: 120_000,
        url: 'http://127.0.0.1:5173',
      },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
})
