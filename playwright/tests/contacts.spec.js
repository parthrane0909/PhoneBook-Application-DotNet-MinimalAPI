import { test, expect } from "@playwright/test";
import { uniqueContact, waitForContactsLoaded } from "./helpers.js";

test("creates, edits, favorites, and deletes a contact through the UI", async ({ page }) => {
  const contact = uniqueContact("Playwright CRUD");
  const updatedName = `${contact.name} Updated`;

  await page.goto("/contacts");
  await page.getByRole("button", { name: "+ Add contact" }).click();
  await expect(page.getByRole("heading", { name: "Add Contact" })).toBeVisible();

  await page.locator("#name").fill(contact.name);
  await page.locator("#phone").fill(contact.phone_number);
  await page.locator("#email").fill(contact.email);
  await page.locator("#address").fill(contact.address);
  await page.getByLabel("New tag name").fill("PlaywrightTag");
  await page.getByRole("button", { name: "Add", exact: true }).click();
  await page.getByRole("button", { name: "Save Contact" }).click();
  await expect(page.locator(".modal-backdrop")).toBeHidden();

  await page.getByLabel("Search contacts by name, phone number, or tag").fill(contact.name);
  await expect(page.getByText(contact.name, { exact: true })).toBeVisible();

  await page.getByText(contact.name, { exact: true }).click();
  await expect(page.getByRole("heading", { name: contact.name })).toBeVisible();
  await page.getByRole("button", { name: "Edit" }).click();
  await page.locator("#edit-name").fill(updatedName);
  await page.getByRole("button", { name: "Save Changes" }).click();
  await expect(page.getByRole("heading", { name: updatedName })).toBeVisible();

  await page.getByRole("button", { name: /Favorite/ }).click();
  await page.getByRole("button", { name: "Close contact" }).click();
  await page.getByRole("link", { name: "Favorites" }).click();
  await expect(page.getByText(updatedName, { exact: true })).toBeVisible();

  await page.getByText(updatedName, { exact: true }).click();
  await page.getByRole("button", { name: "Delete Contact" }).click();
  await expect(page.getByRole("heading", { name: "Delete contact?" })).toBeVisible();
  await page.getByRole("button", { name: "Delete", exact: true }).click();
  await expect(page.getByRole("heading", { name: "Delete contact?" })).toBeHidden();

  await page.getByRole("link", { name: "All Contacts" }).click();
  await page.getByLabel("Search contacts by name, phone number, or tag").fill(updatedName);
  await expect(page.getByText(updatedName, { exact: true })).toHaveCount(0);
});

test("shows frontend validation for invalid contact submissions", async ({ page }) => {
  await page.goto("/contacts");
  await page.getByRole("button", { name: "+ Add contact" }).click();
  await page.getByRole("button", { name: "Save Contact" }).click();
  await expect(page.getByText("Name is required.")).toBeVisible();
  await expect(page.getByText("Phone number is required.")).toBeVisible();

  await page.locator("#name").fill("Invalid Input");
  await page.locator("#phone").fill("12");
  await page.locator("#email").fill("not-an-email");
  await expect(page.locator("#phone")).toHaveValue("12");
  await page.locator("#phone").press("Tab");
  await page.getByRole("button", { name: "Save Contact" }).click();
  await expect(page.locator("#phone").locator("xpath=following-sibling::small")).toContainText(/required|valid phone number/i);

  await page.getByRole("button", { name: "Cancel" }).click();
  await page.getByRole("button", { name: "+ Add contact" }).click();
  await page.locator("#name").fill("Invalid Email");
  await page.locator("#phone").fill("+919876543210");
  await page.locator("#email").fill("not-an-email");
  await page.getByRole("button", { name: "Save Contact" }).click();
  expect(await page.locator("#email").evaluate((input) => input.validity.typeMismatch)).toBeTruthy();
  await expect(page.getByRole("heading", { name: "Add Contact" })).toBeVisible();
});
