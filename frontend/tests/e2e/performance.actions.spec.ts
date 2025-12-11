import { test, expect } from '@playwright/test'
import { authAndLogin, clickButton } from './actions'

test('性能报告与重置操作', async ({ page }) => {
  await page.route('**/api/auth/login', async route => { await route.fulfill({ json: { token: 'test-token', user: 'user' } }) })
  let reportCalls = 0; let resetCalls = 0
  await page.route('**/api/performance/report', async route => { reportCalls++; await route.fulfill({ json: { ok: true, operations: [] } }) })
  await page.route('**/api/performance/reset?operation=eval', async route => { resetCalls++; await route.fulfill({ json: { ok: true } }) })
  await page.route('**/api/performance/metrics', async route => { await route.fulfill({ json: [] }) })
  await authAndLogin(page)
  await clickButton(page, '性能监控')
  await clickButton(page, '生成报告')
  await expect(page.locator('pre').first()).toBeVisible()
  await page.getByPlaceholder('操作名').fill('eval')
  await clickButton(page, '重置指标')
  expect(reportCalls).toBeGreaterThan(0)
  expect(resetCalls).toBeGreaterThan(0)
})
