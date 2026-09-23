// Pure geometry helpers shared by graph.ts: viewport mode and the dpr /
// backing-pixel cap. Both are pure functions of their inputs, so the same
// canvas size always yields the same layout.
export type LayoutMode = "landscape" | "portrait";

export const MIN_NODE_SPACING = 28;
export const COPY_ZONE_PAD = 24;
export const MAX_NEIGHBOUR_GAP = 130;

// A tall viewport (height >= width) gets the portrait layout regardless of
// its exact width, so both narrow phones and tall tablets in portrait get
// the same treatment.
export function modeForSize(w: number, h: number): LayoutMode {
  return h >= w ? "portrait" : "landscape";
}

const MAX_DPR = 2;
const MAX_BACKING_PIXELS = 4_000_000;

export function capDpr(
  cw: number,
  ch: number,
  devicePixelRatio: number,
): number {
  const base = Math.min(devicePixelRatio || 1, MAX_DPR);
  const cap = Math.sqrt(MAX_BACKING_PIXELS / Math.max(1, cw * ch));
  return Math.max(0.75, Math.min(base, cap));
}
