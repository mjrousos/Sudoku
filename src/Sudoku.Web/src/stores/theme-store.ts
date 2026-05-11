import { defineStore } from 'pinia'
import { ref, watch } from 'vue'

type ThemePreference = 'light' | 'dark'

const storageKey = 'sudoku-theme'

export const useThemeStore = defineStore('theme', () => {
  const preference = ref<ThemePreference>(readInitialTheme())

  watch(preference, (value) => {
    document.documentElement.dataset.theme = value
    localStorage.setItem(storageKey, value)
  }, { immediate: true })

  function toggleTheme(): void {
    preference.value = preference.value === 'dark' ? 'light' : 'dark'
  }

  return {
    preference,
    toggleTheme,
  }
})

function readInitialTheme(): ThemePreference {
  const storedTheme = localStorage.getItem(storageKey)
  if (storedTheme === 'light' || storedTheme === 'dark') {
    return storedTheme
  }

  return typeof window.matchMedia === 'function' && window.matchMedia('(prefers-color-scheme: dark)').matches
    ? 'dark'
    : 'light'
}
