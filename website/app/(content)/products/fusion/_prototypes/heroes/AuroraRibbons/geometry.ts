/**
 * Pure path and segment math for the Aurora Ribbons hero: five strands enter
 * top right, braid through a shared band in visible crossing order, then
 * merge into one ribbon that continues to the left edge. No colour or JSX
 * lives here so the shapes stay easy to reason about on their own.
 */

export const VIEW_W = 1600;
export const VIEW_H = 900;

export const STRAND_COUNT = 5;

export const ENTRY_X = VIEW_W;
const ENTRY_Y: readonly number[] = [30, 95, 160, 225, 290];

const BRAID_X0 = 1020;
const BRAID_X1 = 680;
export const BRAID_CENTER_Y = 740;
const BRAID_AMPLITUDE = 85;
const BRAID_SEGMENTS = 40;
const ANGLE_STEP = (3 * Math.PI) / BRAID_SEGMENTS;

export const RIBBON_WIDTH = 30;
export const MERGE_WIDTH = 40;
/** Slightly past `BRAID_X1` so the drift animation never opens a seam at the handoff. */
export const MERGE_X0 = BRAID_X1 + 24;
/** Where the merged ribbon starts fading toward fully transparent at the left edge. */
export const MERGE_FADE_X = 340;

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
    const d = `M ${ENTRY_X} ${startY} C ${c1x} ${startY + (endY - startY) * 0.2}, ${c2x} ${endY - (endY - startY) * 0.2}, ${BRAID_X0} ${endY}`;
    return { index, d };
  });
}

export interface BraidQuad {
  readonly key: string;
  readonly index: number;
  readonly points: string;
}

/**
 * Braid-zone quads in back-to-front paint order, segment by segment, so
 * the strand that is "in front" at each crossing actually covers the one
 * behind it and the weave reads as a real braid, not five plain crossings.
 */
export function buildBraidQuads(): readonly BraidQuad[] {
  const quads: BraidQuad[] = [];
  const segWidth = (BRAID_X0 - BRAID_X1) / BRAID_SEGMENTS;
  const half = RIBBON_WIDTH / 2;

  for (let seg = 0; seg < BRAID_SEGMENTS; seg++) {
    const x0 = BRAID_X0 - seg * segWidth;
    const x1 = BRAID_X0 - (seg + 1) * segWidth;
    const order = Array.from({ length: STRAND_COUNT }, (_, i) => i).sort(
      (a, b) => braidDepth(a, seg) - braidDepth(b, seg),
    );

    for (const index of order) {
      const y0 = braidY(index, seg);
      const y1 = braidY(index, seg + 1);
      const points = `${x0},${y0 - half} ${x1},${y1 - half} ${x1},${y1 + half} ${x0},${y0 + half}`;
      quads.push({ key: `${index}-${seg}`, index, points });
    }
  }

  return quads;
}
