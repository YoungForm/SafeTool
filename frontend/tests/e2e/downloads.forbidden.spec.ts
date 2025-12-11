import { test, expect } from '@playwright/test'
import { setupAuth } from './_helpers'
import fs from 'fs'
import path from 'path'

test('合规PDF下载被拒绝无下载事件', async ({ page }) => {
  await setupAuth(page)
  await page.route('**/api/compliance/report.pdf', async route => {
    await route.fulfill({ status: 403, body: '{"error":"forbidden"}', headers: { 'content-type': 'application/json' } })
  })
  await page.goto('/')
  await page.getByLabel('用户名').fill('user')
  await page.getByLabel('密码').fill('pass')
  await page.getByRole('button', { name: '登录' }).click()
  await page.getByRole('button', { name: '导出PDF' }).click()
  const d = await page.waitForEvent('download')
  const dir = path.resolve(process.cwd(), 'playwright-downloads')
  fs.mkdirSync(dir, { recursive: true })
  const file = path.join(dir, 'compliance-error.json')
  await d.saveAs(file)
  const content = fs.readFileSync(file, 'utf-8')
  expect(content).toContain('forbidden')
})
