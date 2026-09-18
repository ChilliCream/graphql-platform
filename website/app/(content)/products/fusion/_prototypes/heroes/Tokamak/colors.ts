/**
 * Converts a `#rrggbb` hex literal from `BRAND`/`SERVICE_SPECTRUM` to an
 * `rgba(...)` string, the only way canvas code in this hero gets an
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
 * pure `hexB`) and returns an `rgba(...)` string, parsed through the same
 * cached `parseHex` as `hexToRgba`. Used for the tiles' coral warmth tint
 * near the plasma band (planner ruling hc-0-wrc.3 comment 187 item 2), not
 * a new colour source.
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
