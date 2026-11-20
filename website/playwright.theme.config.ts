import { existsSync } from "node:fs";
import { defineConfig, devices } from "@playwright/test";

if (!existsSync("out/index.html")) {
  throw new Error(
    "The appearance tests run against the static export. Run npm run build first.",
  );
}

const PORT = Number(process.env.THEME_TEST_PORT ?? 3206);
const BASE_URL = `http://127.0.0.1:${PORT}`;

/**
 * Behavioral browser tests for the Auto, Light and Dark appearance. They run
 * against the static export in `out` on a port of their own, which is never a
 * reused server. The Docker visual suite keeps its own configuration.
 */
export default defineConfig({
  testDir: "./tests/theme",
  testMatch: "appearance.spec.ts",
  outputDir: "test-results/theme",
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  reporter: process.env.CI ? [["html", { open: "never" }], ["list"]] : "list",
  use: {
    baseURL: BASE_URL,
  },
  projects: [
    {
      name: "chromium",
      use: { ...devices["Desktop Chrome"] },
    },
  ],
  webServer: {
    command: `node tests/theme/serve-export.mjs out ${PORT}`,
    url: BASE_URL,
    reuseExistingServer: false,
    timeout: 60_000,
  },
});
