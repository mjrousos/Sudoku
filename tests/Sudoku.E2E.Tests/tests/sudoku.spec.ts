import { expect, type Page, test } from '@playwright/test'

const puzzle = [
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

const solution = [
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

const gameId = '00000000-0000-0000-0000-000000000001'

test('anonymous player can solve the deterministic puzzle', async ({ page }) => {
  await mockAnonymousGameApi(page)
  await page.goto('/')

  await page.getByRole('button', { name: 'New puzzle' }).click()
  await expect(page.getByRole('grid', { name: 'Sudoku board' })).toBeVisible()
  await expect(page.getByRole('gridcell', { name: 'Row 1, column 1, 5, fixed clue' })).toBeVisible()

  for (const [index, value] of solution.entries()) {
    if (puzzle[index] !== 0) {
      continue
    }

    const row = Math.floor(index / 9) + 1
    const column = (index % 9) + 1
    await page.getByRole('gridcell', { name: `Row ${row}, column ${column}, empty` }).click()
    await page.getByRole('button', { name: value.toString(), exact: true }).click()
  }

  await expect(page.getByRole('status')).toContainText('Solved in 2:03')
  await expect(page.getByRole('status')).toContainText('Leaderboard eligible')
})

test('hint marks an anonymous game as casual', async ({ page }) => {
  await mockAnonymousGameApi(page)
  await page.goto('/')

  await page.getByRole('button', { name: 'New puzzle' }).click()
  await page.getByRole('gridcell', { name: 'Row 1, column 3, empty' }).click()
  await page.getByRole('button', { name: 'Hint selected cell' }).click()

  await expect(page.getByRole('gridcell', { name: 'Row 1, column 3, 4' })).toBeVisible()
  await expect(page.getByText('Disqualified')).toBeVisible()
  await expect(page.getByText('1 hints used')).toBeVisible()
})

test('development sign-in exposes profile, history, and leaderboard UI', async ({ page }) => {
  await mockSignedInApi(page)

  await page.goto('/sign-in')
  await page.getByLabel('Development username').fill('ranked-player')
  await page.getByRole('button', { name: 'Use development sign-in' }).click()

  await expect(page.getByRole('heading', { name: 'ranked-player' })).toBeVisible()

  await page.getByRole('link', { name: 'History' }).click()
  await expect(page.getByText('1 completed games. Current streak: 1 days.')).toBeVisible()
  await expect(page.getByText('Ranked', { exact: true })).toBeVisible()

  await page.getByRole('link', { name: 'Leaderboards' }).click()
  await expect(page.getByRole('cell', { name: 'ranked-player' })).toBeVisible()
  await expect(page.getByText('2:03')).toBeVisible()
})

async function mockAnonymousGameApi(page: Page): Promise<void> {
  await page.route('**/api/antiforgery/token', async (route) => {
    await route.fulfill({ json: { requestToken: 'token' } })
  })
  await page.route('**/api/me', async (route) => {
    await route.fulfill({ status: 401, json: { title: 'Unauthorized', status: 401 } })
  })
  await page.route('**/api/games', async (route) => {
    await route.fulfill({
      json: {
        difficulty: 'easy',
        gameId,
        puzzle,
        startedAt: '2026-05-08T21:58:00Z',
      },
      status: 201,
    })
  })
  await page.route(`**/api/games/${gameId}/hint`, async (route) => {
    await route.fulfill({ json: { index: 2, value: 4 } })
  })
  await page.route(`**/api/games/${gameId}/complete`, async (route) => {
    await route.fulfill({
      json: {
        completedAt: '2026-05-08T22:00:03Z',
        elapsedMs: 123000,
        leaderboardRank: 1,
        qualifiedForLeaderboard: true,
      },
    })
  })
}

async function mockSignedInApi(page: Page): Promise<void> {
  const currentUser = {
    email: 'ranked-player@example.test',
    id: '11111111-1111-1111-1111-111111111111',
    linkedLogins: [{
      displayName: 'Development',
      email: 'ranked-player@example.test',
      isPrimary: true,
      provider: 'Development',
    }],
    profilePictureUrl: null,
    username: 'ranked-player',
    usernameConfirmed: true,
  }

  await mockAnonymousGameApi(page)
  await page.route('**/api/auth/providers', async (route) => {
    await route.fulfill({ json: [] })
  })
  await page.route('**/api/auth/dev/sign-in', async (route) => {
    await route.fulfill({ json: currentUser })
  })
  await page.route('**/api/me', async (route) => {
    await route.fulfill({ json: currentUser })
  })
  await page.route('**/api/me/history*', async (route) => {
    await route.fulfill({
      json: {
        items: [{
          completedAt: '2026-05-08T22:00:03Z',
          difficulty: 'easy',
          elapsedMs: 123000,
          hintCount: 0,
          id: '22222222-2222-2222-2222-222222222222',
          qualifiedForLeaderboard: true,
          solutionRevealed: false,
          startedAt: '2026-05-08T21:58:00Z',
        }],
        totalCount: 1,
      },
    })
  })
  await page.route('**/api/me/stats', async (route) => {
    await route.fulfill({
      json: {
        byDifficulty: [
          { averageTimeMs: 123000, bestTimeMs: 123000, difficulty: 'easy', totalGames: 1 },
          { averageTimeMs: null, bestTimeMs: null, difficulty: 'medium', totalGames: 0 },
          { averageTimeMs: null, bestTimeMs: null, difficulty: 'hard', totalGames: 0 },
        ],
        currentStreakDays: 1,
        totalGames: 1,
      },
    })
  })
  await page.route('**/api/leaderboards/easy/all-time*', async (route) => {
    await route.fulfill({
      json: {
        entries: [{
          completedAt: '2026-05-08T22:00:03Z',
          elapsedMs: 123000,
          profilePictureUrl: null,
          rank: 1,
          username: 'ranked-player',
        }],
        viewerRank: 1,
      },
    })
  })
}
