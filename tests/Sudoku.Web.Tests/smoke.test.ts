import { render, screen } from '@testing-library/vue'
import { createPinia, setActivePinia } from 'pinia'
import { nextTick } from 'vue'

import App from '@/App.vue'
import { router } from '@/router'
import { useThemeStore } from '@/stores/theme-store'

describe('App', () => {
  beforeEach(() => {
    localStorage.clear()
    document.documentElement.removeAttribute('data-theme')
  })

  it('renders the application shell and default play route', async () => {
    const pinia = createPinia()
    setActivePinia(pinia)
    await router.push('/')
    await router.isReady()

    render(App, {
      global: {
        plugins: [pinia, router],
      },
    })

    expect(screen.getByRole('link', { name: 'Sudoku' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Ink a clean grid. No account required.' })).toBeInTheDocument()
  })

  it('redirects authenticated routes to sign-in when anonymous', async () => {
    const pinia = createPinia()
    setActivePinia(pinia)

    await router.push('/history')
    await router.isReady()

    expect(router.currentRoute.value.name).toBe('sign-in')
    expect(router.currentRoute.value.query.returnUrl).toBe('/history')
  })

  it('initializes and toggles the theme preference', async () => {
    localStorage.setItem('sudoku-theme', 'dark')
    const pinia = createPinia()
    setActivePinia(pinia)

    const theme = useThemeStore()
    expect(theme.preference).toBe('dark')

    theme.toggleTheme()
    await nextTick()

    expect(theme.preference).toBe('light')
    expect(document.documentElement.dataset.theme).toBe('light')
  })
})
