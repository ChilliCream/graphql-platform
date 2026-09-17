import { project, ringPoint, type Camera } from "./geometry";
import type { ChamberRow } from "./layout";

interface Pt {
  readonly x: number;
  readonly y: number;
}

export interface Tile {
  /** The four screen-space corners, already inset from the true tile edges so the gap between tiles reads as a dark seam, never a stroke. */
  readonly poly: readonly [Pt, Pt, Pt, Pt];
  /** Diagonal corners for the specular gradient (unfaded). */
  readonly hi: Pt;
  readonly lo: Pt;
  /** 0..1 directional-light shade for this tile's facing. */
  readonly shade: number;
  readonly fastener: boolean;
  /** Approx on-screen size in px, for the fastener/detail size gate. */
  readonly size: number;
}

export interface InstrumentLight {
  readonly x: number;
  readonly y: number;
  readonly r: number;
}

const SEAM_INSET = 0.09;
const LIGHT_DIR = normalize3({ x: 0.4, y: 0.7, z: -0.6 });

function normalize3(v: { x: number; y: number; z: number }) {
  const len = Math.hypot(v.x, v.y, v.z) || 1;
  return { x: v.x / len, y: v.y / len, z: v.z / len };
}

function dot3(
  a: { x: number; y: number; z: number },
  b: { x: number; y: number; z: number },
): number {
  return a.x * b.x + a.y * b.y + a.z * b.z;
}

function clamp(v: number, min: number, max: number): number {
  return Math.max(min, Math.min(max, v));
}

function lerpPt(a: Pt, b: Pt, t: number): Pt {
  return { x: a.x + (b.x - a.x) * t, y: a.y + (b.y - a.y) * t };
}

function insetQuad(
  p: readonly [Pt, Pt, Pt, Pt],
  sizePx: number,
): readonly [Pt, Pt, Pt, Pt] {
  const cx = (p[0].x + p[1].x + p[2].x + p[3].x) / 4;
  const cy = (p[0].y + p[1].y + p[2].y + p[3].y) / 4;
  const center: Pt = { x: cx, y: cy };
  // Small, far-away tiles get a much smaller fractional inset so the seam
  // gap never swallows the tile; the seam still reads as a hairline gap,
  // never a stroke.
  const inset = sizePx < 10 ? SEAM_INSET * 0.35 : SEAM_INSET;
  return [
    lerpPt(p[0], center, inset),
    lerpPt(p[1], center, inset),
    lerpPt(p[2], center, inset),
    lerpPt(p[3], center, inset),
  ];
}

/**
 * The static chamber: a perspective grid of tiles following elliptical
 * rings around the column, one trapezoid per (row, theta-segment) cell.
 * Each tile's silhouette comes from projecting real 3D corners (never a
 * flat 2D grid), its shade from a directional light dotted against the
 * cylinder's local outward normal (the "subtle specular gradient"), and
 * seams are the gap left by insetting each tile toward its own centre --
 * never a stroked edge. Built once per `measure()` (mount + resize), never
 * per frame.
 */
export function buildChamberTiles(
  rows: readonly ChamberRow[],
  thetaSegments: number,
  camera: Camera,
): Tile[] {
  const tiles: Tile[] = [];
  for (let r = 0; r < rows.length - 1; r++) {
    const rowA = rows[r];
    const rowB = rows[r + 1];
    for (let s = 0; s < thetaSegments; s++) {
      const t0 = (s / thetaSegments) * Math.PI * 2;
      const t1 = ((s + 1) / thetaSegments) * Math.PI * 2;
      const wA0 = ringPoint(rowA.radius, t0, rowA.y, rowA.z);
      const wA1 = ringPoint(rowA.radius, t1, rowA.y, rowA.z);
      const wB1 = ringPoint(rowB.radius, t1, rowB.y, rowB.z);
      const wB0 = ringPoint(rowB.radius, t0, rowB.y, rowB.z);
      const c0 = project(wA0, camera);
      const c1 = project(wA1, camera);
      const c2 = project(wB1, camera);
      const c3 = project(wB0, camera);
      if (c0.depth <= 1 || c1.depth <= 1 || c2.depth <= 1 || c3.depth <= 1) {
        continue;
      }
      const wPx = Math.hypot(c1.x - c0.x, c1.y - c0.y);
      const hPx = Math.hypot(c3.x - c0.x, c3.y - c0.y);
      const size = Math.min(wPx, hPx);
      const poly = insetQuad([c0, c1, c2, c3], size);
      const midTheta = (t0 + t1) / 2;
      const normal = { x: Math.cos(midTheta), y: 0, z: Math.sin(midTheta) };
      const shade = clamp(dot3(normal, LIGHT_DIR) * 0.5 + 0.5, 0.16, 1);
      tiles.push({
        poly,
        hi: poly[0],
        lo: poly[2],
        shade,
        fastener: size > 16 && (r * thetaSegments + s) % 3 === 0,
        size,
      });
    }
  }
  return tiles;
}

/**
 * A handful of small cyan instrument lights fixed to wall tiles, scattered
 * around the chamber but weighted toward the outer/near rows so they read
 * at a glance instead of vanishing into the far rings.
 */
export function buildInstrumentLights(
  rows: readonly ChamberRow[],
  camera: Camera,
  rand: () => number,
  count: number,
): InstrumentLight[] {
  const lights: InstrumentLight[] = [];
  const candidateRows = rows.slice(2, rows.length - 2);
  for (let i = 0; i < count; i++) {
    const row = candidateRows[Math.floor(rand() * candidateRows.length)];
    const theta = rand() * Math.PI * 2;
    const p = ringPoint(row.radius * 1.002, theta, row.y, row.z);
    const proj = project(p, camera);
    if (proj.depth <= 1) {
      continue;
    }
    lights.push({
      x: proj.x,
      y: proj.y,
      r: Math.max(1.1, 2.2 * proj.scale * 0.02),
    });
  }
  return lights;
}
