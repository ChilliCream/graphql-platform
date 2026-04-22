/**
 * Utility functions for working with URL parameters
 */

const SAFE_URL_PROTOCOLS = new Set(["http:", "https:", "mailto:", "tel:"]);

const SAFE_IMG_PROTOCOLS = new Set(["http:", "https:"]);

/**
 * Sanitizes a URL to prevent XSS via dangerous protocols such as
 * `javascript:` or `data:`. Returns the original URL when the protocol
 * is safe, or "about:blank" otherwise.  Relative and hash-only URLs are
 * always allowed.
 */
export function sanitizeUrl(url: string): string {
  const trimmed = url.trim();

  // Relative paths and hash links are safe.
  if (
    trimmed === "" ||
    trimmed.startsWith("/") ||
    trimmed.startsWith("#") ||
    trimmed.startsWith("?")
  ) {
    return trimmed;
  }

  try {
    const parsed = new URL(trimmed, "http://placeholder.invalid");

    if (SAFE_URL_PROTOCOLS.has(parsed.protocol)) {
      return trimmed;
    }
  } catch {
    // Malformed URL — reject.
  }

  return "about:blank";
}

/**
 * Sanitizes an image `src` value, allowing only http(s) and relative
 * paths.  Returns an empty string for anything else.
 */
export function sanitizeImageSrc(src: string): string {
  const trimmed = src.trim();

  if (trimmed === "" || trimmed.startsWith("/")) {
    return trimmed;
  }

  try {
    const parsed = new URL(trimmed, "http://placeholder.invalid");

    if (SAFE_IMG_PROTOCOLS.has(parsed.protocol)) {
      return trimmed;
    }
  } catch {
    // Malformed URL — reject.
  }

  return "";
}

/**
 * Gets a query parameter value from the current URL
 * @param paramName - The name of the parameter to retrieve
 * @returns The parameter value or null if not found
 */
export function getQueryParam(paramName: string): string | null {
  if (typeof window === "undefined") {
    return null;
  }

  const urlParams = new URLSearchParams(window.location.search);
  return urlParams.get(paramName);
}

/**
 * Gets a query parameter value and validates it against allowed values
 * @param paramName - The name of the parameter to retrieve
 * @param allowedValues - Array of allowed values
 * @returns The parameter value if valid, or null if invalid/not found
 */
export function getValidatedQueryParam<T extends string>(
  paramName: string,
  allowedValues: readonly T[]
): T | null {
  const value = getQueryParam(paramName);

  if (value && allowedValues.includes(value as T)) {
    return value as T;
  }

  return null;
}

/**
 * Builds a URL with query parameters
 * @param baseUrl - The base URL
 * @param params - Object containing parameter key-value pairs
 * @returns The complete URL with query parameters
 */
export function buildUrlWithParams(
  baseUrl: string,
  params: Record<string, string | number>
): string {
  const url = new URL(
    baseUrl,
    typeof window !== "undefined" ? window.location.origin : ""
  );

  Object.entries(params).forEach(([key, value]) => {
    url.searchParams.set(key, String(value));
  });

  return url.pathname + url.search;
}
