import { readFileSync } from "node:fs";
import { join } from "node:path";
import { expect, test } from "@playwright/test";

/**
 * One screenshot per Storybook story, discovered from the static build's
 * `index.json`. Adding a story automatically adds a snapshot; no per-story
 * wiring needed.
 */
interface StoryEntry {
  readonly type: "story" | "docs";
  readonly id: string;
  readonly title: string;
  readonly name: string;
  readonly tags?: readonly string[];
}

interface StoryIndex {
  readonly entries: Record<string, StoryEntry>;
}

const indexPath = join(process.cwd(), "storybook-static", "index.json");

let index: StoryIndex;
try {
  index = JSON.parse(readFileSync(indexPath, "utf-8")) as StoryIndex;
} catch {
  throw new Error(
    `Could not read ${indexPath}. The Storybook static build must run before the visual tests ` +
      `(Playwright's webServer handles this automatically via "yarn test:visual").`,
  );
}

// Stories tagged "no-snapshot" opt out of pixel diffing (e.g. full-page compositions whose looping
// product visuals can't be frozen to a stable frame). They still render in Storybook for review.
const stories = Object.values(index.entries).filter(
  (entry) => entry.type === "story" && !entry.tags?.includes("no-snapshot"),
);

for (const story of stories) {
  test(story.id, async ({ page }) => {
    await page.goto(
      `/iframe.html?id=${encodeURIComponent(story.id)}&viewMode=story`,
    );

    // Storybook signals a fully rendered story with this class on the body.
    await page.locator("body.sb-show-main").waitFor();
    // Wait for webfonts so text metrics are stable before the screenshot.
    await page.evaluate(() => document.fonts.ready);

    await expect(page).toHaveScreenshot(`${story.id}.png`, {
      fullPage: true,
    });
  });
}
