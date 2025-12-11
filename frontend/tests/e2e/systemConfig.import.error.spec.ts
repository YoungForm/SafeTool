import { test, expect } from '@playwright/test'
import { setupAuth, doLogin } from './_helpers'
import { mockSystemConfigImportError } from './mocks'

test('系统配置导入错误提示', async ({ page }) => {
  await setupAuth(page)
  await mockSystemConfigImportError(page)
  await doLogin(page)
  await page.getByRole('button', { name: '系统配置' }).click()
  await page.getByRole('button', { name: '导入' }).click()
  await expect(page.locator('pre')).toBeVisible()
})
