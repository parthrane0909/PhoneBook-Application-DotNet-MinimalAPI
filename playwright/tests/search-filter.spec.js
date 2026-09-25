import { test, expect } from "@playwright/test";

import {
  createApiContact,
  deleteApiContact,
  waitForContactsLoaded,
} from "./helpers.js";

test.describe("search, filters, tags, and pagination", () => {
  let seededContact;

  test.beforeEach(async ({ request }) => {
    seededContact = await createApiContact(request, {
      name: `Playwright Search ${Date.now()}`,
      phone_number: `+9198765${String(Date.now()).slice(-5)}`,
      email: `search.${Date.now()}@example.com`,
      tags: ["Work", "Friends"],
    });
  });

  test.afterEach(async ({ request }) => {
    await deleteApiContact(request, seededContact?.id);
  });

  test("searches by name, phone, and tag without a refresh", async ({ page }) => {
    await page.goto("/contacts");

    await waitForContactsLoaded(page);

    const search = page.getByLabel(
      "Search contacts by name, phone number, or tag"
    );

    await search.fill(seededContact.name);

    await expect(
      page.getByText(seededContact.name, { exact: true })
    ).toBeVisible();

    await expect(
      page.locator(".summary-card").first().getByText("Search results")
    ).toBeVisible();

    await search.fill(seededContact.phone_number.slice(-5));

    await expect(
      page.getByText(seededContact.name, { exact: true })
    ).toBeVisible();

    await search.fill("work");

    await expect(
      page.getByText(seededContact.name, { exact: true })
    ).toBeVisible();

    await expect(page.getByLabel("Filter by tag")).toHaveValue("");

    await search.fill("");

    await expect(page.getByLabel("Filter by tag")).toHaveValue("");
  });

  test("combines a tag filter with search and clears both", async ({ page }) => {
    await page.goto("/contacts");

    await waitForContactsLoaded(page);

    const search = page.getByLabel(
      "Search contacts by name, phone number, or tag"
    );

    const tagFilter = page.getByLabel("Filter by tag");

    await tagFilter.selectOption("Work");

    await expect(tagFilter).toHaveValue("Work");

    await expect(page).toHaveURL(/tag=Work/);

    await search.fill(seededContact.name);

    await expect(
      page.getByText(seededContact.name, { exact: true })
    ).toBeVisible();
    
    await page.getByRole("button", { name: "Clear filters" }).click();

    await expect(search).toHaveValue("");

    await expect(tagFilter).toHaveValue("");

    await expect(page).toHaveURL(/\/contacts(?:\?|$)/);
  });

  test("filter and pagination controls update visible results", async ({ page }) => {
    await page.goto("/contacts");

    await waitForContactsLoaded(page);

    const tagFilter = page.getByLabel("Filter by tag");

    await tagFilter.selectOption("Work");

    await expect(page.locator(".pagination-info")).toContainText(/of/);

    const next = page.getByRole("button", { name: "Next page" });

    if (await next.isEnabled()) {
      await next.click();

      await expect(page.locator(".page-number.active")).toHaveText("2");

      await page.getByRole("button", { name: "Previous page" }).click();

      await expect(page.locator(".page-number.active")).toHaveText("1");
    }
  });
});