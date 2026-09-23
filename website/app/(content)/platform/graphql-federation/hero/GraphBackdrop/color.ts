// The single local colour helper: every colour value the folder paints
// flows through here from hero/palette.ts (plus the words "white"/"black"),
// so the folder itself holds no hex literals. An optional `mix` blends a
// second palette colour in at a low ratio, which is how service tints stay
// a faint wash on the structural colour instead of a second saturated hue.
// An optional `darken` blends black in afterward -- how the copy-zone
// contrast fix keeps a node or edge's alpha in its normal visible range
// while still lowering the actual luminance it contributes under the text.
export function rgba(
  color: string,
  alpha: number,
  mix?: { readonly with: string; readonly ratio: number },
  darken = 0,
): string {
  let [r, g, b] = toRgb(color);
  if (mix && mix.ratio > 0) {
    const [r1, g1, b1] = toRgb(mix.with);
    const t = mix.ratio;
    r = r + (r1 - r) * t;
    g = g + (g1 - g) * t;
    b = b + (b1 - b) * t;
  }
  if (darken > 0) {
    r = r * (1 - darken);
    g = g * (1 - darken);
    b = b * (1 - darken);
  }
  return `rgba(${Math.round(r)},${Math.round(g)},${Math.round(b)},${alpha})`;
}

function toRgb(color: string): readonly [number, number, number] {
  if (color === "white") {
    return [255, 255, 255];
  }
  if (color === "black") {
    return [0, 0, 0];
  }
  const hex = color.replace("#", "");
  return [
    parseInt(hex.slice(0, 2), 16),
    parseInt(hex.slice(2, 4), 16),
    parseInt(hex.slice(4, 6), 16),
  ];
}

export const WHITE = "white";
