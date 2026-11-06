export const TITLE_LENGTH = { min: 15, max: 70 };
export const DESCRIPTION_LENGTH = { min: 50, max: 170 };

export function lengthWarnings(pathname, title, description) {
  const warnings = [];
  const checks = [
    ["seo-title-length", "title", title, TITLE_LENGTH],
    ["seo-description-length", "description", description, DESCRIPTION_LENGTH],
  ];
  for (const [rule, label, value, { min, max }] of checks) {
    if (value && (value.length < min || value.length > max)) {
      warnings.push([
        rule,
        `${pathname}: ${label} is ${value.length} characters, expected ${min}-${max}`,
      ]);
    }
  }
  return warnings;
}

export function openGraphErrors(pathname, document) {
  const errors = [];
  const content = (selector) =>
    document.querySelector(selector)?.getAttribute("content")?.trim() ?? "";

  if (!content('meta[property="og:title"]')) {
    errors.push(["og-title-missing", `${pathname}: missing og:title`]);
  }
  if (!content('meta[property="og:description"]')) {
    errors.push([
      "og-description-missing",
      `${pathname}: missing og:description`,
    ]);
  }
  const image = content('meta[property="og:image"]');
  if (!image) {
    errors.push(["og-image-missing", `${pathname}: missing og:image`]);
  } else if (!isAbsoluteHttpUrl(image)) {
    errors.push([
      "og-image-not-absolute",
      `${pathname}: og:image is not an absolute URL (${image})`,
    ]);
  }
  if (!content('meta[name="twitter:card"]')) {
    errors.push(["twitter-card-missing", `${pathname}: missing twitter:card`]);
  }
  return errors;
}

export function headingSkips(document) {
  const skips = [];
  let previous = 0;
  for (const heading of document.querySelectorAll("h1, h2, h3, h4, h5, h6")) {
    const level = Number(heading.tagName.slice(1));
    if (previous > 0 && level > previous + 1) {
      skips.push(`h${previous} followed by h${level}`);
    }
    previous = level;
  }
  return skips;
}

export function imagesWithoutAlt(document) {
  return document
    .querySelectorAll("img")
    .filter((image) => !image.hasAttribute("alt")).length;
}

export function isAbsoluteHttpUrl(value) {
  if (typeof value !== "string") {
    return false;
  }
  try {
    return /^https?:$/.test(new URL(value).protocol);
  } catch {
    return false;
  }
}

export function noindexPageErrors(pathname, document, expectedCanonical) {
  const errors = [];
  const robots =
    document.querySelector('meta[name="robots"]')?.getAttribute("content") ??
    "";
  if (!/\bnoindex\b/i.test(robots)) {
    errors.push([
      "noindex-page",
      `${pathname}: noindex page is not marked noindex`,
    ]);
  }
  if (!/\bfollow\b/i.test(robots) || /\bnofollow\b/i.test(robots)) {
    errors.push([
      "noindex-page",
      `${pathname}: noindex page must allow link following`,
    ]);
  }
  const canonicals = document
    .querySelectorAll('link[rel="canonical"]')
    .map((link) => link.getAttribute("href") ?? "");
  if (canonicals.length !== 1 || canonicals[0] !== expectedCanonical) {
    errors.push([
      "noindex-page",
      `${pathname}: expected a single canonical ${expectedCanonical}, got ${canonicals.join(", ") || "none"}`,
    ]);
  }
  return errors;
}
