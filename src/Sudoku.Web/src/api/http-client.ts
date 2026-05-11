import type { AntiforgeryTokenResponse, ProblemDetails } from './contracts'

const defaultApiBaseUrl = ''
const unsafeMethods = new Set(['DELETE', 'PATCH', 'POST', 'PUT'])

let antiforgeryToken: string | undefined

export class ApiError extends Error {
  public readonly status: number
  public readonly problem?: ProblemDetails

  constructor(
    message: string,
    status: number,
    problem?: ProblemDetails,
  ) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.problem = problem
  }

  get code(): string | undefined {
    return typeof this.problem?.code === 'string' ? this.problem.code : undefined
  }

  get retryable(): boolean {
    return this.problem?.retryable === true
  }
}

export interface ApiRequestOptions extends Omit<RequestInit, 'body'> {
  body?: unknown
  retryOnRetryableProblem?: boolean
}

export function buildApiUrl(path: string): string {
  if (/^https?:\/\//i.test(path)) {
    return path
  }

  const configuredBaseUrl = import.meta.env.VITE_API_BASE_URL?.trim() ?? defaultApiBaseUrl
  const baseUrl = configuredBaseUrl.replace(/\/+$/, '')
  const normalizedPath = path.startsWith('/') ? path : `/${path}`

  return `${baseUrl}${normalizedPath}`
}

export async function apiFetchJson<TResponse>(
  path: string,
  options: ApiRequestOptions = {},
): Promise<TResponse> {
  return sendJson<TResponse>(path, options, options.retryOnRetryableProblem === true)
}

export async function apiDownload(path: string, options: ApiRequestOptions = {}): Promise<Blob> {
  const response = await send(path, options)
  return response.blob()
}

export function clearAntiforgeryToken(): void {
  antiforgeryToken = undefined
}

export const clearAntiforgeryTokenForTests = clearAntiforgeryToken

async function sendJson<TResponse>(
  path: string,
  options: ApiRequestOptions,
  canRetry: boolean,
): Promise<TResponse> {
  try {
    const response = await send(path, options)

    if (response.status === 204) {
      return undefined as TResponse
    }

    return (await response.json()) as TResponse
  } catch (error) {
    if (canRetry && error instanceof ApiError && error.retryable) {
      return sendJson<TResponse>(path, options, false)
    }

    throw error
  }
}

async function send(path: string, options: ApiRequestOptions): Promise<Response> {
  const method = (options.method ?? 'GET').toUpperCase()
  const headers = new Headers(options.headers)
  let body: BodyInit | null | undefined

  if (options.body instanceof FormData || options.body instanceof Blob) {
    body = options.body
  } else if (options.body !== undefined) {
    headers.set('Content-Type', 'application/json')
    body = JSON.stringify(options.body)
  }

  if (unsafeMethods.has(method)) {
    headers.set('X-XSRF-TOKEN', await getAntiforgeryToken())
  }

  const response = await fetch(buildApiUrl(path), {
    ...options,
    method,
    headers,
    body,
    credentials: 'include',
  })

  if (!response.ok) {
    throw await createApiError(response)
  }

  return response
}

async function getAntiforgeryToken(): Promise<string> {
  if (antiforgeryToken) {
    return antiforgeryToken
  }

  const response = await fetch(buildApiUrl('/api/antiforgery/token'), {
    credentials: 'include',
  })

  if (!response.ok) {
    throw await createApiError(response)
  }

  const token = (await response.json()) as AntiforgeryTokenResponse
  antiforgeryToken = token.requestToken
  return token.requestToken
}

async function createApiError(response: Response): Promise<ApiError> {
  const contentType = response.headers.get('content-type') ?? ''
  const problem = contentType.includes('application/problem+json')
    ? ((await response.json()) as ProblemDetails)
    : undefined

  const message = problem?.title ?? `Request failed with status ${response.status}`
  return new ApiError(message, response.status, problem)
}
