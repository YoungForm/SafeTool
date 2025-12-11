import { test, expect } from '@playwright/test'
import { setupAuth, doLogin } from './_helpers'

test('键盘Tab可达交互控件', async ({ page }) => {
  await setupAuth(page)
  await doLogin(page)
  await page.keyboard.press('Tab')
  await page.keyboard.press('Tab')
  await page.keyboard.press('Tab')
  const tag = await page.evaluate(() => document.activeElement?.tagName)
  expect(['BUTTON','INPUT','A']).toContain(tag)
})
