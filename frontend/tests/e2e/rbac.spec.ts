import { test, expect } from '@playwright/test'

test('RBAC 加载角色', async ({ page }) => {
  await page.route('**/api/auth/login', async route => {
    await route.fulfill({ json: { token: 'test-token', user: 'user' } })
  })
  await page.route('**/api/rbac/roles', async route => {
    await route.fulfill({ json: [ { Id: 'role1', Name: '管理员' } ] })
  })
  await page.goto('/')
  await page.getByLabel('用户名').fill('user')
  await page.getByLabel('密码').fill('pass')
  await page.getByRole('button', { name: '登录' }).click()
  await page.getByRole('button', { name: 'RBAC' }).click()
  await page.getByRole('button', { name: '加载角色' }).click()
  await expect(page.getByText('role1')).toBeVisible()
})
