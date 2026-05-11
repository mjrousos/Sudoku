<template>
  <section class="auth-page">
    <div class="auth-card">
      <p class="eyebrow">Account optional</p>
      <h1>Sign in when you want history, stats, and leaderboards.</h1>
      <p class="lede">Gameplay stays open to everyone. Social sign-in only unlocks persistence and public rankings.</p>

      <div class="provider-list">
        <button
          v-for="provider in auth.providers"
          :key="provider.provider"
          class="provider-button"
          :disabled="!provider.isConfigured"
          type="button"
          @click="auth.challengeProvider(provider.provider, returnUrl)"
        >
          <span>{{ provider.displayName }}</span>
          <small>{{ provider.isConfigured ? 'Continue' : 'Not configured locally' }}</small>
        </button>
      </div>

      <form v-if="isDevelopment" class="dev-sign-in" @submit.prevent="signInDevelopment">
        <label>
          Development username
          <input v-model="devUsername" autocomplete="username" minlength="3" maxlength="20" pattern="[a-zA-Z0-9_-]{3,20}" required />
        </label>
        <button type="submit" :disabled="auth.isLoading">Use development sign-in</button>
      </form>

      <p v-if="auth.lastError" class="error" role="alert">{{ auth.lastError }}</p>
    </div>
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'

import { useAuthStore } from '../stores/auth-store'

const auth = useAuthStore()
const route = useRoute()
const router = useRouter()
const devUsername = ref('dev-player')
const isDevelopment = import.meta.env.DEV
const returnUrl = computed(() => typeof route.query.returnUrl === 'string' ? route.query.returnUrl : '/profile')

onMounted(async () => {
  await auth.loadProviders()
})

async function signInDevelopment(): Promise<void> {
  await auth.signInForDevelopment(devUsername.value, `${devUsername.value}@example.test`)
  if (auth.isAuthenticated) {
    await router.push(returnUrl.value)
  }
}
</script>

<style scoped>
.auth-page {
  display: grid;
  min-height: 68vh;
  place-items: center;
}

.auth-card {
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: 2rem;
  box-shadow: 0 24px 90px var(--shadow-soft);
  display: grid;
  gap: 1.25rem;
  max-width: 720px;
  padding: clamp(1.25rem, 4vw, 2.5rem);
}

.eyebrow {
  color: var(--color-accent);
  font-size: 0.75rem;
  font-weight: 900;
  letter-spacing: 0.16em;
  margin: 0;
  text-transform: uppercase;
}

h1 {
  font-family: var(--font-display);
  font-size: clamp(2.2rem, 6vw, 4.8rem);
  letter-spacing: -0.06em;
  line-height: 0.95;
  margin: 0;
}

.lede {
  color: var(--color-muted);
  line-height: 1.7;
  margin: 0;
}

.provider-list,
.dev-sign-in {
  display: grid;
  gap: 0.8rem;
}

.provider-button,
.dev-sign-in button {
  align-items: center;
  background: var(--color-ink);
  border: 0;
  border-radius: 999px;
  color: var(--color-paper);
  cursor: pointer;
  display: flex;
  font-weight: 900;
  justify-content: space-between;
  min-height: 3.2rem;
  padding: 0.75rem 1rem;
}

.provider-button:disabled {
  background: var(--color-peer);
  color: var(--color-muted);
  cursor: not-allowed;
}

.provider-button small {
  font-weight: 700;
}

.dev-sign-in {
  border-top: 1px dashed var(--color-border);
  padding-top: 1rem;
}

.dev-sign-in label {
  color: var(--color-muted);
  display: grid;
  font-weight: 800;
  gap: 0.35rem;
}

.dev-sign-in input {
  background: var(--color-paper);
  border: 1px solid var(--color-border);
  border-radius: 1rem;
  color: var(--color-text);
  padding: 0.85rem 1rem;
}

.error {
  background: var(--color-danger-soft);
  border: 1px solid color-mix(in srgb, var(--color-danger) 45%, var(--color-border));
  border-radius: 1rem;
  color: var(--color-danger);
  margin: 0;
  padding: 0.8rem 1rem;
}
</style>
