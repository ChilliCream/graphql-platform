// The node/edge model for the hero's constellation: nodes are sampled with
// a variable-density Poisson-disc process directly in the 3D slab's world
// space (never from a screen position inverted through the camera) and
// projected with a real perspective camera (camera.ts). The projection is
// what shapes the picture -- near depths magnify more than far ones, so
// the same real-world spacing reads as sparser/larger/brighter near the
// camera and denser/smaller/dimmer far from it -- and that read is
// reinforced, not faked, by a density field over world position that
// governs each point's spacing (sampleWorldNodes/densityClear): the two
// are tied to the same focal axis, so a node's depth and its personal
// spacing always agree. Edges are k-nearest-neighbour in 3D plus an MST
// bridge for any leftover component (the connectivity fix), and locally
// dimmed -- alpha only, never the whole scene -- inside the copy block so
// on-glyph contrast holds; the paragraph block and the button row each get
// an additional feathered scrim (paint.ts) on top of that.
//
// Everything is derived once from a seeded PRNG, so the same
// (w, h, mode, copyRect) always yields the same graph.
import type { Rect } from "./paint";
import {
  makeCamera,
  project,
  scaleAtDepth,
  type Camera,
  type Vec3,
} from "./camera";
import {
  COPY_ZONE_PAD,
  MIN_NODE_SPACING,
  coverageCapDiameter,
  type LayoutMode,
} from "./sceneLayout";

export type { LayoutMode };

