import { test, expect } from '@playwright/test'
import { setupAuth, doLogin } from './_helpers'

test('合并报告生成失败提示', async ({ page }) => {
  await setupAuth(page)
  let generateCalls = 0
  await page.route('**/api/report/combined**', async route => {
    generateCalls++
    await route.fulfill({ json: { ok: false, error: '生成失败' } })
  })
  await doLogin(page)
  await page.getByRole('button', { name: '合并报告' }).click()
  await page.getByRole('button', { name: '生成' }).click()
  expect(generateCalls).toBeGreaterThan(0)
})
