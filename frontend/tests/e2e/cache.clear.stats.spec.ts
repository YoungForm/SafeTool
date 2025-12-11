import { test, expect } from '@playwright/test'
import { setupAuth, doLogin } from './_helpers'

test('清空缓存后统计为0', async ({ page }) => {
  await setupAuth(page)
  await page.route('**/api/cache/clear', async route => { await route.fulfill({ json: { ok: true } }) })
  await page.route('**/api/cache/statistics', async route => { await route.fulfill({ json: { entries: 0 } }) })
  await doLogin(page)
  await page.getByRole('button', { name: '缓存管理' }).click()
  await page.getByRole('button', { name: '清空缓存' }).click()
  await page.getByRole('button', { name: '加载统计' }).click()
  await expect(page.locator('pre').first()).toBeVisible()
})
