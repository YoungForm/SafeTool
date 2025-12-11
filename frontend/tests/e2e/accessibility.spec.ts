import { test, expect } from '@playwright/test'
import { setupAuth, doLogin } from './_helpers'

test('基础可访问性：页面存在多个按钮', async ({ page }) => {
  await setupAuth(page)
  await doLogin(page)
  const buttons = await page.getByRole('button').all()
  expect(buttons.length).toBeGreaterThan(3)
})
