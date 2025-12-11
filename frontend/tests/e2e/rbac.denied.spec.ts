import { test, expect } from '@playwright/test'
import { setupAuth } from './_helpers'
import { authAndLogin, clickButton, fillByPlaceholder } from './actions'

test('RBAC 权限拒绝提示', async ({ page }) => {
  await setupAuth(page)
  await page.route('**/api/rbac/check', async route => {
    await route.fulfill({ json: { allowed: false } })
  })
  await authAndLogin(page)
  await clickButton(page, 'RBAC')
  await fillByPlaceholder(page, '权限键', 'component:view-sensitive')
  await clickButton(page, '检查')
  await expect(page.locator('pre')).toBeVisible()
})
