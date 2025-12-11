import { test, expect } from '@playwright/test'
import { setupAuth, doLogin } from './_helpers'

test('RBAC 检查 401', async ({ page }) => {
  await setupAuth(page)
  let calls = 0
  await page.route('**/api/rbac/check', async route => { calls++; await route.fulfill({ status: 401, json: { error: 'unauthorized' } }) })
  await doLogin(page)
  await page.getByRole('button', { name: 'RBAC' }).click()
  await page.getByPlaceholder('权限键').fill('component:view-sensitive')
  await page.getByRole('button', { name: '检查', exact: true }).click()
  expect(calls).toBeGreaterThan(0)
})

test('互通导出 403', async ({ page }) => {
  await setupAuth(page)
  let calls = 0
  await page.route('**/api/interop/export/sistema/*', async route => { calls++; await route.fulfill({ status: 403, json: { error: 'forbidden' } }) })
  await doLogin(page)
  await page.getByRole('button', { name: '互通' }).click()
  await page.getByPlaceholder('项目ID').fill('demo')
  await page.getByRole('button', { name: '导出 SISTEMA CSV' }).click()
  expect(calls).toBeGreaterThan(0)
})

test('性能报告 500', async ({ page }) => {
  await setupAuth(page)
  let calls = 0
  await page.route('**/api/performance/report', async route => { calls++; await route.fulfill({ status: 500, json: { error: 'error' } }) })
  await doLogin(page)
  await page.getByRole('button', { name: '性能监控' }).click()
  await page.getByRole('button', { name: '生成报告' }).click()
  expect(calls).toBeGreaterThan(0)
})
