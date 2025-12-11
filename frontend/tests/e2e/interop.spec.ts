import { test, expect } from '@playwright/test'
import path from 'path'
import { authAndLogin, clickButton } from './actions'

test('互通导出与导入(SISTEMA/PAScal/SET)', async ({ page }) => {
  await page.route('**/api/auth/login', async route => {
    await route.fulfill({ json: { token: 'test-token', user: 'user' } })
  })
  let calls = { model: 0, interop: 0, libJson: 0, libCsv: 0, sistema: 0, pascal: 0, setjson: 0 }
  await page.route('**/api/model/project', async route => { calls.model++; await route.fulfill({ body: '{}', headers: { 'content-type': 'application/json' } }) })
  await page.route('**/api/interop/export?target=project', async route => { calls.interop++; await route.fulfill({ body: '{}', headers: { 'content-type': 'application/json' } }) })
  await page.route('**/api/library/export', async route => { calls.libJson++; await route.fulfill({ body: '{}', headers: { 'content-type': 'application/json' } }) })
  await page.route('**/api/library/export.csv', async route => { calls.libCsv++; await route.fulfill({ body: 'id,model', headers: { 'content-type': 'text/csv' } }) })
  await page.route('**/api/interop/export/sistema/*', async route => { calls.sistema++; await route.fulfill({ body: 'csv', headers: { 'content-type': 'text/csv' } }) })
  await page.route('**/api/interop/export/pascal/*', async route => { calls.pascal++; await route.fulfill({ body: '{}', headers: { 'content-type': 'application/json' } }) })
  await page.route('**/api/interop/export/siemens-set/*', async route => { calls.setjson++; await route.fulfill({ body: '{}', headers: { 'content-type': 'application/json' } }) })
  await page.route('**/api/interop/export/siemens-set/**', async route => { calls.setjson++; await route.fulfill({ body: '{}', headers: { 'content-type': 'application/json' } }) })
  await page.route('**/api/interop/export/siemens-set?**', async route => { calls.setjson++; await route.fulfill({ body: '{}', headers: { 'content-type': 'application/json' } }) })
  await page.route('**/api/interop/import/sistema', async route => { await route.fulfill({ json: { imported: 3 } }) })
  await page.route('**/api/interop/import/pascal', async route => { await route.fulfill({ json: { imported: 2 } }) })

  await authAndLogin(page)
  await clickButton(page, '互通')
  await page.getByPlaceholder('项目ID').fill('demo')
  await page.getByRole('button', { name: '导出项目（model）' }).click()
  await page.getByRole('button', { name: '导出项目（interop）' }).click()
  await page.getByRole('button', { name: '导出库（JSON）' }).click()
  await page.getByRole('button', { name: '导出库（CSV）' }).click()
  await page.getByRole('button', { name: '导出 SISTEMA CSV' }).click()
  await page.getByRole('button', { name: '导出 PAScal JSON' }).click()
  await page.getByRole('button', { name: '导出 Siemens SET JSON' }).click()
  expect(calls.model).toBeGreaterThan(0)
  expect(calls.interop).toBeGreaterThan(0)
  expect(calls.libJson).toBeGreaterThan(0)
  expect(calls.libCsv).toBeGreaterThan(0)
  expect(calls.sistema).toBeGreaterThan(0)
  expect(calls.pascal).toBeGreaterThan(0)
  expect(calls.setjson).toBeGreaterThan(0)

  const sistemaPath = path.resolve(process.cwd(), 'tests', 'e2e', 'fixtures', 'sistema.csv')
  const pascalPath = path.resolve(process.cwd(), 'tests', 'e2e', 'fixtures', 'pascal.json')
  const sistemaDialog = new Promise<void>(resolve => page.once('dialog', dlg => { expect(dlg.message()).toContain('3'); dlg.accept(); resolve() }))
  await page.getByLabel('导入 SISTEMA CSV').setInputFiles(sistemaPath)
  await sistemaDialog
  const pascalDialog = new Promise<void>(resolve => page.once('dialog', dlg => { expect(dlg.message()).toContain('2'); dlg.accept(); resolve() }))
  await page.getByLabel('导入 PAScal JSON').setInputFiles(pascalPath)
  await pascalDialog
})
