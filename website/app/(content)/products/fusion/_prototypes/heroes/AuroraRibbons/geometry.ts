/**
 * Pure path and segment math for the Aurora Ribbons hero: five strands enter
 * top right, braid through a shared band in visible crossing order, then
 * merge into one ribbon that fades out left of the braid. No colour or JSX
 * lives here.
 */

export const VIEW_W = 1600;
export const VIEW_H = 900;

export const STRAND_COUNT = 5;

export const ENTRY_X = VIEW_W;
const ENTRY_Y: readonly number[] = [30, 95, 160, 225, 290];
/** Overlap past `ENTRY_X` covering the drift animation's travel at the entry edge. */
const ENTRY_OVERLAP = 24;

const BRAID_X0 = 1020;
const BRAID_X1 = 680;
export const BRAID_CENTER_Y = 740;
const BRAID_AMPLITUDE = 85;
const BRAID_SEGMENTS = 40;
const ANGLE_STEP = (3 * Math.PI) / BRAID_SEGMENTS;

export const RIBBON_WIDTH = 30;
export const MERGE_WIDTH = 40;
/** Overlap past `BRAID_X1` covering the drift animation's travel at the handoff. */
export const MERGE_X0 = BRAID_X1 + ENTRY_OVERLAP;
/** Where the merged ribbon's fade toward transparent ends, kept inside the narrowest crop's visible band. */
export const MERGE_FADE_X = 700;
/** Length of the merged ribbon's fade, ending at `MERGE_FADE_X` and starting at or right of the narrowest crop's left edge. */
export const MERGE_FADE_LENGTH = 100;
/** Distance left of `BRAID_X1` over which merge-band quads taper from ribbon width to `MERGE_WIDTH`. */
const MERGE_TAPER_LENGTH = 90;
/** Number of quads per strand used to render the merge-band taper. */
const MERGE_TAPER_SEGMENTS = 8;
/** Braid segment at which strand opacity starts ramping down toward the merge band. */
const FADE_START_SEG = 26;
/** Strand opacity at `BRAID_X1`, carried into the merge band and tapered the rest of the way to zero. */
const FADE_FLOOR = 0.12;

function strandPhase(index: number): number {
  return (index * 2 * Math.PI) / STRAND_COUNT;
}

/**
 * Braid strand vertical position at a given segment: `seg` 0 is the entry
 * side (full amplitude, strands apart), `seg` `BRAID_SEGMENTS` is the merge
 * side (amplitude decayed to zero, every strand on the shared centreline).
 */
function braidY(index: number, seg: number): number {
  const amplitude = BRAID_AMPLITUDE * (1 - seg / BRAID_SEGMENTS);
  return (
    BRAID_CENTER_Y + amplitude * Math.sin(strandPhase(index) + seg * ANGLE_STEP)
  );
}

/** Front-to-back depth at a given segment; a strand with a higher value paints later (in front). */
function braidDepth(index: number, seg: number): number {
  return Math.cos(strandPhase(index) + seg * ANGLE_STEP);
}

/** Strand opacity at a given braid segment: 1 before `FADE_START_SEG`, ramping to `FADE_FLOOR` by `BRAID_SEGMENTS`. */
function braidOpacity(seg: number): number {
  if (seg < FADE_START_SEG) return 1;
  const frac = (seg - FADE_START_SEG) / (BRAID_SEGMENTS - FADE_START_SEG);
  return 1 - frac * (1 - FADE_FLOOR);
}

export interface EntryPath {
  readonly index: number;
  readonly d: string;
}

/** One smooth path per strand from its top-right entry point to where it joins the braid. */
export function buildEntryPaths(): readonly EntryPath[] {
  return Array.from({ length: STRAND_COUNT }, (_, index) => {
    const startY = ENTRY_Y[index];
    const endY = braidY(index, 0);
    const c1x = ENTRY_X - 230;
    const c2x = BRAID_X0 + 230;
    const d = `M ${ENTRY_X + ENTRY_OVERLAP} ${startY} C ${c1x} ${startY + (endY - startY) * 0.2}, ${c2x} ${endY - (endY - startY) * 0.2}, ${BRAID_X0} ${endY}`;
    return { index, d };
  });
}

