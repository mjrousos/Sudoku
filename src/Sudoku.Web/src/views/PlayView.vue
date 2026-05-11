<template>
  <section class="play-page">
    <div class="play-hero">
      <p class="eyebrow">Daily-paper focus, server-kept time</p>
      <h1>Ink a clean grid. No account required.</h1>
      <p class="lede">
        Start with an on-the-fly puzzle, move by keyboard or touch, use notes when you are reasoning, and let the server judge the final board.
      </p>
    </div>

    <div class="game-layout">
      <aside class="control-panel" aria-label="Game controls">
        <div class="difficulty-card">
          <p class="panel-label">Difficulty</p>
          <div class="difficulty-options">
            <label v-for="option in difficulties" :key="option.value" :class="{ active: game.difficulty === option.value }">
              <input v-model="selectedDifficulty" :value="option.value" name="difficulty" type="radio" />
              <span>{{ option.label }}</span>
              <small>{{ option.detail }}</small>
            </label>
          </div>
          <button class="primary-action" type="button" :disabled="game.status === 'loading'" @click="startGame">
            {{ game.status === 'loading' ? 'Composing puzzle...' : 'New puzzle' }}
          </button>
        </div>

        <div class="meter-card" aria-live="polite">
          <span>Time</span>
          <strong>{{ game.elapsedDisplay }}</strong>
          <small>{{ game.emptyCellCount }} cells open</small>
        </div>

        <div class="meter-card">
          <span>Leaderboard</span>
          <strong>{{ game.disqualifiedFromLeaderboard ? 'Disqualified' : 'Eligible' }}</strong>
          <small>{{ game.hintCount }} hints used</small>
        </div>

        <div class="action-stack">
          <button type="button" :disabled="!canUseGameActions" @click="game.requestHint">Hint selected cell</button>
          <button type="button" :disabled="!canUseGameActions" @click="confirmReveal">Reveal solution</button>
        </div>
      </aside>

      <div class="board-stage">
        <div v-if="game.status === 'idle'" class="empty-state">
          <span class="stamp">Ready</span>
          <h2>Choose a difficulty and press New puzzle.</h2>
          <p>The board will keep your notes, undo stack, conflict highlights, and elapsed time in one focused workspace.</p>
        </div>

        <div v-else class="board-stack">
          <SudokuGrid
            :board="game.board"
            :has-conflict="game.hasConflict"
            :is-given="game.isGiven"
            :is-peer="game.isPeer"
            :is-same-value="game.isSameValue"
            :notes="game.notes"
            :selected-index="game.selectedIndex"
            :status="game.status"
            @erase="game.eraseSelected"
            @input="game.inputDigit"
            @move="game.moveSelection"
            @select="game.selectCell"
          />

          <NumberPad
            :can-redo="game.canRedo"
            :can-undo="game.canUndo"
            :disabled="game.status !== 'playing'"
            :notes-mode="game.notesMode"
            @erase="game.eraseSelected"
            @input="game.inputDigit"
            @redo="game.redo"
            @toggle-notes="game.toggleNotesMode"
            @undo="game.undo"
          />
        </div>

        <p v-if="game.lastError" class="status-message error" role="alert">{{ game.lastError }}</p>
        <p v-else-if="game.status === 'loading'" class="status-message">Generating a unique puzzle. If the server times out, the client retries once.</p>
        <p v-else-if="game.status === 'lost'" class="status-message error" role="alert">This in-memory puzzle is no longer available. Start a new one.</p>
        <p v-else-if="game.status === 'completed' && game.completion" class="status-message success" role="status">
          Solved in {{ game.elapsedDisplay }}. {{ game.completion.qualifiedForLeaderboard ? 'Leaderboard eligible.' : 'Hints or reveal kept this one casual.' }}
        </p>
      </div>
    </div>
  </section>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue'

import NumberPad from '../components/NumberPad.vue'
import SudokuGrid from '../components/SudokuGrid.vue'
import type { Difficulty } from '../api/contracts'
import { useGameStore } from '../stores/game-store'

const difficulties: Array<{ value: Difficulty; label: string; detail: string }> = [
  { value: 'easy', label: 'Easy', detail: 'wide-open warmup' },
  { value: 'medium', label: 'Medium', detail: 'balanced newspaper' },
  { value: 'hard', label: 'Hard', detail: 'sparse and thorny' },
]

const game = useGameStore()
const selectedDifficulty = ref<Difficulty>(game.difficulty)
const canUseGameActions = computed(() => game.status === 'playing')

watch(selectedDifficulty, (value) => {
  game.difficulty = value
})

async function startGame(): Promise<void> {
  await game.startNewGame(selectedDifficulty.value)
}

async function confirmReveal(): Promise<void> {
  if (window.confirm('Reveal the full solution? This disqualifies the puzzle from leaderboards.')) {
    await game.revealSolution()
  }
}
</script>

<style scoped>
.play-page {
  display: grid;
  gap: clamp(1.5rem, 4vw, 3rem);
}

.play-hero {
  margin-inline: auto;
  max-width: 980px;
  text-align: center;
}

.eyebrow {
  color: var(--color-accent);
  font-size: 0.8rem;
  font-weight: 900;
  letter-spacing: 0.18em;
  margin-bottom: 0.75rem;
  text-transform: uppercase;
}

