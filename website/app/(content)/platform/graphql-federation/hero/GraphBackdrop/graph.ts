// The node/edge model for the hero's constellation: a real 3D slab
// projected through a perspective camera (camera.ts), sampled with a
// hexagonal jittered lattice so the largest empty circle anywhere on the
// canvas is bounded (the coverage fix), connected with k-nearest-neighbour
// edges in 3D plus an MST bridge for any leftover component (the edge
// fix), and locally dimmed -- alpha only, never the whole scene -- inside
// the copy block so on-glyph contrast holds (the contrast fix).
//
// Everything is derived once from a seeded PRNG, so the same
// (w, h, mode, copyRect) always yields the same graph.
import type { Rect } from "./paint";
import { makeCamera, placeAt, project, type Camera, type Vec3 } from "./camera";
import {
  COPY_ZONE_PAD,
  MIN_NODE_SPACING,
  coverageCapDiameter,
  hexSpacing,
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
  /** avgT > 0.5: the nearer half of edges by depth, paint.ts's near-band tint applies to these (and only these, when not copy-zone darkened). */
  readonly near: boolean;
}

export interface GraphModel {
  readonly nodes: readonly GraphNode[];
  readonly edges: readonly GraphEdge[];
}

// The camera: a slab from z = 0 (nearest) to CAM_Z_RANGE (farthest) sitting
// CAM_DIST in front of the lens. far/near scale ratio = CAM_DIST /
// (CAM_DIST + CAM_Z_RANGE) ≈ 0.43, i.e. near nodes project ≈2.3x the
// scale of far ones -- comfortably over the ticket's 2x depth bar.
const CAM_DIST = 520;
const CAM_Z_RANGE = 680;
const CAM_FOCAL = 860;

// How strongly a node's screen position pulls its depth toward the focal
// axis (0 = pure noise, 1 = position decides depth outright); the rest is
// per-node noise, so the read is a gentle bias, not a mechanical wedge.
const FOCAL_WEIGHT = 0.55;

const FAR_R = 2;
const FAR_R_SPAN = 1;
const NEAR_R = 4;
const NEAR_R_SPAN = 2;

const HUB_COUNT = 5;
const HUB_MIN_SEPARATION = 150;
const HUB_MIN_COPY_CLEARANCE = 90;

const COPY_NEAR_CAP = 0.15;
// One tier for the whole copy-clear zone (the copy rect plus its pad):
// alpha capped low enough, and darkened enough, that the paragraph's own
// ink keeps its plain-navy contrast even against a node or edge sitting
// directly under a glyph row -- verified per row after this pass, not a
// separate, stricter cap for a "text zone" nested inside it.
const COPY_ZONE_ALPHA_CAP = 0.08;
const COPY_ZONE_DARKEN = 0.35;

const KNN_K = 3;
const EDGE_LEN_CAP = 220;
const DEGREE_MIN = 2;
const DEGREE_MAX = 4;

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

function isFarEnough(
  x: number,
  y: number,
  placed: readonly Point[],
  minDist: number,
): boolean {
  for (const p of placed) {
    if (Math.hypot(p.x - x, p.y - y) < minDist) {
      return false;
    }
  }
  return true;
}

/**
 * A jittered hexagonal lattice over the whole canvas (with a one-spacing
 * overhang on every side so the edges of the frame get the same coverage
 * as the interior). A regular hex lattice at spacing `s` has covering
 * radius `s / sqrt(3)` -- see sceneLayout.hexSpacing, which already solves
 * for `s` from the per-width empty-circle cap -- so this is the coverage
 * fix: no point on the canvas can be farther than the cap from its
 * nearest node, by construction, not by a post-hoc gap-fill pass.
 */
function buildLattice(
  w: number,
  h: number,
  spacing: number,
  rand: () => number,
): Point[] {
  const rowHeight = (spacing * Math.sqrt(3)) / 2;
  const jitter = spacing * 0.12;
  const pts: Point[] = [];
  let row = 0;
  for (let y = -rowHeight; y <= h + rowHeight; y += rowHeight) {
    const xOffset = row % 2 === 0 ? 0 : spacing / 2;
    for (let x = -spacing + xOffset; x <= w + spacing; x += spacing) {
      const jx = (rand() * 2 - 1) * jitter;
      const jy = (rand() * 2 - 1) * jitter;
      const px = x + jx;
      const py = y + jy;
      if (isFarEnough(px, py, pts, MIN_NODE_SPACING)) {
        pts.push({ x: px, y: py });
      } else if (isFarEnough(x, y, pts, MIN_NODE_SPACING)) {
        // The jitter pushed two neighbours too close together; fall back
        // to the unjittered lattice point, which always satisfies both
        // the spacing floor and the coverage guarantee.
        pts.push({ x, y });
      }
    }
    row++;
  }
  return pts;
}

