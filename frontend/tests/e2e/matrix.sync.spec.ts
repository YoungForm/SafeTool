import { test, expect } from '@playwright/test'
import { setupAuth } from './_helpers'
import { authAndLogin, clickButton } from './actions'

test('矩阵同步操作', async ({ page }) => {
  await setupAuth(page)
  let syncCalls = 0
  await page.route('**/api/matrix/sync', async route => {
    syncCalls++
    await route.fulfill({ json: { ok: true } })
  })
  await page.route('**/api/compliance/matrix?**', async route => {
    syncCalls++
    await route.fulfill({ json: { ok: true } })
  })
  await page.route('**/api/compliance/matrix', async route => {
    syncCalls++
    await route.fulfill({ json: { ok: true } })
  })
  await authAndLogin(page)
  await clickButton(page, '同步矩阵')
  expect(syncCalls).toBeGreaterThan(0)
})
