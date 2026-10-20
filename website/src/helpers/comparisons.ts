import fs from "node:fs";
import path from "node:path";
import { PRODUCTS, type Product } from "@/src/data/products";
import { readFrontmatter } from "./readFrontmatter";

export const COMPARISONS_ROOT = path.join(process.cwd(), "content/compare");
export const COMPARISONS_PATH = "/compare";

const SLUG_RE = /^[a-z0-9]+(?:-[a-z0-9]+)*$/;
const DATE_RE = /^\d{4}-(?:0[1-9]|1[0-2])-(?:0[1-9]|[12]\d|3[01])$/;
const DOCS_ROOT = path.join(process.cwd(), "content/docs");

/** A table cell: `true` renders a check, `false` a dash, a string its text. */
export type ComparisonCell = boolean | string;

export type ComparisonFeatureGroup = {
  readonly title: string;
  readonly rows: readonly {
    readonly label: string;
    readonly product: ComparisonCell;
    readonly competitor: ComparisonCell;
  }[];
};

export type ComparisonSummary = {
  readonly slug: string;
  readonly href: string;
  /** Absolute path of the Markdown source. */
  readonly file: string;
  readonly title: string;
  /** `<title>` override; falls back to `title`. */
  readonly metaTitle: string;
  readonly description: string;
  /** Introduction under the page heading; falls back to `description`. */
  readonly lead: string;
  /** The ChilliCream product compared, from {@link PRODUCTS}. */
  readonly product: Product;
  readonly competitor: string;
  /** Date (`YYYY-MM-DD`) the competitor facts were last checked against their sources. */
  readonly lastChecked: string;
  /** Docs path of the guide for moving from the competitor, or null. */
  readonly migrationGuide: string | null;
  readonly features: readonly ComparisonFeatureGroup[];
  readonly sources: readonly { readonly title: string; readonly url: string }[];
  /** What the product does not have yet, shown above the ways to get it. */
  readonly gaps: readonly string[];
  /** Questions and answers shown in the FAQ, empty when the page has none. */
  readonly faq: readonly {
    readonly question: string;
    readonly answer: string;
  }[];
};

/**
 * Lists every comparison in `content/compare`, grouped by product in
 * {@link PRODUCTS} order, then by competitor. A comparison is a
 * `slug.md` file. Throws on the first invalid file so the build fails.
 */
export function listComparisons(): ComparisonSummary[] {
  if (!fs.existsSync(COMPARISONS_ROOT)) {
    return [];
  }

  return fs
    .readdirSync(COMPARISONS_ROOT, { withFileTypes: true })
    .filter((entry) => entry.isFile() && entry.name.endsWith(".md"))
    .map((entry) => readComparison(entry.name))
    .sort(
      (a, b) =>
        PRODUCTS.indexOf(a.product) - PRODUCTS.indexOf(b.product) ||
        a.competitor.localeCompare(b.competitor),
    );
}

export function comparisonUrl(slug: string): string {
  return `${COMPARISONS_PATH}/${slug}`;
}

/** A `YYYY-MM-DD` date as it reads on the page, e.g. "October 20, 2026". */
export function formatCheckedDate(date: string): string {
  return new Date(`${date}T00:00:00Z`).toLocaleDateString("en-US", {
    year: "numeric",
    month: "long",
    day: "numeric",
    timeZone: "UTC",
  });
}

