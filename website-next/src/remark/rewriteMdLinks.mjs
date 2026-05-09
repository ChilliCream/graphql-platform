import fs from "node:fs";
import path from "node:path";

const CONTENT_ROOTS = ["docs", "blogs"];
const RULE_ID = "remark-rewrite-md-links";
const PAGE_FILE_RE = /^page\.(tsx?|jsx?|mdx?)$/;

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
        const contentRoot = CONTENT_ROOTS.find((r) => segments[0] === r);

        if (contentRoot) {
          const subSegments = segments.slice(1);
          if (subSegments.length === 0) {
            file.fail(
              `Broken root-absolute link "${node.url}" — content root "/${contentRoot}" has no index page`,
              node,
              RULE_ID
            );
            return;
          }
          if (!contentFileExists(cwd, contentRoot, subSegments)) {
            file.fail(
              `Broken root-absolute link "${node.url}" — no matching file found under ${contentRoot}/`,
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
      node.url = `/${cleanRel}${hashPart}`;
    });
  };
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

function contentFileExists(cwd, contentRoot, subSegments) {
  const joined = subSegments.join("/");
  const candidates = [
    `${joined}.md`,
    `${joined}.mdx`,
    `${joined}/index.md`,
    `${joined}/index.mdx`,
  ];
  return candidates.some((c) =>
    fs.existsSync(path.join(cwd, contentRoot, c))
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
