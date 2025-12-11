import { test, expect } from '@playwright/test'

test('规则分层加载与对比', async ({ page }) => {
  await page.route('**/api/auth/login', async route => {
    await route.fulfill({ json: { token: 'test-token', user: 'user' } })
  })
  await page.route('**/api/rules/hierarchy/?**', async route => {
    await route.fulfill({ json: [ { Level: 'project', LevelId: 'demo', Key: 'risk.map', Value: 'PLc' } ] })
  })
  await page.route('**/api/rules/hierarchy/compare', async route => {
    await route.fulfill({ json: { diffs: [] } })
  })
  await page.goto('/')
  await page.getByLabel('用户名').fill('user')
  await page.getByLabel('密码').fill('pass')
  await page.getByRole('button', { name: '登录' }).click()
  await page.getByRole('button', { name: '规则分层' }).click()
  await page.getByRole('button', { name: '加载' }).click()
  await expect(page.locator('div').filter({ hasText: 'risk.map' }).first()).toBeVisible()
  await page.getByRole('button', { name: '行业vs项目对比' }).click()
  await expect(page.locator('pre')).toBeVisible()
})
