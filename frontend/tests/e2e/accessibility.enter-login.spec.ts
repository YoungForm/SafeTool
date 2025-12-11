import { test, expect } from '@playwright/test'
import { setupAuth } from './_helpers'

test('回车触发登录按钮', async ({ page }) => {
  await setupAuth(page)
  await page.goto('/')
  await page.getByLabel('用户名').fill('user')
  await page.getByLabel('密码').fill('pass')
  await page.getByRole('button', { name: '登录' }).focus()
  await page.keyboard.press('Enter')
  await expect(page.getByRole('button', { name: '合规自检' })).toBeVisible()
})