function readComparison(fileName: string): ComparisonSummary {
  const slug = fileName.slice(0, -".md".length);
  if (!SLUG_RE.test(slug)) {
    return invalid(
      fileName,
      "Expected a lowercase, hyphenated file name, e.g. mocha-vs-mediatr.md.",
    );
  }

  const file = path.join(COMPARISONS_ROOT, fileName);
  const frontmatter = readFrontmatter(file);

  const productSlug = requiredString(frontmatter, "product", fileName);
  const product =
    PRODUCTS.find((candidate) => candidate.slug === productSlug) ??
    invalid(
      fileName,
      `"product" must be one of: ${PRODUCTS.map((candidate) => candidate.slug).join(", ")}.`,
    );

  const lastChecked = requiredString(frontmatter, "lastChecked", fileName);
  if (!DATE_RE.test(lastChecked)) {
    invalid(fileName, `"lastChecked" must be a quoted date (YYYY-MM-DD).`);
  }

  const title = requiredString(frontmatter, "title", fileName);
  const description = requiredString(frontmatter, "description", fileName);
  return {
    slug,
    href: comparisonUrl(slug),
    file,
    title,
    metaTitle: optionalString(frontmatter, "metaTitle", fileName) ?? title,
    description,
    lead: optionalString(frontmatter, "lead", fileName) ?? description,
    product,
    competitor: requiredString(frontmatter, "competitor", fileName),
    lastChecked,
    migrationGuide: readMigrationGuide(frontmatter, fileName),
    features: readList(frontmatter, "features", fileName, (item, at) => ({
      title: requiredString(item, "title", fileName, at),
      rows: readList(item, "rows", fileName, (row, rowAt) => ({
        label: requiredString(row, "label", fileName, rowAt),
        product: readCell(row, "product", fileName, rowAt),
        competitor: readCell(row, "competitor", fileName, rowAt),
      })),
    })),
    sources: readList(frontmatter, "sources", fileName, (item, at) => {
      const url = requiredString(item, "url", fileName, at);
      if (!url.startsWith("https://")) {
        invalid(fileName, `${at}"url" must be an https:// link.`);
      }
      return { title: requiredString(item, "title", fileName, at), url };
    }),
    gaps: readGaps(frontmatter, fileName),
    faq:
      frontmatter.faq === undefined
        ? []
        : readList(frontmatter, "faq", fileName, (item, at) => ({
            question: requiredString(item, "question", fileName, at),
            answer: requiredString(item, "answer", fileName, at),
          })),
  };
}

function readMigrationGuide(
  frontmatter: Record<string, unknown>,
  fileName: string,
): string | null {
  const guide = optionalString(frontmatter, "migrationGuide", fileName);
  if (guide === null) {
    return null;
  }
  const docPath = guide.startsWith("/docs/")
    ? path.join(DOCS_ROOT, `${guide.slice("/docs/".length)}.md`)
    : null;
  if (docPath === null || !fs.existsSync(docPath)) {
    return invalid(
      fileName,
      `"migrationGuide" must be the path of a docs page, e.g. "/docs/mocha/migration/coming-from-mediatr".`,
    );
  }
  return guide;
}

function readGaps(
  frontmatter: Record<string, unknown>,
  fileName: string,
): string[] {
  const value = frontmatter.gaps;
  if (value === undefined || value === null) {
    return [];
  }
  if (
    !Array.isArray(value) ||
    !value.every((gap) => typeof gap === "string" && gap.trim().length > 0)
  ) {
    return invalid(fileName, `"gaps" must be a list of non-empty strings.`);
  }
  return value.map((gap: string) => gap.trim());
}

function readList<T>(
  source: Record<string, unknown>,
  key: string,
  fileName: string,
  read: (item: Record<string, unknown>, at: string) => T,
): T[] {
  const value = source[key];
  if (!Array.isArray(value) || value.length === 0) {
    return invalid(fileName, `"${key}" must be a non-empty list.`);
  }
  return value.map((item, index) => {
    const at = `${key}[${index}].`;
    if (!isRecord(item)) {
      return invalid(fileName, `"${key}[${index}]" must be an object.`);
    }
    return read(item, at);
  });
}

function readCell(
  source: Record<string, unknown>,
  key: string,
  fileName: string,
  at: string,
): ComparisonCell {
  const value = source[key];
  if (typeof value === "boolean") {
    return value;
  }
  return requiredString(source, key, fileName, at);
}

function requiredString(
  source: Record<string, unknown>,
  key: string,
  fileName: string,
  at = "",
): string {
  return (
    optionalString(source, key, fileName, at) ??
    invalid(fileName, `"${at}${key}" is required.`)
  );
}

function optionalString(
  source: Record<string, unknown>,
  key: string,
  fileName: string,
  at = "",
): string | null {
  const value = source[key];
  if (value === undefined || value === null) {
    return null;
  }
  if (typeof value !== "string" || value.trim().length === 0) {
    return invalid(
      fileName,
      `"${at}${key}" must be a non-empty, quoted string.`,
    );
  }
  return value.trim();
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

function invalid(fileName: string, message: string): never {
  throw new Error(
    `[compare] Invalid comparison "content/compare/${fileName}": ${message}`,
  );
}
