import fs from "node:fs";
import path from "node:path";

/**
 * Root directory that holds all docs markdown content. Shared by the docs page
 * and its Open Graph image route so they enumerate the exact same slugs.
 */
export const CONTENT_ROOT = path.join(process.cwd(), "content/docs");

/**
 * Enumerates the doc slugs (one `string[]` per route) from the markdown files
 * under `CONTENT_ROOT`. `index.md(x)` files map to their parent directory.
 *
 * `output: export` requires at least one prerendered path, so a `__empty__`
 * placeholder is returned when no content is present; the page renders a 404
 * for it via `notFound()`.
 */
export function listDocSlugs(): string[][] {
  const slugs = walk(CONTENT_ROOT)
    .filter((f) => /\.mdx?$/.test(f))
    .map((f) => path.relative(CONTENT_ROOT, f).replace(/\.mdx?$/, ""))
    .map((rel) => rel.split(path.sep))
    .map((parts) =>
      parts[parts.length - 1] === "index" ? parts.slice(0, -1) : parts,
    )
    .filter((slug) => slug.length > 0);

  return slugs.length > 0 ? slugs : [["__empty__"]];
}

/**
 * Separator used to flatten a doc slug (`["foo", "bar"]`) into the single
 * opaque `[id]` segment of the Open Graph image route. Catch-all segments
 * cannot be followed by the `opengraph-image` file convention, so the docs
 * share-card route lives under `app/docs-og/[id]` and the page metadata points
 * at it. No doc slug contains this separator.
 */
const SLUG_ID_SEPARATOR = "__";

/** Flattens a doc slug into the opaque `[id]` segment. */
export function encodeDocId(slug: string[]): string {
  return slug.join(SLUG_ID_SEPARATOR);
}

/** Expands an opaque `[id]` segment back into a doc slug. */
export function decodeDocId(id: string): string[] {
  return id.split(SLUG_ID_SEPARATOR);
}

/**
 * Resolves a doc slug to its markdown file path relative to `CONTENT_ROOT`,
 * or `null` when no matching file exists.
 */
export function resolveFile(slug: string[]): string | null {
  const joined = slug.join("/");
  const candidates = [
    `${joined}.md`,
    `${joined}.mdx`,
    `${joined}/index.md`,
    `${joined}/index.mdx`,
  ];

  for (const c of candidates) {
    if (fs.existsSync(path.join(CONTENT_ROOT, c))) {
      return c;
    }
  }
  return null;
}

function walk(dir: string): string[] {
  const entries = fs.readdirSync(dir, { withFileTypes: true });
  return entries.flatMap((e) => {
    const full = path.join(dir, e.name);
    return e.isDirectory() ? walk(full) : [full];
  });
}
