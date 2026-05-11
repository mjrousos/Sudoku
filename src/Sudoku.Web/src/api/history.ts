import type { Difficulty, GameHistoryPageResponse, ProfileStatsResponse } from './contracts'
import { apiFetchJson } from './http-client'

export function getGameHistory(difficulty?: Difficulty): Promise<GameHistoryPageResponse> {
  const query = difficulty === undefined ? '' : `?difficulty=${difficulty}`
  return apiFetchJson<GameHistoryPageResponse>(`/api/me/history${query}`)
}

export function getProfileStats(): Promise<ProfileStatsResponse> {
  return apiFetchJson<ProfileStatsResponse>('/api/me/stats')
}
