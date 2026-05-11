<template>
  <section class="profile-page">
    <div class="profile-hero">
      <div class="avatar" aria-hidden="true">
        <img v-if="auth.currentUser?.profilePictureUrl" :src="auth.currentUser.profilePictureUrl" alt="" />
        <span v-else>{{ initials }}</span>
      </div>
      <div>
        <p class="eyebrow">Player profile</p>
        <h1>{{ auth.currentUser?.username }}</h1>
        <p>{{ auth.currentUser?.email ?? 'No primary email selected yet.' }}</p>
      </div>
    </div>

    <div class="profile-grid">
      <form class="panel" @submit.prevent="saveUsername">
        <h2>Public username</h2>
        <p>This is the name that will appear on leaderboards.</p>
        <label>
          Username
          <input v-model="username" minlength="3" maxlength="20" pattern="[a-zA-Z0-9_-]{3,20}" required />
        </label>
        <button type="submit">Save username</button>
      </form>

      <div class="panel">
        <h2>Linked providers</h2>
        <p>Keep at least one provider linked so you can get back into your account.</p>
        <div class="login-list">
          <article v-for="login in auth.currentUser?.linkedLogins" :key="login.provider" class="login-row">
            <div>
              <strong>{{ login.displayName }}</strong>
              <small>{{ login.email ?? login.provider }}</small>
            </div>
            <span v-if="login.isPrimary" class="pill">Primary</span>
            <button v-else type="button" @click="auth.makePrimary(login.provider)">Make primary</button>
            <button type="button" @click="auth.unlink(login.provider)">Unlink</button>
          </article>
        </div>
        <div v-if="linkableProviders.length > 0" class="link-list">
          <button
            v-for="provider in linkableProviders"
            :key="provider.provider"
            :disabled="!provider.isConfigured"
            type="button"
            @click="auth.challengeProvider(provider.provider, '/profile')"
          >
            Link {{ provider.displayName }}
          </button>
        </div>
      </div>

      <div class="panel">
        <h2>Account controls</h2>
        <p>Upload an avatar, export your data, or permanently remove the account.</p>
        <label>
          Profile picture
          <input accept="image/png,image/jpeg,image/webp" type="file" @change="uploadPicture" />
        </label>
        <button type="button" :disabled="!auth.currentUser?.profilePictureUrl" @click="auth.removePicture">Remove picture</button>
        <button type="button" @click="auth.downloadAccountExport">Export account JSON</button>
        <button class="danger-button" type="button" @click="deleteAccount">Delete account</button>
        <button type="button" @click="signOut">Sign out</button>
      </div>
    </div>
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRouter } from 'vue-router'

import { useAuthStore } from '../stores/auth-store'

const auth = useAuthStore()
const router = useRouter()
const username = ref(auth.currentUser?.username ?? '')
const initials = computed(() => auth.currentUser?.username.slice(0, 2).toUpperCase() ?? '??')
const linkableProviders = computed(() => auth.providers.filter((provider) => !provider.isLinked))

onMounted(async () => {
  await auth.loadProviders()
})

watch(() => auth.currentUser?.username, (value) => {
  username.value = value ?? ''
})

async function saveUsername(): Promise<void> {
  await auth.saveUsername(username.value)
}

async function signOut(): Promise<void> {
  await auth.signOutCurrentUser()
  await router.push('/')
}

async function uploadPicture(event: Event): Promise<void> {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]
  if (file === undefined) {
    return
  }

  await auth.uploadPicture(file)
  input.value = ''
}

async function deleteAccount(): Promise<void> {
  if (!window.confirm('Delete this Sudoku account and all saved data? This cannot be undone.')) {
    return
  }

  await auth.deleteCurrentAccount()
  await router.push('/')
}
</script>

<style scoped>
.profile-page {
  display: grid;
  gap: 1.5rem;
}

.profile-hero,
.panel {
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  box-shadow: 0 20px 70px var(--shadow-soft);
}

.profile-hero {
  align-items: center;
  border-radius: 2rem;
  display: flex;
  gap: 1.25rem;
  padding: clamp(1rem, 3vw, 2rem);
}

.avatar {
  align-items: center;
  background: var(--color-ink);
  border-radius: 1.5rem;
  color: var(--color-paper);
  display: flex;
  flex: 0 0 5rem;
  font-family: var(--font-display);
  font-size: 2rem;
  font-weight: 900;
  height: 5rem;
  justify-content: center;
  overflow: hidden;
  width: 5rem;
}

.avatar img {
  height: 100%;
  object-fit: cover;
  width: 100%;
}

.eyebrow {
  color: var(--color-accent);
  font-size: 0.75rem;
  font-weight: 900;
  letter-spacing: 0.16em;
  margin: 0;
  text-transform: uppercase;
}

h1,
h2 {
  font-family: var(--font-display);
  letter-spacing: -0.05em;
  margin: 0;
}

h1 {
  font-size: clamp(2.4rem, 7vw, 5rem);
  line-height: 0.95;
}

.profile-hero p,
.panel p,
.login-row small {
  color: var(--color-muted);
}

.profile-grid {
  display: grid;
  gap: 1rem;
  grid-template-columns: repeat(3, minmax(0, 1fr));
}

.panel {
  border-radius: 1.5rem;
  display: grid;
  gap: 0.8rem;
  padding: 1.25rem;
}

.panel label {
  color: var(--color-muted);
  display: grid;
  font-weight: 800;
  gap: 0.35rem;
}

.panel input {
  background: var(--color-paper);
  border: 1px solid var(--color-border);
  border-radius: 1rem;
  color: var(--color-text);
  padding: 0.85rem 1rem;
}

.panel button,
.login-row button {
  background: var(--color-ink);
  border: 0;
  border-radius: 999px;
  color: var(--color-paper);
  cursor: pointer;
  font-weight: 900;
  min-height: 2.6rem;
  padding: 0.65rem 1rem;
}

.panel button:disabled {
  cursor: not-allowed;
  opacity: 0.5;
}

.danger-button {
  background: var(--color-danger) !important;
}

.login-list,
.link-list {
  display: grid;
  gap: 0.65rem;
}

.link-list {
  border-top: 1px dashed var(--color-border);
  padding-top: 0.8rem;
}

.login-row {
  align-items: center;
  border: 1px solid var(--color-border);
  border-radius: 1rem;
  display: grid;
  gap: 0.65rem;
  grid-template-columns: 1fr auto auto;
  padding: 0.75rem;
}

.login-row div {
  display: grid;
}

.pill {
  background: var(--color-selected);
  border-radius: 999px;
  color: var(--color-text);
  font-weight: 900;
  padding: 0.45rem 0.7rem;
}

@media (max-width: 980px) {
  .profile-grid {
    grid-template-columns: 1fr;
  }
}
</style>
