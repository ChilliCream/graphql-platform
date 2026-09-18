/**
 * Converts a `#rrggbb` hex literal from `BRAND`/`SERVICE_SPECTRUM` to an
 * `rgba(...)` string, the only way canvas/DOM code in this hero gets an
 * alpha-blended colour. Each hex is parsed once and the RGB triple is
 * cached, the same idea as `board.ts`'s `GLOW_RGB` constant: no re-parse at
 * every call site, no new colour library.
 */
const rgbCache = new Map<string, readonly [number, number, number]>();

function parseHex(hex: string): readonly [number, number, number] {
  const cached = rgbCache.get(hex);
  if (cached) {
    return cached;
  }
  const rgb: readonly [number, number, number] = [
    parseInt(hex.slice(1, 3), 16),
    parseInt(hex.slice(3, 5), 16),
    parseInt(hex.slice(5, 7), 16),
  ];
  rgbCache.set(hex, rgb);
  return rgb;
}

export function hexToRgba(hex: string, alpha: number): string {
  const [r, g, b] = parseHex(hex);
  return `rgba(${r},${g},${b},${alpha})`;
}

/**
 * Linearly blends two `#rrggbb` hex literals by `t` (0 = pure `hexA`, 1 =
 * pure `hexB`) and returns an `rgba(...)` string. Both inputs still come
 * from `BRAND`/`SERVICE_SPECTRUM`, parsed through the same cached
 * `parseHex` as `hexToRgba` -- this is the warm-tint bleed from the halo
 * onto the sphere's core-facing hemisphere (ticket hc-0-wrc.5), not a new
 * colour source.
 */
export function mixHexToRgba(
  hexA: string,
  hexB: string,
  t: number,
  alpha: number,
): string {
  const [ar, ag, ab] = parseHex(hexA);
  const [br, bg, bb] = parseHex(hexB);
  const r = Math.round(ar + (br - ar) * t);
  const g = Math.round(ag + (bg - ag) * t);
  const b = Math.round(ab + (bb - ab) * t);
  return `rgba(${r},${g},${b},${alpha})`;
}

/**
 * White for filament centrelines and hot cores, as an `rgba(...)` string --
 * the single place this hero builds a white colour (ticket hc-0-wrc.6 /
 * v12 review minor: "no inline rgba(255,255,255,a) strings or raw
 * triples"). This is a sibling to `hexToRgba` rather than a call through it:
 * white has no `#rrggbb` source to parse (`BRAND`/`SERVICE_SPECTRUM` carry
 * no white, and the folder's "no hex literals" rule forbids adding one just
 * to feed the parser), so the RGB triple is written directly here, once.
 */
export function whiteRgba(alpha: number): string {
  return `rgba(255,255,255,${alpha})`;
}

/**
 * Caps how far a run's warm tint (`FilamentPath.warm`) can pull its glow
 * colour from cyan toward coral, so the seam-facing hemisphere bleeds warm
 * without the shell losing its one dominant cyan/teal light (README craft
 * bar's restrained palette). Shared by `index.tsx` (live layer) and
 * `paint.ts` (static layer) -- ticket hc-0-wrc.6 review minor: was
 * duplicated in both files.
 */
export const WARM_TINT_MAX = 0.3;
