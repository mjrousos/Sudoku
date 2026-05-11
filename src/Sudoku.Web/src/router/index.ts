import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router'

import { useAuthStore } from '../stores/auth-store'
import HistoryView from '../views/HistoryView.vue'
import HowToPlayView from '../views/HowToPlayView.vue'
import LeaderboardsView from '../views/LeaderboardsView.vue'
import NotFoundView from '../views/NotFoundView.vue'
import PlayView from '../views/PlayView.vue'
import ProfileView from '../views/ProfileView.vue'
import SignInView from '../views/SignInView.vue'

declare module 'vue-router' {
  interface RouteMeta {
    requiresAuth?: boolean
  }
}

const routes: RouteRecordRaw[] = [
  { path: '/', name: 'play', component: PlayView },
  { path: '/leaderboards', name: 'leaderboards', component: LeaderboardsView },
  { path: '/history', name: 'history', component: HistoryView, meta: { requiresAuth: true } },
  { path: '/profile', name: 'profile', component: ProfileView, meta: { requiresAuth: true } },
  { path: '/sign-in', name: 'sign-in', component: SignInView },
  { path: '/how-to-play', name: 'how-to-play', component: HowToPlayView },
  { path: '/:pathMatch(.*)*', name: 'not-found', component: NotFoundView },
]

export const router = createRouter({
  history: createWebHistory(),
  routes,
})

router.beforeEach(async (to) => {
  const auth = useAuthStore()
  if (to.meta.requiresAuth) {
    await auth.initialize()
  }

  if (to.meta.requiresAuth && !auth.isAuthenticated) {
    return {
      name: 'sign-in',
      query: { returnUrl: to.fullPath },
    }
  }

  return true
})
