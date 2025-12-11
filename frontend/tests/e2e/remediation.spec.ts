import { test, expect } from '@playwright/test'

test('联动整改生成建议', async ({ page }) => {
  await page.route('**/api/auth/login', async route => {
    await route.fulfill({ json: { token: 'test-token', user: 'user' } })
  })
  await page.route('**/api/remediation/linked/demo', async route => {
    await route.fulfill({ json: [ { Title: '项1', Description: '描述', Owner: 'owner', Due: '2025-12-31' } ] })
  })
  await page.goto('/')
  await page.getByLabel('用户名').fill('user')
  await page.getByLabel('密码').fill('pass')
  await page.getByRole('button', { name: '登录' }).click()
  await page.getByRole('button', { name: '联动整改' }).click()
  await page.getByPlaceholder('项目ID').fill('demo')
  await page.getByRole('button', { name: '生成建议' }).click()
  await expect(page.locator('div').filter({ hasText: '项1' }).first()).toBeVisible()
})
