import { test, expect } from '@playwright/test'
import { authAndLogin } from './actions'

test('登录并看到导航', async ({ page }) => {
  await authAndLogin(page)
  await expect(page.getByRole('button', { name: '合规自检' })).toBeVisible()
})
