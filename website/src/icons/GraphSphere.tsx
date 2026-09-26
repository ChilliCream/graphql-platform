import type { ComponentPropsWithoutRef } from "react";

/**
 * Static geodesic network sphere: a subdivided icosahedron (12 base
 * vertices + one edge-midpoint subdivision = 42 vertices, 120 edges),
 * rotated to a slight tilt and projected through a small perspective
 * camera, all computed once below as plain module-scope math (no client
 * JS, no animation, no canvas). Depth reads as 3D the same way the
 * federation hero's backdrop does: alpha and size fall off from front to
 * back, never the other way round.
 */

interface Vec3 {
  readonly x: number;
  readonly y: number;
  readonly z: number;
}

function normalize(v: Vec3): Vec3 {
  const len = Math.sqrt(v.x * v.x + v.y * v.y + v.z * v.z);
  return { x: v.x / len, y: v.y / len, z: v.z / len };
}

function midpoint(a: Vec3, b: Vec3): Vec3 {
  return normalize({
    x: (a.x + b.x) / 2,
    y: (a.y + b.y) / 2,
    z: (a.z + b.z) / 2,
  });
}

// Canonical icosahedron: 12 vertices, 20 triangular faces.
const PHI = (1 + Math.sqrt(5)) / 2;
const BASE_VERTICES: readonly Vec3[] = (
  [
    [-1, PHI, 0],
    [1, PHI, 0],
    [-1, -PHI, 0],
    [1, -PHI, 0],
    [0, -1, PHI],
    [0, 1, PHI],
    [0, -1, -PHI],
    [0, 1, -PHI],
    [PHI, 0, -1],
    [PHI, 0, 1],
    [-PHI, 0, -1],
    [-PHI, 0, 1],
  ] as const
).map(([x, y, z]) => normalize({ x, y, z }));

const BASE_FACES: readonly (readonly [number, number, number])[] = [
  [0, 11, 5],
  [0, 5, 1],
  [0, 1, 7],
  [0, 7, 10],
  [0, 10, 11],
  [1, 5, 9],
  [5, 11, 4],
  [11, 10, 2],
  [10, 7, 6],
  [7, 1, 8],
  [3, 9, 4],
  [3, 4, 2],
  [3, 2, 6],
  [3, 6, 8],
  [3, 8, 9],
  [4, 9, 5],
  [2, 4, 11],
  [6, 2, 10],
  [8, 6, 7],
  [9, 8, 1],
];

/**
 * One level of edge-midpoint subdivision: each of the 20 faces splits into
 * 4, adding one new vertex per original edge (30 of them) for 42 vertices
 * total, and 120 unique edges -- the reference's density.
 */
function buildGeodesic(): {
  vertices: readonly Vec3[];
  edges: readonly (readonly [number, number])[];
} {
  const vertices = BASE_VERTICES.slice();
  const midpoints = new Map<string, number>();
  const edgeSet = new Map<string, readonly [number, number]>();

  function midIndex(i: number, j: number): number {
    const key = i < j ? `${i}-${j}` : `${j}-${i}`;
    const existing = midpoints.get(key);
    if (existing !== undefined) return existing;
    const idx = vertices.length;
    vertices.push(midpoint(vertices[i], vertices[j]));
    midpoints.set(key, idx);
    return idx;
  }

  function addEdge(i: number, j: number): void {
    const key = i < j ? `${i}-${j}` : `${j}-${i}`;
    if (!edgeSet.has(key)) edgeSet.set(key, i < j ? [i, j] : [j, i]);
  }

  for (const [a, b, c] of BASE_FACES) {
    const ab = midIndex(a, b);
    const bc = midIndex(b, c);
    const ca = midIndex(c, a);
    for (const [p, q, r] of [
      [a, ab, ca],
      [b, bc, ab],
      [c, ca, bc],
      [ab, bc, ca],
    ] as const) {
      addEdge(p, q);
      addEdge(q, r);
      addEdge(r, p);
    }
  }

  return { vertices, edges: [...edgeSet.values()] };
}

const { vertices: SPHERE_VERTICES, edges: SPHERE_EDGES } = buildGeodesic();

// Slight tilt: a small rotation on each axis, so the sphere reads as a
// specific 3D view rather than a flat, symmetric rosette.
const TILT_X = (-16 * Math.PI) / 180;
const TILT_Y = (24 * Math.PI) / 180;
const TILT_Z = (7 * Math.PI) / 180;

function rotate(v: Vec3): Vec3 {
  // Rx
  let { x, y, z } = v;
  let y1 = y * Math.cos(TILT_X) - z * Math.sin(TILT_X);
  let z1 = y * Math.sin(TILT_X) + z * Math.cos(TILT_X);
  y = y1;
  z = z1;
  // Ry
  let x1 = x * Math.cos(TILT_Y) + z * Math.sin(TILT_Y);
  z1 = -x * Math.sin(TILT_Y) + z * Math.cos(TILT_Y);
  x = x1;
  z = z1;
  // Rz
  x1 = x * Math.cos(TILT_Z) - y * Math.sin(TILT_Z);
  y1 = x * Math.sin(TILT_Z) + y * Math.cos(TILT_Z);
  return { x: x1, y: y1, z: z1 };
}

const ROTATED = SPHERE_VERTICES.map(rotate);

// Perspective camera: a unit sphere sits at distance CAM_DIST from the
// lens; CAM_K sets how many screen px one world unit covers at z = 0. z
// runs -1 (nearest the viewer) .. 1 (farthest), so scale(z) = CAM_K / (CAM_DIST
// + z) gives the nearest vertices roughly 2x the screen scale of the
// farthest ones.
const CAM_DIST = 3;
const CAM_K = 450;
const VIEWBOX = 400;
const CENTER = VIEWBOX / 2;