/**
 * 0 at the frame's bottom-left, 1 at its top-right: the axis the focal
 * structure reads along. Nodes near 0 get a near (small z) bias, nodes
 * near 1 a far bias, so the camera reads as looking down and across the
 * slab rather than straight at a flat wall of points.
 */
function focalBias(x: number, y: number, w: number, h: number): number {
  const nx = w > 0 ? x / w : 0.5;
  const ny = h > 0 ? y / h : 0.5;
  return clamp01((nx - ny + 1) / 2);
}

function pickHubs(
  pts: readonly Point[],
  w: number,
  h: number,
  copyRect: Rect | null,
  rand: () => number,
): Set<number> {
  // The lattice overhangs the canvas by one spacing on every side (for
  // edge coverage); a hub picked from that overhang would render off-
  // screen and waste one of the 4-6 slots, so candidates are restricted
  // to points actually on the canvas.
  const candidates = pts
    .map((p, i) => ({ i, p }))
    .filter(
      ({ p }) =>
        p.x >= 0 &&
        p.x <= w &&
        p.y >= 0 &&
        p.y <= h &&
        (!copyRect || distToRect(p.x, p.y, copyRect) >= HUB_MIN_COPY_CLEARANCE),
    );
  const shuffled = [...candidates];
  for (let i = shuffled.length - 1; i > 0; i--) {
    const j = Math.floor(rand() * (i + 1));
    [shuffled[i], shuffled[j]] = [shuffled[j], shuffled[i]];
  }
  const hubs = new Set<number>();
  const chosen: Point[] = [];
  for (const { i, p } of shuffled) {
    if (hubs.size >= HUB_COUNT) {
      break;
    }
    if (
      chosen.every(
        (c) => Math.hypot(c.x - p.x, c.y - p.y) >= HUB_MIN_SEPARATION,
      )
    ) {
      hubs.add(i);
      chosen.push(p);
    }
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

  for (let i = 0; i < n; i++) {
    const candidates = [];
    for (let j = 0; j < n; j++) {
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
  // neighbour to any node left under 2.
  for (let i = 0; i < n; i++) {
    let guard = 0;
    while (degree[i] < DEGREE_MIN && guard < n) {
      guard++;
      let target = -1;
      let bestD = Infinity;
      for (let j = 0; j < n; j++) {
        if (
          j === i ||
          edgeSet.has(key(i, j)) ||
          distScreen(i, j) > EDGE_LEN_CAP
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

  const spacing = hexSpacing(coverageCapDiameter(w));
  const pts = buildLattice(w, h, spacing, rand);

  const cam: Camera = makeCamera(w / 2, h / 2, CAM_FOCAL, CAM_DIST);
  const copyZone: Rect | null = copyRect
    ? expandRect(copyRect, COPY_ZONE_PAD)
    : null;

  const hubs = pickHubs(pts, w, h, copyRect, rand);
  const z = pts.map((p) => {
    const bias = focalBias(p.x, p.y, w, h);
    const t = clamp01(bias * FOCAL_WEIGHT + rand() * (1 - FOCAL_WEIGHT));
    return t * CAM_Z_RANGE;
  });
  hubs.forEach((i) => {
    z[i] = 0;
  });

  const world: Vec3[] = pts.map((p, i) => placeAt(p.x, p.y, z[i], cam));
  const projected = world.map((v) => project(v, cam));
  const screen: Point[] = projected.map((p) => ({ x: p.x, y: p.y }));

  let scaleMin = Infinity;
  let scaleMax = -Infinity;
  for (const p of projected) {
    if (p.scale < scaleMin) {
      scaleMin = p.scale;
    }
    if (p.scale > scaleMax) {
      scaleMax = p.scale;
    }
  }
  const scaleRange = Math.max(1e-6, scaleMax - scaleMin);
  const nearT = projected.map((p) =>
    clamp01((p.scale - scaleMin) / scaleRange),
  );

  const nodes: GraphNode[] = pts.map((p, i) => {
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
    // Near edges: alpha 0.35-0.55 (comment 529's own bar, capped at its
    // top), width 1-1.5px. The luminance floor outside the copy zone is
    // met by the tint itself (paint.ts), not by pushing alpha or width
    // past this band.
    let alpha =
      avgT > 0.5 ? 0.35 + (avgT - 0.5) * 2 * 0.2 : 0.18 + avgT * 2 * 0.12;
    const lineWidth = avgT > 0.5 ? 1 + (avgT - 0.5) * 2 * 0.5 : 1;
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
