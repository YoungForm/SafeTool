import { test, expect } from '@playwright/test'

test('合并报告生成', async ({ page }) => {
  await page.route('**/api/auth/login', async route => {
    await route.fulfill({ json: { token: 'test-token', user: 'user' } })
  })
  await page.route('**/api/report/combined', async route => {
    await route.fulfill({ body: '<html><body><h1>合并报告</h1></body></html>', headers: { 'content-type': 'text/html' } })
  })
  await page.goto('/')
  await page.getByLabel('用户名').fill('user')
  await page.getByLabel('密码').fill('pass')
  await page.getByRole('button', { name: '登录' }).click()
  await page.getByRole('button', { name: '合并报告' }).click()
  await page.getByRole('button', { name: '生成' }).click()
  await expect(page.locator('iframe')).toBeVisible()
})
