const DEFAULT_OPTIONS: Intl.DateTimeFormatOptions = {
  year: "numeric",
  month: "short",
  day: "numeric",
};

/**
 * Formats a date (ISO string or `Date`) as a human-readable en-US string.
 * Pass `options` to override the default `Mon D, YYYY` shape. An unparseable
 * string is returned unchanged so callers never render `Invalid Date`.
 */
export function formatDate(
  date: string | Date,
  options: Intl.DateTimeFormatOptions = DEFAULT_OPTIONS,
): string {
  const d = typeof date === "string" ? new Date(date) : date;
  if (Number.isNaN(d.getTime())) {
    return typeof date === "string" ? date : "";
  }
  return d.toLocaleDateString("en-US", options);
}
