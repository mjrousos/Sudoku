<template>
  <div ref="gridElement" class="sudoku-grid" role="grid" aria-label="Sudoku board" @keydown="onKeydown">
    <div v-for="row in 9" :key="row" class="sudoku-row" role="row">
      <button
        v-for="column in 9"
        :key="cellIndex(row, column)"
        :aria-label="cellAriaLabel(cellIndex(row, column))"
        :aria-selected="selectedIndex === cellIndex(row, column)"
        :class="cellClasses(cellIndex(row, column))"
        :data-cell-index="cellIndex(row, column)"
        :disabled="status === 'loading'"
        role="gridcell"
        type="button"
        @click="$emit('select', cellIndex(row, column))"
      >
        <span v-if="board[cellIndex(row, column)] !== 0" class="cell-value">
          {{ board[cellIndex(row, column)] }}
        </span>
        <span v-else class="cell-notes" aria-hidden="true">
          <span v-for="digit in 9" :key="digit">{{ notes[cellIndex(row, column)].includes(digit) ? digit : '' }}</span>
        </span>
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { nextTick, ref, watch } from 'vue'

import type { GameStatus } from '../stores/game-store'

const props = defineProps<{
  board: number[]
  hasConflict: (index: number) => boolean
  isGiven: (index: number) => boolean
  isPeer: (index: number) => boolean
  isSameValue: (index: number) => boolean
  notes: number[][]
  selectedIndex: number | null
  status: GameStatus
}>()

const emit = defineEmits<{
  erase: []
  input: [value: number]
  move: [rowDelta: number, columnDelta: number]
  select: [index: number]
}>()

const gridElement = ref<HTMLElement | null>(null)

watch(() => props.selectedIndex, async (index) => {
  if (index === null || gridElement.value === null || !gridElement.value.contains(document.activeElement)) {
    return
  }

  await nextTick()
  gridElement.value.querySelector<HTMLButtonElement>(`[data-cell-index="${index}"]`)?.focus()
})

function cellIndex(row: number, column: number): number {
  return (row - 1) * 9 + column - 1
}

function cellClasses(index: number): Record<string, boolean> {
  return {
    cell: true,
    'cell-selected': props.selectedIndex === index,
    'cell-given': props.isGiven(index),
    'cell-peer': props.isPeer(index),
    'cell-same': props.isSameValue(index),
    'cell-conflict': props.hasConflict(index),
  }
}

function cellAriaLabel(index: number): string {
  const row = Math.floor(index / 9) + 1
  const column = (index % 9) + 1
  const value = props.board[index] === 0 ? 'empty' : props.board[index].toString()
  const fixed = props.isGiven(index) ? ', fixed clue' : ''
  return `Row ${row}, column ${column}, ${value}${fixed}`
}

function onKeydown(event: KeyboardEvent): void {
  if (event.key >= '1' && event.key <= '9') {
    event.preventDefault()
    emit('input', Number(event.key))
    return
  }

  const keyActions: Record<string, () => void> = {
    ArrowDown: () => emit('move', 1, 0),
    ArrowLeft: () => emit('move', 0, -1),
    ArrowRight: () => emit('move', 0, 1),
    ArrowUp: () => emit('move', -1, 0),
    Backspace: () => emit('erase'),
    Delete: () => emit('erase'),
    Space: () => emit('erase'),
  }

  const action = keyActions[event.key]
  if (action !== undefined) {
    event.preventDefault()
    action()
  }
}
</script>

<style scoped>
.sudoku-grid {
  aspect-ratio: 1;
  background: var(--color-ink);
  border: 3px solid var(--color-ink);
  box-shadow: 0 28px 80px var(--shadow-board);
  display: grid;
  gap: 3px;
  max-width: min(82vw, 620px);
  min-width: min(92vw, 320px);
  padding: 3px;
  position: relative;
}

.sudoku-grid::after {
  background:
    linear-gradient(90deg, transparent calc(33.333% - 1px), var(--color-ink) calc(33.333% - 1px), var(--color-ink) calc(33.333% + 2px), transparent calc(33.333% + 2px)),
    linear-gradient(90deg, transparent calc(66.666% - 2px), var(--color-ink) calc(66.666% - 2px), var(--color-ink) calc(66.666% + 1px), transparent calc(66.666% + 1px)),
    linear-gradient(0deg, transparent calc(33.333% - 1px), var(--color-ink) calc(33.333% - 1px), var(--color-ink) calc(33.333% + 2px), transparent calc(33.333% + 2px)),
    linear-gradient(0deg, transparent calc(66.666% - 2px), var(--color-ink) calc(66.666% - 2px), var(--color-ink) calc(66.666% + 1px), transparent calc(66.666% + 1px));
  content: "";
  inset: 3px;
  pointer-events: none;
  position: absolute;
}

.sudoku-row {
  display: grid;
  gap: 3px;
  grid-template-columns: repeat(9, 1fr);
}

.cell {
  align-items: center;
  background: var(--color-paper);
  border: 0;
  color: var(--color-text);
  cursor: pointer;
  display: grid;
  font-family: var(--font-display);
  font-size: clamp(1.35rem, 4vw, 2.35rem);
  font-weight: 700;
  justify-items: center;
  line-height: 1;
  min-height: 0;
  padding: 0;
  position: relative;
  transition: background-color 140ms ease, box-shadow 140ms ease, transform 140ms ease;
}

.cell:hover:not(:disabled) {
  box-shadow: inset 0 0 0 3px color-mix(in srgb, var(--color-accent) 40%, transparent);
}

.cell:focus-visible {
  outline: 4px solid var(--color-focus);
  outline-offset: -4px;
  z-index: 2;
}

.cell-selected {
  background: var(--color-selected);
  box-shadow: inset 0 0 0 3px var(--color-accent);
}

.cell-peer {
  background: var(--color-peer);
}

.cell-same {
  background: var(--color-same);
}

.cell-given {
  color: var(--color-ink);
  font-weight: 900;
}

.cell:not(.cell-given) {
  color: var(--color-player);
}

.cell-conflict {
  background: var(--color-danger-soft);
  color: var(--color-danger);
}

.cell-notes {
  color: var(--color-muted);
  display: grid;
  font-family: var(--font-body);
  font-size: clamp(0.48rem, 1.5vw, 0.78rem);
  font-weight: 800;
  gap: 1px;
  grid-template-columns: repeat(3, 1fr);
  height: 78%;
  letter-spacing: -0.04em;
  width: 78%;
}

.cell-notes span {
  align-items: center;
  display: flex;
  justify-content: center;
}
</style>
