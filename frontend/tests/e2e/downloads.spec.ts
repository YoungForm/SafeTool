import { test, expect } from '@playwright/test'

test('合规PDF与IEC PDF导出触发请求', async ({ page }) => {
  await page.route('**/api/auth/login', async route => {
    await route.fulfill({ json: { token: 'test-token', user: 'user' } })
  })
  let compliancePdfCalls = 0
  await page.route('**/api/compliance/report.pdf', async route => {
    compliancePdfCalls++
    await route.fulfill({ body: Buffer.from('%PDF-1.4'), headers: { 'content-type': 'application/pdf' } })
  })
  let iecPdfCalls = 0
  await page.route('**/api/iec62061/report.pdf', async route => {
    iecPdfCalls++
    await route.fulfill({ body: Buffer.from('%PDF-1.4'), headers: { 'content-type': 'application/pdf' } })
  })
  await page.goto('/')
  await page.getByLabel('用户名').fill('user')
  await page.getByLabel('密码').fill('pass')
  await page.getByRole('button', { name: '登录' }).click()
  await page.getByRole('button', { name: '导出PDF' }).click()
  expect(compliancePdfCalls).toBeGreaterThan(0)
  await page.getByRole('button', { name: 'IEC 62061' }).click()
  await page.getByRole('button', { name: '导出PDF' }).click()
  expect(iecPdfCalls).toBeGreaterThan(0)
})

test('证据下载触发请求', async ({ page }) => {
  await page.route('**/api/auth/login', async route => {
    await route.fulfill({ json: { token: 'test-token', user: 'user' } })
  })
  await page.route('**/api/evidence', async route => {
    await route.fulfill({ json: [ { id: 'E1', name: '证据1', type: 'certificate', status: 'active', url: '' } ] })
  })
  let downloadCalls = 0
  await page.route('**/api/evidence/E1/download', async route => {
    downloadCalls++
    await route.fulfill({ body: Buffer.from('file'), headers: { 'content-type': 'application/octet-stream' } })
  })
  await page.goto('/')
  await page.getByLabel('用户名').fill('user')
  await page.getByLabel('密码').fill('pass')
  await page.getByRole('button', { name: '登录' }).click()
  await page.getByRole('button', { name: '证据库' }).click()
  await page.getByRole('button', { name: '加载' }).click()
  await page.getByRole('button', { name: '下载' }).click()
  expect(downloadCalls).toBeGreaterThan(0)
})
