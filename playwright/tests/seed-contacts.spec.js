import { test } from "@playwright/test";

test.setTimeout(15 * 60 * 1000); // 15 minutes

test("create contacts up to 1000", async ({ page, request }) => {
  await page.goto("/contacts");

  // Check how many contacts already exist
const response = await request.get(
  "http://localhost:8080/api/contacts/?page=1&limit=1"
);

  const data = await response.json();

  const existingContacts = data.total;

  console.log(`Existing contacts: ${existingContacts}`);

  if (existingContacts >= 1000) {
    console.log("Already have 1000 contacts.");
    return;
  }

  const start = existingContacts + 1;

  console.log(`Creating contacts ${start} to 1000...`);

  for (let i = start; i <= 1000; i++) {
    // Open Add Contact
    await page.getByRole("button", { name: "+ Add contact" }).click();

    // Fill form
    await page.locator("#name").fill(`Test User ${i}`);

    await page
      .locator("#phone")
      .fill(`900000${String(i).padStart(4, "0")}`);

    await page
      .locator("#email")
      .fill(`testuser${i}@example.com`);

    // Save
    await page.getByRole("button", { name: /save/i }).click();

    // IMPORTANT:
    // Wait until the modal disappears completely
    await page.locator(".modal-backdrop").waitFor({
      state: "hidden",
      timeout: 10000,
    });

    // Progress every 25 contacts
    if (i % 25 === 0) {
      console.log(`Created through contact ${i}`);
    }
  }

  console.log("Finished. Total contacts should now be 1000.");
});