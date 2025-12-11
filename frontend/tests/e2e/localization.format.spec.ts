import { test, expect } from '@playwright/test'

test('本地化格式化接口演示', async ({ page }) => {
  await page.route('**/api/auth/login', async route => {
    await route.fulfill({ json: { token: 'test-token', user: 'user' } })
  })
  await page.route('**/api/localization/format/unit?**', async route => { await route.fulfill({ json: { formatted: '10h' } }) })
  await page.route('**/api/localization/format/time?**', async route => { await route.fulfill({ json: { formatted: '1.5小时' } }) })
  await page.route('**/api/localization/format/percentage?**', async route => { await route.fulfill({ json: { formatted: '95%' } }) })
  await page.route('**/api/localization/format/datetime', async route => { await route.fulfill({ json: { formatted: '2025-12-10 08:00' } }) })
  await page.route('**/api/localization/format/number', async route => { await route.fulfill({ json: { formatted: '12,345.68' } }) })
  await page.goto('/')
  await page.getByLabel('用户名').fill('user')
  await page.getByLabel('密码').fill('pass')
  await page.getByRole('button', { name: '登录' }).click()
  await page.getByRole('button', { name: '本地化格式化' }).click()
  const expectDialog = async (contains: string) => new Promise<void>(resolve => page.once('dialog', dlg => { expect(dlg.message()).toContain(contains); dlg.accept(); resolve() }))
  const d1 = expectDialog('10h'); await page.getByRole('button', { name: '格式化', exact: true }).nth(0).click(); await d1
  const d2 = expectDialog('1.5'); await page.getByRole('button', { name: '格式化', exact: true }).nth(1).click(); await d2
  const d3 = expectDialog('%'); await page.getByRole('button', { name: '格式化', exact: true }).nth(2).click(); await d3
  const d4 = expectDialog('2025-12-10'); await page.getByRole('button', { name: '格式化', exact: true }).nth(3).click(); await d4
  const d5 = expectDialog('12,345.68'); await page.getByRole('button', { name: '格式化', exact: true }).nth(4).click(); await d5
})
