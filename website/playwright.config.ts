import { existsSync } from "node:fs";
import { defineConfig, devices } from "@playwright/test";

if (
  process.env.WEBSITE_VISUAL_DOCKER !== "playwright-1.62.1-noble-amd64" ||
  process.platform !== "linux" ||
  process.arch !== "x64" ||
  !existsSync("/.dockerenv")
) {
  throw new Error(
    "Visual tests must run in the pinned Docker renderer. Use npm run test:visual or npm run test:visual:update, not npx playwright test.",
  );
}

// Dedicated port for the static build under test. Deliberately not 6006 so it
// never reuses a running `storybook dev` server, whose on-demand story index
// would make screenshots non-deterministic.
const PORT = 6166;
const STORYBOOK_URL = `http://127.0.0.1:${PORT}`;

/**
 * Visual regression tests for Storybook stories.
 *
 * Tests and baseline updates run in the pinned Linux/amd64 Playwright Docker
 * image through scripts/visual-tests-docker.mjs, locally and in CI.
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
      // Animations are frozen by Playwright; allow 3% differing pixels.
      maxDiffPixelRatio: 0.03,
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
    command: `npx http-server storybook-static -p ${PORT} -s`,
    url: `${STORYBOOK_URL}/index.json`,
    reuseExistingServer: !process.env.CI,
    timeout: 60_000,
  },
});
