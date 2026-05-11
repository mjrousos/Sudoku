import { createPinia, setActivePinia } from 'pinia'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

import type { CurrentUserResponse } from '@/api/contracts'
import { clearAntiforgeryTokenForTests } from '@/api/http-client'
import { useAuthStore } from '@/stores/auth-store'

describe('auth store', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    clearAntiforgeryTokenForTests()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('treats unauthorized current-user responses as anonymous', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValueOnce(problemResponse({ title: 'Unauthorized' }, 401)))

    const auth = useAuthStore()
    await auth.initialize()

    expect(auth.initialized).toBe(true)
    expect(auth.isAuthenticated).toBe(false)
  })

  it('signs in for development and refreshes antiforgery before profile updates', async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(jsonResponse({ requestToken: 'anonymous-token' }))
      .mockResolvedValueOnce(jsonResponse(currentUser('dev-player')))
      .mockResolvedValueOnce(jsonResponse([]))
      .mockResolvedValueOnce(jsonResponse({ requestToken: 'signed-in-token' }))
      .mockResolvedValueOnce(jsonResponse(currentUser('better-name')))
    vi.stubGlobal('fetch', fetchMock)

    const auth = useAuthStore()
    await auth.signInForDevelopment('dev-player', 'dev@example.test')
    await auth.saveUsername('better-name')

    expect(auth.currentUser?.username).toBe('better-name')
    expect(fetchMock).toHaveBeenNthCalledWith(4, '/api/antiforgery/token', {
      credentials: 'include',
    })

    const updateHeaders = fetchMock.mock.calls[4][1].headers as Headers
    expect(updateHeaders.get('X-XSRF-TOKEN')).toBe('signed-in-token')
  })
})

function currentUser(username: string): CurrentUserResponse {
  return {
    email: `${username}@example.test`,
    id: '00000000-0000-0000-0000-000000000001',
    linkedLogins: [{
      displayName: 'Development',
      email: `${username}@example.test`,
      isPrimary: true,
      provider: 'Development',
    }],
    profilePictureUrl: undefined,
    username,
    usernameConfirmed: true,
  }
}

function jsonResponse(body: unknown): Response {
  return new Response(JSON.stringify(body), {
    headers: { 'content-type': 'application/json' },
    status: 200,
  })
}

function problemResponse(body: unknown, status: number): Response {
  return new Response(JSON.stringify(body), {
    headers: { 'content-type': 'application/problem+json' },
    status,
  })
}
