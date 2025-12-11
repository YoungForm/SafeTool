import { test, expect } from '@playwright/test'
import { setupAuth, doLogin } from './_helpers'

test('IEC60204 过载保护错误提示', async ({ page }) => {
  await setupAuth(page)
  await page.route('**/api/iec60204/overload-protection/check', async route => {
    await route.fulfill({ json: { ok: false, error: '配置错误' } })
  })
  await doLogin(page)
  await page.getByRole('button', { name: 'IEC60204检查' }).click()
  await page.getByRole('button', { name: '检查过载保护' }).click()
  await expect(page.locator('pre').first()).toBeVisible()
})
