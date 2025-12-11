import { test, expect } from '@playwright/test'

test('SRECS 分解分析', async ({ page }) => {
  await page.route('**/api/auth/login', async route => {
    await route.fulfill({ json: { token: 'test-token', user: 'user' } })
  })
  await page.route('**/api/srecs/decomposition/analyze', async route => {
    await route.fulfill({ json: { structure: [{ id: 'N1' }] } })
  })
  await page.goto('/')
  await page.getByLabel('用户名').fill('user')
  await page.getByLabel('密码').fill('pass')
  await page.getByRole('button', { name: '登录' }).click()
  await page.getByRole('button', { name: 'SRECS分解' }).click()
  await page.getByRole('button', { name: '分析' }).click()
  await expect(page.locator('pre')).toBeVisible()
})
