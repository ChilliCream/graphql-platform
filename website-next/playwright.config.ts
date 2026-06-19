import { defineConfig, devices } from "@playwright/test";

// Dedicated port for the static build under test. Deliberately not 6006 so it
// never reuses a running `storybook dev` server, whose on-demand story index
// would make screenshots non-deterministic.
const PORT = 6166;
const STORYBOOK_URL = `http://127.0.0.1:${PORT}`;

/**
 * Visual regression tests for Storybook stories.
 *
 * Baselines are rendered in a Linux + Chromium environment (the devcontainer,
 * which mirrors CI). Generate and update them there, never from a host OS,
 * otherwise font rendering will differ and every screenshot will diff.
 */
export default defineConfig({
  testDir: "./tests/visual",
  snapshotPathTemplate: "tests/visual/__screenshots__/{arg}{ext}",
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  reporter: process.env.CI ? [["html", { open: "never" }], ["list"]] : "list",
  expect: {
    toHaveScreenshot: {
      // Animations are frozen by Playwright; this absorbs minor anti-aliasing noise.
      maxDiffPixelRatio: 0.01,
    },
  },
  use: {
    baseURL: STORYBOOK_URL,
  },
  projects: [
    {
      name: "chromium",
      use: { ...devices["Desktop Chrome"] },
    },
  ],
  webServer: {
    // Serves the static Storybook. The build itself runs in the "test:visual"
    // script beforehand, because Playwright enumerates the per-story tests by
    // reading storybook-static/index.json at collection time (before webServer
    // would otherwise run). During iteration you can pre-run a server on this
    // port and it will be reused.
    command: `yarn http-server storybook-static -p ${PORT} -s`,
    url: `${STORYBOOK_URL}/index.json`,
    reuseExistingServer: !process.env.CI,
    timeout: 60_000,
  },
});
