/**
 * Utility functions for working with URL parameters
 */

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
