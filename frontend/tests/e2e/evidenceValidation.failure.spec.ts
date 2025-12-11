import { test, expect } from '@playwright/test'
import { setupAuth, doLogin } from './_helpers'

test('证据校验失败信息显示', async ({ page }) => {
  await setupAuth(page)
  await page.route('**/api/evidence/validation/E1', async route => {
    await route.fulfill({ json: { ok: false, error: '缺失字段' } })
  })
  await doLogin(page)
  await page.getByRole('button', { name: '证据校验' }).click()
  await page.getByPlaceholder('证据ID').fill('E1')
  await page.getByRole('button', { name: '校验单个' }).click()
  await expect(page.locator('pre')).toBeVisible()
})
