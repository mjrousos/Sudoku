<template>
  <section class="rankings-page">
    <div class="page-heading">
      <p class="eyebrow">Public rankings</p>
      <h1>Fast grids, clean wins.</h1>
      <p>Hints and revealed solutions stay out of ranked play.</p>
    </div>

    <div class="filters">
      <label>
        Difficulty
        <select v-model="difficulty">
          <option value="easy">Easy</option>
          <option value="medium">Medium</option>
          <option value="hard">Hard</option>
        </select>
      </label>
      <label>
        Window
        <select v-model="window">
          <option value="daily">Daily</option>
          <option value="weekly">Weekly</option>
          <option value="all-time">All-time</option>
        </select>
      </label>
      <label class="checkbox-label">
        <input v-model="bestPerPlayer" type="checkbox" />
        Best per player
      </label>
    </div>

    <div class="leaderboard-card">
      <p v-if="isLoading">Loading leaderboard...</p>
      <p v-else-if="leaderboard?.entries.length === 0">No ranked completions yet.</p>
      <table v-else>
        <thead>
          <tr>
            <th>Rank</th>
            <th>Player</th>
            <th>Time</th>
            <th>Completed</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="entry in leaderboard?.entries" :key="`${entry.rank}-${entry.username}-${entry.completedAt}`">
            <td>#{{ entry.rank }}</td>
            <td class="player-cell">
              <img v-if="entry.profilePictureUrl" :src="entry.profilePictureUrl" alt="" />
              <span>{{ entry.username }}</span>
            </td>
            <td>{{ formatElapsed(entry.elapsedMs) }}</td>
            <td>{{ formatDate(entry.completedAt) }}</td>
          </tr>
        </tbody>
      </table>
      <p v-if="leaderboard?.viewerRank" class="viewer-rank">Your rank: #{{ leaderboard.viewerRank }}</p>
    </div>
  </section>
</template>

<script setup lang="ts">
import { onMounted, ref, watch } from 'vue'

import type { Difficulty, LeaderboardResponse, LeaderboardWindow } from '../api/contracts'
import { getLeaderboard } from '../api/leaderboards'

const difficulty = ref<Difficulty>('easy')
const window = ref<LeaderboardWindow>('all-time')
const bestPerPlayer = ref(true)
const leaderboard = ref<LeaderboardResponse | null>(null)
const isLoading = ref(false)

onMounted(loadLeaderboard)
watch([difficulty, window, bestPerPlayer], loadLeaderboard)

async function loadLeaderboard(): Promise<void> {
  isLoading.value = true
  try {
    leaderboard.value = await getLeaderboard(difficulty.value, window.value, bestPerPlayer.value)
  } finally {
    isLoading.value = false
  }
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
.rankings-page {
  display: grid;
  gap: 1.25rem;
  margin-inline: auto;
  max-width: 1060px;
}

.page-heading,
.filters,
.leaderboard-card {
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  box-shadow: 0 20px 70px var(--shadow-soft);
}

.page-heading {
  border-radius: 2rem;
  padding: clamp(1.25rem, 4vw, 2.5rem);
}

.eyebrow {
  color: var(--color-accent);
  font-size: 0.75rem;
  font-weight: 900;
  letter-spacing: 0.16em;
  margin: 0 0 0.5rem;
  text-transform: uppercase;
}

h1 {
  font-family: var(--font-display);
  font-size: clamp(2.5rem, 7vw, 5.5rem);
  letter-spacing: -0.06em;
  line-height: 0.9;
  margin: 0;
}

.page-heading p:last-child {
  color: var(--color-muted);
}

.filters {
  align-items: end;
  border-radius: 1.5rem;
  display: flex;
  flex-wrap: wrap;
  gap: 1rem;
  padding: 1rem;
}

label {
  color: var(--color-muted);
  display: grid;
  font-weight: 900;
  gap: 0.35rem;
}

select {
  background: var(--color-paper);
  border: 1px solid var(--color-border);
  border-radius: 999px;
  color: var(--color-text);
  min-width: 11rem;
  padding: 0.7rem 1rem;
}

.checkbox-label {
  align-items: center;
  display: flex;
  gap: 0.5rem;
  min-height: 2.8rem;
}

.leaderboard-card {
  border-radius: 1.5rem;
  overflow: hidden;
  padding: 1rem;
}

table {
  border-collapse: collapse;
  width: 100%;
}

th,
td {
  border-bottom: 1px solid var(--color-border);
  padding: 0.9rem;
  text-align: left;
}

th {
  color: var(--color-muted);
  font-size: 0.75rem;
  letter-spacing: 0.14em;
  text-transform: uppercase;
}

.player-cell {
  align-items: center;
  display: flex;
  gap: 0.6rem;
  font-weight: 900;
}

.player-cell img {
  border-radius: 0.65rem;
  height: 2rem;
  object-fit: cover;
  width: 2rem;
}

.viewer-rank {
  color: var(--color-accent);
  font-weight: 900;
}
</style>
