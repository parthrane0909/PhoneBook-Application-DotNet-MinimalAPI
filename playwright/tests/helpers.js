import { expect } from "@playwright/test";

export const apiBaseURL = process.env.PHONEBOOK_API_URL || "http://localhost:8080/api";

export function uniqueContact(prefix = "Playwright User") {
  const token = `${Date.now()}-${Math.floor(Math.random() * 100000)}`;
  return {
    name: `${prefix} ${token}`,
    phone_number: `+919${token.replace(/\D/g, "").slice(-9).padStart(9, "0")}`,
    email: `playwright.${token.replace(/\D/g, "")}@example.com`,
    address: "Playwright Test Address",
    tags: [],
  };
}

export async function createApiContact(request, overrides = {}) {
  const contact = { ...uniqueContact(), ...overrides };
  const response = await request.post(`${apiBaseURL}/contacts/`, { data: contact });
  expect(response.ok()).toBeTruthy();
  return response.json();
}

export async function deleteApiContact(request, id) {
  if (!id) return;
  const response = await request.delete(`${apiBaseURL}/contacts/${id}`);
  expect([200, 404]).toContain(response.status());
}

export async function getFirstTag(request) {
  const response = await request.get(`${apiBaseURL}/contacts/tags`);
  expect(response.ok()).toBeTruthy();
  const tags = await response.json();
  return tags[0]?.name || "Work";
}

export async function contactsTotal(request) {
  const response = await request.get(`${apiBaseURL}/contacts/?page=1&limit=1`);
  expect(response.ok()).toBeTruthy();
  return (await response.json()).total;
}

export async function ensureMinimumContacts(request, minimum) {
  const existing = await contactsTotal(request);
  const created = [];

  for (let i = existing; i < minimum; i += 1) {
    created.push(
      await createApiContact(request, {
        name: `Playwright Dataset ${String(i + 1).padStart(4, "0")}`,
        phone_number: `+918${String(Date.now()).slice(-7)}${String(i).padStart(4, "0")}`.slice(0, 16),
        email: `playwright.dataset.${i}.${Date.now()}@example.com`,
      }),
    );
  }

  return created;
}

export async function waitForContactsLoaded(page) {
  await expect(page.locator(".table-frame")).toBeVisible();
  await expect(page.locator(".spinner")).toHaveCount(0);
}
