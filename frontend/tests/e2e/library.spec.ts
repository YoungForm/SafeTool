import { test, expect } from '@playwright/test'

test('组件库加载与替代建议', async ({ page }) => {
  await page.route('**/api/auth/login', async route => {
    await route.fulfill({ json: { token: 'test-token', user: 'user' } })
  })
  await page.route('**/api/library/components', async route => {
    await route.fulfill({ json: [
      { id: 'C1', manufacturer: 'A', model: 'M1', category: 'sensor', parameters: { PFHd: '1e-7' } },
      { id: 'C2', manufacturer: 'A', model: 'M2', category: 'sensor', parameters: { PFHd: '8e-8' } }
    ] })
  })
  await page.route('**/api/component/replacement/*', async route => {
    await route.fulfill({ json: { Suggestions: [ { componentId: 'C2', manufacturer: 'A', model: 'M2', category: 'sensor', score: 80, matchDetails: [], advantages: [], disadvantages: [], compatibilityNotes: [] } ] } })
  })
  await page.goto('/')
  await page.getByLabel('用户名').fill('user')
  await page.getByLabel('密码').fill('pass')
  await page.getByRole('button', { name: '登录' }).click()
  await page.getByRole('button', { name: '组件库' }).click()
  await page.getByRole('button', { name: '加载' }).click()
  await expect(page.getByText('C1')).toBeVisible()
  await page.getByRole('button', { name: '替代建议' }).first().click()
  await expect(page.getByText('分数:')).toBeVisible()
})
