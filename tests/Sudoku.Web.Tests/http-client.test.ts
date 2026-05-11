import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

import { ApiError, apiFetchJson, buildApiUrl, clearAntiforgeryTokenForTests } from '@/api/http-client'

describe('http client', () => {
  beforeEach(() => {
    clearAntiforgeryTokenForTests()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('builds API URLs from relative paths', () => {
    expect(buildApiUrl('/api/status')).toBe('/api/status')
  })

  it('adds credentials and antiforgery headers for state-changing JSON requests', async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(jsonResponse({ requestToken: 'token-1' }))
      .mockResolvedValueOnce(jsonResponse({ gameId: 'game-1' }))

    vi.stubGlobal('fetch', fetchMock)

    await apiFetchJson('/api/games', {
      method: 'POST',
      body: { difficulty: 'easy' },
    })

    expect(fetchMock).toHaveBeenNthCalledWith(1, '/api/antiforgery/token', {
      credentials: 'include',
    })
    expect(fetchMock).toHaveBeenNthCalledWith(2, '/api/games', expect.objectContaining({
      body: JSON.stringify({ difficulty: 'easy' }),
      credentials: 'include',
      method: 'POST',
    }))

    const requestHeaders = fetchMock.mock.calls[1][1].headers as Headers
    expect(requestHeaders.get('Content-Type')).toBe('application/json')
    expect(requestHeaders.get('X-XSRF-TOKEN')).toBe('token-1')
  })

  it('parses Problem Details errors', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValueOnce(problemResponse({
      title: 'Invalid request',
      status: 400,
      code: 'validation_failed',
    })))

    await expect(apiFetchJson('/api/status')).rejects.toMatchObject<ApiError>({
      status: 400,
      code: 'validation_failed',
    })
  })

  it('retries once for retryable Problem Details errors', async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(jsonResponse({ requestToken: 'token-1' }))
      .mockResolvedValueOnce(problemResponse({
        title: 'Generation timed out',
        status: 503,
        code: 'generation_timeout',
        retryable: true,
      }))
      .mockResolvedValueOnce(jsonResponse({ gameId: 'game-1' }))

    vi.stubGlobal('fetch', fetchMock)

    await apiFetchJson('/api/games', {
      method: 'POST',
      body: { difficulty: 'hard' },
      retryOnRetryableProblem: true,
    })

    expect(fetchMock).toHaveBeenCalledTimes(3)
  })
})

function jsonResponse(body: unknown): Response {
  return new Response(JSON.stringify(body), {
    headers: { 'content-type': 'application/json' },
    status: 200,
  })
}

function problemResponse(body: unknown): Response {
  return new Response(JSON.stringify(body), {
    headers: { 'content-type': 'application/problem+json' },
    status: 400,
  })
}
