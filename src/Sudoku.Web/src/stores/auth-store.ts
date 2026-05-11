import { defineStore } from 'pinia'
import { computed, ref } from 'vue'

import type { AuthProviderResponse, CurrentUserResponse } from '../api/contracts'
import {
  developmentSignIn,
  deleteAccount,
  deleteProfilePicture,
  exportAccount,
  getAuthProviders,
  getCurrentUser,
  getProviderChallengeUrl,
  setPrimaryLogin,
  signOut,
  unlinkProvider,
  updateProfile,
  uploadProfilePicture,
} from '../api/auth'
import { ApiError } from '../api/http-client'

export const useAuthStore = defineStore('auth', () => {
  const currentUser = ref<CurrentUserResponse | null>(null)
  const providers = ref<AuthProviderResponse[]>([])
  const initialized = ref(false)
  const isLoading = ref(false)
  const lastError = ref<string | null>(null)
  const isAuthenticated = computed(() => currentUser.value !== null)

  async function initialize(): Promise<void> {
    if (initialized.value) {
      return
    }

    isLoading.value = true
    try {
      currentUser.value = await getCurrentUser()
    } catch (error) {
      if (!(error instanceof ApiError) || error.status !== 401) {
        lastError.value = describeAuthError(error)
      }

      currentUser.value = null
    } finally {
      initialized.value = true
      isLoading.value = false
    }
  }

  async function loadProviders(): Promise<void> {
    providers.value = await getAuthProviders()
  }

  function challengeProvider(provider: string, returnUrl = window.location.pathname): void {
    window.location.assign(getProviderChallengeUrl(provider, returnUrl))
  }

  async function signInForDevelopment(username: string, email?: string): Promise<void> {
    isLoading.value = true
    lastError.value = null
    try {
      currentUser.value = await developmentSignIn({
        email,
        subject: username,
        username,
      })
      initialized.value = true
      await loadProviders()
    } catch (error) {
      lastError.value = describeAuthError(error)
    } finally {
      isLoading.value = false
    }
  }

  async function signOutCurrentUser(): Promise<void> {
    await signOut()
    currentUser.value = null
    providers.value = []
    initialized.value = true
  }

  async function saveUsername(username: string): Promise<void> {
    currentUser.value = await updateProfile({ username })
  }

  async function makePrimary(provider: string): Promise<void> {
    currentUser.value = await setPrimaryLogin({ provider })
    await loadProviders()
  }

  async function unlink(provider: string): Promise<void> {
    currentUser.value = await unlinkProvider(provider)
    await loadProviders()
  }

  async function uploadPicture(file: File): Promise<void> {
    const response = await uploadProfilePicture(file)
    if (currentUser.value !== null) {
      currentUser.value = {
        ...currentUser.value,
        profilePictureUrl: response.profilePictureUrl ?? undefined,
      }
    }
  }

  async function removePicture(): Promise<void> {
    await deleteProfilePicture()
    if (currentUser.value !== null) {
      currentUser.value = {
        ...currentUser.value,
        profilePictureUrl: undefined,
      }
    }
  }

  async function downloadAccountExport(): Promise<void> {
    const blob = await exportAccount()
    const url = URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url
    link.download = 'sudoku-account-export.json'
    link.click()
    URL.revokeObjectURL(url)
  }

  async function deleteCurrentAccount(): Promise<void> {
    await deleteAccount()
    currentUser.value = null
    providers.value = []
    initialized.value = true
  }

  function setCurrentUser(user: CurrentUserResponse | null): void {
    currentUser.value = user
    initialized.value = true
  }

  return {
    challengeProvider,
    currentUser,
    initialize,
    initialized,
    isAuthenticated,
    isLoading,
    lastError,
    deleteCurrentAccount,
    downloadAccountExport,
    loadProviders,
    makePrimary,
    providers,
    removePicture,
    saveUsername,
    setCurrentUser,
    signInForDevelopment,
    signOutCurrentUser,
    unlink,
    uploadPicture,
  }
})

function describeAuthError(error: unknown): string {
  if (error instanceof ApiError) {
    return error.problem?.detail ?? error.message
  }

  return 'Authentication is unavailable. Try again.'
}
