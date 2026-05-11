import type { Difficulty, LeaderboardResponse, LeaderboardWindow } from './contracts'
import { apiFetchJson } from './http-client'

export function getLeaderboard(
  difficulty: Difficulty,
  window: LeaderboardWindow,
  bestPerPlayer: boolean,
): Promise<LeaderboardResponse> {
  const params = new URLSearchParams({
    bestPerPlayer: bestPerPlayer.toString(),
  })
  return apiFetchJson<LeaderboardResponse>(`/api/leaderboards/${difficulty}/${window}?${params}`)
}
