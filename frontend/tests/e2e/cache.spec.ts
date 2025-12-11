import { test, expect } from '@playwright/test'

test('缓存统计加载', async ({ page }) => {
  await page.route('**/api/auth/login', async route => {
    await route.fulfill({ json: { token: 'test-token', user: 'user' } })
  })
  await page.route('**/api/cache/statistics', async route => {
    await route.fulfill({ json: { entries: 3 } })
  })
  await page.goto('/')
  await page.getByLabel('用户名').fill('user')
  await page.getByLabel('密码').fill('pass')
  await page.getByRole('button', { name: '登录' }).click()
  await page.getByRole('button', { name: '缓存管理' }).click()
  await page.getByRole('button', { name: '加载统计' }).click()
  await expect(page.locator('pre')).toBeVisible()
})
