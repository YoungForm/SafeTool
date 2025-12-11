import { test, expect } from '@playwright/test'
import { authAndLogin } from './actions'
import { mockInteropExports } from './mocks'
import path from 'path'

test('互通导入无效文件反馈', async ({ page }) => {
  await page.route('**/api/auth/login', async route => { await route.fulfill({ json: { token: 'test-token', user: 'user' } }) })
  await mockInteropExports(page)
  await page.route('**/api/interop/import/sistema', async route => {
    await route.fulfill({ json: { imported: 0 } })
  })
  await page.route('**/api/interop/import/pascal', async route => {
    await route.fulfill({ json: { imported: 0 } })
  })
  await authAndLogin(page)
  await page.getByRole('button', { name: '互通' }).click()
  const sistemaPath = path.resolve(process.cwd(), 'tests', 'e2e', 'fixtures', 'sistema.csv')
  const pascalPath = path.resolve(process.cwd(), 'tests', 'e2e', 'fixtures', 'pascal.json')
  const d1 = new Promise<void>(resolve => page.once('dialog', dlg => { expect(dlg.message()).toContain('0'); dlg.accept(); resolve() }))
  await page.getByLabel('导入 SISTEMA CSV').setInputFiles(sistemaPath)
  await d1
  const d2 = new Promise<void>(resolve => page.once('dialog', dlg => { expect(dlg.message()).toContain('0'); dlg.accept(); resolve() }))
  await page.getByLabel('导入 PAScal JSON').setInputFiles(pascalPath)
  await d2
})
