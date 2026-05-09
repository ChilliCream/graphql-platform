import path from "node:path";

const CONTENT_ROOTS = ["docs", "blogs"];

export default function remarkRewriteMdLinks() {
  return (tree, file) => {
    const sourcePath = file?.path;
    if (!sourcePath) {
      return;
    }

    const sourceDir = path.dirname(sourcePath);
    const cwd = file.cwd ?? process.cwd();

    walk(tree, (node) => {
      if (node.type !== "link" || typeof node.url !== "string") {
        return;
      }

      // Skip absolute URLs, protocol-relative, hash-only, root-absolute paths.
      if (/^([a-z]+:|\/\/|#|\/)/i.test(node.url)) {
        return;
      }

      const match = /^([^#?]+\.mdx?)(#[^?]*)?(\?.*)?$/i.exec(node.url);
      if (!match) {
        return;
      }

      const [, filePart, hashPart = ""] = match;
      const absResolved = path.resolve(sourceDir, filePart);
      let rel = path.relative(cwd, absResolved).split(path.sep).join("/");

      const root = CONTENT_ROOTS.find(
        (r) => rel === r || rel.startsWith(`${r}/`)
      );
      if (!root) {
        return;
      }

      rel = rel.replace(/\.mdx?$/i, "").replace(/\/index$/, "");

      node.url = `/${rel}${hashPart}`;
    });
  };
}

function walk(node, fn) {
  fn(node);
  if (Array.isArray(node.children)) {
    for (const child of node.children) {
      walk(child, fn);
    }
  }
}
