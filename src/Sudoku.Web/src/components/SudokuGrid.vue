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
  const row = Math.floor(index / 9)
  const column = index % 9

  return {
    cell: true,
    'cell-box-left': column === 3 || column === 6,
    'cell-box-top': row === 3 || row === 6,
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
  --gridline: color-mix(in srgb, var(--color-ink) 34%, var(--color-paper));
  --gridline-strong: var(--color-ink);
  --segment-line-width: clamp(3px, 0.72cqw, 5px);

  aspect-ratio: 1;
  background: var(--gridline-strong);
  border: var(--segment-line-width) solid var(--gridline-strong);
  box-shadow: 0 28px 80px var(--shadow-board);
  container-type: inline-size;
  display: grid;
  gap: 0;
  grid-template-rows: repeat(9, minmax(0, 1fr));
  max-width: min(82vw, 620px);
  min-width: min(92vw, 320px);
  overflow: hidden;
  position: relative;
}

.sudoku-row {
  display: grid;
  grid-template-columns: repeat(9, minmax(0, 1fr));
  min-height: 0;
}

.cell {
  align-items: center;
  background: var(--color-paper);
  border: 0;
  border-bottom: 1px solid var(--gridline);
  border-right: 1px solid var(--gridline);
  color: var(--color-text);
  cursor: pointer;
  display: grid;
  font-family: var(--font-display);
  font-weight: 700;
  justify-items: center;
  line-height: 1;
  min-height: 0;
  padding: 0;
  position: relative;
  transition: background-color 140ms ease, box-shadow 140ms ease, transform 140ms ease;
}

.cell-value {
  display: block;
  font-size: clamp(1rem, 5.35cqw, 2.05rem);
  line-height: 0.9;
  max-height: 100%;
  max-width: 100%;
  overflow: hidden;
}

.sudoku-row:last-child .cell {
  border-bottom: 0;
}

.cell:nth-child(9) {
  border-right: 0;
}

.cell-box-left {
  border-left: var(--segment-line-width) solid var(--gridline-strong);
}

.cell-box-top {
  border-top: var(--segment-line-width) solid var(--gridline-strong);
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
  font-size: clamp(0.42rem, 1.7cqw, 0.68rem);
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
