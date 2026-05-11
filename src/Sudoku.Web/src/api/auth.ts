import type {
  AuthProviderResponse,
  CurrentUserResponse,
  DevelopmentSignInRequest,
  ProfilePictureResponse,
  SetPrimaryLoginRequest,
  UpdateProfileRequest,
} from './contracts'
import { apiDownload, apiFetchJson, buildApiUrl, clearAntiforgeryToken } from './http-client'

export function getCurrentUser(): Promise<CurrentUserResponse> {
  return apiFetchJson<CurrentUserResponse>('/api/me')
}

export function getAuthProviders(): Promise<AuthProviderResponse[]> {
  return apiFetchJson<AuthProviderResponse[]>('/api/auth/providers')
}

export function getProviderChallengeUrl(provider: string, returnUrl: string): string {
  return buildApiUrl(`/api/auth/challenge/${encodeURIComponent(provider)}?returnUrl=${encodeURIComponent(returnUrl)}`)
}

export async function signOut(): Promise<void> {
  await apiFetchJson<void>('/api/auth/sign-out', {
    method: 'POST',
  })
  clearAntiforgeryToken()
}

export async function developmentSignIn(request: DevelopmentSignInRequest): Promise<CurrentUserResponse> {
  const user = await apiFetchJson<CurrentUserResponse>('/api/auth/dev/sign-in', {
    method: 'POST',
    body: request,
  })
  clearAntiforgeryToken()
  return user
}

export function updateProfile(request: UpdateProfileRequest): Promise<CurrentUserResponse> {
  return apiFetchJson<CurrentUserResponse>('/api/me', {
    method: 'PUT',
    body: request,
  })
}

export function setPrimaryLogin(request: SetPrimaryLoginRequest): Promise<CurrentUserResponse> {
  return apiFetchJson<CurrentUserResponse>('/api/me/providers/primary', {
    method: 'POST',
    body: request,
  })
}

export function unlinkProvider(provider: string): Promise<CurrentUserResponse> {
  return apiFetchJson<CurrentUserResponse>(`/api/me/providers/${encodeURIComponent(provider)}`, {
    method: 'DELETE',
  })
}

export function uploadProfilePicture(file: File): Promise<ProfilePictureResponse> {
  const formData = new FormData()
  formData.append('file', file)
  return apiFetchJson<ProfilePictureResponse>('/api/me/profile-picture', {
    method: 'POST',
    body: formData,
  })
}

export function deleteProfilePicture(): Promise<ProfilePictureResponse> {
  return apiFetchJson<ProfilePictureResponse>('/api/me/profile-picture', {
    method: 'DELETE',
  })
}

export function exportAccount(): Promise<Blob> {
  return apiDownload('/api/me/export', {
    method: 'POST',
  })
}

export async function deleteAccount(): Promise<void> {
  await apiFetchJson<void>('/api/me', {
    method: 'DELETE',
  })
  clearAntiforgeryToken()
}
