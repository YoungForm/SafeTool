import { test, expect } from '@playwright/test'

test('缓存清空与删键', async ({ page }) => {
  await page.route('**/api/auth/login', async route => {
    await route.fulfill({ json: { token: 'test-token', user: 'user' } })
  })
  let clearCalls = 0; let deleteCalls = 0
  await page.route('**/api/cache/clear', async route => { clearCalls++; await route.fulfill({ json: { ok: true } }) })
  await page.route('**/api/cache/mykey', async route => { deleteCalls++; await route.fulfill({ json: { ok: true } }) })
  await page.route('**/api/cache/statistics', async route => { await route.fulfill({ json: { entries: 0 } }) })
  await page.goto('/')
  await page.getByLabel('用户名').fill('user')
  await page.getByLabel('密码').fill('pass')
  await page.getByRole('button', { name: '登录' }).click()
  await page.getByRole('button', { name: '缓存管理' }).click()
  await page.getByRole('button', { name: '清空缓存' }).click()
  await page.getByPlaceholder('键').fill('mykey')
  await page.getByRole('button', { name: '删除键' }).click()
  expect(clearCalls).toBeGreaterThan(0)
  expect(deleteCalls).toBeGreaterThan(0)
})
