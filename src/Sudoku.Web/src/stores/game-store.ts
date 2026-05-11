import { defineStore } from 'pinia'
import { computed, ref } from 'vue'

import type { CompleteGameResponse, CreateGameResponse, Difficulty } from '../api/contracts'
import { completeGame, createGame, requestGameHint, revealGameSolution } from '../api/games'
import { ApiError } from '../api/http-client'

export type GameStatus = 'idle' | 'loading' | 'playing' | 'completed' | 'lost' | 'error'

interface Snapshot {
  board: number[]
  notes: number[][]
}

const gridSize = 9
const cellCount = gridSize * gridSize
const undoLimit = 100
const digits = [1, 2, 3, 4, 5, 6, 7, 8, 9] as const
const peerIndexes = Array.from({ length: cellCount }, (_, index) => buildPeerIndexes(index))

export const useGameStore = defineStore('game', () => {
  const status = ref<GameStatus>('idle')
  const gameId = ref<string | null>(null)
  const difficulty = ref<Difficulty>('easy')
  const puzzle = ref<number[]>(emptyBoard())
  const board = ref<number[]>(emptyBoard())
  const notes = ref<number[][]>(emptyNotes())
  const selectedIndex = ref<number | null>(null)
  const startedAt = ref<string | null>(null)
  const hintCount = ref(0)
  const disqualifiedFromLeaderboard = ref(false)
  const notesMode = ref(false)
  const completion = ref<CompleteGameResponse | null>(null)
  const lastError = ref<string | null>(null)
  const undoStack = ref<Snapshot[]>([])
  const redoStack = ref<Snapshot[]>([])
  const nowMs = ref(Date.now())
  const isCompleting = ref(false)
  let timerId: ReturnType<typeof setInterval> | undefined

  const selectedValue = computed(() => selectedIndex.value === null ? 0 : board.value[selectedIndex.value])
  const elapsedMs = computed(() => {
    if (completion.value !== null) {
      return completion.value.elapsedMs
    }

    if (startedAt.value === null) {
      return 0
    }

    return Math.max(0, nowMs.value - Date.parse(startedAt.value))
  })
  const elapsedDisplay = computed(() => formatElapsed(elapsedMs.value))
  const canUndo = computed(() => undoStack.value.length > 0 && status.value === 'playing')
  const canRedo = computed(() => redoStack.value.length > 0 && status.value === 'playing')
  const isCompleteLocally = computed(() => board.value.every((value) => value !== 0) && !hasAnyConflicts(board.value))
  const emptyCellCount = computed(() => board.value.filter((value) => value === 0).length)

  async function startNewGame(nextDifficulty = difficulty.value): Promise<void> {
    stopTimer()
    difficulty.value = nextDifficulty
    status.value = 'loading'
    gameId.value = null
    completion.value = null
    lastError.value = null
    selectedIndex.value = null

    try {
      loadGame(await createGame({ difficulty: nextDifficulty }))
    } catch (error) {
      status.value = 'error'
      lastError.value = describeError(error)
    }
  }

  function abandonGame(): void {
    stopTimer()
    status.value = 'idle'
    gameId.value = null
    puzzle.value = emptyBoard()
    board.value = emptyBoard()
    notes.value = emptyNotes()
    selectedIndex.value = null
    startedAt.value = null
    hintCount.value = 0
    disqualifiedFromLeaderboard.value = false
    completion.value = null
    lastError.value = null
    undoStack.value = []
    redoStack.value = []
  }

  function selectCell(index: number): void {
    if (index < 0 || index >= cellCount) {
      return
    }

    selectedIndex.value = index
  }

  function moveSelection(rowDelta: number, columnDelta: number): void {
    const current = selectedIndex.value ?? firstEditableIndex()
    if (current === null) {
      return
    }

    const row = Math.min(gridSize - 1, Math.max(0, rowOf(current) + rowDelta))
    const column = Math.min(gridSize - 1, Math.max(0, columnOf(current) + columnDelta))
    selectCell(row * gridSize + column)
  }

  function inputDigit(value: number): void {
    if (!digits.includes(value as (typeof digits)[number]) || !canEditSelectedCell()) {
      return
    }

    const index = selectedIndex.value!
    if (notesMode.value) {
      toggleNoteAt(index, value)
      return
    }

    setCellValue(index, value)
    if (isCompleteLocally.value) {
      void completeCurrentGame()
    }
  }

  function eraseSelected(): void {
    if (!canEditSelectedCell()) {
      return
    }

    const index = selectedIndex.value!
    if (board.value[index] === 0 && notes.value[index].length === 0) {
      return
    }

    recordUndo()
    board.value = replaceAt(board.value, index, 0)
    notes.value = replaceAt(notes.value, index, [])
    lastError.value = null
  }

  function toggleNotesMode(): void {
    notesMode.value = !notesMode.value
  }

  function undo(): void {
    const snapshot = undoStack.value.pop()
    if (snapshot === undefined || status.value !== 'playing') {
      return
    }

    redoStack.value.push(captureSnapshot())
    restoreSnapshot(snapshot)
    lastError.value = null
  }

  function redo(): void {
    const snapshot = redoStack.value.pop()
    if (snapshot === undefined || status.value !== 'playing') {
      return
    }

    undoStack.value.push(captureSnapshot())
    restoreSnapshot(snapshot)
    lastError.value = null
  }

  async function requestHint(): Promise<void> {
    if (gameId.value === null || status.value !== 'playing') {
      return
    }

    const hintIndex = selectedIndex.value !== null && !isGiven(selectedIndex.value)
      ? selectedIndex.value
      : firstEditableIndex()

    if (hintIndex === null) {
      return
    }

    try {
      const hint = await requestGameHint(gameId.value, { index: hintIndex })
      recordUndo()
      board.value = replaceAt(board.value, hint.index, hint.value)
      notes.value = clearRelatedNotes(notes.value, hint.index, hint.value)
      hintCount.value += 1
      disqualifiedFromLeaderboard.value = true
      selectedIndex.value = hint.index
      lastError.value = null

      if (isCompleteLocally.value) {
        await completeCurrentGame()
      }
    } catch (error) {
      handlePlayError(error)
    }
  }

  async function revealSolution(): Promise<void> {
    if (gameId.value === null || status.value !== 'playing') {
      return
    }

    try {
      const reveal = await revealGameSolution(gameId.value)
      recordUndo()
      board.value = [...reveal.solution]
      notes.value = emptyNotes()
      disqualifiedFromLeaderboard.value = true
      await completeCurrentGame()
    } catch (error) {
      handlePlayError(error)
    }
  }

  async function completeCurrentGame(): Promise<void> {
    if (gameId.value === null || status.value !== 'playing' || isCompleting.value) {
      return
    }

    isCompleting.value = true
    try {
      completion.value = await completeGame(gameId.value, { board: board.value })
      status.value = 'completed'
      stopTimer()
      lastError.value = null
    } catch (error) {
      handlePlayError(error)
    } finally {
      isCompleting.value = false
    }
  }

  function isGiven(index: number): boolean {
    return puzzle.value[index] !== 0
  }

  function isPeer(index: number): boolean {
    return selectedIndex.value !== null && peerIndexes[selectedIndex.value].includes(index)
  }

  function isSameValue(index: number): boolean {
    return selectedValue.value !== 0 && board.value[index] === selectedValue.value
  }

  function hasConflict(index: number): boolean {
    return hasCellConflict(board.value, index)
  }

  function canEditSelectedCell(): boolean {
    return selectedIndex.value !== null && status.value === 'playing' && !isGiven(selectedIndex.value)
  }

  function loadGame(response: CreateGameResponse): void {
    gameId.value = response.gameId
    difficulty.value = response.difficulty
    puzzle.value = [...response.puzzle]
    board.value = [...response.puzzle]
    notes.value = emptyNotes()
    selectedIndex.value = firstEditableIndexForPuzzle(response.puzzle)
    startedAt.value = response.startedAt
    hintCount.value = 0
    disqualifiedFromLeaderboard.value = false
    completion.value = null
    lastError.value = null
    undoStack.value = []
    redoStack.value = []
    status.value = 'playing'
    startTimer()
  }

  function setCellValue(index: number, value: number): void {
    recordUndo()
    board.value = replaceAt(board.value, index, value)
    notes.value = clearRelatedNotes(notes.value, index, value)
    lastError.value = hasCellConflict(board.value, index)
      ? 'That digit collides with another cell in its row, column, or box.'
      : null
  }

  function toggleNoteAt(index: number, value: number): void {
    if (board.value[index] !== 0) {
      return
    }

    recordUndo()
    const currentNotes = notes.value[index]
    const nextNotes = currentNotes.includes(value)
      ? currentNotes.filter((note) => note !== value)
      : [...currentNotes, value].sort((left, right) => left - right)

    notes.value = replaceAt(notes.value, index, nextNotes)
    lastError.value = null
  }

  function recordUndo(): void {
    undoStack.value.push(captureSnapshot())
    if (undoStack.value.length > undoLimit) {
      undoStack.value.shift()
    }

    redoStack.value = []
  }

  function captureSnapshot(): Snapshot {
    return {
      board: [...board.value],
      notes: notes.value.map((cellNotes) => [...cellNotes]),
    }
  }

  function restoreSnapshot(snapshot: Snapshot): void {
    board.value = [...snapshot.board]
    notes.value = snapshot.notes.map((cellNotes) => [...cellNotes])
  }

  function firstEditableIndex(): number | null {
    return firstEditableIndexForPuzzle(puzzle.value)
  }

  function startTimer(): void {
    stopTimer()
    nowMs.value = Date.now()
    timerId = setInterval(() => {
      nowMs.value = Date.now()
    }, 1000)
  }

  function stopTimer(): void {
    if (timerId !== undefined) {
      clearInterval(timerId)
      timerId = undefined
    }
  }

  function handlePlayError(error: unknown): void {
    const code = error instanceof ApiError ? error.code : undefined
    lastError.value = describeError(error)

    if (code === 'game_session_lost') {
      status.value = 'lost'
      stopTimer()
    }
  }

  return {
    abandonGame,
    board,
    canRedo,
    canUndo,
    completion,
    difficulty,
    disqualifiedFromLeaderboard,
    elapsedDisplay,
    elapsedMs,
    emptyCellCount,
    eraseSelected,
    gameId,
    hasConflict,
    hintCount,
    inputDigit,
    isGiven,
    isPeer,
    isSameValue,
    lastError,
    moveSelection,
    notes,
    notesMode,
    puzzle,
    redo,
    requestHint,
    revealSolution,
    selectCell,
    selectedIndex,
    startNewGame,
    status,
    toggleNotesMode,
    undo,
  }
})

