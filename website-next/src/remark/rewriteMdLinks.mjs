import fs from "node:fs";
import path from "node:path";

const CONTENT_ROOTS = ["content/docs", "content/blogs"];
const RULE_ID = "remark-rewrite-md-links";
const PAGE_FILE_RE = /^page\.(tsx?|jsx?|mdx?)$/;
const BLOG_STEM_RE = /^(\d{4})-(\d{2})-(\d{2})-(.+)$/;

export default function remarkRewriteMdLinks() {
  return (tree, file) => {
    const sourcePath = file?.path;
    if (!sourcePath) {
      return;
    }

    const sourceDir = path.dirname(sourcePath);
    const cwd = file.cwd ?? process.cwd();
    const appDir = path.join(cwd, "app");

    walk(tree, (node) => {
      if (node.type !== "link" || typeof node.url !== "string") {
        return;
      }

      // Skip absolute URLs, protocol-relative, hash-only.
      if (/^([a-z]+:|\/\/|#)/i.test(node.url)) {
        return;
      }

      // Root-absolute: check content roots for /docs and /blogs, otherwise
      // verify a matching literal/dynamic route exists in app/.
      if (node.url.startsWith("/")) {
        const pathPart = node.url.split(/[#?]/, 1)[0];
        const segments = pathPart.split("/").filter(Boolean);

        if (segments[0] === "docs") {
          const subSegments = segments.slice(1);
          if (subSegments.length === 0) {
            file.fail(
              `Broken root-absolute link "${node.url}" — /docs has no index page`,
              node,
              RULE_ID
            );
            return;
          }
          if (!docsFileExists(cwd, subSegments)) {
            file.fail(
              `Broken root-absolute link "${node.url}" — no matching file found under docs/`,
              node,
              RULE_ID
            );
          }
          return;
        }

        if (segments[0] === "blogs") {
          if (!blogsRouteExists(cwd, segments.slice(1))) {
            file.fail(
              `Broken root-absolute link "${node.url}" — no matching post found under blogs/ ` +
                `(expected /blogs/YYYY/MM/DD/slug)`,
              node,
              RULE_ID
            );
          }
          return;
        }

        if (!appRouteExists(appDir, segments)) {
          file.fail(
            `Broken root-absolute link "${node.url}" — no matching page found in app/`,
            node,
            RULE_ID
          );
        }
        return;
      }

      const match = /^([^#?]+\.mdx?)(#[^?]*)?(\?.*)?$/i.exec(node.url);
      if (!match) {
        return;
      }

      const [, filePart, hashPart = ""] = match;
      const absResolved = path.resolve(sourceDir, filePart);

      if (!fs.existsSync(absResolved)) {
        file.fail(
          `Broken markdown link "${node.url}" — file not found at ${path.relative(cwd, absResolved)}`,
          node,
          RULE_ID
        );
      }

      const rel = path.relative(cwd, absResolved).split(path.sep).join("/");
      const root = CONTENT_ROOTS.find(
        (r) => rel === r || rel.startsWith(`${r}/`)
      );
      if (!root) {
        file.fail(
          `Markdown link "${node.url}" resolves outside the content roots (${CONTENT_ROOTS.join(", ")}): ${rel}`,
          node,
          RULE_ID
        );
      }

      const cleanRel = rel.replace(/\.mdx?$/i, "").replace(/\/index$/, "");
      // Strip the on-disk "content/" prefix to produce the public URL.
      const urlRel = cleanRel.replace(/^content\//, "");

      if (root === "content/blogs") {
        const blogUrl = blogUrlFromCleanRel(urlRel);
        if (blogUrl === null) {
          file.fail(
            `Markdown link "${node.url}" resolves to a blog file with an invalid name "${urlRel}". ` +
              `Expected content/blogs/YYYY-MM-DD-slug.md or content/blogs/YYYY-MM-DD-slug/YYYY-MM-DD-slug.md`,
            node,
            RULE_ID
          );
          return;
        }
        node.url = `${blogUrl}${hashPart}`;
        return;
      }

      node.url = `/${urlRel}${hashPart}`;
    });
  };
}

/** Convert a path relative to cwd (without extension) under blogs/ into the
 *  canonical /blogs/YYYY/MM/DD/slug URL, or null if it doesn't match. */
function blogUrlFromCleanRel(cleanRel) {
  // cleanRel looks like "blogs/2019-06-05-foo" or "blogs/2019-06-05-foo/2019-06-05-foo"
  const segments = cleanRel.split("/");
  if (segments[0] !== "blogs") {
    return null;
  }
  const stem = segments[1];
  if (!stem) {
    return null;
  }
  const m = BLOG_STEM_RE.exec(stem);
  if (!m) {
    return null;
  }
  const [, yyyy, mm, dd, slug] = m;
  return `/blogs/${yyyy}/${mm}/${dd}/${slug}`;
}

function appRouteExists(appDir, segments) {
  if (!fs.existsSync(appDir)) {
    return false;
  }
  return resolveSegments(appDir, segments);
}

function resolveSegments(dir, segments) {
  let entries;
  try {
    entries = fs.readdirSync(dir, { withFileTypes: true });
  } catch {
    return false;
  }

  if (segments.length === 0) {
    if (entries.some((e) => e.isFile() && PAGE_FILE_RE.test(e.name))) {
      return true;
    }
    // Route groups are transparent: descend without consuming a segment.
    for (const entry of entries) {
      if (!entry.isDirectory()) continue;
      if (entry.name.startsWith("(") && entry.name.endsWith(")")) {
        if (resolveSegments(path.join(dir, entry.name), segments)) {
          return true;
        }
      }
    }
    return false;
  }

  const [head, ...rest] = segments;

  for (const entry of entries) {
    if (!entry.isDirectory()) continue;
    const name = entry.name;
    const sub = path.join(dir, name);

    if (name === head) {
      if (resolveSegments(sub, rest)) return true;
    } else if (name.startsWith("(") && name.endsWith(")")) {
      // Route group: transparent, does not consume a segment.
      if (resolveSegments(sub, segments)) return true;
    } else if (
      name.startsWith("[") &&
      name.endsWith("]") &&
      !name.startsWith("[...") &&
      !name.startsWith("[[...")
    ) {
      // Dynamic single segment. Catch-all routes are intentionally ignored.
      if (resolveSegments(sub, rest)) return true;
    }
  }

  return false;
}

function docsFileExists(cwd, subSegments) {
  const joined = subSegments.join("/");
  const candidates = [
    `${joined}.md`,
    `${joined}.mdx`,
    `${joined}/index.md`,
    `${joined}/index.mdx`,
  ];
  return candidates.some((c) =>
    fs.existsSync(path.join(cwd, "content", "docs", c))
  );
}

/** Verify that /blogs/YYYY/MM/DD/slug maps to an actual blog file on disk. */
function blogsRouteExists(cwd, subSegments) {
  if (subSegments.length < 4) {
    return false;
  }
  const [yyyy, mm, dd, ...rest] = subSegments;
  if (!/^\d{4}$/.test(yyyy) || !/^\d{2}$/.test(mm) || !/^\d{2}$/.test(dd)) {
    return false;
  }
  const slug = rest.join("/");
  if (!slug) {
    return false;
  }
  const stem = `${yyyy}-${mm}-${dd}-${slug}`;
  const candidates = [
    `${stem}.md`,
    `${stem}.mdx`,
    `${stem}/${stem}.md`,
    `${stem}/${stem}.mdx`,
  ];
  return candidates.some((c) =>
    fs.existsSync(path.join(cwd, "content", "blogs", c))
  );
}

function walk(node, fn) {
  fn(node);
  if (Array.isArray(node.children)) {
    for (const child of node.children) {
      walk(child, fn);
    }
  }
}
