import { test, expect } from '@playwright/test'
import { setupAuth, doLogin } from './_helpers'

test('报告模板渲染失败提示', async ({ page }) => {
  await setupAuth(page)
  let renderCalls = 0
  await page.route('**/api/report/template/render/**', async route => {
    renderCalls++
    await route.fulfill({ json: { ok: false, error: '渲染失败' } })
  })
  await page.route('**/api/report/template/*/render', async route => {
    renderCalls++
    await route.fulfill({ json: { ok: false, error: '渲染失败' } })
  })
  await doLogin(page)
  await page.getByRole('button', { name: '报告模板' }).click()
  await page.getByRole('button', { name: '渲染' }).click()
  expect(renderCalls).toBeGreaterThan(0)
})
