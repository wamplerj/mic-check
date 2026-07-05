import { test, expect } from '@playwright/test';

/**
 * Unauthenticated — does not use the shared storageState fixture (see global-setup.ts) since
 * it exercises the login flow itself.
 */
test.use({ storageState: { cookies: [], origins: [] } });

const ADMIN_EMAIL = process.env.E2E_ADMIN_EMAIL ?? 'admin@miccheck.local';
const ADMIN_PASSWORD = process.env.E2E_ADMIN_PASSWORD ?? 'MicCheckQa!2026';

test('valid credentials sign the admin in and land on the authenticated shell', async ({ page }) => {
  await page.goto('/login');

  await page.getByTestId('email-input').locator('input').fill(ADMIN_EMAIL);
  await page.getByTestId('password-input').locator('input').fill(ADMIN_PASSWORD);
  await page.getByTestId('login-submit').click();

  await page.waitForURL((url) => !url.pathname.startsWith('/login'));
  await expect(page.getByText('Features', { exact: true })).toBeVisible();
});

test('invalid credentials surface a login error and stay on the login page', async ({ page }) => {
  await page.goto('/login');

  await page.getByTestId('email-input').locator('input').fill(ADMIN_EMAIL);
  await page.getByTestId('password-input').locator('input').fill('not-the-right-password');
  await page.getByTestId('login-submit').click();

  await expect(page.getByTestId('login-error')).toBeVisible();
  await expect(page).toHaveURL(/\/login/);
});