export interface BraidQuad {
  readonly key: string;
  readonly index: number;
  readonly points: string;
  readonly opacity: number;
}

/** Overlap on adjacent braid quads' shared edges, closing the antialiasing seam between them. */
const QUAD_OVERLAP = 0.75;

/**
 * Braid-zone quads in back-to-front paint order, segment by segment. Within
 * each segment, the strand in front at that crossing paints after the
 * strands behind it.
 */
export function buildBraidQuads(): readonly BraidQuad[] {
  const quads: BraidQuad[] = [];
  const segWidth = (BRAID_X0 - BRAID_X1) / BRAID_SEGMENTS;
  const half = RIBBON_WIDTH / 2;

  for (let seg = 0; seg < BRAID_SEGMENTS; seg++) {
    const x0 = BRAID_X0 - seg * segWidth + (seg === 0 ? 0 : QUAD_OVERLAP);
    const x1 = BRAID_X0 - (seg + 1) * segWidth;
    const opacity = braidOpacity(seg);
    const order = Array.from({ length: STRAND_COUNT }, (_, i) => i).sort(
      (a, b) => braidDepth(a, seg) - braidDepth(b, seg),
    );

    for (const index of order) {
      const y0 = braidY(index, seg);
      const y1 = braidY(index, seg + 1);
      const points = `${x0},${y0 - half} ${x1},${y1 - half} ${x1},${y1 + half} ${x0},${y0 + half}`;
      quads.push({ key: `${index}-${seg}`, index, points, opacity });
    }
  }

  return quads;
}

export interface MergeQuad {
  readonly key: string;
  readonly index: number;
  readonly points: string;
  readonly opacity: number;
}

/**
 * Quads that continue each braid strand past `BRAID_X1` into the merge band,
 * over the merged ribbon underneath. Each strand widens toward `MERGE_WIDTH`
 * and its opacity tapers from `FADE_FLOOR`, where `braidOpacity` leaves it, to
 * zero.
 */
export function buildMergeQuads(): readonly MergeQuad[] {
  const quads: MergeQuad[] = [];
  const segWidth = MERGE_TAPER_LENGTH / MERGE_TAPER_SEGMENTS;
  const order = Array.from({ length: STRAND_COUNT }, (_, i) => i).sort(
    (a, b) => braidDepth(a, BRAID_SEGMENTS) - braidDepth(b, BRAID_SEGMENTS),
  );

  for (let seg = 0; seg < MERGE_TAPER_SEGMENTS; seg++) {
    const frac0 = seg / MERGE_TAPER_SEGMENTS;
    const frac1 = (seg + 1) / MERGE_TAPER_SEGMENTS;
    const half0 =
      RIBBON_WIDTH / 2 + (MERGE_WIDTH / 2 - RIBBON_WIDTH / 2) * frac0;
    const half1 =
      RIBBON_WIDTH / 2 + (MERGE_WIDTH / 2 - RIBBON_WIDTH / 2) * frac1;
    const x0 = BRAID_X1 - seg * segWidth + QUAD_OVERLAP;
    const x1 = BRAID_X1 - (seg + 1) * segWidth;
    const opacity = FADE_FLOOR * (1 - frac1) ** 2;

    for (const index of order) {
      const points = `${x0},${BRAID_CENTER_Y - half0} ${x1},${BRAID_CENTER_Y - half1} ${x1},${BRAID_CENTER_Y + half1} ${x0},${BRAID_CENTER_Y + half0}`;
      quads.push({ key: `merge-${index}-${seg}`, index, points, opacity });
    }
  }

  return quads;
}
