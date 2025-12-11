import { test, expect } from '@playwright/test'
import { authAndLogin, clickButton } from './actions'

test('IEC62061评估与报告', async ({ page }) => {
  await page.route('**/api/auth/login', async route => { await route.fulfill({ json: { token: 'test-token', user: 'user' } }) })
  await page.route('**/api/iec62061/evaluate', async route => {
    await route.fulfill({ json: { PFHd: 1e-7, AchievedSIL: 'SIL2', warnings: [] } })
  })
  await page.route('**/api/iec62061/report', async route => {
    await route.fulfill({ body: '<html><body><h1>IEC报告</h1></body></html>', headers: { 'content-type': 'text/html' } })
  })
  await authAndLogin(page)
  await clickButton(page, 'IEC 62061')
  await clickButton(page, '执行评估')
  await expect(page.getByText('SIL:')).toBeVisible()
  await clickButton(page, '预览报告')
  await expect(page.locator('iframe')).toBeVisible()
})
