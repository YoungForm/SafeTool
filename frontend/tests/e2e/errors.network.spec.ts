import { test, expect } from '@playwright/test'
import { setupAuth, doLogin } from './_helpers'

test('性能监控网络中断提示', async ({ page }) => {
  await setupAuth(page)
  await page.route('**/api/performance/metrics', async route => { await route.abort() })
  await doLogin(page)
  await page.getByRole('button', { name: '性能监控' }).click()
  await expect(page.locator('pre').first()).toBeVisible()
})
