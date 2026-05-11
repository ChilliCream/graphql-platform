/**
 * Demotes every markdown heading by one level (h1 -> h2, h2 -> h3, ...).
 * The page layout owns the h1 (the frontmatter title), so authors are free
 * to write h1 in source while the rendered tree keeps a single page-level h1.
 */
export default function remarkDemoteHeadings() {
  return (tree) => {
    walk(tree, (node) => {
      if (node.type === "heading" && typeof node.depth === "number") {
        node.depth = Math.min(node.depth + 1, 6);
      }
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