h1 {
  font-family: var(--font-display);
  font-size: clamp(2.7rem, 8vw, 6.4rem);
  letter-spacing: -0.07em;
  line-height: 0.88;
  margin: 0;
}

.lede {
  color: var(--color-muted);
  font-size: clamp(1rem, 2vw, 1.25rem);
  line-height: 1.7;
  margin: 1.25rem auto 0;
  max-width: 720px;
}

.game-layout {
  align-items: start;
  display: grid;
  gap: clamp(1rem, 3vw, 2rem);
  grid-template-columns: minmax(240px, 320px) minmax(0, 1fr);
}

.control-panel,
.board-stage {
  background: color-mix(in srgb, var(--color-surface) 92%, transparent);
  border: 1px solid var(--color-border);
  box-shadow: 0 20px 70px var(--shadow-soft);
}

.control-panel {
  border-radius: 2rem;
  display: grid;
  gap: 1rem;
  padding: 1rem;
  position: sticky;
  top: 1rem;
}

.difficulty-card,
.meter-card {
  background: var(--color-paper);
  border: 1px solid var(--color-border);
  border-radius: 1.45rem;
  padding: 1rem;
}

.panel-label,
.meter-card span {
  color: var(--color-muted);
  font-size: 0.74rem;
  font-weight: 900;
  letter-spacing: 0.15em;
  margin: 0 0 0.75rem;
  text-transform: uppercase;
}

.difficulty-options {
  display: grid;
  gap: 0.55rem;
}

.difficulty-options label {
  border: 1px solid var(--color-border);
  border-radius: 1rem;
  cursor: pointer;
  display: grid;
  gap: 0.15rem;
  padding: 0.8rem 0.9rem;
}

.difficulty-options label.active {
  background: var(--color-selected);
  border-color: var(--color-accent);
}

.difficulty-options input {
  clip: rect(0 0 0 0);
  clip-path: inset(50%);
  height: 1px;
  overflow: hidden;
  position: absolute;
  white-space: nowrap;
  width: 1px;
}

.difficulty-options span,
.meter-card strong {
  font-family: var(--font-display);
  font-size: 1.35rem;
  font-weight: 900;
}

.difficulty-options small,
.meter-card small {
  color: var(--color-muted);
}

.primary-action,
.action-stack button {
  border: 0;
  border-radius: 999px;
  cursor: pointer;
  font-weight: 900;
  min-height: 2.9rem;
}

.primary-action {
  background: var(--color-accent);
  color: var(--color-paper);
  margin-top: 1rem;
  width: 100%;
}

.meter-card {
  display: grid;
  gap: 0.15rem;
}

.action-stack {
  display: grid;
  gap: 0.65rem;
}

.action-stack button {
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  color: var(--color-text);
}

.primary-action:hover:not(:disabled),
.action-stack button:hover:not(:disabled) {
  transform: translateY(-1px);
}

button:disabled {
  cursor: not-allowed;
  opacity: 0.55;
}

.board-stage {
  border-radius: 2.5rem;
  min-height: 520px;
  overflow: hidden;
  padding: clamp(1rem, 3vw, 2rem);
}

.board-stage::before {
  background:
    linear-gradient(90deg, color-mix(in srgb, var(--color-border) 38%, transparent) 1px, transparent 1px),
    linear-gradient(0deg, color-mix(in srgb, var(--color-border) 38%, transparent) 1px, transparent 1px);
  background-size: 42px 42px;
  content: "";
  inset: 0;
  mask-image: radial-gradient(circle at 50% 35%, black, transparent 72%);
  opacity: 0.45;
  pointer-events: none;
  position: fixed;
  z-index: -1;
}

.empty-state {
  align-content: center;
  display: grid;
  min-height: 460px;
  place-items: center;
  text-align: center;
}

.stamp {
  border: 2px solid var(--color-accent);
  border-radius: 999px;
  color: var(--color-accent);
  font-weight: 900;
  letter-spacing: 0.16em;
  padding: 0.4rem 0.8rem;
  text-transform: uppercase;
  transform: rotate(-6deg);
}

.empty-state h2 {
  font-family: var(--font-display);
  font-size: clamp(2rem, 5vw, 4rem);
  letter-spacing: -0.05em;
  margin: 1rem 0 0;
  max-width: 680px;
}

.empty-state p {
  color: var(--color-muted);
  max-width: 520px;
}

.board-stack {
  align-items: center;
  display: grid;
  gap: 1.25rem;
  justify-items: center;
}

.status-message {
  border: 1px solid var(--color-border);
  border-radius: 1rem;
  margin: 1rem auto 0;
  max-width: min(82vw, 620px);
  padding: 0.85rem 1rem;
  text-align: center;
}

.status-message.error {
  background: var(--color-danger-soft);
  border-color: color-mix(in srgb, var(--color-danger) 45%, var(--color-border));
  color: var(--color-danger);
}

.status-message.success {
  background: var(--color-success-soft);
  border-color: color-mix(in srgb, var(--color-success) 45%, var(--color-border));
  color: var(--color-success);
}

@media (max-width: 980px) {
  .game-layout {
    grid-template-columns: 1fr;
  }

  .control-panel {
    position: static;
  }
}
</style>
