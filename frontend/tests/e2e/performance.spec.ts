import { test, expect } from '@playwright/test'
import { authAndLogin, clickButton } from './actions'

test('性能监控加载指标与警告', async ({ page }) => {
  await page.route('**/api/auth/login', async route => { await route.fulfill({ json: { token: 'test-token', user: 'user' } }) })
  await page.route('**/api/performance/metrics', async route => {
    await route.fulfill({ json: [ { operation: 'evaluate', count: 10 } ] })
  })
  await page.route('**/api/performance/warnings', async route => {
    await route.fulfill({ json: [ { operation: 'report', message: 'slow' } ] })
  })
  await authAndLogin(page)
  await clickButton(page, '性能监控')
  await clickButton(page, '加载指标')
  await clickButton(page, '加载警告')
  await expect(page.locator('pre').first()).toBeVisible()
})
