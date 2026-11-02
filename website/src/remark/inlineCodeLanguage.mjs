const LANGUAGE_PREFIX = /^#!([\w+#-]+)\s+(.+)$/s;

/**
 * Reads a leading `#!lang` prefix on inline code (`` `#!graphql type Query` ``),
 * strips it from the code and forwards the language onto the resulting <code>
 * element as a `data-language` attribute so React components can highlight it.
 */
export default function remarkInlineCodeLanguage() {
  return (tree) => {
    walk(tree, (node) => {
      if (node.type !== "inlineCode") {
        return;
      }
      const match = node.value.match(LANGUAGE_PREFIX);
      if (!match) {
        return;
      }
      node.value = match[2];
      node.data ??= {};
      node.data.hProperties ??= {};
      node.data.hProperties["data-language"] = match[1];
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
