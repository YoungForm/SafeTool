import { test, expect } from '@playwright/test'

test('IEC60204 过载保护检查', async ({ page }) => {
  await page.route('**/api/auth/login', async route => {
    await route.fulfill({ json: { token: 'test-token', user: 'user' } })
  })
  await page.route('**/api/iec60204/overload-protection/check', async route => {
    await route.fulfill({ json: { ok: true, items: [] } })
  })
  await page.goto('/')
  await page.getByLabel('用户名').fill('user')
  await page.getByLabel('密码').fill('pass')
  await page.getByRole('button', { name: '登录' }).click()
  await page.getByRole('button', { name: 'IEC60204检查' }).click()
  await page.getByRole('button', { name: '检查过载保护' }).click()
  await expect(page.locator('pre').first()).toBeVisible()
})
