import fs from "node:fs";
import path from "node:path";

const CONTENT_ROOTS = ["content/docs", "content/blog"];
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

    const publicDir = path.join(cwd, "public");

    walk(tree, (node) => {
      const isLink = node.type === "link";
      const isImage = node.type === "image";
      if ((!isLink && !isImage) || typeof node.url !== "string") {
        return;
      }

      // Skip absolute URLs, protocol-relative, hash-only.
      if (/^([a-z]+:|\/\/|#)/i.test(node.url)) {
        return;
      }

      // Relative reference that resolves into public/ — rewrite to a rooted
      // URL (e.g. "../../../public/image.png" -> "/image.png"). Applies to both
      // links and image sources.
      if (!node.url.startsWith("/")) {
        const publicUrl = rewritePublicAsset(
          node.url,
          sourceDir,
          publicDir,
          cwd,
          file,
          node,
        );
        if (publicUrl !== null) {
          node.url = publicUrl;
          return;
        }
      }

      // Root-absolute asset sources (e.g. an image at "/image.png") are already
      // rooted; only links get route-existence validation below.
      if (node.url.startsWith("/") && !isLink) {
        return;
      }

      // Root-absolute: check content roots for /docs and /blog, otherwise
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
              RULE_ID,
            );
            return;
          }
          if (!docsFileExists(cwd, subSegments)) {
            file.fail(
              `Broken root-absolute link "${node.url}" — no matching file found under docs/`,
              node,
              RULE_ID,
            );
          }
          return;
        }

        if (segments[0] === "blog") {
          if (!blogsRouteExists(cwd, segments.slice(1))) {
            file.fail(
              `Broken root-absolute link "${node.url}" — no matching post found under blog/ ` +
                `(expected /blog/YYYY-MM-DD-slug)`,
              node,
              RULE_ID,
            );
          }
          return;
        }

        if (!appRouteExists(appDir, segments)) {
          file.fail(
            `Broken root-absolute link "${node.url}" — no matching page found in app/`,
            node,
            RULE_ID,
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
          RULE_ID,
        );
      }

      const rel = path.relative(cwd, absResolved).split(path.sep).join("/");
      const root = CONTENT_ROOTS.find(
        (r) => rel === r || rel.startsWith(`${r}/`),
      );
      if (!root) {
        file.fail(
          `Markdown link "${node.url}" resolves outside the content roots (${CONTENT_ROOTS.join(", ")}): ${rel}`,
          node,
          RULE_ID,
        );
      }

      const cleanRel = rel.replace(/\.mdx?$/i, "").replace(/\/index$/, "");
      // Strip the on-disk "content/" prefix to produce the public URL.
      const urlRel = cleanRel.replace(/^content\//, "");

      if (root === "content/blog") {
        const blogUrl = blogUrlFromCleanRel(urlRel);
        if (blogUrl === null) {
          file.fail(
            `Markdown link "${node.url}" resolves to a blog file with an invalid name "${urlRel}". ` +
              `Expected content/blog/YYYY-MM-DD-slug.md or content/blog/YYYY-MM-DD-slug/YYYY-MM-DD-slug.md`,
            node,
            RULE_ID,
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

/**
 * Resolve a relative URL against the source file and, if it lands inside
 * public/, return its rooted URL (path relative to public/, with a leading
 * "/"). Returns null when the target is outside public/ so the caller can fall
 * through to other handling. Fails the build for a public/ path that doesn't
 * exist on disk.
 */
function rewritePublicAsset(url, sourceDir, publicDir, cwd, file, node) {
  const m = /^([^#?]+)([#?].*)?$/.exec(url);
  if (!m) {
    return null;
  }

  const [, filePart, suffix = ""] = m;
  const absResolved = path.resolve(sourceDir, filePart);
  const relToPublic = path.relative(publicDir, absResolved);

  // Outside public/ (escapes upward or onto another drive) — not our concern.
  if (relToPublic.startsWith("..") || path.isAbsolute(relToPublic)) {
    return null;
  }

  if (!fs.existsSync(absResolved)) {
    file.fail(
      `Broken asset link "${url}" — file not found at ${path.relative(cwd, absResolved)}`,
      node,
      RULE_ID,
    );
    return null;
  }

  const urlPath = relToPublic.split(path.sep).join("/");
  return `/${urlPath}${suffix}`;
}

/** Convert a path relative to cwd (without extension) under blog/ into the
 *  canonical /blog/YYYY-MM-DD-slug URL, or null if it doesn't match. */
function blogUrlFromCleanRel(cleanRel) {
  // cleanRel looks like "blog/2019-06-05-foo" or "blog/2019-06-05-foo/2019-06-05-foo"
  const segments = cleanRel.split("/");
  if (segments[0] !== "blog") {
    return null;
  }
  const stem = segments[1];
  if (!stem || !BLOG_STEM_RE.test(stem)) {
    return null;
  }
  return `/blog/${stem}`;
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
    fs.existsSync(path.join(cwd, "content", "docs", c)),
  );
}

/** Verify that /blog/YYYY-MM-DD-slug maps to an actual blog file on disk. */
function blogsRouteExists(cwd, subSegments) {
  if (subSegments.length !== 1) {
    return false;
  }
  const stem = subSegments[0];
  if (!BLOG_STEM_RE.test(stem)) {
    return false;
  }
  const candidates = [
    `${stem}.md`,
    `${stem}.mdx`,
    `${stem}/${stem}.md`,
    `${stem}/${stem}.mdx`,
  ];
  return candidates.some((c) =>
    fs.existsSync(path.join(cwd, "content", "blog", c)),
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
