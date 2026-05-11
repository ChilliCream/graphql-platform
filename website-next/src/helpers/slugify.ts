import type { ReactNode } from "react";

export function getText(node: ReactNode): string {
  if (node == null || typeof node === "boolean") {
    return "";
  }
  if (typeof node === "string" || typeof node === "number") {
    return String(node);
  }
  if (Array.isArray(node)) {
    return node.map(getText).join("");
  }
  if (typeof node === "object" && "props" in node) {
    return getText((node as { props: { children?: ReactNode } }).props.children);
  }
  return "";
}

export function slugify(node: ReactNode): string {
  return getText(node)
    .toLowerCase()
    .trim()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "");
}
