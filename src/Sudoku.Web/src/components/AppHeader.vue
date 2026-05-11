<template>
  <header class="app-header">
    <RouterLink class="brand" to="/">Sudoku</RouterLink>
    <nav class="nav" aria-label="Primary">
      <RouterLink to="/">Play</RouterLink>
      <RouterLink to="/leaderboards">Leaderboards</RouterLink>
      <RouterLink to="/history">History</RouterLink>
      <RouterLink to="/how-to-play">How to Play</RouterLink>
    </nav>
    <div class="header-actions">
      <button type="button" class="theme-toggle" @click="theme.toggleTheme">
        {{ theme.preference === 'dark' ? 'Light' : 'Dark' }} mode
      </button>
      <RouterLink v-if="!auth.isAuthenticated" class="sign-in" to="/sign-in">Sign in</RouterLink>
      <RouterLink v-else class="sign-in" to="/profile">{{ auth.currentUser?.username }}</RouterLink>
    </div>
  </header>
</template>

<script setup lang="ts">
import { RouterLink } from 'vue-router'

import { useAuthStore } from '../stores/auth-store'
import { useThemeStore } from '../stores/theme-store'

const auth = useAuthStore()
const theme = useThemeStore()
</script>

<style scoped>
.app-header {
  align-items: center;
  background: var(--color-surface);
  border-bottom: 1px solid var(--color-border);
  display: flex;
  gap: 1rem;
  justify-content: space-between;
  padding: 1rem clamp(1rem, 3vw, 2rem);
}

.brand {
  color: var(--color-text);
  font-size: 1.35rem;
  font-weight: 800;
  text-decoration: none;
}

.nav,
.header-actions {
  align-items: center;
  display: flex;
  gap: 0.75rem;
}

.nav a,
.sign-in {
  color: var(--color-muted);
  font-weight: 650;
  text-decoration: none;
}

.nav a.router-link-active,
.nav a:hover,
.sign-in:hover {
  color: var(--color-accent);
}

.theme-toggle {
  background: transparent;
  border: 1px solid var(--color-border);
  border-radius: 999px;
  color: var(--color-text);
  cursor: pointer;
  font: inherit;
  padding: 0.45rem 0.75rem;
}

@media (max-width: 720px) {
  .app-header {
    align-items: flex-start;
    flex-direction: column;
  }

  .nav,
  .header-actions {
    flex-wrap: wrap;
  }
}
</style>
