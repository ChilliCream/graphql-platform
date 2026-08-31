import { readdirSync, readFileSync } from "node:fs";
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

interface StorybookPreview {
  readonly currentRender?: {
    readonly id: string;
    readonly phase: string;
  };
}

declare global {
  interface Window {
    readonly __STORYBOOK_PREVIEW__?: StorybookPreview;
  }
}

const indexPath = join(process.cwd(), "storybook-static", "index.json");
const snapshotDir = join(process.cwd(), "tests", "visual", "__screenshots__");

let index: StoryIndex;
try {
  index = JSON.parse(readFileSync(indexPath, "utf-8")) as StoryIndex;
} catch {
  throw new Error(
    `Could not read ${indexPath}. The Storybook static build must run before the visual tests ` +
      `(Playwright's webServer handles this automatically via "yarn test:visual").`,
  );
}

const stories = Object.values(index.entries).filter(
  (entry) => entry.type === "story" && !entry.tags?.includes("no-snapshot"),
);

// A renamed or deleted story leaves its baseline behind, where it is never
// compared again and never updated. Nothing else in the suite would notice.
test("every baseline belongs to a story", () => {
  const expected = new Set(stories.map((story) => `${story.id}.png`));
  const orphans = readdirSync(snapshotDir)
    .filter((file) => file.endsWith(".png"))
    .filter((file) => !expected.has(file))
    .sort();

  expect(
    orphans,
    `${orphans.length} baseline(s) have no matching story. Delete them, or restore the ` +
      `story they belonged to:\n  ${orphans.join("\n  ")}`,
  ).toEqual([]);
});

for (const story of stories) {
  test(story.id, async ({ page }) => {
    await page.goto(
      `/iframe.html?id=${encodeURIComponent(story.id)}&viewMode=story`,
    );

    // Storybook signals a fully rendered story with this class on the body.
    await page.locator("body.sb-show-main").waitFor();
    // The body shell appears before async play functions have completed. Wait
    // for Storybook's final render phase so interaction snapshots cannot race
    // an empty or pre-interaction canvas.
    await page.waitForFunction((storyId) => {
      const render = window.__STORYBOOK_PREVIEW__?.currentRender;
      return render?.id === storyId && render.phase === "finished";
    }, story.id);
    // Wait for webfonts so text metrics are stable before the screenshot.
    await page.evaluate(() => document.fonts.ready);

    // Wait for every image to finish loading (or fail) so the screenshot
    // never races a slow request, e.g. the YouTube poster from i.ytimg.com.
    await page.evaluate(() =>
      Promise.all(
        Array.from(document.images)
          .filter((img) => !img.complete)
          .map(
            (img) =>
              new Promise((resolve) => {
                img.addEventListener("load", resolve, { once: true });
                img.addEventListener("error", resolve, { once: true });
              }),
          ),
      ),
    );

    await expect(page).toHaveScreenshot(`${story.id}.png`, {
      fullPage: true,
    });
  });
}
