import { project, ringPoint, type Camera } from "./geometry";
import type { ChamberRow, TorusParams } from "./sceneLayout";

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
  /** 0..1 directional-light shade for this tile's facing (the "specular gradient" hotspot from the projected normal). */
  readonly shade: number;
  /** 0..1 -- 1 where this tile's row sits right at the plasma torus, 0 far from it. Drives both the brightness boost and the coral tint: "brighter only near the plasma" (planner ruling 187 item 2). */
  readonly warmth: number;
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
/**
 * The column's own seam gap is narrower than the wall's (hc-0-wrc.3 review
 * 2, F1 residual): its own floor alpha is already lower than the wall's, so
 * a full-width seam gap lets a disproportionate amount of the (comparably
 * brighter) wall show through behind it, pulling the column's measured
 * mean luminance up above the wall's own instead of below it. Seams still
 * read as gaps, never strokes -- just narrower ones on the column.
 */
const COLUMN_SEAM_INSET = SEAM_INSET * 0.55;
const LIGHT_DIR = normalize3({ x: 0.4, y: 0.7, z: -0.6 });
/**
 * The column's near half happens to face `LIGHT_DIR` almost head-on (its
 * outward normal has `z < 0`, matching `LIGHT_DIR.z`), so the raw dot-based
 * shade below would make the column the single brightest surface in the
 * chamber -- the "pale glass cylinder" the reviewer measured at 0.153 mean
 * luminance against the wall's 0.113-0.137 (hc-0-wrc.3 review 2, F1/F4).
 * Scaling the column's shade down to this ceiling (relative to the wall's
 * own 0..1 range) keeps its directional specular variation while landing at
 * or below the wall's own achieved brightness.
 */
const COLUMN_SHADE_CEILING = 0.15;

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
  baseInset: number,
): readonly [Pt, Pt, Pt, Pt] {
  const cx = (p[0].x + p[1].x + p[2].x + p[3].x) / 4;
  const cy = (p[0].y + p[1].y + p[2].y + p[3].y) / 4;
  const center: Pt = { x: cx, y: cy };
  // Small, far-away tiles get a much smaller fractional inset so the seam
  // gap never swallows the tile; the seam still reads as a hairline gap,
  // never a stroke.
  const inset = sizePx < 10 ? baseInset * 0.35 : baseInset;
  return [
    lerpPt(p[0], center, inset),
    lerpPt(p[1], center, inset),
    lerpPt(p[2], center, inset),
    lerpPt(p[3], center, inset),
  ];
}

/**
 * How close a point sits to the plasma torus, in world units, folded into a
 * smoothstepped 0..1 falloff: 1 right at the band, 0 a few tube-radii away.
 * Distance blends the radius gap to the torus' major radius with the height
 * gap to the torus' own height, so only points that are both near the
 * column's radius AND near the plasma's height light up -- the column
 * lights up at the band, the wide outer wall (whose radius is nowhere near
 * the torus') stays dark, matching "brighter only near the plasma". Takes
 * the tile's own mid `y`/`radius` (not a per-row max) and smoothsteps the
 * falloff so neighbouring tiles never jump between two discrete states --
 * fix 2's correction for the hard rim ellipse this produced as a per-row max.
 */
function tileWarmth(y: number, radius: number, torus: TorusParams): number {
  const dr = radius - torus.R;
  const dy = y - torus.y;
  const dist = Math.hypot(dr, dy);
  const falloff = torus.a * 2.1;
  const t = clamp(1 - dist / falloff, 0, 1);
  return t * t * (3 - 2 * t);
}

/**
 * A cylinder tile's outward normal, dotted against the camera's own forward
 * view direction (from `cosTilt`/`sinTilt`, the world x=0 on-axis camera
 * this scene always uses): positive when the normal points the same way the
 * camera looks (a back face on a convex object viewed from outside),
 * negative when it points back toward the camera (a front face).
 */
function facingAway(midTheta: number, camera: Camera): boolean {
  const normal = { x: Math.cos(midTheta), y: 0, z: Math.sin(midTheta) };
  const forward = { x: 0, y: camera.sinTilt, z: camera.cosTilt };
  return dot3(normal, forward) >= 0;
}

/**
 * The static chamber: a perspective grid of tiles following elliptical
 * rings around the column, one trapezoid per (row, theta-segment) cell.
 * Each tile's silhouette comes from projecting real 3D corners (never a
 * flat 2D grid), its shade from a directional light dotted against the
 * cylinder's local outward normal (the "subtle specular gradient"), its
 * warmth from its distance to the plasma torus, and seams are the gap left
 * by insetting each tile toward its own centre -- never a stroked edge.
 * Built once per `measure()` (mount + resize), never per frame. Called once
 * for the column's rows and once for the wall's rows; the results are
 * concatenated by the caller.
 *
 * `kind` back-face-culls each tile by its outward normal against the
 * camera's own forward direction: for the `"column"` (a convex cylinder
 * seen from outside-in, i.e. from inside the vessel looking at it) only the
 * near half -- tiles whose normal faces back toward the camera -- is kept;
 * for the `"wall"` (the concave inside of a far larger cylinder wrapping
 * the viewer) only the far/inner half -- tiles whose normal faces the same
 * way the camera looks -- is kept, since that is the half whose concave
 * face the viewer is actually inside of. Without this, both faces of both
 * cylinders painted on top of each other read as one pale, doubled-up
 * silhouette instead of a dark column standing in front of a wrapping wall.
 */
export function buildChamberTiles(
  rows: readonly ChamberRow[],
  thetaSegments: number,
  camera: Camera,
  torus: TorusParams,
  kind: "column" | "wall",
): Tile[] {
  const tiles: Tile[] = [];
  for (let r = 0; r < rows.length - 1; r++) {
    const rowA = rows[r];
    const rowB = rows[r + 1];
    const warmth = tileWarmth(
      (rowA.y + rowB.y) / 2,
      (rowA.radius + rowB.radius) / 2,
      torus,
    );
    // Stagger alternate column rows by half a theta segment (a brick-style
    // offset) so each row's seam gaps land between the row above/below's
    // tile faces instead of stacking into one continuous vertical line the
    // full height of the column (hc-0-wrc.3 review 2, F1: "seams stack into
    // full-height vertical lines"). Wall rows are unaffected -- their seams
    // were never the reported issue.
    const rowStagger =
      kind === "column" && r % 2 === 1 ? 0.5 / thetaSegments : 0;
    for (let s = 0; s < thetaSegments; s++) {
      const t0 = (s / thetaSegments + rowStagger) * Math.PI * 2;
      const t1 = ((s + 1) / thetaSegments + rowStagger) * Math.PI * 2;
      const midTheta = (t0 + t1) / 2;
      const away = facingAway(midTheta, camera);
      if (kind === "column" ? away : !away) {
        continue;
      }
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
      const poly = insetQuad(
        [c0, c1, c2, c3],
        size,
        kind === "column" ? COLUMN_SEAM_INSET : SEAM_INSET,
      );
      const normal = { x: Math.cos(midTheta), y: 0, z: Math.sin(midTheta) };
      const rawShade = clamp(dot3(normal, LIGHT_DIR) * 0.5 + 0.5, 0.16, 1);
      const shade =
        kind === "column" ? rawShade * COLUMN_SHADE_CEILING : rawShade;
      tiles.push({
        poly,
        hi: poly[0],
        lo: poly[2],
        shade,
        warmth,
        fastener: size > 18 && (r * thetaSegments + s) % 3 === 0,
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
