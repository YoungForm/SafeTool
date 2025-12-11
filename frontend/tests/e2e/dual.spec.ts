import { test, expect } from '@playwright/test'

test('双标准并行评估', async ({ page }) => {
  await page.route('**/api/auth/login', async route => {
    await route.fulfill({ json: { token: 'test-token', user: 'user' } })
  })
  await page.route('**/api/dual-standard/evaluate', async route => {
    await route.fulfill({ json: { ok: true, iso: {}, iec: {} } })
  })
  await page.goto('/')
  await page.getByLabel('用户名').fill('user')
  await page.getByLabel('密码').fill('pass')
  await page.getByRole('button', { name: '登录' }).click()
  await page.getByRole('button', { name: '双标准评估' }).click()
  await page.getByRole('button', { name: '执行并行评估' }).click()
  await expect(page.locator('pre')).toBeVisible()
})
