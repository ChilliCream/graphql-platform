// Pure geometry helpers shared by graph.ts: viewport mode, the coverage
// cap per width and the dpr / backing-pixel cap. All are pure functions of
// their inputs, so the same canvas size always yields the same layout.
export type LayoutMode = "landscape" | "portrait";

export const MIN_NODE_SPACING = 28;
export const COPY_ZONE_PAD = 24;

// A tall viewport (height >= width) gets the portrait layout regardless of
// its exact width, so both narrow phones and tall tablets in portrait get
// the same treatment.
export function modeForSize(w: number, h: number): LayoutMode {
  return h >= w ? "portrait" : "landscape";
}

/**
 * The largest-empty-circle diameter the coverage lattice must guarantee,
 * per the ticket's per-width caps (140 at 1440 and 1920, 120 at 1024 and
 * 768, 100 below that).
 */
export function coverageCapDiameter(w: number): number {
  if (w >= 1280) {
    return 140;
  }
  if (w >= 700) {
    return 120;
  }
  return 100;
}

/**
 * A hexagonal lattice's covering radius (the farthest any point in the
 * plane sits from its nearest lattice point) is `spacing / sqrt(3)`; the
 * per-point jitter graph.ts applies can push that worst case out by up to
 * its own diagonal reach, so the 0.72 margin here budgets for the ideal
 * lattice AND that jitter, verified empirically (test-results/rh1-*)
 * against the real largest-empty-circle measurement, not just the
 * unjittered formula.
 */
export function hexSpacing(capDiameter: number): number {
  return (capDiameter / 2) * Math.sqrt(3) * 0.72;
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
