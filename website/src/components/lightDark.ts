import "./lightDark.module.css";

/**
 * CSS value that resolves to `light` under the Light appearance and to `dark`
 * under the Dark appearance. Use it in inline styles and SVG presentation
 * attributes, where it is evaluated against the resolved appearance.
 */
export function lightDark(light: string, dark: string): string {
  return `var(--cc-if-light, ${light}) var(--cc-if-dark, ${dark})`;
}
