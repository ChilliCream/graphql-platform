/**
 * Canonical, absolute base URL of the production site (no trailing slash).
 * Override with `NEXT_PUBLIC_SITE_URL` for preview/staging deployments.
 */
export const SITE_URL = (
  process.env.NEXT_PUBLIC_SITE_URL ?? "https://chillicream.com"
).replace(/\/+$/, "");
