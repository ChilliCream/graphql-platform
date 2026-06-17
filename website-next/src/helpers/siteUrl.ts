/**
 * Canonical, absolute base URL of the site (no trailing slash). Used for
 * `metadataBase`, so it drives every absolute `og:url` / `og:image` / canonical
 * link.
 *
 * Resolution order:
 * 1. `NEXT_PUBLIC_SITE_URL` (preview/staging deployments).
 * 2. The local dev origin while running `next dev`, so links point at localhost
 *    instead of production (port matches the `dev` script's `-p 3001`).
 * 3. The production site.
 */
function resolveSiteUrl(): string {
  if (process.env.NEXT_PUBLIC_SITE_URL) {
    return process.env.NEXT_PUBLIC_SITE_URL;
  }

  if (process.env.NODE_ENV === "development") {
    return `http://localhost:${process.env.PORT ?? 3001}`;
  }

  return "https://chillicream.com";
}

export const SITE_URL = resolveSiteUrl().replace(/\/+$/, "");

/**
 * Turns a path into an absolute URL against {@link SITE_URL}. Root-relative
 * paths (`/foo`) are prefixed with the site origin; values that are already
 * absolute (`https://…`) or protocol-relative (`//…`) are returned unchanged.
 */
export function toAbsoluteUrl(pathOrUrl: string): string {
  if (/^(https?:)?\/\//.test(pathOrUrl)) {
    return pathOrUrl;
  }
  return `${SITE_URL}${pathOrUrl.startsWith("/") ? "" : "/"}${pathOrUrl}`;
}
