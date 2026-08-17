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
  const configured = process.env.NEXT_PUBLIC_SITE_URL;
  const value =
    configured ??
    (process.env.NODE_ENV === "development"
      ? `http://localhost:${process.env.PORT ?? 3001}`
      : "https://chillicream.com");
  const url = new URL(value);

  if (!/^https?:$/.test(url.protocol)) {
    throw new Error("NEXT_PUBLIC_SITE_URL must use http or https.");
  }
  if (
    url.username ||
    url.password ||
    url.pathname !== "/" ||
    url.search ||
    url.hash
  ) {
    throw new Error(
      "NEXT_PUBLIC_SITE_URL must be an origin without credentials, a path, a query, or a fragment.",
    );
  }

  return url.origin;
}

export const SITE_URL = resolveSiteUrl();

/**
 * Turns a path into an absolute URL against {@link SITE_URL}. Root-relative
 * paths (`/foo`) are resolved against the site origin. Absolute and
 * protocol-relative URLs are normalized to fully-qualified HTTP(S) URLs.
 */
export function toAbsoluteUrl(pathOrUrl: string): string {
  if (/^\/\//.test(pathOrUrl)) {
    return new URL(`${new URL(SITE_URL).protocol}${pathOrUrl}`).href;
  }
  return new URL(pathOrUrl, `${SITE_URL}/`).href;
}
