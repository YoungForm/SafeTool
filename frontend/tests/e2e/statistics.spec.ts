import { test, expect } from '@playwright/test'
import { authAndLogin, clickButton } from './actions'

test('统计报表加载与展示', async ({ page }) => {
  await page.route('**/api/auth/login', async route => { await route.fulfill({ json: { token: 'test-token', user: 'user' } }) })
  await page.route('**/api/statistics/system?**', async route => {
    await route.fulfill({ json: { Projects: 2, Evidences: 5 } })
  })
  await page.route('**/api/statistics/system', async route => {
    await route.fulfill({ json: { Projects: 2, Evidences: 5 } })
  })
  await authAndLogin(page)
  await clickButton(page, '统计报表')
  await clickButton(page, '加载')
  await expect(page.locator('pre')).toBeVisible()
})
