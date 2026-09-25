import { test, expect } from "@playwright/test";
import { createApiContact, deleteApiContact, waitForContactsLoaded } from "./helpers.js";

test("switches between light and dark appearance settings", async ({ page }) => {
  await page.goto("/settings");
  const appearance = page.getByRole("combobox").first();

  await appearance.selectOption("dark");
  await expect(page.locator("html")).toHaveAttribute("data-theme", "dark");
  await expect(page.getByRole("heading", { name: "Settings" })).toBeVisible();

  await appearance.selectOption("light");
  await expect(page.locator("html")).toHaveAttribute("data-theme", "light");
});

test("marks a contact as recently viewed", async ({ page, request }) => {
  const contact = await createApiContact(request, {
    name: `Playwright Recently Viewed ${Date.now()}`,
  });

  try {
    await page.goto("/contacts");
    await waitForContactsLoaded(page);
    await page.getByLabel("Search contacts by name, phone number, or tag").fill(contact.name);
    await page.getByText(contact.name, { exact: true }).click();
    await expect(page.getByRole("heading", { name: contact.name })).toBeVisible();
    await page.getByRole("button", { name: "Close contact" }).click();
    await page.getByRole("link", { name: "Recently viewed" }).click();
    await expect(page.getByText(contact.name, { exact: true })).toBeVisible();
  } finally {
    await deleteApiContact(request, contact.id);
  }
});
