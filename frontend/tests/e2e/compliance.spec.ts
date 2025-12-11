import { test, expect } from '@playwright/test'
import { authAndLogin, clickButton } from './actions'

test('合规自检与报告生成', async ({ page }) => {
  await page.route('**/api/auth/login', async route => { await route.fulfill({ json: { token: 'test-token', user: 'user' } }) })
  await page.route('**/api/compliance/evaluate', async route => {
    await route.fulfill({ json: { isCompliant: true, summary: '合规', nonConformities: [] } })
  })
  await page.route('**/api/compliance/report', async route => {
    await route.fulfill({ body: '<html><body><h1>报告</h1></body></html>', headers: { 'content-type': 'text/html' } })
  })
  await authAndLogin(page)
  await clickButton(page, '执行自检')
  await expect(page.getByText('自检结果')).toBeVisible()
  await clickButton(page, '生成报告')
  await expect(page.locator('iframe')).toBeVisible()
})
