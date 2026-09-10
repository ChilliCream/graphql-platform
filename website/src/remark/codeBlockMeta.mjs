/**
 * Forwards the markdown code-block meta string (` ```js {5}` -> "{5}") onto the
 * resulting <code> element as a `data-meta` attribute so React components can
 * read it.
 */
export default function remarkCodeBlockMeta() {
  return (tree) => {
    walk(tree, (node) => {
      if (node.type !== "code" || !node.meta) {
        return;
      }
      node.data ??= {};
      node.data.hProperties ??= {};
      node.data.hProperties["data-meta"] = node.meta;
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
