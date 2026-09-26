// Timing and per-frame math for GraphSphere's motion: spin speed, dot paths, and fade/breathe envelopes.
import { SPHERE_EDGES, SPHERE_VERTICES } from "./graphSphereGeometry";

/** One full revolution about the vertical axis every ROTATION_PERIOD_MS (60-90s). */
export const ROTATION_PERIOD_MS = 75_000;

export const DOT_COUNT = 4;
const DOT_HOPS = 3;
const DOT_HOP_MS = 4200;
const DOT_HOP_STAGGER_MS = 650;
const DOT_SEED_STRIDE = Math.floor(SPHERE_VERTICES.length / DOT_COUNT);

export const DOT_RADIUS = 2.4;
export const DOT_MAX_ALPHA = 0.85;

export const BREATHE_PERIOD_MS = 5200;
export const BREATHE_AMPLITUDE = 0.06;

function buildAdjacency(): readonly (readonly number[])[] {
  const adjacency: number[][] = Array.from(
    { length: SPHERE_VERTICES.length },
    () => [],
  );
  for (const [a, b] of SPHERE_EDGES) {
    adjacency[a].push(b);
    adjacency[b].push(a);
  }
  for (const list of adjacency) list.sort((a, b) => a - b);
  return adjacency;
}

const ADJACENCY = buildAdjacency();

/** A deterministic walk of `hops` edges, never doubling back on the step just taken. */
function walkPath(start: number, hops: number): readonly number[] {
  const path = [start];
  let prev = -1;
  let current = start;
  for (let i = 0; i < hops; i++) {
    const neighbors = ADJACENCY[current];
    const next = neighbors.find((n) => n !== prev) ?? neighbors[0];
    path.push(next);
    prev = current;
    current = next;
  }
  return path;
}

export interface DotPath {
  readonly path: readonly number[];
  readonly hopMs: number;
}

export const DOT_PATHS: readonly DotPath[] = Array.from(
  { length: DOT_COUNT },
  (_, i) => ({
    path: walkPath(i * DOT_SEED_STRIDE, DOT_HOPS),
    hopMs: DOT_HOP_MS + i * DOT_HOP_STAGGER_MS,
  }),
);

export interface DotFrame {
  readonly fromVertex: number;
  readonly toVertex: number;
  readonly frac: number;
  readonly alpha: number;
}

/** Ping-pongs a dot along its fixed path, fading in and out on every edge crossing. */
export function dotFrame(dot: DotPath, elapsedMs: number): DotFrame {
  const hops = dot.path.length - 1;
  const cycleMs = dot.hopMs * hops * 2;
  const s = (elapsedMs % cycleMs) / dot.hopMs;
  const forward = s < hops;
  const u = forward ? s : s - hops;
  const leg = Math.min(hops - 1, Math.floor(u));
  const frac = u - leg;
  const fromIndex = forward ? leg : hops - leg;
  const toIndex = forward ? leg + 1 : hops - leg - 1;
  return {
    fromVertex: dot.path[fromIndex],
    toVertex: dot.path[toIndex],
    frac,
    alpha: DOT_MAX_ALPHA * Math.sin(Math.PI * frac),
  };
}

/** A very slight radius multiplier for a haloed node's own gentle breathing. */
export function breatheScale(phaseOffset: number, elapsedMs: number): number {
  return (
    1 +
    BREATHE_AMPLITUDE *
      Math.sin((2 * Math.PI * elapsedMs) / BREATHE_PERIOD_MS + phaseOffset)
  );
}

export function prefersReducedMotion(): boolean {
  return (
    typeof window !== "undefined" &&
    window.matchMedia("(prefers-reduced-motion: reduce)").matches
  );
}
