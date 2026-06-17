import { visit } from "unist-util-visit";

/**
 * MDX requires the `style` attribute on HAST elements to be an object, not a
 * CSS string. Some rehype plugins (notably `rehype-mermaid`) embed raw CSS
 * strings. This pass walks the tree and converts every string `style` into a
 * camelCased object.
 */
export default function rehypeStyleStringToObject() {
  return (tree) => {
    visit(tree, "element", (node) => {
      const style = node.properties?.style;
      if (typeof style !== "string") {
        return;
      }
      node.properties.style = parseStyle(style);
    });
  };
}

function parseStyle(value) {
  const result = {};
  for (const decl of value.split(";")) {
    const idx = decl.indexOf(":");
    if (idx < 0) {
      continue;
    }
    const rawProp = decl.slice(0, idx).trim();
    const rawValue = decl.slice(idx + 1).trim();
    if (!rawProp || !rawValue) {
      continue;
    }
    result[camelize(rawProp)] = rawValue;
  }
  return result;
}

function camelize(prop) {
  if (prop.startsWith("--")) {
    return prop;
  }
  return prop.toLowerCase().replace(/-([a-z])/g, (_, ch) => ch.toUpperCase());
}
