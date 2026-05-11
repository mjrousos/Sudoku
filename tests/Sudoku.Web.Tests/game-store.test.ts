import { createPinia, setActivePinia } from 'pinia'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

import type { CreateGameResponse } from '@/api/contracts'
import { clearAntiforgeryTokenForTests } from '@/api/http-client'
import { useGameStore } from '@/stores/game-store'

const fixedPuzzle = [
  5, 3, 0, 0, 7, 0, 0, 0, 0,
  6, 0, 0, 1, 9, 5, 0, 0, 0,
  0, 9, 8, 0, 0, 0, 0, 6, 0,
  8, 0, 0, 0, 6, 0, 0, 0, 3,
  4, 0, 0, 8, 0, 3, 0, 0, 1,
  7, 0, 0, 0, 2, 0, 0, 0, 6,
  0, 6, 0, 0, 0, 0, 2, 8, 0,
  0, 0, 0, 4, 1, 9, 0, 0, 5,
  0, 0, 0, 0, 8, 0, 0, 7, 9,
]

const fixedSolution = [
  5, 3, 4, 6, 7, 8, 9, 1, 2,
  6, 7, 2, 1, 9, 5, 3, 4, 8,
  1, 9, 8, 3, 4, 2, 5, 6, 7,
  8, 5, 9, 7, 6, 1, 4, 2, 3,
  4, 2, 6, 8, 5, 3, 7, 9, 1,
  7, 1, 3, 9, 2, 4, 8, 5, 6,
  9, 6, 1, 5, 3, 7, 2, 8, 4,
  2, 8, 7, 4, 1, 9, 6, 3, 5,
  3, 4, 5, 2, 8, 6, 1, 7, 9,
]

describe('game store', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    clearAntiforgeryTokenForTests()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
    useGameStore().abandonGame()
  })

  it('starts a generated puzzle and supports notes with undo and redo', async () => {
    vi.stubGlobal('fetch', vi.fn()
      .mockResolvedValueOnce(jsonResponse({ requestToken: 'token-1' }))
      .mockResolvedValueOnce(jsonResponse(createGameResponse('medium'))))

    const game = useGameStore()
    await game.startNewGame('medium')

    expect(game.status).toBe('playing')
    expect(game.difficulty).toBe('medium')
    expect(game.selectedIndex).toBe(2)

    game.toggleNotesMode()
    game.inputDigit(4)

    expect(game.notes[2]).toEqual([4])
    expect(game.canUndo).toBe(true)

    game.undo()
    expect(game.notes[2]).toEqual([])

    game.redo()
    expect(game.notes[2]).toEqual([4])
  })

  it('requests hints and marks the puzzle as leaderboard disqualified', async () => {
    vi.stubGlobal('fetch', vi.fn()
      .mockResolvedValueOnce(jsonResponse({ requestToken: 'token-1' }))
      .mockResolvedValueOnce(jsonResponse(createGameResponse()))
      .mockResolvedValueOnce(jsonResponse({ index: 2, value: 4 })))

    const game = useGameStore()
    await game.startNewGame('easy')
    game.selectCell(2)
    await game.requestHint()

    expect(game.board[2]).toBe(4)
    expect(game.hintCount).toBe(1)
    expect(game.disqualifiedFromLeaderboard).toBe(true)
  })

  it('auto-completes a full valid board through the server', async () => {
    vi.stubGlobal('fetch', vi.fn()
      .mockResolvedValueOnce(jsonResponse({ requestToken: 'token-1' }))
      .mockResolvedValueOnce(jsonResponse(createGameResponse()))
      .mockResolvedValueOnce(jsonResponse({
        elapsedMs: 123000,
        completedAt: '2026-05-08T22:00:00Z',
        qualifiedForLeaderboard: true,
        leaderboardRank: null,
      })))

    const game = useGameStore()
    await game.startNewGame('easy')
    game.board = [...fixedSolution]
    game.board[2] = 0
    game.selectCell(2)
    game.inputDigit(4)

    await vi.waitFor(() => expect(game.status).toBe('completed'))
    expect(game.completion?.elapsedMs).toBe(123000)
    expect(game.elapsedDisplay).toBe('2:03')
  })
})

function createGameResponse(difficulty: CreateGameResponse['difficulty'] = 'easy'): CreateGameResponse {
  return {
    difficulty,
    gameId: '00000000-0000-0000-0000-000000000001',
    puzzle: fixedPuzzle,
    startedAt: '2026-05-08T21:58:00Z',
  }
}

function jsonResponse(body: unknown): Response {
  return new Response(JSON.stringify(body), {
    headers: { 'content-type': 'application/json' },
    status: 200,
  })
}
