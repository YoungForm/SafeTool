import { test, expect } from '@playwright/test'
import { setupAuth, doLogin } from './_helpers'

test('证据批量校验失败列表', async ({ page }) => {
  await setupAuth(page)
  await page.route('**/api/evidence/validation/batch', async route => {
    await route.fulfill({ json: { results: [ { id: 'E1', ok: false, error: '校验失败' } ] } })
  })
  await doLogin(page)
  await page.getByRole('button', { name: '证据校验' }).click()
  await page.getByRole('button', { name: '校验批量' }).click()
  await expect(page.locator('pre')).toBeVisible()
})
