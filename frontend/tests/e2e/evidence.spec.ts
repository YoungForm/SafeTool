import { test, expect } from '@playwright/test'
import { authAndLogin, clickButton } from './actions'

test('证据库加载', async ({ page }) => {
  await page.route('**/api/auth/login', async route => { await route.fulfill({ json: { token: 'test-token', user: 'user' } }) })
  await page.route('**/api/evidence', async route => {
    await route.fulfill({ json: [ { id: 'E1', name: '证据1', type: 'certificate', status: 'active' } ] })
  })
  await authAndLogin(page)
  await clickButton(page, '证据库')
  await clickButton(page, '加载')
  await expect(page.locator('div').filter({ hasText: '证据1' }).first()).toBeVisible()
})
