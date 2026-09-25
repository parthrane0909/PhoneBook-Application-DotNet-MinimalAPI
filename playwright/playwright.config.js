import { defineConfig } from "@playwright/test";

export default defineConfig({
  testDir: "./tests",

  timeout: 120000,

  expect: {
    timeout: 10000,
  },

  reporter: [["list"], ["html", { open: "never" }]],

  retries: process.env.CI ? 2 : 0,

  forbidOnly: !!process.env.CI,

  workers: 1,

  use: {
    baseURL: "http://127.0.0.1:8080",
    headless: true,
    trace: "retain-on-failure",
    screenshot: "only-on-failure",
  },

  projects: [
    {
      name: "chromium",
      use: {
        browserName: "chromium",
      },
    },
  ],
});