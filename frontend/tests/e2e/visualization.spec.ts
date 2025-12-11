import { test, expect } from '@playwright/test'

test('通道可视化数据加载', async ({ page }) => {
  await page.route('**/api/auth/login', async route => {
    await route.fulfill({ json: { token: 'test-token', user: 'user' } })
  })
  await page.route('**/api/visualization/channels/*', async route => {
    await route.fulfill({ json: { nodes: [], edges: [] } })
  })
  await page.goto('/')
  await page.getByLabel('用户名').fill('user')
  await page.getByLabel('密码').fill('pass')
  await page.getByRole('button', { name: '登录' }).click()
  await page.getByRole('button', { name: '通道可视化' }).click()
  await page.getByPlaceholder('功能ID').fill('SF-TEST-001')
  await page.getByRole('button', { name: '加载' }).click()
  await expect(page.locator('pre')).toBeVisible()
})
