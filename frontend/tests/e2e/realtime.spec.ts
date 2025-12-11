import { test, expect } from '@playwright/test'

test('实时计算会话创建与计算', async ({ page }) => {
  await page.route('**/api/auth/login', async route => {
    await route.fulfill({ json: { token: 'test-token', user: 'user' } })
  })
  await page.route('**/api/realtime/session', async route => {
    await route.fulfill({ json: { SessionId: 'sess-1' } })
  })
  await page.route('**/api/realtime/calculate/sess-1', async route => {
    await route.fulfill({ json: { ok: true, result: {} } })
  })
  await page.goto('/')
  await page.getByLabel('用户名').fill('user')
  await page.getByLabel('密码').fill('pass')
  await page.getByRole('button', { name: '登录' }).click()
  await page.getByRole('button', { name: '实时计算' }).click()
  await page.getByRole('button', { name: '创建会话' }).click()
  await page.getByRole('button', { name: '执行计算' }).click()
  await expect(page.locator('pre').first()).toBeVisible()
})
