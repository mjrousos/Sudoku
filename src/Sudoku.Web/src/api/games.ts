import type {
  CompleteGameRequest,
  CompleteGameResponse,
  CreateGameRequest,
  CreateGameResponse,
  GameMetadataResponse,
  HintRequest,
  HintResponse,
  RevealSolutionResponse,
} from './contracts'
import { apiFetchJson } from './http-client'

export function createGame(request: CreateGameRequest): Promise<CreateGameResponse> {
  return apiFetchJson<CreateGameResponse>('/api/games', {
    method: 'POST',
    body: request,
    retryOnRetryableProblem: true,
  })
}

export function getGame(gameId: string): Promise<GameMetadataResponse> {
  return apiFetchJson<GameMetadataResponse>(`/api/games/${gameId}`)
}

export function requestGameHint(gameId: string, request: HintRequest): Promise<HintResponse> {
  return apiFetchJson<HintResponse>(`/api/games/${gameId}/hint`, {
    method: 'POST',
    body: request,
  })
}

export function revealGameSolution(gameId: string): Promise<RevealSolutionResponse> {
  return apiFetchJson<RevealSolutionResponse>(`/api/games/${gameId}/reveal`, {
    method: 'POST',
  })
}

export function completeGame(gameId: string, request: CompleteGameRequest): Promise<CompleteGameResponse> {
  return apiFetchJson<CompleteGameResponse>(`/api/games/${gameId}/complete`, {
    method: 'POST',
    body: request,
  })
}
