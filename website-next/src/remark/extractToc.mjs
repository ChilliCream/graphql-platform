/**
 * Assigns a unique, slugified `id` to every heading node and stores a TOC
 * array (depth 2 + 3) on `file.data.toc`. The recma companion plugin emits
 * the TOC as a named export so consumers can render it. Heading rendering
 * picks the `id` up via `node.data.hProperties.id`, keeping anchor links
 * and TOC entries in sync even when two headings share the same text.
 */
export default function remarkExtractToc() {
  return (tree, file) => {
    const seen = new Map();
    const toc = [];
    walk(tree, (node) => {
      if (node.type !== "heading") {
        return;
      }
      const text = nodeText(node).trim();
      if (!text) {
        return;
      }
      const id = uniqueSlug(seen, slugify(text));
      node.data ??= {};
      node.data.hProperties ??= {};
      node.data.hProperties.id = id;
      if (node.depth === 2 || node.depth === 3) {
        toc.push({ id, text, depth: node.depth });
      }
    });
    file.data ??= {};
    file.data.toc = toc;
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

function nodeText(node) {
  if (node.type === "text" || node.type === "inlineCode") {
    return node.value ?? "";
  }
  if (Array.isArray(node.children)) {
    return node.children.map(nodeText).join("");
  }
  return "";
}

function slugify(text) {
  return text
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "");
}

function uniqueSlug(seen, base) {
  const count = seen.get(base) ?? 0;
  seen.set(base, count + 1);
  return count === 0 ? base : `${base}-${count + 1}`;
}
