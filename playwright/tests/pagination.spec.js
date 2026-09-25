import { test, expect } from "@playwright/test";
import {
  createApiContact,
  deleteApiContact,
  ensureMinimumContacts,
  waitForContactsLoaded,
} from "./helpers.js";

test("shows adaptive pagination and navigates between pages", async ({ page, request }) => {
  await ensureMinimumContacts(request, 120);

  await page.goto("/contacts");
  await waitForContactsLoaded(page);
  await expect(page.locator(".page-number").first()).toHaveText("1");
  await expect(page.locator(".page-number").nth(4)).toHaveText("5");
  await expect(page.getByRole("button", { name: "Previous page" })).toBeDisabled();

  await page.getByRole("button", { name: "Next page" }).click();
  await expect(page.locator(".page-number.active")).toHaveText("2");
  await expect(page.getByRole("button", { name: "Previous page" })).toBeEnabled();

  for (let currentPage = 2; currentPage < 10; currentPage += 1) {
    await page.getByRole("button", { name: "Next page" }).click();
  }
  await expect(page.locator(".page-number.active")).toHaveText("10");
  await expect(page.locator(".page-number")).toHaveText(["8", "9", "10", "11", "12"]);
});

test("resets and recalculates pagination when search changes", async ({ page, request }) => {
  await ensureMinimumContacts(request, 12);
  const aarav = await createApiContact(request, { name: "Aarav Mehta" });

  try {
    await page.goto("/contacts");
    await waitForContactsLoaded(page);
    const search = page.getByLabel("Search contacts by name, phone number, or tag");

    await page.getByRole("button", { name: "Next page" }).click();
    await expect(page.locator(".page-number.active")).toHaveText("2");
    await search.fill("Aarav Mehta");
    await expect(page.locator(".page-number.active")).toHaveText("1");
    await expect(page.locator(".summary-card").first()).toContainText("Search results");
  } finally {
    await deleteApiContact(request, aarav.id);
  }
});
