import { test, expect } from '@playwright/test'

test('T1/T10D 参数管理与建议', async ({ page }) => {
  await page.route('**/api/auth/login', async route => {
    await route.fulfill({ json: { token: 'test-token', user: 'user' } })
  })
  await page.route('**/api/t1t10d/manage', async route => {
    await route.fulfill({ json: { ok: true } })
  })
  await page.route('**/api/t1t10d/suggest?**', async route => {
    await route.fulfill({ json: { t1: 2000, t10d: 10000 } })
  })
  await page.goto('/')
  await page.getByLabel('用户名').fill('user')
  await page.getByLabel('密码').fill('pass')
  await page.getByRole('button', { name: '登录' }).click()
  await page.getByRole('button', { name: 'T1/T10D' }).click()
  await page.getByRole('button', { name: '提交管理' }).click()
  await expect(page.locator('pre').first()).toBeVisible()
  await page.getByRole('button', { name: '获取建议' }).click()
  await expect(page.locator('pre').last()).toBeVisible()
})
