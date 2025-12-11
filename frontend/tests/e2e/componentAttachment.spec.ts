import { test, expect } from '@playwright/test'

test('组件附件列表加载', async ({ page }) => {
  await page.route('**/api/auth/login', async route => {
    await route.fulfill({ json: { token: 'test-token', user: 'user' } })
  })
  await page.route('**/api/component/attachment/CMP1', async route => {
    await route.fulfill({ json: [ { id: 'A1', name: 'Datasheet', type: 'datasheet', description: '描述' } ] })
  })
  await page.goto('/')
  await page.getByLabel('用户名').fill('user')
  await page.getByLabel('密码').fill('pass')
  await page.getByRole('button', { name: '登录' }).click()
  await page.getByRole('button', { name: '组件附件' }).click()
  await page.getByPlaceholder('组件ID').fill('CMP1')
  await page.getByRole('button', { name: '加载' }).click()
  await expect(page.locator('div').filter({ hasText: 'Datasheet' }).first()).toBeVisible()
})
