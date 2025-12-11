import { test, expect } from '@playwright/test'

test('系统配置加载', async ({ page }) => {
  await page.route('**/api/auth/login', async route => {
    await route.fulfill({ json: { token: 'test-token', user: 'user' } })
  })
  await page.route('**/api/system/config/', async route => {
    await route.fulfill({ json: { appName: 'SafeTool', version: '1.0' } })
  })
  await page.goto('/')
  await page.getByLabel('用户名').fill('user')
  await page.getByLabel('密码').fill('pass')
  await page.getByRole('button', { name: '登录' }).click()
  await page.getByRole('button', { name: '系统配置' }).click()
  await page.getByRole('button', { name: '加载配置' }).click()
  await expect(page.locator('pre')).toBeVisible()
})
