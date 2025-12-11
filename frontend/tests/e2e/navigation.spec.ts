import { test, expect } from '@playwright/test'
import { authAndLogin } from './actions'

test('登录后导航按钮可用', async ({ page }) => {
  await authAndLogin(page)
  const buttons = ['合规自检','IEC 62061','验证矩阵','证据库','互通','系统配置']
  for (const name of buttons) {
    await expect(page.getByRole('button', { name })).toBeVisible()
  }
})
