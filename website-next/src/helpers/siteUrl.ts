/**
 * Canonical, absolute base URL of the production site (no trailing slash).
 * Override with `NEXT_PUBLIC_SITE_URL` for preview/staging deployments.
 */
export const SITE_URL = (
  process.env.NEXT_PUBLIC_SITE_URL ?? "https://chillicream.com"
).replace(/\/+$/, "");

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
