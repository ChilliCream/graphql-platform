/**
 * Converts a `#rrggbb` hex literal from `BRAND` to an
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
 * `BRAND` has no white -- every white this hero draws (fastener highlights,
 * streak/filament cores) would otherwise be an ad hoc `rgba(255,255,255,a)`
 * literal or a raw `[255,255,255]` triple at each call site. The two triples
 * live here once instead, the same idea as `parseHex`'s cache: one source of
 * truth, routed through by name.
 */
const WHITE_RGB: readonly [number, number, number] = [255, 255, 255];
/** The streak/filament near-white core, with a faint warm cast. */
const WARM_WHITE_RGB: readonly [number, number, number] = [255, 244, 240];

export function whiteToRgba(alpha: number): string {
  const [r, g, b] = WHITE_RGB;
  return `rgba(${r},${g},${b},${alpha})`;
}

export function warmWhiteToRgba(alpha: number): string {
  const [r, g, b] = WARM_WHITE_RGB;
  return `rgba(${r},${g},${b},${alpha})`;
}

/**
 * Linearly blends two `#rrggbb` hex literals by `t` (0 = pure `hexA`, 1 =
 * pure `hexB`) and returns an `rgba(...)` string, parsed through the same
 * cached `parseHex` as `hexToRgba`. Used for the tiles' coral warmth tint
 * near the plasma band, not a new colour source.
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
