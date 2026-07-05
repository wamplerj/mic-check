import { test, expect } from '@playwright/test';
import { authFile } from './global-setup';

test.use({ storageState: authFile });

test('the sidebar renders and each primary link navigates to its view', async ({ page }) => {
  await page.goto('/dashboard');

  const links: Array<[name: string, path: string]> = [
    ['Dashboard', '/dashboard'],
    ['Features', '/features'],
    ['Segments', '/segments'],
    ['Identities', '/identities'],
    ['Audit Logs', '/audit-logs'],
    ['Settings', '/settings'],
  ];

  for (const [name, path] of links) {
    // Sidebar entries are clickable divs (VerticalNavLink), not <a> elements, so match by text.
    await page.locator('li').filter({ hasText: name }).first().click();
    await expect(page).toHaveURL(new RegExp(`${path}$`));
  }
});
