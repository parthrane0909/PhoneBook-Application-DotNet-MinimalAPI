import { test, expect } from "@playwright/test";
import { waitForContactsLoaded } from "./helpers.js";

test("loads the contacts workspace and primary navigation", async ({ page }) => {
  await page.goto("/contacts");
  await waitForContactsLoaded(page);

  await expect(page.getByText("Phonebook", { exact: true })).toBeVisible();
  await expect(page.getByRole("link", { name: "All Contacts" })).toBeVisible();
  await expect(page.getByRole("link", { name: "Favorites" })).toBeVisible();
  await expect(page.getByRole("link", { name: "Recently viewed" })).toBeVisible();
  await expect(page.getByRole("link", { name: "Settings" })).toBeVisible();
  await expect(page.locator(".contact-table, .state-message").first()).toBeVisible();
});

test("supports the responsive mobile shell and search control", async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto("/contacts");
  await waitForContactsLoaded(page);

  await expect(page.getByRole("button", { name: "Open navigation" })).toBeVisible();
  await expect(page.getByLabel("Search contacts by name, phone number, or tag")).toBeVisible();
  await page.getByRole("button", { name: "Open navigation" }).click();
  await expect(page.getByRole("button", { name: "Close navigation" })).toBeVisible();
});