function emptyBoard(): number[] {
  return Array.from({ length: cellCount }, () => 0)
}

function emptyNotes(): number[][] {
  return Array.from({ length: cellCount }, () => [])
}

function replaceAt<T>(items: T[], index: number, value: T): T[] {
  const next = [...items]
  next[index] = value
  return next
}

function clearRelatedNotes(notes: number[][], index: number, value: number): number[][] {
  return notes.map((cellNotes, cellIndex) => {
    if (cellIndex === index) {
      return []
    }

    return peerIndexes[index].includes(cellIndex)
      ? cellNotes.filter((note) => note !== value)
      : cellNotes
  })
}

function buildPeerIndexes(index: number): number[] {
  const row = rowOf(index)
  const column = columnOf(index)
  const boxRow = Math.floor(row / 3) * 3
  const boxColumn = Math.floor(column / 3) * 3
  const peers = new Set<number>()

  for (let i = 0; i < gridSize; i += 1) {
    peers.add(row * gridSize + i)
    peers.add(i * gridSize + column)
  }

  for (let rowOffset = 0; rowOffset < 3; rowOffset += 1) {
    for (let columnOffset = 0; columnOffset < 3; columnOffset += 1) {
      peers.add((boxRow + rowOffset) * gridSize + boxColumn + columnOffset)
    }
  }

  peers.delete(index)
  return [...peers]
}

function rowOf(index: number): number {
  return Math.floor(index / gridSize)
}

function columnOf(index: number): number {
  return index % gridSize
}

function hasCellConflict(board: number[], index: number): boolean {
  const value = board[index]
  return value !== 0 && peerIndexes[index].some((peerIndex) => board[peerIndex] === value)
}

function hasAnyConflicts(board: number[]): boolean {
  return board.some((_, index) => hasCellConflict(board, index))
}

function firstEditableIndexForPuzzle(puzzle: number[]): number | null {
  const index = puzzle.findIndex((value) => value === 0)
  return index === -1 ? null : index
}

function formatElapsed(milliseconds: number): string {
  const totalSeconds = Math.floor(milliseconds / 1000)
  const minutes = Math.floor(totalSeconds / 60)
  const seconds = totalSeconds % 60
  return `${minutes}:${seconds.toString().padStart(2, '0')}`
}

function describeError(error: unknown): string {
  if (error instanceof ApiError) {
    if (error.code === 'game_session_lost') {
      return 'This puzzle session was lost. Start a new puzzle to continue.'
    }

    if (error.status === 422) {
      return 'The filled board is not the solution yet. Keep checking the highlighted cells.'
    }

    return error.message
  }

  return 'Something went wrong. Try again.'
}
