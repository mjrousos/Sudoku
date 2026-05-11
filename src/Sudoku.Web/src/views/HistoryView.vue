<template>
  <section class="history-page">
    <div class="page-heading">
      <p class="eyebrow">Private archive</p>
      <h1>Your solved-grid ledger.</h1>
      <p>{{ stats?.totalGames ?? 0 }} completed games. Current streak: {{ stats?.currentStreakDays ?? 0 }} days.</p>
    </div>

    <div class="stats-grid">
      <article v-for="item in stats?.byDifficulty" :key="item.difficulty" class="stat-card">
        <span>{{ item.difficulty }}</span>
        <strong>{{ item.totalGames }}</strong>
        <small>Best {{ item.bestTimeMs === null ? '-' : formatElapsed(item.bestTimeMs) }}</small>
        <small>Avg {{ item.averageTimeMs === null ? '-' : formatElapsed(item.averageTimeMs) }}</small>
      </article>
    </div>

    <div class="history-card">
      <div class="history-header">
        <h2>Recent games</h2>
        <select v-model="difficultyFilter">
          <option value="">All difficulties</option>
          <option value="easy">Easy</option>
          <option value="medium">Medium</option>
          <option value="hard">Hard</option>
        </select>
      </div>

      <p v-if="isLoading">Loading history...</p>
      <p v-else-if="history?.items.length === 0">No completed games yet.</p>
      <article v-for="game in history?.items" :key="game.id" class="history-row">
        <div>
          <strong>{{ game.difficulty }}</strong>
          <small>{{ formatDate(game.completedAt) }}</small>
        </div>
        <span>{{ formatElapsed(game.elapsedMs) }}</span>
        <em>{{ game.qualifiedForLeaderboard ? 'Ranked' : 'Casual' }}</em>
      </article>
    </div>
  </section>
</template>

<script setup lang="ts">
import { onMounted, ref, watch } from 'vue'

import type { Difficulty, GameHistoryPageResponse, ProfileStatsResponse } from '../api/contracts'
import { getGameHistory, getProfileStats } from '../api/history'

const history = ref<GameHistoryPageResponse | null>(null)
const stats = ref<ProfileStatsResponse | null>(null)
const difficultyFilter = ref<Difficulty | ''>('')
const isLoading = ref(false)

onMounted(async () => {
  await Promise.all([loadHistory(), loadStats()])
})

watch(difficultyFilter, loadHistory)

async function loadHistory(): Promise<void> {
  isLoading.value = true
  try {
    history.value = await getGameHistory(difficultyFilter.value === '' ? undefined : difficultyFilter.value)
  } finally {
    isLoading.value = false
  }
}

async function loadStats(): Promise<void> {
  stats.value = await getProfileStats()
}

function formatElapsed(milliseconds: number): string {
  const totalSeconds = Math.floor(milliseconds / 1000)
  const minutes = Math.floor(totalSeconds / 60)
  const seconds = totalSeconds % 60
  return `${minutes}:${seconds.toString().padStart(2, '0')}`
}

function formatDate(value: string): string {
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(value))
}
</script>

<style scoped>
.history-page {
  display: grid;
  gap: 1.25rem;
  margin-inline: auto;
  max-width: 1060px;
}

.page-heading,
.stat-card,
.history-card {
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  box-shadow: 0 20px 70px var(--shadow-soft);
}

.page-heading {
  border-radius: 2rem;
  padding: clamp(1.25rem, 4vw, 2.5rem);
}

.eyebrow,
.stat-card span {
  color: var(--color-accent);
  font-size: 0.75rem;
  font-weight: 900;
  letter-spacing: 0.16em;
  margin: 0;
  text-transform: uppercase;
}

h1 {
  font-family: var(--font-display);
  font-size: clamp(2.5rem, 7vw, 5.5rem);
  letter-spacing: -0.06em;
  line-height: 0.9;
  margin: 0.5rem 0;
}

.page-heading p:last-child,
.history-row small {
  color: var(--color-muted);
}

.stats-grid {
  display: grid;
  gap: 1rem;
  grid-template-columns: repeat(3, minmax(0, 1fr));
}

.stat-card {
  border-radius: 1.5rem;
  display: grid;
  gap: 0.35rem;
  padding: 1rem;
}

.stat-card strong {
  font-family: var(--font-display);
  font-size: 3rem;
}

.stat-card small {
  color: var(--color-muted);
}

.history-card {
  border-radius: 1.5rem;
  display: grid;
  gap: 0.75rem;
  padding: 1rem;
}

.history-header,
.history-row {
  align-items: center;
  display: grid;
  gap: 1rem;
  grid-template-columns: 1fr auto auto;
}

h2 {
  font-family: var(--font-display);
  margin: 0;
}

select {
  background: var(--color-paper);
  border: 1px solid var(--color-border);
  border-radius: 999px;
  color: var(--color-text);
  padding: 0.7rem 1rem;
}

.history-row {
  border: 1px solid var(--color-border);
  border-radius: 1rem;
  padding: 0.9rem;
}

.history-row div {
  display: grid;
}

.history-row span {
  font-family: var(--font-display);
  font-size: 1.4rem;
  font-weight: 900;
}

.history-row em {
  color: var(--color-muted);
  font-style: normal;
  font-weight: 900;
}

@media (max-width: 760px) {
  .stats-grid,
  .history-header,
  .history-row {
    grid-template-columns: 1fr;
  }
}
</style>
