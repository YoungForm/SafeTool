import { test, expect } from '@playwright/test'

test('证据包生成', async ({ page }) => {
  await page.route('**/api/auth/login', async route => {
    await route.fulfill({ json: { token: 'test-token', user: 'user' } })
  })
  await page.route('**/api/evidence/package/generate', async route => {
    await route.fulfill({ json: { projectId: 'demo', items: [ { id: 'E1' } ] } })
  })
  await page.goto('/')
  await page.getByLabel('用户名').fill('user')
  await page.getByLabel('密码').fill('pass')
  await page.getByRole('button', { name: '登录' }).click()
  await page.getByRole('button', { name: '证据包' }).click()
  await page.getByRole('button', { name: '生成' }).click()
  await expect(page.locator('pre')).toBeVisible()
})
