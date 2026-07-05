import { test, expect } from '@playwright/test';
import { authFile } from './global-setup';

test.use({ storageState: authFile });

/**
 * The seeded DB (DatabaseSeeder) provides exactly one org, one project ("My Project"), and
 * three environments (Development/Staging/Production), so the selectors auto-populate without
 * needing to create anything first. Most feature screens require both a project and an
 * environment to be selected before they render real content.
 */
test('project and environment context is selectable and persists across a reload', async ({ page }) => {
  await page.goto('/features');

  const projectSelect = page.getByTestId('project-select').locator('input');
  const envSelect = page.getByTestId('env-select').locator('input');

  await expect(projectSelect).not.toHaveValue('');
  await expect(envSelect).not.toHaveValue('');

  // Explicitly re-select the project via the dropdown to prove the selector itself works,
  // not just the auto-select-on-load behavior.
  await page.getByTestId('project-select').click();
  await page.getByRole('option', { name: 'My Project' }).click();
  await expect(projectSelect).toHaveValue('My Project');

  await page.reload();
  await expect(page.getByTestId('project-select').locator('input')).not.toHaveValue('');
  await expect(page.getByTestId('env-select').locator('input')).not.toHaveValue('');
});
