import { test, expect } from '@playwright/test'

test('批量评估执行', async ({ page }) => {
  await page.route('**/api/auth/login', async route => {
    await route.fulfill({ json: { token: 'test-token', user: 'user' } })
  })
  await page.route('**/api/batch/evaluation/iso13849', async route => {
    await route.fulfill({ json: { ok: true, items: [] } })
  })
  await page.route('**/api/batch/evaluation/iec62061', async route => {
    await route.fulfill({ json: { ok: true, items: [] } })
  })
  await page.route('**/api/batch/evaluation/combined', async route => {
    await route.fulfill({ json: { ok: true, combined: [] } })
  })
  await page.goto('/')
  await page.getByLabel('用户名').fill('user')
  await page.getByLabel('密码').fill('pass')
  await page.getByRole('button', { name: '登录' }).click()
  await page.getByRole('button', { name: '批量评估' }).click()
  await page.getByRole('button', { name: '执行 ISO 批评估' }).click()
  await expect(page.locator('pre').first()).toBeVisible()
  await page.getByRole('button', { name: '执行 IEC 批评估' }).click()
  await expect(page.locator('pre').nth(1)).toBeVisible()
  await page.getByRole('button', { name: '执行联合批评估' }).click()
  await expect(page.locator('pre').last()).toBeVisible()
})
