import { test, expect } from '@playwright/test'

test('矩阵增强条款索引与检查', async ({ page }) => {
  await page.route('**/api/auth/login', async route => {
    await route.fulfill({ json: { token: 'test-token', user: 'user' } })
  })
  await page.route('**/api/compliance/matrix/enhancement/demo/clause-index?standard=ISO13849-1', async route => {
    await route.fulfill({ json: [ { Clause: '4.1', Title: '要求A' } ] })
  })
  await page.route('**/api/compliance/matrix/enhancement/demo/check', async route => {
    await route.fulfill({ json: { missing: [], inconsistent: [] } })
  })
  await page.route('**/api/compliance/matrix/enhancement/demo/traceability', async route => {
    await route.fulfill({ json: [ { Requirement: 'A', Evidence: 'E1', Result: 'pass' } ] })
  })
  await page.goto('/')
  await page.getByLabel('用户名').fill('user')
  await page.getByLabel('密码').fill('pass')
  await page.getByRole('button', { name: '登录' }).click()
  await page.getByRole('button', { name: '矩阵增强' }).click()
  await page.getByPlaceholder('项目ID').fill('demo')
  await page.getByRole('button', { name: '条款索引' }).click()
  await expect(page.getByText('4.1')).toBeVisible()
  await page.getByRole('button', { name: '缺失/不一致检查' }).click()
  await expect(page.locator('pre')).toBeVisible()
  await page.getByRole('button', { name: '追溯链生成' }).click()
  await expect(page.getByText('E1')).toBeVisible()
})
