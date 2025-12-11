import { test, expect } from '@playwright/test'

test('报告模板列表与渲染', async ({ page }) => {
  await page.route('**/api/auth/login', async route => {
    await route.fulfill({ json: { token: 'test-token', user: 'user' } })
  })
  await page.route('**/api/report/template', async route => {
    await route.fulfill({ json: [ { Id: 'tpl-1', Name: '模板1' } ] })
  })
  await page.route('**/api/report/template/tpl-1/render', async route => {
    await route.fulfill({ body: '<html><body><h1>模板渲染</h1></body></html>', headers: { 'content-type': 'text/html' } })
  })
  await page.goto('/')
  await page.getByLabel('用户名').fill('user')
  await page.getByLabel('密码').fill('pass')
  await page.getByRole('button', { name: '登录' }).click()
  await page.getByRole('button', { name: '报告模板' }).click()
  await page.getByRole('button', { name: '加载模板' }).click()
  await expect(page.getByText('模板1')).toBeVisible()
  await page.getByRole('button', { name: '渲染' }).click()
  await expect(page.locator('iframe')).toBeVisible()
})
