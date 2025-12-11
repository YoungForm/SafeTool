import { test, expect } from '@playwright/test'

test('离线部署配置与文档', async ({ page }) => {
  await page.route('**/api/auth/login', async route => {
    await route.fulfill({ json: { token: 'test-token', user: 'user' } })
  })
  await page.route('**/api/deployment/generate?type=offline', async route => {
    await route.fulfill({ json: { type: 'offline', settings: {} } })
  })
  await page.route('**/api/deployment/offline/document', async route => {
    await route.fulfill({ body: '# 文档', headers: { 'content-type': 'text/markdown' } })
  })
  await page.route('**/api/deployment/validate', async route => {
    await route.fulfill({ json: { ok: true } })
  })
  await page.goto('/')
  await page.getByLabel('用户名').fill('user')
  await page.getByLabel('密码').fill('pass')
  await page.getByRole('button', { name: '登录' }).click()
  await page.getByRole('button', { name: '离线部署' }).click()
  await page.getByRole('button', { name: '生成配置' }).click()
  await expect(page.locator('pre').first()).toBeVisible()
  await page.getByRole('button', { name: '查看文档' }).click()
  await expect(page.locator('pre').nth(1)).toBeVisible()
})
