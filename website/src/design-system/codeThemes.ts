import { bundledThemes, type ThemeRegistration } from "shiki";
import { CODE_THEME_DARK } from "./languages";

// Darkest surface a Light token can sit on: the code background under the
// line highlight (`--color-cc-code-bg` with the heading ink at 8%).
const LIGHT_SURFACE = [217, 211, 199] as const;
const MIN_CONTRAST = 4.5;

function parseHex(value: string): [number, number, number] | null {
  const match = /^#([0-9a-f]{6})(?:[0-9a-f]{2})?$/i.exec(value);
  if (!match) {
    return null;
  }
  const n = parseInt(match[1], 16);
  return [(n >> 16) & 255, (n >> 8) & 255, n & 255];
}

function luminance(rgb: readonly number[]): number {
  const [r, g, b] = rgb.map((v) => {
    const c = v / 255;
    return c <= 0.04045 ? c / 12.92 : ((c + 0.055) / 1.055) ** 2.4;
  });
  return 0.2126 * r + 0.7152 * g + 0.0722 * b;
}

function contrast(a: readonly number[], b: readonly number[]): number {
  const [hi, lo] = [luminance(a), luminance(b)].sort((x, y) => y - x);
  return (hi + 0.05) / (lo + 0.05);
}

function toHex(rgb: readonly number[]): string {
  return `#${rgb.map((v) => Math.round(v).toString(16).padStart(2, "0")).join("")}`;
}

function readable(color: string): string {
  const rgb = parseHex(color);
  if (!rgb) {
    return color;
  }
  for (let keep = 1; keep > 0; keep -= 0.02) {
    const darker = rgb.map((v) => v * keep);
    if (contrast(darker, LIGHT_SURFACE) >= MIN_CONTRAST) {
      return keep === 1 ? color : toHex(darker);
    }
  }
  return "#000000";
}

let lightTheme: Promise<ThemeRegistration> | undefined;

/**
 * The Light syntax theme: GitHub Light with every token color darkened until
 * it reads at 4.5:1 on the cream code surface.
 */
export function getLightCodeTheme(): Promise<ThemeRegistration> {
  return (lightTheme ??= bundledThemes["github-light-default"]().then(
    ({ default: base }): ThemeRegistration => ({
      ...base,
      name: "chillicream-light",
      colors: Object.fromEntries(
        Object.entries(base.colors ?? {}).map(([key, value]) => [
          key,
          key === "editor.foreground" ? readable(value) : value,
        ]),
      ),
      tokenColors: (base.tokenColors ?? []).map((rule) => ({
        ...rule,
        settings: rule.settings.foreground
          ? { ...rule.settings, foreground: readable(rule.settings.foreground) }
          : rule.settings,
      })),
    }),
  ));
}

/** Shiki options that emit both site themes as CSS variables on every token. */
export async function getSiteThemeOptions() {
  return {
    themes: { light: await getLightCodeTheme(), dark: CODE_THEME_DARK },
    defaultColor: false,
  } as const;
}
