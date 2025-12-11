import { test, expect } from '@playwright/test'
import { setupAuth } from './_helpers'

test('登录失败保持导航禁用', async ({ page }) => {
  await setupAuth(page)
  await page.route('**/api/auth/login', async route => {
    await route.fulfill({ status: 401, json: { error: 'invalid' } })
  })
  await page.goto('/')
  await page.getByLabel('用户名').fill('bad')
  await page.getByLabel('密码').fill('bad')
  await page.getByRole('button', { name: '登录' }).click()
  await expect(page.getByRole('button', { name: '登录' })).toBeEnabled()
})
