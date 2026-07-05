import { chromium, type FullConfig } from '@playwright/test';
import path from 'node:path';

/**
 * Deterministic dev/QA seed admin credentials — see DatabaseSeeder.SeedAdminEmail /
 * SeedAdminPassword in src/api/MicCheck.Api/Data/DatabaseSeeder.cs. Seeding only ever runs in
 * Development, which is what both local dev and the QA deployment run as.
 */
const ADMIN_EMAIL = process.env.E2E_ADMIN_EMAIL ?? 'admin@miccheck.local';
const ADMIN_PASSWORD = process.env.E2E_ADMIN_PASSWORD ?? 'MicCheckQa!2026';

export const authFile = path.join(__dirname, '.auth', 'admin.json');

export default async function globalSetup(config: FullConfig): Promise<void> {
  const baseURL = config.projects[0]?.use?.baseURL ?? 'http://localhost:5173';

  const browser = await chromium.launch();
  const page = await browser.newPage({ baseURL });

  await page.goto('/login');
  await page.getByTestId('email-input').locator('input').fill(ADMIN_EMAIL);
  await page.getByTestId('password-input').locator('input').fill(ADMIN_PASSWORD);
  await page.getByTestId('login-submit').click();

  // A successful login redirects off /login onto the authenticated shell.
  await page.waitForURL((url) => !url.pathname.startsWith('/login'), { timeout: 15_000 });

  await page.context().storageState({ path: authFile });
  await browser.close();
}
