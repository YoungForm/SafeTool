import { Page } from '@playwright/test'

export async function setupAuth(page: Page) {
  await page.route('**/api/auth/login', async route => {
    await route.fulfill({ json: { token: 'test-token', user: 'user' } })
  })
}

export async function doLogin(page: Page) {
  await page.goto('/')
  await page.getByLabel('用户名').fill('user')
  await page.getByLabel('密码').fill('pass')
  await page.getByRole('button', { name: '登录' }).click()
}

export async function mockJson(page: Page, pattern: string, json: any, status: number = 200, headers: Record<string, string> = {}) {
  await page.route(pattern, async route => {
    await route.fulfill({ status, json, headers })
  })
}

export async function mockBinary(page: Page, pattern: string, body: Buffer | string, contentType: string) {
  await page.route(pattern, async route => {
    await route.fulfill({ body: typeof body === 'string' ? Buffer.from(body) : body, headers: { 'content-type': contentType } })
  })
}
