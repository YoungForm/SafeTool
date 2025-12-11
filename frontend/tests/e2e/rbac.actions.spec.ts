import { test, expect } from '@playwright/test'
import { authAndLogin, clickButton } from './actions'

test('RBAC 角色创建/分配/移除/检查权限', async ({ page }) => {
  await page.route('**/api/auth/login', async route => { await route.fulfill({ json: { token: 'test-token', user: 'user' } }) })
  await page.route('**/api/rbac/roles', async route => {
    if (route.request().method() === 'POST') {
      await route.fulfill({ json: { ok: true } })
    } else {
      await route.fulfill({ json: [ { Id: 'role1', Name: '管理员' } ] })
    }
  })
  await page.route('**/api/rbac/users/user/permissions', async route => { await route.fulfill({ json: [ 'component:view-sensitive' ] }) })
  await page.route('**/api/rbac/users/user/roles/role1', async route => { await route.fulfill({ json: { ok: true } }) })
  await page.route('**/api/rbac/check', async route => { await route.fulfill({ json: { allowed: true } }) })
  await authAndLogin(page)
  await clickButton(page, 'RBAC')
  await clickButton(page, '加载角色')
  await page.getByPlaceholder('角色ID').fill('role1')
  await page.getByPlaceholder('权限列表(JSON数组)').fill('["component:view-sensitive"]')
  await clickButton(page, '创建角色')
  await page.getByPlaceholder('用户ID').fill('user')
  await clickButton(page, '加载权限')
  await clickButton(page, '分配角色')
  await clickButton(page, '移除角色')
  await page.getByPlaceholder('权限键').fill('component:view-sensitive')
  await clickButton(page, '检查')
  await expect(page.locator('strong').filter({ hasText: '权限：' })).toBeVisible()
})
