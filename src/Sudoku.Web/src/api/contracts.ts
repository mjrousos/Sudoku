export type Difficulty = 'easy' | 'medium' | 'hard'

export interface AntiforgeryTokenResponse {
  requestToken: string
}

export interface ProblemDetails {
  type?: string
  title?: string
  status?: number
  detail?: string
  instance?: string
  errors?: Record<string, string[]>
  [key: string]: unknown
}

export interface CreateGameRequest {
  difficulty: Difficulty
}

export interface CreateGameResponse {
  gameId: string
  difficulty: Difficulty
  puzzle: number[]
  startedAt: string
}

export interface GameMetadataResponse extends CreateGameResponse {
  hintCount: number
  disqualifiedFromLeaderboard: boolean
}

export interface HintRequest {
  index: number
}

export interface HintResponse {
  index: number
  value: number
}

export interface RevealSolutionResponse {
  solution: number[]
}

export interface CompleteGameRequest {
  board: number[]
}

export interface CompleteGameResponse {
  elapsedMs: number
  completedAt: string
  qualifiedForLeaderboard: boolean
  leaderboardRank: number | null
}

export interface CompletedGameHistoryResponse {
  id: string
  difficulty: Difficulty
  elapsedMs: number
  startedAt: string
  completedAt: string
  qualifiedForLeaderboard: boolean
  hintCount: number
  solutionRevealed: boolean
}

export interface GameHistoryPageResponse {
  items: CompletedGameHistoryResponse[]
  totalCount: number
}

export interface CurrentUserResponse {
  id: string
  username: string
  email?: string
  profilePictureUrl?: string
  usernameConfirmed: boolean
  linkedLogins: LinkedLoginResponse[]
}

export interface LinkedLoginResponse {
  provider: string
  displayName: string
  email?: string
  isPrimary: boolean
}

export interface AuthProviderResponse {
  provider: string
  displayName: string
  isConfigured: boolean
  isLinked: boolean
  isPrimary: boolean
}

export interface DevelopmentSignInRequest {
  username?: string
  email?: string
  subject?: string
}

export interface UpdateProfileRequest {
  username: string
}

export interface SetPrimaryLoginRequest {
  provider: string
}

export interface ProfilePictureResponse {
  profilePictureUrl: string | null
}

export interface DifficultyStatsResponse {
  difficulty: Difficulty
  totalGames: number
  bestTimeMs: number | null
  averageTimeMs: number | null
}

export interface ProfileStatsResponse {
  totalGames: number
  currentStreakDays: number
  byDifficulty: DifficultyStatsResponse[]
}

export type LeaderboardWindow = 'daily' | 'weekly' | 'all-time'

export interface LeaderboardEntryResponse {
  rank: number
  username: string
  profilePictureUrl?: string
  elapsedMs: number
  completedAt: string
}

export interface LeaderboardResponse {
  entries: LeaderboardEntryResponse[]
  viewerRank: number | null
}
