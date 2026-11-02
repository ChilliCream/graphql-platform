import type { Metadata } from "next";
import { ImageResponse } from "next/og";
import { loadShareCardFonts } from "@/src/og/fonts";
import { enableSatoriHooks } from "@/src/og/satoriHooks";
import { ShareCard } from "@/src/og/ShareCard";

/** Shared 1200x630 frame for every marketing share card. */
export const shareCardSize = {
  width: 1200,
  height: 630,
};

export const shareCardContentType = "image/png";

/**
 * The subset of a route's `page` module the share card reads its title from:
 * either a static `metadata` export or a `generateMetadata` function.
 */
type PageModule = {
  readonly default?: unknown;
  readonly metadata?: Metadata;
  readonly generateMetadata?: () => Metadata | Promise<Metadata>;
};

/** Pulls the human-readable title out of a resolved `Metadata.title`. */
function titleToString(title: Metadata["title"]): string | undefined {
  if (typeof title === "string") {
    return title;
  }

  if (title && typeof title === "object") {
    if ("absolute" in title && title.absolute) {
      return title.absolute;
    }

    if ("default" in title && title.default) {
      return title.default;
    }
  }

  return undefined;
}

/**
 * Resolves a page's window title from its `page` module at build time, or
 * `undefined` when the page declares none (the card then shows the bare hero).
 */
async function resolvePageTitle(page: PageModule): Promise<string | undefined> {
  const metadata = page.metadata ?? (await page.generateMetadata?.());

  return titleToString(metadata?.title);
}

/**
 * Builds the `opengraph-image` default export for a marketing page: the landing
 * hero artwork with the page's own title rendered under the headline. The title
 * is read from the sibling `page` module, so each route's `opengraph-image.tsx`
 * is identical boilerplate that just forwards `import * as page from "./page"`.
 */
export function createPageShareCardImage(page: PageModule) {
  return async function Image() {
    enableSatoriHooks();

    const [pageTitle, fonts] = await Promise.all([
      resolvePageTitle(page),
      loadShareCardFonts(),
    ]);

    return new ImageResponse(<ShareCard pageTitle={pageTitle} />, {
      ...shareCardSize,
      fonts,
    });
  };
}
