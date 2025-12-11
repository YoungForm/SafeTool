import { test, expect } from '@playwright/test'

test('电气图纸验证', async ({ page }) => {
  await page.route('**/api/auth/login', async route => {
    await route.fulfill({ json: { token: 'test-token', user: 'user' } })
  })
  await page.route('**/api/electrical-drawing/validate', async route => {
    await route.fulfill({ json: { drawingId: 'D1', isValid: false, issues: ['图纸文件名不能为空'] } })
  })
  await page.goto('/')
  await page.getByLabel('用户名').fill('user')
  await page.getByLabel('密码').fill('pass')
  await page.getByRole('button', { name: '登录' }).click()
  await page.getByRole('button', { name: '图纸关联' }).click()
  await page.getByPlaceholder('图纸ID').fill('D1')
  await page.getByPlaceholder('文件名').fill('')
  await page.getByRole('button', { name: '验证图纸信息' }).click()
  await expect(page.getByText('图纸文件名不能为空')).toBeVisible()
})
