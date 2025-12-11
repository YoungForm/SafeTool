import { Page } from '@playwright/test'

export async function mockAuthSuccess(page: Page) {
  await page.route('**/api/auth/login', async route => {
    await route.fulfill({ json: { token: 'test-token', user: 'user' } })
  })
}

export async function mockInteropExports(page: Page) {
  await page.route('**/api/interop/export/sistema/*', async route => {
    await route.fulfill({ body: 'csv', headers: { 'content-type': 'text/csv' } })
  })
  await page.route('**/api/interop/export/pascal/*', async route => {
    await route.fulfill({ body: '{}', headers: { 'content-type': 'application/json' } })
  })
  await page.route('**/api/interop/export/siemens-set/*', async route => {
    await route.fulfill({ body: '{}', headers: { 'content-type': 'application/json' } })
  })
  await page.route('**/api/interop/export/siemens-set/**', async route => {
    await route.fulfill({ body: '{}', headers: { 'content-type': 'application/json' } })
  })
  await page.route('**/api/interop/export/siemens-set?**', async route => {
    await route.fulfill({ body: '{}', headers: { 'content-type': 'application/json' } })
  })
}

export async function mockEvidencePackageDownloads(page: Page) {
  await page.route('**/api/evidence/package/generate', async route => {
    await route.fulfill({ json: { projectId: 'demo', items: [ { id: 'E1' } ] } })
  })
  await page.route('**/api/evidence/package/export/json', async route => {
    await route.fulfill({ body: '{}', headers: { 'content-type': 'application/json' } })
  })
  await page.route('**/api/evidence/package/export/report', async route => {
    await route.fulfill({ body: '<html></html>', headers: { 'content-type': 'text/html' } })
  })
}

export async function mockStatistics(page: Page) {
  await page.route('**/api/statistics/system?**', async route => {
    await route.fulfill({ json: { Projects: 2, Evidences: 5 } })
  })
  await page.route('**/api/statistics/system', async route => {
    await route.fulfill({ json: { Projects: 2, Evidences: 5 } })
  })
}

export async function mockSystemConfigImportError(page: Page) {
  await page.route('**/api/system/config/import', async route => {
    await route.fulfill({ json: { ok: false, error: 'invalid' } })
  })
}

export async function mockPerformanceReportError(page: Page) {
  await page.route('**/api/performance/report', async route => {
    await route.fulfill({ status: 500, json: { ok: false, error: 'server error' } })
  })
}
