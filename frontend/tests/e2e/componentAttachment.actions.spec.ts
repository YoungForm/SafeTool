import { test, expect } from '@playwright/test'
import path from 'path'

test('组件附件上传/下载/删除', async ({ page }) => {
  await page.route('**/api/auth/login', async route => {
    await route.fulfill({ json: { token: 'test-token', user: 'user' } })
  })
  let uploadCalls = 0; let downloadCalls = 0; let deleteCalls = 0
  await page.route('**/api/component/attachment/CMP1', async route => {
    if (route.request().method() === 'POST') {
      uploadCalls++; await route.fulfill({ json: { ok: true } })
    } else {
      await route.fulfill({ json: [ { id: 'A1', name: 'Datasheet', type: 'datasheet', description: '描述' } ] })
    }
  })
  await page.route('**/api/component/attachment/CMP1/A1/download', async route => { downloadCalls++; await route.fulfill({ body: 'file', headers: { 'content-type': 'application/octet-stream' } }) })
  await page.route('**/api/component/attachment/CMP1/A1', async route => { deleteCalls++; await route.fulfill({ json: { ok: true } }) })
  await page.goto('/')
  await page.getByLabel('用户名').fill('user')
  await page.getByLabel('密码').fill('pass')
  await page.getByRole('button', { name: '登录' }).click()
  await page.getByRole('button', { name: '组件附件' }).click()
  await page.getByPlaceholder('组件ID').fill('CMP1')
  await page.getByRole('button', { name: '加载' }).click()
  const fpath = path.resolve(process.cwd(), 'tests', 'e2e', 'fixtures', 'sistema.csv')
  await page.getByPlaceholder('名称').fill('Datasheet')
  await page.getByPlaceholder('类型').fill('datasheet')
  await page.getByPlaceholder('描述').fill('描述')
  await page.locator('input[type="file"]').setInputFiles(fpath)
  await page.getByRole('button', { name: '上传' }).click()
  await page.getByRole('button', { name: '下载' }).click()
  await page.getByRole('button', { name: '删除' }).click()
  expect(uploadCalls).toBeGreaterThan(0)
  expect(downloadCalls).toBeGreaterThan(0)
  expect(deleteCalls).toBeGreaterThan(0)
})
