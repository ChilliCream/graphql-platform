// The node/edge model for the hero's constellation: one continuous graph
// scattered across the whole frame (comment 540), thinner but never empty
// behind the copy block, with a handful of hub nodes and short-to-medium
// proximity edges that keep the whole thing one connected component.
// Positions are plain canvas-pixel coordinates; "depth" is a single 0..1
// value per node that only drives size and alpha -- the cheap read of
// "near nodes bigger and brighter, far nodes small and dim" a still frame
// needs. Everything is derived once from a seeded PRNG, so the same
// (w, h, copyRect) always yields the same graph.
import type { Rect } from "./paint";
import {
  COPY_ZONE_PAD,
  MAX_NEIGHBOUR_GAP,
  MIN_NODE_SPACING,
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

export interface GraphNode {
  readonly x: number;
  readonly y: number;
  /** 0 = farthest allowed, 1 = nearest -- drives size and alpha only. */
  readonly depth: number;
  readonly hub: boolean;
  /** Inside the padded copy-clear zone: capped dim, never a hub. */
  readonly behindCopy: boolean;
}

export interface Point {
  readonly x: number;
  readonly y: number;
}

export interface GraphEdge {
  readonly a: number;
  readonly b: number;
  readonly points: readonly Point[];
  readonly alpha: number;
}

export interface GraphModel {
  readonly nodes: readonly GraphNode[];
  readonly edges: readonly GraphEdge[];
}

// 1440x900 baseline: 100-180 nodes (comment 540); 140 sits comfortably in
// the middle. Scaled by frame area for other landscape sizes, and to ~60%
// of that for portrait, same convention as every other number in this
// spec.
const BASE_TARGET = 140;
const BASE_AREA = 1440 * 900;
const PORTRAIT_SCALE = 0.6;

function targetCount(w: number, h: number, mode: LayoutMode): number {
  const scaled = Math.round((BASE_TARGET * (w * h)) / BASE_AREA);
  const landscapeEquivalent = Math.max(100, Math.min(240, scaled));
  return mode === "portrait"
    ? Math.round(landscapeEquivalent * PORTRAIT_SCALE)
    : landscapeEquivalent;
}

function distToRect(x: number, y: number, rect: Rect): number {
  const dx = Math.max(rect.x - x, x - (rect.x + rect.width), 0);
  const dy = Math.max(rect.y - y, y - (rect.y + rect.height), 0);
  return dx > 0 && dy > 0 ? Math.hypot(dx, dy) : Math.max(dx, dy);
}

function smoothstep(t: number): number {
  const c = t < 0 ? 0 : t > 1 ? 1 : t;
  return c * c * (3 - 2 * c);
}

const DENSITY_FALLOFF = 260;
const MIN_DENSITY = 0.16;

/** Keep-probability for a candidate point: full density away from the
 * copy block, thinning smoothly toward (but never reaching zero at) the
 * copy rect itself, so the graph reads as one continuous field with
 * "gentle density variation" rather than a hole. */
function densityWeight(x: number, y: number, copyRect: Rect | null): number {
  if (!copyRect) {
    return 1;
  }
  const d = distToRect(x, y, copyRect);
  const t = smoothstep(d / DENSITY_FALLOFF);
  return MIN_DENSITY + (1 - MIN_DENSITY) * t;
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

function scatter(
  w: number,
  h: number,
  copyRect: Rect | null,
  target: number,
  rand: () => number,
): Point[] {
  const pts: Point[] = [];
  const maxAttempts = target * 60;
  let attempts = 0;
  while (pts.length < target && attempts < maxAttempts) {
    attempts++;
    const x = rand() * w;
    const y = rand() * h;
    if (rand() > densityWeight(x, y, copyRect)) {
      continue;
    }
    if (isFarEnough(x, y, pts, MIN_NODE_SPACING)) {
      pts.push({ x, y });
    }
  }
  return pts;
}

/** Bounds the nearest-neighbour gap for every point to ~MAX_NEIGHBOUR_GAP
 * by inserting a bridging point near any node whose nearest neighbour is
 * too far away -- what keeps the field from reading as "islands with an
 * empty band" once density thins out behind the copy. */
function fillGaps(
  pts: Point[],
  w: number,
  h: number,
  rand: () => number,
): void {
  for (let round = 0; round < 6; round++) {
    let changedAny = false;
    const n = pts.length;
    for (let i = 0; i < n; i++) {
      let nearest = Infinity;
      for (let j = 0; j < pts.length; j++) {
        if (j === i) {
          continue;
        }
        const d = Math.hypot(pts[i].x - pts[j].x, pts[i].y - pts[j].y);
        if (d < nearest) {
          nearest = d;
        }
      }
      if (nearest <= MAX_NEIGHBOUR_GAP) {
        continue;
      }
      for (let attempt = 0; attempt < 24; attempt++) {
        const angle = rand() * Math.PI * 2;
        const dist = MIN_NODE_SPACING + rand() * (MAX_NEIGHBOUR_GAP * 0.55);
        const x = Math.min(
          w - 4,
          Math.max(4, pts[i].x + Math.cos(angle) * dist),
        );
        const y = Math.min(
          h - 4,
          Math.max(4, pts[i].y + Math.sin(angle) * dist),
        );
        if (isFarEnough(x, y, pts, MIN_NODE_SPACING)) {
          pts.push({ x, y });
          changedAny = true;
          break;
        }
      }
    }
    if (!changedAny) {
      break;
    }
  }
}

const HUB_COUNT = 5;
const HUB_MIN_SEPARATION = 150;
const HUB_MIN_COPY_CLEARANCE = 90;

function pickHubs(
  pts: readonly Point[],
  copyRect: Rect | null,
  rand: () => number,
): Set<number> {
  const candidates = pts
    .map((p, i) => ({ i, p }))
    .filter(
      ({ p }) =>
        !copyRect || distToRect(p.x, p.y, copyRect) >= HUB_MIN_COPY_CLEARANCE,
    );
  // Shuffle deterministically, then greedily take well-separated ones.
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

/** k-nearest-neighbour proximity edges, then bridge any leftover separate
 * components (a scattered point cloud's k-NN graph is very likely already
 * one component, but this guarantees it regardless of seed or viewport). */
function buildEdges(
  pts: readonly Point[],
  rand: () => number,
): { a: number; b: number }[] {
  const n = pts.length;
  const k = 2;
  const edgeSet = new Set<string>();
  const edges: { a: number; b: number }[] = [];
  const addEdge = (a: number, b: number) => {
    if (a === b) {
      return;
    }
    const key = a < b ? `${a}-${b}` : `${b}-${a}`;
    if (edgeSet.has(key)) {
      return;
    }
    edgeSet.add(key);
    edges.push({ a, b });
  };

  for (let i = 0; i < n; i++) {
    const dists = pts
      .map((p, j) => ({ j, d: Math.hypot(p.x - pts[i].x, p.y - pts[i].y) }))
      .filter((e) => e.j !== i)
      .sort((x, y) => x.d - y.d)
      .slice(0, k);
    for (const { j } of dists) {
      addEdge(i, j);
    }
  }

  const uf = makeUnionFind(n);
  for (const e of edges) {
    uf.union(e.a, e.b);
  }
  // Bridge stray components: connect each component's closest pair of
  // nodes to the main body.
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
  for (let i = 1; i < roots.length; i++) {
    const groupA = byRoot.get(roots[0])!;
    const groupB = byRoot.get(roots[i])!;
    let best = { a: groupA[0], b: groupB[0], d: Infinity };
    for (const a of groupA) {
      for (const b of groupB) {
        const d = Math.hypot(pts[a].x - pts[b].x, pts[a].y - pts[b].y);
        if (d < best.d) {
          best = { a, b, d };
        }
      }
    }
    addEdge(best.a, best.b);
    uf.union(best.a, best.b);
    groupA.push(...groupB);
  }

  // Degree 2-4: trim the longest edges off any node over the cap, but only
  // when an alternate path still connects its two endpoints afterward --
  // otherwise trimming could turn a bridge edge into a second component.
  const degree = new Array(n).fill(0);
  for (const e of edges) {
    degree[e.a]++;
    degree[e.b]++;
  }
  const byLength = [...edges].sort(
    (x, y) =>
      Math.hypot(pts[y.a].x - pts[y.b].x, pts[y.a].y - pts[y.b].y) -
      Math.hypot(pts[x.a].x - pts[x.b].x, pts[x.a].y - pts[x.b].y),
  );
  const keptEdges: { a: number; b: number }[] = [...edges];
  const adjacency: Set<number>[] = Array.from({ length: n }, () => new Set());
  for (const e of keptEdges) {
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
  for (const e of byLength) {
    if (degree[e.a] <= 4 && degree[e.b] <= 4) {
      continue;
    }
    if (degree[e.a] <= 2 || degree[e.b] <= 2) {
      continue;
    }
    adjacency[e.a].delete(e.b);
    adjacency[e.b].delete(e.a);
    if (stillConnected(e.a, e.b)) {
      degree[e.a]--;
      degree[e.b]--;
      const idx = keptEdges.indexOf(e);
      keptEdges.splice(idx, 1);
    } else {
      adjacency[e.a].add(e.b);
      adjacency[e.b].add(e.a);
    }
  }
  const finalEdges = keptEdges;
  for (let i = 0; i < n; i++) {
    let tries = 0;
    while (degree[i] < 2 && tries < n) {
      const j = Math.floor(rand() * n);
      const key = i < j ? `${i}-${j}` : `${j}-${i}`;
      if (j !== i && !edgeSet.has(key)) {
        finalEdges.push({ a: i, b: j });
        edgeSet.add(key);
        degree[i]++;
        degree[j]++;
      }
      tries++;
    }
  }
  return finalEdges;
}

export function buildGraph(
  w: number,
  h: number,
  mode: LayoutMode,
  copyRect: Rect | null,
): GraphModel {
  const rand = mulberry32(mode === "portrait" ? 0xf00dc0de : 0x0c0ffee1);
  const target = targetCount(w, h, mode);
  const pts = scatter(w, h, copyRect, target, rand);
  fillGaps(pts, w, h, rand);

  const padded: Rect | null = copyRect
    ? {
        x: copyRect.x - COPY_ZONE_PAD,
        y: copyRect.y - COPY_ZONE_PAD,
        width: copyRect.width + COPY_ZONE_PAD * 2,
        height: copyRect.height + COPY_ZONE_PAD * 2,
      }
    : null;
  const hubs = pickHubs(pts, copyRect, rand);

  const nodes: GraphNode[] = pts.map((p, i) => {
    const behindCopy = !!padded && distToRect(p.x, p.y, padded) <= 0;
    const isHub = hubs.has(i);
    const depth = isHub
      ? 0.92 + rand() * 0.08
      : behindCopy
        ? rand() * 0.28
        : rand();
    return { x: p.x, y: p.y, depth, hub: isHub, behindCopy };
  });

  const edgePairs = buildEdges(pts, rand);
  const edges: GraphEdge[] = edgePairs.map(({ a, b }) => {
    const avgDepth = (nodes[a].depth + nodes[b].depth) / 2;
    const alpha =
      avgDepth > 0.5
        ? 0.35 + (avgDepth - 0.5) * 2 * 0.2
        : 0.18 + avgDepth * 2 * 0.12;
    return {
      a,
      b,
      points: [
        { x: nodes[a].x, y: nodes[a].y },
        { x: nodes[b].x, y: nodes[b].y },
      ],
      alpha,
    };
  });

  return { nodes, edges };
}
