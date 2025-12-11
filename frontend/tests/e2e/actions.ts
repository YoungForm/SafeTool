import { Page } from '@playwright/test'
import fs from 'fs'
import path from 'path'
import { setupAuth, doLogin } from './_helpers'

export async function authAndLogin(page: Page, username: string = 'user', password: string = 'pass') {
  await setupAuth(page)
  await page.goto('/')
  await page.getByLabel('用户名').fill(username)
  await page.getByLabel('密码').fill(password)
  await page.getByRole('button', { name: '登录' }).click()
}

export async function clickButton(page: Page, name: string, exact: boolean = true) {
  await page.getByRole('button', { name, exact }).click()
}

export async function waitAndClickDownload(page: Page, name: string, exact: boolean = true) {
  const d = page.waitForEvent('download')
  await clickButton(page, name, exact)
  return await d
}

export async function saveDownload(dl: any, filename: string) {
  const dir = path.resolve(process.cwd(), 'playwright-downloads')
  fs.mkdirSync(dir, { recursive: true })
  const file = path.join(dir, filename)
  await dl.saveAs(file)
  return file
}

export async function fillByPlaceholder(page: Page, placeholder: string, value: string) {
  await page.getByPlaceholder(placeholder).fill(value)
}
