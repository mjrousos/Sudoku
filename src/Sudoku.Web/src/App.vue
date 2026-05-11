<template>
  <div class="app-shell">
    <AppHeader />
    <main class="app-main">
      <RouterView />
    </main>
  </div>
</template>

<script setup lang="ts">
import { onMounted } from 'vue'
import { RouterView } from 'vue-router'

import AppHeader from './components/AppHeader.vue'
import { useAuthStore } from './stores/auth-store'

const auth = useAuthStore()

onMounted(() => {
  void auth.initialize()
})
</script>
