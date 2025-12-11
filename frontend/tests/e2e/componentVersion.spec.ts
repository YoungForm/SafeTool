import { test, expect } from '@playwright/test'

test('组件版本列表加载', async ({ page }) => {
  await page.route('**/api/auth/login', async route => {
    await route.fulfill({ json: { token: 'test-token', user: 'user' } })
  })
  await page.route('**/api/component/version/CMP1', async route => {
    await route.fulfill({ json: [ { Version: 'v1', User: 'user', Time: '2025-01-01' } ] })
  })
  await page.goto('/')
  await page.getByLabel('用户名').fill('user')
  await page.getByLabel('密码').fill('pass')
  await page.getByRole('button', { name: '登录' }).click()
  await page.getByRole('button', { name: '组件版本' }).click()
  await page.getByPlaceholder('组件ID').fill('CMP1')
  await page.getByRole('button', { name: '加载版本' }).click()
  await expect(page.getByText('v1')).toBeVisible()
})
