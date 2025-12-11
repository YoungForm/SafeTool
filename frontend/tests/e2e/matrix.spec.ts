import { test, expect } from '@playwright/test'
import { authAndLogin, clickButton, fillByPlaceholder } from './actions'

test('验证矩阵加载', async ({ page }) => {
  await page.route('**/api/auth/login', async route => { await route.fulfill({ json: { token: 'test-token', user: 'user' } }) })
  await page.route('**/api/compliance/matrix?projectId=demo', async route => {
    await route.fulfill({ json: [
      { standard: 'ISO13849-1', clause: '4.1', requirement: '要求A', reference: 'REF1', evidenceId: 'E1', result: '符合', owner: 'user', due: '2025-12-31' },
      { standard: 'IEC62061', clause: '6.2', requirement: '要求B', reference: 'REF2', evidenceId: 'E2', result: '需整改', owner: 'user', due: '2025-12-31' }
    ] })
  })
  await authAndLogin(page)
  await clickButton(page, '验证矩阵')
  await fillByPlaceholder(page, '项目ID', 'demo')
  await clickButton(page, '加载')
  await expect(page.getByText('ISO13849-1')).toBeVisible()
  await expect(page.getByText('IEC62061')).toBeVisible()
})
