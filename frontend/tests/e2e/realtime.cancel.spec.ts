import { test, expect } from '@playwright/test'
import { setupAuth, doLogin } from './_helpers'

test('实时计算取消', async ({ page }) => {
  await setupAuth(page)
  await page.route('**/api/realtime/session', async route => {
    await route.fulfill({ json: { SessionId: 'sess-1' } })
  })
  await page.route('**/api/realtime/calculate/sess-1', async route => {
    await route.fulfill({ json: { ok: true } })
  })
  await page.route('**/api/realtime/cancel/sess-1', async route => {
    await route.fulfill({ json: { ok: true } })
  })
  await doLogin(page)
  await page.getByRole('button', { name: '实时计算' }).click()
  await page.getByRole('button', { name: '创建会话' }).click()
  await page.getByRole('button', { name: '执行计算' }).click()
  await expect(page.locator('pre').first()).toBeVisible()
})
