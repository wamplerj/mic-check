import { test, expect } from '@playwright/test';
import { authFile } from './global-setup';

test.use({ storageState: authFile });

test('the features table renders for the selected project', async ({ page }) => {
  await page.goto('/features');

  await expect(page.getByTestId('features-table')).toBeVisible();
  await expect(page.getByText('Select a project')).toHaveCount(0);
});

test('creating a feature adds it to the table and its toggle can be flipped', async ({ page }) => {
  const featureName = `e2e_feature_${Date.now()}`;

  await page.goto('/features');

  await page.getByTestId('create-feature-btn').click();
  await page.getByTestId('feature-name-input').locator('input').fill(featureName);
  await page.getByTestId('feature-dialog-save').click();

  const row = page.getByRole('row', { name: new RegExp(featureName) });
  await expect(row).toBeVisible();

  // Creating a feature triggers a fresh fetch of feature states for the current environment;
  // wait for it to settle so the toggle below acts on real, loaded state rather than racing it.
  await page.waitForLoadState('networkidle');

  const toggle = row.locator('input[type="checkbox"]');
  const wasChecked = await toggle.isChecked();

  // Assert on the real PATCH landing, not just the switch's transient DOM state: if the
  // feature-state map hasn't loaded for this row yet, the click handler no-ops silently and
  // the switch briefly flashes the native checkbox state before Vue snaps it back - which
  // reads as "toggled" for an instant even though nothing was ever sent to the API.
  const patchResponse = page.waitForResponse(
    (res) => res.request().method() === 'PATCH' && res.url().includes('/featurestate/') && res.ok(),
  );
  await toggle.click({ force: true });
  await patchResponse;
  await expect(toggle).toBeChecked({ checked: !wasChecked });

  // Reload to confirm the toggle was persisted to the real API/DB, not just local state.
  await page.reload();
  const reloadedRow = page.getByRole('row', { name: new RegExp(featureName) });
  await expect(reloadedRow.locator('input[type="checkbox"]')).toBeChecked({ checked: !wasChecked });
});
