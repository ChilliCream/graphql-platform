/**
 * CSS color that resolves to `light` under the Light appearance and to `dark`
 * under the Dark appearance. Use it in inline styles only, where the browser
 * evaluates it against the page's resolved color scheme.
 */
export function lightDark(light: string, dark: string): string {
  return `light-dark(${light}, ${dark})`;
}
