import { test, expect } from '@playwright/test'

test('证据校验与证据链', async ({ page }) => {
  await page.route('**/api/auth/login', async route => {
    await route.fulfill({ json: { token: 'test-token', user: 'user' } })
  })
  await page.route('**/api/evidence/validation/*', async route => {
    if (route.request().url().includes('/batch')) {
      await route.fulfill({ json: { results: [] } })
    } else if (route.request().url().includes('/chain/')) {
      await route.fulfill({ json: { ok: true } })
    } else {
      await route.fulfill({ json: { ok: true } })
    }
  })
  await page.goto('/')
  await page.getByLabel('用户名').fill('user')
  await page.getByLabel('密码').fill('pass')
  await page.getByRole('button', { name: '登录' }).click()
  await page.getByRole('button', { name: '证据校验' }).click()
  await page.getByPlaceholder('证据ID').fill('E1')
  await page.getByRole('button', { name: '校验单个' }).click()
  await expect(page.locator('pre').first()).toBeVisible()
  await page.getByRole('button', { name: '校验批量' }).click()
  await expect(page.locator('pre').nth(1)).toBeVisible()
  await page.getByRole('button', { name: '验证链路' }).click()
  await expect(page.locator('pre').last()).toBeVisible()
})
