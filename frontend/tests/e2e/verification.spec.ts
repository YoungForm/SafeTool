import { test, expect } from '@playwright/test'
import { authAndLogin, clickButton, fillByPlaceholder } from './actions'

test('验证清单加载', async ({ page }) => {
  await page.route('**/api/auth/login', async route => { await route.fulfill({ json: { token: 'test-token', user: 'user' } }) })
  await page.route('**/api/verification/items?projectId=demo&standard=ISO13849-2', async route => {
    await route.fulfill({ json: [ { code: 'V1', title: '条目1', clause: '4.1' } ] })
  })
  await authAndLogin(page)
  await clickButton(page, '验证清单')
  await fillByPlaceholder(page, '项目ID', 'demo')
  await clickButton(page, '加载')
  await expect(page.getByText('条目1')).toBeVisible()
})
