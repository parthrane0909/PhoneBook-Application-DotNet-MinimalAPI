import { test, expect } from "@playwright/test";
import fs from "node:fs/promises";
import path from "node:path";
import { apiBaseURL, deleteApiContact, waitForContactsLoaded } from "./helpers.js";

async function writeCsv(testInfo, rows) {
  const filePath = testInfo.outputPath("playwright-contacts.csv");
  await fs.writeFile(filePath, rows.map((row) => row.join(",")).join("\n"));
  return filePath;
}

async function deleteContactsByName(request, name) {
  const response = await request.get(`${apiBaseURL}/contacts/?search=${encodeURIComponent(name)}&page=1&limit=100`);
  if (!response.ok()) return;
  const data = await response.json();
  for (const contact of data.contacts.filter((item) => item.name.startsWith(name))) {
    await deleteApiContact(request, contact.id);
  }
}

test("imports CSV contacts with preview, tags, and drag/drop-compatible upload", async ({ page, request }, testInfo) => {
  const prefix = `Playwright Import ${Date.now()}`;
  const csvPath = await writeCsv(testInfo, [
    ["name", "phone_number", "email", "address", "tags"],
    [`${prefix} 01`, "+919000000001", "import01@example.com", "Mumbai", '"Work,Friends"'],
    [`${prefix} 02`, "+919000000002", "import02@example.com", "Pune", '"Family"'],
    [`${prefix} 03`, "+919000000003", "import03@example.com", "Delhi", '"Work"'],
  ]);

  try {
    await page.goto("/contacts");
    await waitForContactsLoaded(page);
    await page.getByRole("button", { name: "Import" }).click();
    await expect(page.getByRole("heading", { name: "Import contacts" })).toBeVisible();

    await page.locator('input[type="file"]').setInputFiles(csvPath);
    await expect(page.getByText("3 ready to import")).toBeVisible();
    await expect(page.locator(".import-preview")).toContainText("Work, Friends");
    await expect(page.getByRole("button", { name: "Import 3" })).toBeEnabled();
    await page.getByRole("button", { name: "Import 3" }).click();
    await expect(page.getByRole("heading", { name: "Import contacts" })).toBeHidden();

    const search = page.getByLabel("Search contacts by name, phone number, or tag");
    await search.fill(`${prefix} 01`);
    await expect(page.getByText(`${prefix} 01`, { exact: true })).toBeVisible();
    await expect(page.getByText(`${prefix} 02`, { exact: true })).toHaveCount(0);
    await expect(
      page.locator(".contact-table-row").filter({ hasText: `${prefix} 01` }).locator(".tag-pill").filter({ hasText: "Work" }),
    ).toBeVisible();

    await search.fill("");
    await page.getByRole("button", { name: "Import" }).click();
    await page.locator('input[type="file"]').setInputFiles(csvPath);
    await expect(page.getByText("3 ready to import")).toBeVisible();
  } finally {
    await deleteContactsByName(request, prefix);
  }
});

test("reports invalid import rows and ignores unrelated CSV columns", async ({ page, request }, testInfo) => {
  const prefix = `Playwright Invalid Import ${Date.now()}`;
  const csvPath = await writeCsv(testInfo, [
    ["name", "phone_number", "email", "address", "tags", "internal_id"],
    [`${prefix} Valid`, "+919000000011", "valid-import@example.com", "Mumbai", "Work", "should-ignore"],
    ["", "+919000000012", "missing-name@example.com", "Pune", "", "1"],
    [`${prefix} Bad Phone`, "12", "bad-phone@example.com", "Delhi", "", "2"],
    [`${prefix} Bad Email`, "+919000000013", "not-an-email", "Delhi", "", "3"],
    [`${prefix} Duplicate`, "+919000000011", "duplicate@example.com", "Mumbai", "", "4"],
  ]);

  try {
    await page.goto("/contacts");
    await page.getByRole("button", { name: "Import" }).click();
    await page.locator('input[type="file"]').setInputFiles(csvPath);
    await expect(page.getByText("1 ready to import")).toBeVisible();
    await expect(page.locator(".import-errors")).toContainText(/Name is required|Invalid phone|Invalid email|Duplicate/);
    await page.getByRole("button", { name: "Import 1" }).click();
    await expect(page.getByRole("heading", { name: "Import contacts" })).toBeHidden();
  } finally {
    await deleteContactsByName(request, prefix);
  }
});

test("accepts a CSV through the visible import dropzone", async ({ page }) => {
  await page.goto("/contacts");
  await page.getByRole("button", { name: "Import" }).click();

  await page.locator(".file-dropzone").evaluate((dropzone) => {
    const file = new File(
      ["name,phone_number,email,address,tags\nDrop User,+919000000099,drop@example.com,Mumbai,Work"],
      "playwright-drop.csv",
      { type: "text/csv" },
    );
    const dataTransfer = new DataTransfer();
    dataTransfer.items.add(file);
    dropzone.dispatchEvent(new DragEvent("drop", { bubbles: true, dataTransfer }));
  });

  await expect(page.getByText("1 ready to import")).toBeVisible();
  await expect(page.locator(".import-preview")).toContainText("Drop User");
});

test("exports the active contacts as CSV with tags", async ({ page }) => {
  await page.goto("/contacts");
  await waitForContactsLoaded(page);
  const downloadPromise = page.waitForEvent("download");
  await page.getByRole("button", { name: "Export" }).click();
  const download = await downloadPromise;
  expect(download.suggestedFilename()).toBe("phonebook-contacts.csv");
  const filePath = await download.path();
  const contents = await fs.readFile(filePath, "utf8");
  expect(contents).toContain("name,phone_number,email,address,tags");
});
