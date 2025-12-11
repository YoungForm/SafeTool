import { test, expect } from '@playwright/test'

test('本地化语言与文案加载', async ({ page }) => {
  await page.route('**/api/auth/login', async route => {
    await route.fulfill({ json: { token: 'test-token', user: 'user' } })
  })
  await page.route('**/api/localization/languages', async route => {
    await route.fulfill({ json: ['zh-CN','en-US'] })
  })
  await page.route('**/api/localization/strings/zh-CN', async route => {
    await route.fulfill({ json: { title: '标题' } })
  })
  await page.goto('/')
  await page.getByLabel('用户名').fill('user')
  await page.getByLabel('密码').fill('pass')
  await page.getByRole('button', { name: '登录' }).click()
  await page.getByRole('button', { name: '本地化格式化' }).click()
  await page.getByRole('button', { name: '加载语言' }).click()
  await page.getByRole('button', { name: '加载文案' }).click()
  await expect(page.locator('pre')).toBeVisible()
})
