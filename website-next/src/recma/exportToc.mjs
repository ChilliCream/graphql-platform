/**
 * Companion to the remark `extractToc` plugin: reads `file.data.toc` and
 * prepends an `export const toc = [...]` declaration to the compiled module
 * so consumers can `import { toc } from "@/docs/foo.md"`.
 */
export default function recmaExportToc() {
  return (tree, file) => {
    const toc = file?.data?.toc ?? [];
    tree.body.unshift({
      type: "ExportNamedDeclaration",
      specifiers: [],
      source: null,
      declaration: {
        type: "VariableDeclaration",
        kind: "const",
        declarations: [
          {
            type: "VariableDeclarator",
            id: { type: "Identifier", name: "toc" },
            init: toEstree(toc),
          },
        ],
      },
    });
  };
}

function toEstree(value) {
  if (value === null) {
    return { type: "Literal", value: null, raw: "null" };
  }
  if (typeof value === "string") {
    return { type: "Literal", value, raw: JSON.stringify(value) };
  }
  if (typeof value === "number") {
    return { type: "Literal", value, raw: String(value) };
  }
  if (typeof value === "boolean") {
    return { type: "Literal", value, raw: String(value) };
  }
  if (Array.isArray(value)) {
    return {
      type: "ArrayExpression",
      elements: value.map(toEstree),
    };
  }
  if (typeof value === "object") {
    return {
      type: "ObjectExpression",
      properties: Object.entries(value).map(([k, v]) => ({
        type: "Property",
        key: /^[A-Za-z_$][A-Za-z0-9_$]*$/.test(k)
          ? { type: "Identifier", name: k }
          : { type: "Literal", value: k, raw: JSON.stringify(k) },
        value: toEstree(v),
        kind: "init",
        method: false,
        shorthand: false,
        computed: false,
      })),
    };
  }
  return { type: "Literal", value: String(value), raw: JSON.stringify(String(value)) };
}
