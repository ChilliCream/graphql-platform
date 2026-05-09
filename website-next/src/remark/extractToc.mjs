/**
 * Walks heading nodes (depth 2 + 3) and stores a TOC array on `file.data.toc`
 * so the recma companion plugin can emit it as a named export from the
 * compiled MDX module.
 */
export default function remarkExtractToc() {
  return (tree, file) => {
    const toc = [];
    walk(tree, (node) => {
      if (node.type !== "heading") {
        return;
      }
      if (node.depth !== 2 && node.depth !== 3) {
        return;
      }
      const text = nodeText(node).trim();
      if (!text) {
        return;
      }
      toc.push({ id: slugify(text), text, depth: node.depth });
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
