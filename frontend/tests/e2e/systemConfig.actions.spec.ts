import { test, expect } from '@playwright/test'
import { authAndLogin, clickButton } from './actions'

test('系统配置更新/重置/导出/导入与单项设置', async ({ page }) => {
  await page.route('**/api/auth/login', async route => { await route.fulfill({ json: { token: 'test-token', user: 'user' } }) })
  await page.route('**/api/system/config/', async route => {
    if (route.request().method() === 'PUT') {
      await route.fulfill({ json: { appName: 'SafeTool', version: '1.1' } })
    } else {
      await route.fulfill({ json: { appName: 'SafeTool', version: '1.0' } })
    }
  })
  await page.route('**/api/system/config/reset', async route => { await route.fulfill({ json: { appName: 'SafeTool', version: '1.0' } }) })
  await page.route('**/api/system/config/export', async route => { await route.fulfill({ body: '{"appName":"SafeTool"}', headers: { 'content-type': 'application/json' } }) })
  await page.route('**/api/system/config/import', async route => { await route.fulfill({ json: { appName: 'SafeTool', version: '1.2' } }) })
  await page.route('**/api/system/config/setting/appName', async route => {
    if (route.request().method() === 'POST') {
      await route.fulfill({ json: { ok: true } })
    } else {
      await route.fulfill({ body: 'SafeTool', headers: { 'content-type': 'text/plain' } })
    }
  })
  await authAndLogin(page)
  await clickButton(page, '系统配置')
  await clickButton(page, '加载配置')
  await clickButton(page, '更新配置')
  await clickButton(page, '重置配置')
  await clickButton(page, '导出配置')
  await page.getByPlaceholder('键').fill('appName')
  await page.getByRole('button', { name: '读取' }).click()
  await page.getByPlaceholder('值').fill('SafeTool')
  await page.getByRole('button', { name: '设置', exact: true }).nth(1).click()
  await page.getByRole('button', { name: '导入' }).click()
  await expect(page.locator('pre').first()).toBeVisible()
})
