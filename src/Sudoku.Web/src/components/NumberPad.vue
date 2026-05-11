<template>
  <div class="number-pad" aria-label="Number entry controls">
    <button
      v-for="digit in 9"
      :key="digit"
      :disabled="disabled"
      class="digit-button"
      type="button"
      @click="$emit('input', digit)"
    >
      {{ digit }}
    </button>
    <button :disabled="disabled" class="tool-button" type="button" @click="$emit('erase')">Erase</button>
    <button :aria-pressed="notesMode" :disabled="disabled" class="tool-button" type="button" @click="$emit('toggleNotes')">
      Notes {{ notesMode ? 'on' : 'off' }}
    </button>
    <button :disabled="disabled || !canUndo" class="tool-button" type="button" @click="$emit('undo')">Undo</button>
    <button :disabled="disabled || !canRedo" class="tool-button" type="button" @click="$emit('redo')">Redo</button>
  </div>
</template>

<script setup lang="ts">
defineProps<{
  canRedo: boolean
  canUndo: boolean
  disabled: boolean
  notesMode: boolean
}>()

defineEmits<{
  erase: []
  input: [value: number]
  redo: []
  toggleNotes: []
  undo: []
}>()
</script>

<style scoped>
.number-pad {
  display: grid;
  gap: 0.65rem;
  grid-template-columns: repeat(9, minmax(2.5rem, 1fr));
}

.digit-button,
.tool-button {
  border: 1px solid var(--color-border);
  border-radius: 999px;
  cursor: pointer;
  font-weight: 850;
  min-height: 2.8rem;
  transition: transform 140ms ease, background-color 140ms ease, border-color 140ms ease;
}

.digit-button {
  background: var(--color-ink);
  color: var(--color-paper);
  font-family: var(--font-display);
  font-size: 1.25rem;
}

.tool-button {
  background: var(--color-surface);
  color: var(--color-text);
  grid-column: span 2;
}

.tool-button:nth-last-child(1) {
  grid-column: span 3;
}

.digit-button:hover:not(:disabled),
.tool-button:hover:not(:disabled) {
  border-color: var(--color-accent);
  transform: translateY(-1px);
}

.tool-button[aria-pressed="true"] {
  background: var(--color-accent);
  color: var(--color-paper);
}

button:disabled {
  cursor: not-allowed;
  opacity: 0.45;
}

@media (max-width: 720px) {
  .number-pad {
    grid-template-columns: repeat(3, 1fr);
  }

  .tool-button,
  .tool-button:nth-last-child(1) {
    grid-column: span 1;
  }
}
</style>