function scaleAtZ(z: number): number {
  return CAM_K / (CAM_DIST + z);
}

/** 0 at the farthest vertex, 1 at the nearest -- the single depth cue everything else reads from. */
function depthT(z: number): number {
  return (1 - z) / 2;
}

const PROJECTED = ROTATED.map((v) => {
  const scale = scaleAtZ(v.z);
  return {
    x: CENTER + v.x * scale,
    y: CENTER - v.y * scale,
    t: depthT(v.z),
  };
});

const NODE_FAR_R = 2.2;
const NODE_NEAR_R = 5.6;
const NODE_FAR_ALPHA = 0.22;
const NODE_NEAR_ALPHA = 0.95;
const EDGE_FAR_ALPHA = 0.08;
const EDGE_NEAR_ALPHA = 0.55;
const EDGE_FAR_WIDTH = 0.5;
const EDGE_NEAR_WIDTH = 1.5;
const HALO_COUNT = 3;

function lerp(from: number, to: number, t: number): number {
  return from + (to - from) * t;
}

/** Every 4th vertex reads teal instead of cyan, so the sphere carries both site accent hues rather than one flat colour. */
function isTeal(index: number): boolean {
  return index % 4 === 0;
}

const HALO_INDICES = new Set(
  PROJECTED.map((p, i) => [p.t, i] as const)
    .sort((a, b) => b[0] - a[0])
    .slice(0, HALO_COUNT)
    .map(([, i]) => i),
);

const HALO_ORDER = [...HALO_INDICES];

const NODES = PROJECTED.map((p, i) => ({
  x: p.x,
  y: p.y,
  r: lerp(NODE_FAR_R, NODE_NEAR_R, p.t),
  alpha: lerp(NODE_FAR_ALPHA, NODE_NEAR_ALPHA, p.t),
  teal: isTeal(i),
  // Index into the halo gradient defs, or -1 for a node with no halo.
  haloId: HALO_ORDER.indexOf(i),
})).sort((a, b) => a.r - b.r);

// Edges are quantised into a handful of depth bands and each band drawn as
// one path, so the 120 edges share a few `stroke`/`stroke-width` groups
// instead of repeating those attributes 120 times.
const EDGE_BANDS = 6;

interface EdgeBand {
  readonly alpha: number;
  readonly width: number;
  readonly d: string;
}

const edgeBuckets: string[][] = Array.from({ length: EDGE_BANDS }, () => []);
for (const [i, j] of SPHERE_EDGES) {
  const a = PROJECTED[i];
  const b = PROJECTED[j];
  const t = (a.t + b.t) / 2;
  const band = Math.min(EDGE_BANDS - 1, Math.floor(t * EDGE_BANDS));
  edgeBuckets[band].push(
    `M${a.x.toFixed(1)},${a.y.toFixed(1)} L${b.x.toFixed(1)},${b.y.toFixed(1)}`,
  );
}

const EDGE_GROUPS: readonly EdgeBand[] = edgeBuckets
  .map((segments, band) => {
    const t = (band + 0.5) / EDGE_BANDS;
    return {
      alpha: lerp(EDGE_FAR_ALPHA, EDGE_NEAR_ALPHA, t),
      width: lerp(EDGE_FAR_WIDTH, EDGE_NEAR_WIDTH, t),
      d: segments.join(" "),
    };
  })
  .filter((group) => group.d.length > 0);

/** Abstract geodesic network sphere for the Fusion closing band. */
export function GraphSphere(props: ComponentPropsWithoutRef<"svg">) {
  return (
    <svg viewBox={`0 0 ${VIEWBOX} ${VIEWBOX}`} aria-hidden="true" {...props}>
      <defs>
        {NODES.filter((n) => n.haloId >= 0).map((n) => (
          <radialGradient key={n.haloId} id={`graph-sphere-halo-${n.haloId}`}>
            <stop
              offset="0%"
              stopColor={
                n.teal ? "var(--color-cc-success)" : "var(--color-cc-accent)"
              }
              stopOpacity={0.35}
            />
            <stop
              offset="100%"
              stopColor={
                n.teal ? "var(--color-cc-success)" : "var(--color-cc-accent)"
              }
              stopOpacity={0}
            />
          </radialGradient>
        ))}
      </defs>

      {EDGE_GROUPS.map((group, i) => (
        <path
          key={i}
          d={group.d}
          fill="none"
          stroke="var(--color-cc-accent)"
          strokeOpacity={group.alpha.toFixed(2)}
          strokeWidth={group.width.toFixed(2)}
          strokeLinecap="round"
        />
      ))}

      {NODES.map((n, i) => {
        const color = n.teal
          ? "var(--color-cc-success)"
          : "var(--color-cc-accent)";
        return (
          <g key={i}>
            {n.haloId >= 0 && (
              <circle
                cx={n.x.toFixed(1)}
                cy={n.y.toFixed(1)}
                r={(n.r * 3.5).toFixed(1)}
                fill={`url(#graph-sphere-halo-${n.haloId})`}
              />
            )}
            <circle
              cx={n.x.toFixed(1)}
              cy={n.y.toFixed(1)}
              r={n.r.toFixed(2)}
              fill={color}
              fillOpacity={n.alpha.toFixed(2)}
            />
          </g>
        );
      })}
    </svg>
  );
}