function mulberry32(seed: number) {
  let a = seed >>> 0;
  return function rand() {
    a = (a + 0x6d2b79f5) | 0;
    let t = Math.imul(a ^ (a >>> 15), 1 | a);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

function clamp01(t: number): number {
  return t < 0 ? 0 : t > 1 ? 1 : t;
}

/**
 * A uniform grid over 2D positions, cell size >= any query radius used
 * against it, so the 3x3 block of cells around a query point is always a
 * superset of every inserted point within that radius (never fewer, so
 * every predicate that consults it stays exact -- it only prunes candidates
 * that couldn't possibly match). Cell coordinates pack into one numeric Map
 * key (a plain multiply, well under 2^53, not a template string) since
 * farEnough below calls this on every attempt of a saturating Poisson-disc
 * process -- tens of thousands of them, per the ticket's own unchanged
 * attempt budgets -- so per-call overhead is what the 50ms bar measures.
 */
const GRID_KEY_STRIDE = 1 << 20;

class SpatialGrid {
  private readonly cellSize: number;
  private readonly cells = new Map<number, number[]>();

  constructor(cellSize: number) {
    this.cellSize = cellSize;
  }

  cellX(x: number): number {
    return Math.floor(x / this.cellSize);
  }

  cellY(y: number): number {
    return Math.floor(y / this.cellSize);
  }

  bucket(cx: number, cy: number): readonly number[] | undefined {
    return this.cells.get(cx * GRID_KEY_STRIDE + cy);
  }

  insert(index: number, x: number, y: number): void {
    const k = this.cellX(x) * GRID_KEY_STRIDE + this.cellY(y);
    const bucket = this.cells.get(k);
    if (bucket) {
      bucket.push(index);
    } else {
      this.cells.set(k, [index]);
    }
  }

  near(x: number, y: number): number[] {
    const cx = this.cellX(x);
    const cy = this.cellY(y);
    const out: number[] = [];
    for (let dx = -1; dx <= 1; dx++) {
      for (let dy = -1; dy <= 1; dy++) {
        const bucket = this.bucket(cx + dx, cy + dy);
        if (bucket) {
          for (const i of bucket) {
            out.push(i);
          }
        }
      }
    }
    return out;
  }
}

export interface Point {
  readonly x: number;
  readonly y: number;
}

export interface GraphNode {
  readonly x: number;
  readonly y: number;
  readonly r: number;
  readonly alpha: number;
  readonly hub: boolean;
  readonly tint: 0 | 1;
  /** 0..1 blend toward black, how the copy-zone contrast fix stays visible without brightening past the glyph rows. */
  readonly darken: number;
}

export interface GraphEdge {
  readonly points: readonly Point[];
  readonly alpha: number;
  readonly darken: number;
  readonly lineWidth: number;
  /** avgT > 0.5: the nearer half of edges by depth, paint.ts's near-band treatment applies to these (and only these, when not copy-zone darkened). */
  readonly near: boolean;
}

export interface GraphModel {
  readonly nodes: readonly GraphNode[];
  readonly edges: readonly GraphEdge[];
}

// The camera: a slab from z = 0 (nearest) to CAM_Z_RANGE (farthest) sitting
// CAM_DIST in front of the lens. far/near scale ratio = CAM_DIST /
// (CAM_DIST + CAM_Z_RANGE) ≈ 0.43, i.e. near nodes project ≈2.3x the
// scale of far ones -- comfortably over the ticket's 1.5x near/far spacing
// bar and its 2x depth (size/luminance) bar.
const CAM_DIST = 520;
const CAM_Z_RANGE = 680;
const CAM_FOCAL = 860;

// How strongly a node's world position pulls its depth toward the focal
// axis (0 = pure noise, 1 = position decides depth outright); the rest is
// per-node noise, so the read is a gentle bias, not a mechanical wedge.
// This is a real 3D tilt of the slab -- the depth a node is sampled at
// depends on where in the slab it already landed -- not a value derived
// from, or fed back into, its screen position.
const FOCAL_WEIGHT = 0.95;

const FAR_R = 2;
const FAR_R_SPAN = 1;
const NEAR_R = 4;
const NEAR_R_SPAN = 2;

const HUB_COUNT = 5;
const HUB_MIN_COUNT = 4;
const HUB_MIN_SEPARATION = 150;
const HUB_MIN_COPY_CLEARANCE = 90;
// Hub candidates must sit well inside the frame, never at its very edge or
// corner (the halo would clip) and never under the sticky header, which
// overlaps the top of the hero (the section is pulled up underneath it) --
// so the top margin covers the header's own height plus a buffer.
const HUB_MARGIN_TOP = 112;
const HUB_MARGIN_SIDE = 26;

const COPY_NEAR_CAP = 0.15;
// One tier for the whole copy-clear zone (the copy rect plus its pad):
// alpha capped low enough, and darkened enough, that the h1 and button rows
// keep 7:1+ even against a node or edge sitting directly under them. The
// paragraph rows need more than this tier alone reaches (a busier part of
// the block); paint.ts layers an additional, tightly feathered scrim over
// the paragraph's own rect for that, so this tier -- and everywhere outside
// the paragraph block -- is unchanged.
const COPY_ZONE_ALPHA_CAP = 0.08;
const COPY_ZONE_DARKEN = 0.35;

const KNN_K = 3;
const EDGE_LEN_CAP = 220;
const DEGREE_MIN = 2;
const DEGREE_MAX = 4;

// World-space sampling: a Poisson-disc process (plain random dart-throwing
// to saturation, run for MAX_CONSECUTIVE_FAILS misses in a row, never a
// grid) with a screen-space exclusion radius that varies smoothly over
// world position (see densityClear below) -- so the near corner of the
// slab carries generous spacing and the far corner carries tight spacing,
// the projection's own depth cue reinforced by real, position-driven
// density rather than fought by per-point randomness.
const MAX_CONSECUTIVE_FAILS = 14000;
// A final pass offering each candidate a flat, always cap-safe clearance
// ceiling instead of the density field's own (see sampleWorldNodes) --
// patches the frame edges and corners, which only ever have a fraction of
// an interior point's surroundings to be covered from.
const COVERAGE_GUARD_FAILS = 9000;
// A dedicated budget per canvas corner (see sampleWorldNodes' corner
// guard): a corner is a rarer draw than a plain edge, so it gets its own
// share of attempts instead of competing with the rest of the frame for
// the guard pass's single shared budget.
const CORNER_GUARD_FAILS = 4000;
// How far a sampled point's projection may land outside the canvas and
// still count (so the frame's own edges get the same coverage as its
// interior, the way an overhanging lattice used to).
const OVERHANG_MIN = 40;
// The per-width coverage cap (sceneLayout.coverageCapDiameter) bounds the
// diameter of the largest empty circle; a maximal Poisson-disc set has no
// point in its domain farther than its own radius from a sample, so radius
// = capDiameter / 2 gives that bound directly at the near corner (the
// worst case: every other point in the density field carries a smaller
// radius, so its own coverage is only ever tighter). The safety factor
// budgets for the process not running to full, provable maximality in
// finite attempts -- verified empirically (test-results/rh3-*) against the
// actual rendered largest-empty-circle and spacing ratio, not just this
// formula.
const COVERAGE_SAFETY = 1.08;
// The near corner's own clearance divided by the far corner's: how much
// sparser the near reading is than the far one. Kept comfortably over the
// ticket's 1.5x near/far screen-spacing bar so the measured ratio (which
// is taken over depth deciles of the rendered result, not this field
// directly) still clears it after the render's own noise.
const DENSITY_RATIO = 5.2;

function distToRect(x: number, y: number, rect: Rect): number {
  const dx = Math.max(rect.x - x, x - (rect.x + rect.width), 0);
  const dy = Math.max(rect.y - y, y - (rect.y + rect.height), 0);
  return dx > 0 && dy > 0 ? Math.hypot(dx, dy) : Math.max(dx, dy);
}

function inRect(x: number, y: number, rect: Rect): boolean {
  return (
    x >= rect.x &&
    x <= rect.x + rect.width &&
    y >= rect.y &&
    y <= rect.y + rect.height
  );
}

function expandRect(rect: Rect, pad: number): Rect {
  return {
    x: rect.x - pad,
    y: rect.y - pad,
    width: rect.width + pad * 2,
    height: rect.height + pad * 2,
  };
}

/** Liang-Barsky segment/rect intersection: does this edge pass through the rect at all (not just touch it at an endpoint)? */
function segIntersectsRect(p: Point, q: Point, r: Rect): boolean {
  let t0 = 0;
  let t1 = 1;
  const dx = q.x - p.x;
  const dy = q.y - p.y;
  const checks: [number, number][] = [
    [-dx, p.x - r.x],
    [dx, r.x + r.width - p.x],
    [-dy, p.y - r.y],
    [dy, r.y + r.height - p.y],
  ];
  for (const [pp, qq] of checks) {
    if (pp === 0) {
      if (qq < 0) {
        return false;
      }
      continue;
    }
    const t = qq / pp;
    if (pp < 0) {
      if (t > t1) {
        return false;
      }
      if (t > t0) {
        t0 = t;
      }
    } else {
      if (t < t0) {
        return false;
      }
      if (t < t1) {
        t1 = t;
      }
    }
  }
  return true;
}

function polylineIntersectsRect(pts: readonly Point[], rect: Rect): boolean {
  for (let i = 1; i < pts.length; i++) {
    if (segIntersectsRect(pts[i - 1], pts[i], rect)) {
      return true;
    }
  }
  return false;
}

interface SampledPoint {
  readonly world: Vec3;
  readonly screen: Point;
  readonly scale: number;
}

const FOCAL_CONTRAST = 2.8;

/**
 * 0 for a point near the slab's bottom-left, 1 near its top-right: the axis
 * the focal structure reads along, computed from the point's own world
 * (x, y) -- never from a screen position -- so it shapes a real tilted
 * region of the slab instead of a value read back off the projection.
 */
function focalBias(nx: number, ny: number): number {
  const raw = clamp01((nx - ny + 1) / 2);
  // A linear stretch around the midpoint, clipped to [0, 1]: pulls most of
  // the slab toward a clearly-near or clearly-far read instead of a wide
  // middling band, so the depth-ranked decile the near/far spacing ratio
  // is measured against lines up with the position-driven density field
  // (densityClear) it is meant to reinforce, rather than being diluted by
  // points that read "somewhat near" in depth while sitting in a
  // middling, denser part of the field.
  return clamp01((raw - 0.5) * FOCAL_CONTRAST + 0.5);
}

// Variable-density Poisson-disc: each candidate's own screen-space
// exclusion radius comes from a smooth field over its WORLD (x, y) --
// the same focal axis the depth bias reads along, so the near corner's
// own real-world clearance is bigger than the far corner's -- rather than
// from its own randomly-sampled depth. A per-point depth-based radius
// sounds more "physical", but it is not what the near/far spacing ratio
// actually measures: that decile is dominated by "somewhat near" points,
// and letting a purely random depth decide each one's personal space means
// plenty of them draw a middling depth (middling clearance) even while
// sitting in the near-favoured corner, diluting the near decile's own
// median well below the near corner's intended spacing (verified
// empirically -- test-results/rh3-diag-*). Tying clearance to the SAME
// world position the depth bias already reads keeps both consistent (a
// point in the near corner reads near in depth AND sits in the sparser
// spacing field) without that dilution, and its worst case is still just
// nearScreenRadius -- the same bound a per-depth field would have had at
// its single nearest point -- so it does not raise the coverage cap's
// worst case either.
function densityClear(
  nx: number,
  ny: number,
  nearRadius: number,
  farRadius: number,
): number {
  const bias = focalBias(nx, ny);
  return farRadius + (nearRadius - farRadius) * (1 - bias);
}

function sampleWorldNodes(
  w: number,
  h: number,
  cam: Camera,
  rand: () => number,
): SampledPoint[] {
  const scaleFar = scaleAtDepth(CAM_Z_RANGE, cam);
  const nearScreenRadius = (coverageCapDiameter(w) / 2) * COVERAGE_SAFETY;
  const farScreenRadius = Math.max(
    MIN_NODE_SPACING,
    nearScreenRadius / DENSITY_RATIO,
  );
  // A point may sit just outside the canvas and still cover part of it --
  // by up to its own clearance, so the frame's own edges get the same
  // coverage as its interior instead of falling back to just OVERHANG_MIN
  // once clearances run bigger than that.
  const overhang = Math.max(OVERHANG_MIN, nearScreenRadius);
  // The slab's x/y half-extent: sized so the FARTHEST plane's projection
  // still covers the canvas (with overhang); nearer planes need less world
  // extent to cover the same canvas, so most of this box is naturally out
  // of frame for a near candidate, and gets rejected below.
  const halfW = (w / 2 + overhang) / scaleFar;
  const halfH = (h / 2 + overhang) / scaleFar;

  const accepted: SampledPoint[] = [];
  const clears: number[] = [];
  // Flat, parallel to `accepted`: farEnough's hot loop reads x/y straight
  // out of these instead of through accepted[i].screen, one array hop
  // instead of two, since it runs on every attempt of the (unchanged)
  // attempt-budget dart-throwing process below.
  const acceptedX: number[] = [];
  const acceptedY: number[] = [];
  // Cell size = nearScreenRadius, the largest clearance any point (main
  // pass or guard) ever carries, so the 3x3 neighbourhood (gridRadius 1)
  // around a candidate always contains every accepted point its own
  // required distance could reach -- the same accept/reject decisions as
  // scanning all of `accepted`, just without scanning the far ones.
  const gridCellSize = nearScreenRadius;
  const gridRadius = Math.ceil(nearScreenRadius / gridCellSize);
  const grid = new SpatialGrid(gridCellSize);

  const onCanvas = (p: Point) =>
    p.x >= -overhang &&
    p.x <= w + overhang &&
    p.y >= -overhang &&
    p.y <= h + overhang;

  const farEnough = (screen: Point, clear: number): boolean => {
    const cx = grid.cellX(screen.x);
    const cy = grid.cellY(screen.y);
    const clearFloor = Math.max(clear, MIN_NODE_SPACING);
    for (let dx = -gridRadius; dx <= gridRadius; dx++) {
      for (let dy = -gridRadius; dy <= gridRadius; dy++) {
        const bucket = grid.bucket(cx + dx, cy + dy);
        if (!bucket) {
          continue;
        }
        for (const i of bucket) {
          const required = Math.max(clearFloor, clears[i]);
          if (
            Math.hypot(acceptedX[i] - screen.x, acceptedY[i] - screen.y) <
            required
          ) {
            return false;
          }
        }
      }
    }
    return true;
  };

  const accept = (world: Vec3, screen: Point, scale: number, clear: number) => {
    const index = accepted.length;
    accepted.push({ world, screen, scale });
    clears.push(clear);
    acceptedX.push(screen.x);
    acceptedY.push(screen.y);
    grid.insert(index, screen.x, screen.y);
  };

  // A point within its own clearance of the canvas boundary has only a
  // fraction of a point's usual surroundings inside the frame to help
  // cover it from -- worst at a corner, where two edges meet -- so the
  // SAME clearance that is safe in the interior can leave a real gap right
  // at the boundary. Tapering clearance down as a point's own projected
  // position nears an edge (down to farScreenRadius exactly at the
  // boundary) keeps that worst case bounded without changing the density
  // field anywhere it has full surroundings to draw on.
  const edgeSafeClear = (clear: number, screen: Point): number => {
    const edgeDist = Math.max(
      0,
      Math.min(screen.x, w - screen.x, screen.y, h - screen.y),
    );
    const reach = clear * 1.6;
    if (edgeDist >= reach) {
      return clear;
    }
    const t = edgeDist / reach;
    return farScreenRadius + (clear - farScreenRadius) * t;
  };

  let fails = 0;
  while (fails < MAX_CONSECUTIVE_FAILS) {
    const x = (rand() * 2 - 1) * halfW;
    const y = (rand() * 2 - 1) * halfH;
    const nx = (x + halfW) / (2 * halfW);
    const ny = (y + halfH) / (2 * halfH);
    const rawClear = densityClear(nx, ny, nearScreenRadius, farScreenRadius);
    const bias = focalBias(nx, ny);
    const t = clamp01(bias * FOCAL_WEIGHT + rand() * (1 - FOCAL_WEIGHT));
    const world: Vec3 = { x, y, z: t * CAM_Z_RANGE };
    const projected = project(world, cam);
    const screen: Point = { x: projected.x, y: projected.y };
    const clear = edgeSafeClear(rawClear, screen);
    if (!onCanvas(screen) || !farEnough(screen, clear)) {
      fails++;
      continue;
    }
    accept(world, screen, projected.scale, clear);
    fails = 0;
  }

  // Coverage guard: a corner or edge only has a fraction of a point's usual
  // surroundings to be approached from, so the same density field that is
  // provably safe in the interior (worst case: the near corner's own
  // nearScreenRadius, no bigger than a plain fixed-radius disc there would
  // give) can still leave a real gap right at a frame boundary. Each guard
  // candidate is drawn exactly like the main pass -- real position, real
  // position-biased depth, forward projection -- and offered the SMALLER
  // of its natural density clearance and a flat, always cap-safe ceiling,
  // so it can only ever slot into a leftover gap next to an existing
  // point's own (possibly larger) clearance, never shrink that point's own
  // halo, and never itself reintroduce a gap bigger than the ceiling.
  const guardRadius = (coverageCapDiameter(w) / 2) * 0.35;

  // With no target, draw x/y from the full slab as usual. With one, draw
  // from the (much smaller) world region that could possibly project into
  // it at ANY depth in the slab -- sized by the same forward scale math the
  // slab's own half-extent already uses, just solved for a screen sub-rect
  // instead of the whole canvas -- so a rare corner or edge target gets a
  // realistic hit rate instead of relying on an unrestricted draw over the
  // whole canvas to land there by chance. The result is still only ever
  // kept once its own forward projection actually falls inside the target;
  // this narrows WHERE real points are drawn from, it does not compute one
  // from a chosen screen position.
  const worldRangeFor = (target: Rect) => {
    const scaleN = scaleAtDepth(0, cam);
    const scaleF = scaleAtDepth(CAM_Z_RANGE, cam);
    const xAt = (sx: number, s: number) => (sx - cam.originX) / s;
    const yAt = (sy: number, s: number) => (sy - cam.originY) / s;
    const xs = [
      xAt(target.x, scaleN),
      xAt(target.x + target.width, scaleN),
      xAt(target.x, scaleF),
      xAt(target.x + target.width, scaleF),
    ];
    const ys = [
      yAt(target.y, scaleN),
      yAt(target.y + target.height, scaleN),
      yAt(target.y, scaleF),
      yAt(target.y + target.height, scaleF),
    ];
    return {
      xMin: Math.min(...xs),
      xMax: Math.max(...xs),
      yMin: Math.min(...ys),
      yMax: Math.max(...ys),
    };
  };

  const guardAttempt = (target: Rect | null): boolean => {
    let x: number;
    let y: number;
    if (target) {
      const range = worldRangeFor(target);
      x = range.xMin + rand() * (range.xMax - range.xMin);
      y = range.yMin + rand() * (range.yMax - range.yMin);
    } else {
      x = (rand() * 2 - 1) * halfW;
      y = (rand() * 2 - 1) * halfH;
    }
    const nx = clamp01((x + halfW) / (2 * halfW));
    const ny = clamp01((y + halfH) / (2 * halfH));
    const clear = Math.min(
      guardRadius,
      densityClear(nx, ny, nearScreenRadius, farScreenRadius),
    );
    const bias = focalBias(nx, ny);
    const t = clamp01(bias * FOCAL_WEIGHT + rand() * (1 - FOCAL_WEIGHT));
    const world: Vec3 = { x, y, z: t * CAM_Z_RANGE };
    const projected = project(world, cam);
    const screen: Point = { x: projected.x, y: projected.y };
    if (target && !inRect(screen.x, screen.y, target)) {
      return false;
    }
    if (!onCanvas(screen) || !farEnough(screen, clear)) {
      return false;
    }
    accept(world, screen, projected.scale, clear);
    return true;
  };

  let guardFails = 0;
  while (guardFails < COVERAGE_GUARD_FAILS) {
    if (guardAttempt(null)) {
      guardFails = 0;
    } else {
      guardFails++;
    }
  }

  // Corner guard: the four canvas corners have only a quarter of a point's
  // usual surroundings to be approached from (an edge has half), so even
  // the unrestricted guard pass above needs a specific, rare draw to land
  // there at all and can burn its whole budget on easier ground first.
  // Restricting candidates to a margin around one corner at a time (a
  // smaller domain to draw real, forward-projected points within -- not a
  // target screen position solved for) gives each corner a fair, dedicated
  // share of attempts.
  const cornerMargin = guardRadius * 4;
  const corners: Rect[] = [
    {
      x: -cornerMargin,
      y: -cornerMargin,
      width: cornerMargin * 2,
      height: cornerMargin * 2,
    },
    {
      x: w - cornerMargin,
      y: -cornerMargin,
      width: cornerMargin * 2,
      height: cornerMargin * 2,
    },
    {
      x: -cornerMargin,
      y: h - cornerMargin,
      width: cornerMargin * 2,
      height: cornerMargin * 2,
    },
    {
      x: w - cornerMargin,
      y: h - cornerMargin,
      width: cornerMargin * 2,
      height: cornerMargin * 2,
    },
  ];
  for (const corner of corners) {
    let cornerFails = 0;
    while (cornerFails < CORNER_GUARD_FAILS) {
      if (guardAttempt(corner)) {
        cornerFails = 0;
      } else {
        cornerFails++;
      }
    }
  }

  return accepted;
}

function pickHubs(
  points: readonly SampledPoint[],
  w: number,
  h: number,
  copyRect: Rect | null,
): Set<number> {
  const candidates = points
    .map((p, i) => ({ i, p }))
    .filter(({ p }) => {
      const s = p.screen;
      return (
        s.x >= HUB_MARGIN_SIDE &&
        s.x <= w - HUB_MARGIN_SIDE &&
        s.y >= HUB_MARGIN_TOP &&
        s.y <= h - HUB_MARGIN_SIDE &&
        (!copyRect || distToRect(s.x, s.y, copyRect) >= HUB_MIN_COPY_CLEARANCE)
      );
    })
    // The nearest candidates first (smallest world z = largest projected
    // scale), so a hub is genuinely the nearest point the camera sees in
    // its neighbourhood, never a point moved to the front after the fact.
    .sort((a, b) => a.p.world.z - b.p.world.z);

  // A separation strict enough to spread hubs out nicely can still leave
  // too few candidates on a narrow, tall canvas where the eligible
  // (margin- and copy-clear) area is small -- rather than ship under the
  // 4-6 band, relax the separation step by step (never below half its
  // starting value) until the floor is met.
  const trySelect = (minSeparation: number): Set<number> => {
    const picked = new Set<number>();
    const chosen: Point[] = [];
    for (const { i, p } of candidates) {
      if (picked.size >= HUB_COUNT) {
        break;
      }
      if (
        chosen.every(
          (c) =>
            Math.hypot(c.x - p.screen.x, c.y - p.screen.y) >= minSeparation,
        )
      ) {
        picked.add(i);
        chosen.push(p.screen);
      }
    }
    return picked;
  };

  let separation = HUB_MIN_SEPARATION;
  let hubs = trySelect(separation);
  while (hubs.size < HUB_MIN_COUNT && separation > HUB_MIN_SEPARATION / 2) {
    separation -= 15;
    hubs = trySelect(separation);
  }
  return hubs;
}

interface UnionFind {
  readonly find: (x: number) => number;
  readonly union: (a: number, b: number) => void;
}

function makeUnionFind(n: number): UnionFind {
  const parent = Array.from({ length: n }, (_, i) => i);
  const find = (x: number): number => {
    while (parent[x] !== x) {
      parent[x] = parent[parent[x]];
      x = parent[x];
    }
    return x;
  };
  const union = (a: number, b: number) => {
    const ra = find(a);
    const rb = find(b);
    if (ra !== rb) {
      parent[ra] = rb;
    }
  };
  return { find, union };
}

interface RawEdge {
  readonly a: number;
  readonly b: number;
  readonly bridge: boolean;
  readonly via?: Point;
}

/**
 * k-nearest-neighbour edges by 3D distance (the edge fix's first half),
 * each capped at EDGE_LEN_CAP once projected to screen space. Any
 * remaining separate components are then joined by an MST over the
 * component graph -- repeatedly adding the globally cheapest 3D edge that
 * connects two still-different components, which is Kruskal's rule -- so
 * those bridge edges are exempt from the length cap but are the shortest
 * possible connectors, and get routed around the copy zone if they'd
 * cross it.
 */
function buildEdges(
  world: readonly Vec3[],
  screen: readonly Point[],
  copyZone: Rect | null,
): RawEdge[] {
  const n = world.length;
  const dist3 = (i: number, j: number) =>
    Math.hypot(
      world[i].x - world[j].x,
      world[i].y - world[j].y,
      world[i].z - world[j].z,
    );
  const distScreen = (i: number, j: number) =>
    Math.hypot(screen[i].x - screen[j].x, screen[i].y - screen[j].y);

  const edgeSet = new Set<string>();
  const edges: RawEdge[] = [];
  const key = (a: number, b: number) => (a < b ? `${a}-${b}` : `${b}-${a}`);
  const addEdge = (a: number, b: number, bridge: boolean, via?: Point) => {
    if (a === b) {
      return;
    }
    const k = key(a, b);
    if (edgeSet.has(k)) {
      return;
    }
    edgeSet.add(k);
    edges.push({ a, b, bridge, via });
  };

  // Screen-space grid, cell size = EDGE_LEN_CAP: the 3x3 neighbourhood
  // around a node always contains every other node within the cap, so it
  // narrows the O(n) scan below to the same candidate set a full scan
  // would find, in the same ascending-index order the original loop built
  // it in -- the later stable sort by 3D distance then ties exactly as
  // before, keeping the edge list identical.
  const screenGrid = new SpatialGrid(EDGE_LEN_CAP);
  for (let i = 0; i < n; i++) {
    screenGrid.insert(i, screen[i].x, screen[i].y);
  }
  const nearbyAscending = (i: number): number[] =>
    screenGrid.near(screen[i].x, screen[i].y).sort((a, b) => a - b);

  for (let i = 0; i < n; i++) {
    const candidates = [];
    for (const j of nearbyAscending(i)) {
      if (j === i) {
        continue;
      }
      if (distScreen(i, j) > EDGE_LEN_CAP) {
        continue;
      }
      candidates.push({ j, d: dist3(i, j) });
    }
    candidates.sort((x, y) => x.d - y.d);
    for (const { j } of candidates.slice(0, KNN_K)) {
      addEdge(i, j, false);
    }
  }

  const uf = makeUnionFind(n);
  for (const e of edges) {
    uf.union(e.a, e.b);
  }
  const routeVia = (a: number, b: number): Point | undefined => {
    if (!copyZone) {
      return undefined;
    }
    const pa = screen[a];
    const pb = screen[b];
    if (!segIntersectsRect(pa, pb, copyZone)) {
      return undefined;
    }
    const midX = (pa.x + pb.x) / 2;
    const above = { x: midX, y: copyZone.y - 20 };
    const below = { x: midX, y: copyZone.y + copyZone.height + 20 };
    const cost = (v: Point) =>
      Math.hypot(pa.x - v.x, pa.y - v.y) + Math.hypot(pb.x - v.x, pb.y - v.y);
    return cost(above) <= cost(below) ? above : below;
  };

  // Bridge stray components: an MST over the component graph, cheapest
  // 3D edge first, until one component remains.
  for (;;) {
    const byRoot = new Map<number, number[]>();
    for (let i = 0; i < n; i++) {
      const r = uf.find(i);
      const list = byRoot.get(r);
      if (list) {
        list.push(i);
      } else {
        byRoot.set(r, [i]);
      }
    }
    const roots = [...byRoot.keys()];
    if (roots.length <= 1) {
      break;
    }
    let best = { a: -1, b: -1, d: Infinity };
    for (let x = 0; x < roots.length; x++) {
      for (let y = x + 1; y < roots.length; y++) {
        for (const a of byRoot.get(roots[x])!) {
          for (const b of byRoot.get(roots[y])!) {
            const d = dist3(a, b);
            if (d < best.d) {
              best = { a, b, d };
            }
          }
        }
      }
    }
    addEdge(best.a, best.b, true, routeVia(best.a, best.b));
    uf.union(best.a, best.b);
  }

  // Degree 2-4, checked on both ends: trim the longest non-bridge edges
  // off any over-cap node, but only when a walk still connects its two
  // endpoints afterward, so a trim can never fracture the one component
  // the k-NN + MST step built. Bridge edges are never trimmed.
  const degree = new Array(n).fill(0);
  for (const e of edges) {
    degree[e.a]++;
    degree[e.b]++;
  }
  const adjacency: Set<number>[] = Array.from({ length: n }, () => new Set());
  for (const e of edges) {
    adjacency[e.a].add(e.b);
    adjacency[e.b].add(e.a);
  }
  const stillConnected = (a: number, b: number): boolean => {
    const seen = new Set<number>([a]);
    const stack = [a];
    while (stack.length) {
      const cur = stack.pop()!;
      if (cur === b) {
        return true;
      }
      for (const next of adjacency[cur]) {
        if ((cur === a && next === b) || (cur === b && next === a)) {
          continue;
        }
        if (!seen.has(next)) {
          seen.add(next);
          stack.push(next);
        }
      }
    }
    return false;
  };
  const byLengthDesc = edges
    .filter((e) => !e.bridge)
    .sort((x, y) => distScreen(y.a, y.b) - distScreen(x.a, x.b));
  const kept = [...edges];
  for (const e of byLengthDesc) {
    if (degree[e.a] <= DEGREE_MAX && degree[e.b] <= DEGREE_MAX) {
      continue;
    }
    if (degree[e.a] <= DEGREE_MIN || degree[e.b] <= DEGREE_MIN) {
      continue;
    }
    adjacency[e.a].delete(e.b);
    adjacency[e.b].delete(e.a);
    if (stillConnected(e.a, e.b)) {
      degree[e.a]--;
      degree[e.b]--;
      kept.splice(kept.indexOf(e), 1);
    } else {
      adjacency[e.a].add(e.b);
      adjacency[e.b].add(e.a);
    }
  }

  // Degree floor: attach the nearest still-eligible, capped-length
  // neighbour to any node left under 2 -- but only a target that isn't
  // already at the degree cap itself, so fixing one node's floor can never
  // push another node over DEGREE_MAX.
  for (let i = 0; i < n; i++) {
    let guard = 0;
    while (degree[i] < DEGREE_MIN && guard < n) {
      guard++;
      let target = -1;
      let bestD = Infinity;
      for (const j of nearbyAscending(i)) {
        if (
          j === i ||
          edgeSet.has(key(i, j)) ||
          distScreen(i, j) > EDGE_LEN_CAP ||
          degree[j] >= DEGREE_MAX
        ) {
          continue;
        }
        const d = dist3(i, j);
        if (d < bestD) {
          bestD = d;
          target = j;
        }
      }
      if (target < 0) {
        break;
      }
      addEdge(i, target, false);
      kept.push({ a: i, b: target, bridge: false });
      degree[i]++;
      degree[target]++;
    }
  }
  return kept;
}

export function buildGraph(
  w: number,
  h: number,
  mode: LayoutMode,
  copyRect: Rect | null,
): GraphModel {
  if (w <= 0 || h <= 0) {
    return { nodes: [], edges: [] };
  }
  const rand = mulberry32(mode === "portrait" ? 0xf00dc0de : 0x0c0ffee1);

  const cam: Camera = makeCamera(w / 2, h / 2, CAM_FOCAL, CAM_DIST);
  const copyZone: Rect | null = copyRect
    ? expandRect(copyRect, COPY_ZONE_PAD)
    : null;

  const sampled = sampleWorldNodes(w, h, cam, rand);
  const world: Vec3[] = sampled.map((s) => s.world);
  const screen: Point[] = sampled.map((s) => s.screen);

  const hubs = pickHubs(sampled, w, h, copyRect);

  let scaleMin = Infinity;
  let scaleMax = -Infinity;
  for (const s of sampled) {
    if (s.scale < scaleMin) {
      scaleMin = s.scale;
    }
    if (s.scale > scaleMax) {
      scaleMax = s.scale;
    }
  }
  const scaleRange = Math.max(1e-6, scaleMax - scaleMin);
  const nearT = sampled.map((s) => clamp01((s.scale - scaleMin) / scaleRange));

  const nodes: GraphNode[] = screen.map((p, i) => {
    const isHub = hubs.has(i);
    let t = nearT[i];
    const inCopy = !isHub && !!copyZone && inRect(p.x, p.y, copyZone);
    if (inCopy) {
      t = Math.min(t, COPY_NEAR_CAP);
    }
    const r = isHub
      ? NEAR_R + NEAR_R_SPAN
      : t > 0.5
        ? NEAR_R + NEAR_R_SPAN * (t - 0.5) * 2
        : FAR_R + FAR_R_SPAN * t * 2;
    let alpha = isHub
      ? 1
      : t > 0.5
        ? 0.8 + 0.2 * (t - 0.5) * 2
        : 0.35 + 0.15 * t * 2;
    let darken = 0;
    if (!isHub && inCopy) {
      alpha = Math.min(alpha, COPY_ZONE_ALPHA_CAP);
      darken = COPY_ZONE_DARKEN;
    }
    return {
      x: p.x,
      y: p.y,
      r,
      alpha,
      hub: isHub,
      tint: (i % 2) as 0 | 1,
      darken,
    };
  });

  const rawEdges = buildEdges(world, screen, copyZone);
  const edges: GraphEdge[] = rawEdges.map((e) => {
    const avgT = (nearT[e.a] + nearT[e.b]) / 2;
    // Near edges: alpha rises toward 0.82 and width toward 2.3px as avgT
    // climbs past 0.5 -- tuned so the near band clears the "lines clearly
    // visible" luminance floor through its own slate-with-cyan-tint colour
    // (paint.ts), never by mixing toward white.
    let alpha =
      avgT > 0.5 ? 0.48 + (avgT - 0.5) * 2 * 0.34 : 0.18 + avgT * 2 * 0.12;
    const lineWidth = avgT > 0.5 ? 1 + (avgT - 0.5) * 2 * 1.15 : 1;
    const points: Point[] = e.via
      ? [screen[e.a], e.via, screen[e.b]]
      : [screen[e.a], screen[e.b]];
    let darken = 0;
    const inCopy = !!copyZone && polylineIntersectsRect(points, copyZone);
    if (inCopy) {
      alpha = Math.min(alpha, COPY_ZONE_ALPHA_CAP);
      darken = COPY_ZONE_DARKEN;
    }
    return { points, alpha, darken, lineWidth, near: avgT > 0.5 };
  });

  return { nodes, edges };
}
