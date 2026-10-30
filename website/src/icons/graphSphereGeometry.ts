interface Vec3 {
  readonly x: number;
  readonly y: number;
  readonly z: number;
}

export interface GraphGeometry {
  readonly vertices: readonly Vec3[];
  readonly edges: readonly (readonly [number, number])[];
  readonly radiusPx: number;
  readonly depthRadius: number;
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
    if (existing !== undefined) {
      return existing;
    }
    const idx = vertices.length;
    vertices.push(midpoint(vertices[i], vertices[j]));
    midpoints.set(key, idx);
    return idx;
  }

  function addEdge(i: number, j: number): void {
    const key = i < j ? `${i}-${j}` : `${j}-${i}`;
    if (!edgeSet.has(key)) {
      edgeSet.set(key, i < j ? [i, j] : [j, i]);
    }
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

const built = buildGeodesic();
export const SPHERE_VERTICES = built.vertices;
export const SPHERE_EDGES = built.edges;

const TILT_X = (-16 * Math.PI) / 180;
const TILT_Y = (24 * Math.PI) / 180;
const TILT_Z = (7 * Math.PI) / 180;

function tilt(v: Vec3): Vec3 {
  let { x, y, z } = v;
  let y1 = y * Math.cos(TILT_X) - z * Math.sin(TILT_X);
  let z1 = y * Math.sin(TILT_X) + z * Math.cos(TILT_X);
  y = y1;
  z = z1;
  let x1 = x * Math.cos(TILT_Y) + z * Math.sin(TILT_Y);
  z1 = -x * Math.sin(TILT_Y) + z * Math.cos(TILT_Y);
  x = x1;
  z = z1;
  x1 = x * Math.cos(TILT_Z) - y * Math.sin(TILT_Z);
  y1 = x * Math.sin(TILT_Z) + y * Math.cos(TILT_Z);
  return { x: x1, y: y1, z };
}

function rotateY(v: Vec3, theta: number): Vec3 {
  const cos = Math.cos(theta);
  const sin = Math.sin(theta);
  return {
    x: v.x * cos + v.z * sin,
    y: v.y,
    z: -v.x * sin + v.z * cos,
  };
}

const CAM_DIST = 3;
const CAM_K = 450;
export const VIEWBOX = 400;
const CENTER = VIEWBOX / 2;

export const SPHERE_RADIUS_PX = CAM_K / CAM_DIST;

function scaleAtZ(z: number): number {
  return CAM_K / (CAM_DIST + z);
}

function depthT(z: number): number {
  return (1 - z) / 2;
}

export interface ProjectedVertex {
  readonly x: number;
  readonly y: number;
  readonly t: number;
}

export const SPHERE_GRAPH: GraphGeometry = {
  vertices: SPHERE_VERTICES,
  edges: SPHERE_EDGES,
  radiusPx: SPHERE_RADIUS_PX,
  depthRadius: 1,
};

/** A fixed asymmetric faceted network for the Federation closing CTA. */
export const FACETED_GRAPH: GraphGeometry = {
  vertices: [
    { x: -0.96, y: -0.23, z: -0.33 },
    { x: -0.74, y: 0.06, z: 0.56 },
    { x: -0.66, y: 0.46, z: -0.19 },
    { x: -0.28, y: 0.98, z: 0.08 },
    { x: -0.06, y: 0.55, z: 0.63 },
    { x: 0.27, y: 0.86, z: -0.37 },
    { x: 0.59, y: 0.66, z: 0.48 },
    { x: 0.85, y: 0.39, z: -0.19 },
    { x: 0.58, y: 0.03, z: 0.59 },
    { x: 0.78, y: -0.35, z: 0.13 },
    { x: 0.1, y: -0.54, z: -0.3 },
    { x: -0.31, y: -0.39, z: 0.58 },
    { x: -0.76, y: -0.6, z: -0.54 },
    { x: -0.12, y: 0.19, z: -0.64 },
    { x: 0.33, y: 0.28, z: -0.62 },
    { x: -0.41, y: 0.15, z: 0.08 },
    { x: 0.28, y: 0.33, z: 0.07 },
    { x: 0.02, y: -0.25, z: 0.11 },
    { x: -0.63, y: -0.44, z: 0.11 },
    { x: -0.5, y: -0.11, z: -0.57 },
    { x: 0.49, y: -0.08, z: -0.16 },
    { x: 0.44, y: 0.83, z: 0.07 },
    { x: 0.07, y: 0.09, z: 0.65 },
    { x: -0.12, y: 0.45, z: -0.24 },
    { x: -0.54, y: 0.53, z: 0.35 },
    { x: -0.31, y: -0.25, z: -0.21 },
    { x: 0.12, y: -0.11, z: -0.35 },
    { x: -0.27, y: 0.24, z: 0.5 },
    { x: 0.69, y: 0.29, z: 0.3 },
    { x: 0.46, y: -0.2, z: 0.27 },
    { x: 0.14, y: 0.64, z: -0.07 },
    { x: -0.06, y: 0.36, z: 0.21 },
    { x: -0.66, y: -0.04, z: -0.12 },
    { x: -0.09, y: 0.05, z: -0.1 },
    { x: 0.15, y: 0.73, z: 0.34 },
    { x: 0.17, y: -0.3, z: 0.53 },
    { x: 0.33, y: 0.32, z: 0.49 },
    { x: -0.27, y: -0.09, z: 0.33 },
    { x: 0.53, y: 0.58, z: -0.44 },
    { x: -0.45, y: 0.29, z: -0.44 },
    { x: -0.21, y: -0.41, z: 0.23 },
    { x: 0.86, y: 0.02, z: -0.07 },
  ],
  edges: [
    [0, 12],
    [0, 18],
    [0, 19],
    [0, 32],
    [1, 11],
    [1, 24],
    [1, 27],
    [2, 15],
    [2, 24],
    [2, 32],
    [2, 39],
    [3, 24],
    [3, 30],
    [3, 34],
    [4, 22],
    [4, 24],
    [4, 27],
    [4, 31],
    [4, 34],
    [4, 36],
    [5, 21],
    [5, 30],
    [5, 38],
    [6, 21],
    [6, 28],
    [6, 34],
    [6, 36],
    [7, 28],
    [7, 38],
    [7, 41],
    [8, 22],
    [8, 28],
    [8, 29],
    [8, 35],
    [8, 36],
    [9, 20],
    [9, 29],
    [9, 41],
    [10, 17],
    [10, 25],
    [10, 26],
    [11, 18],
    [11, 35],
    [11, 37],
    [11, 40],
    [12, 19],
    [12, 25],
    [13, 14],
    [13, 19],
    [13, 23],
    [13, 26],
    [13, 39],
    [14, 20],
    [14, 26],
    [14, 38],
    [15, 24],
    [15, 27],
    [15, 31],
    [15, 32],
    [15, 33],
    [15, 37],
    [16, 20],
    [16, 28],
    [16, 30],
    [16, 31],
    [16, 36],
    [17, 25],
    [17, 26],
    [17, 29],
    [17, 33],
    [17, 35],
    [17, 37],
    [17, 40],
    [18, 32],
    [18, 40],
    [19, 25],
    [19, 32],
    [19, 39],
    [20, 26],
    [20, 29],
    [20, 41],
    [21, 30],
    [21, 34],
    [21, 38],
    [22, 27],
    [22, 35],
    [22, 36],
    [22, 37],
    [23, 30],
    [23, 33],
    [23, 39],
    [24, 27],
    [25, 26],
    [25, 32],
    [25, 33],
    [25, 40],
    [26, 33],
    [27, 31],
    [27, 37],
    [28, 29],
    [28, 36],
    [28, 41],
    [29, 35],
    [29, 41],
    [30, 31],
    [30, 34],
    [30, 38],
    [31, 33],
    [31, 34],
    [32, 39],
    [33, 37],
    [34, 36],
    [35, 40],
    [37, 40],
  ],
  radiusPx: 150,
  depthRadius: 1.11,
};

export type GraphVariant = "sphere" | "faceted";

export function graphForVariant(variant: GraphVariant): GraphGeometry {
  return variant === "faceted" ? FACETED_GRAPH : SPHERE_GRAPH;
}

export function projectVertex(
  graph: GraphGeometry,
  index: number,
  theta: number,
): ProjectedVertex {
  const rotated = tilt(rotateY(graph.vertices[index], theta));
  const scale = scaleAtZ(rotated.z);
  return {
    x: CENTER + rotated.x * scale,
    y: CENTER - rotated.y * scale,
    t: Math.min(1, Math.max(0, depthT(rotated.z / graph.depthRadius))),
  };
}

export function lerp(from: number, to: number, t: number): number {
  return from + (to - from) * t;
}

function buildIcosahedronEdges(): readonly (readonly [number, number])[] {
  const edgeSet = new Map<string, readonly [number, number]>();
  for (const [a, b, c] of BASE_FACES) {
    for (const [i, j] of [
      [a, b],
      [b, c],
      [c, a],
    ] as const) {
      const key = i < j ? `${i}-${j}` : `${j}-${i}`;
      if (!edgeSet.has(key)) {
        edgeSet.set(key, i < j ? [i, j] : [j, i]);
      }
    }
  }
  return [...edgeSet.values()];
}

/** The plain 12-vertex icosahedron, undivided, for icons too small for the geodesic mesh. */
export const ICOSAHEDRON_VERTEX_COUNT = BASE_VERTICES.length;
export const ICOSAHEDRON_EDGES = buildIcosahedronEdges();

export function projectIcosahedronVertex(
  index: number,
  center: number,
  camK: number,
  camDist: number,
): ProjectedVertex {
  const rotated = tilt(BASE_VERTICES[index]);
  const scale = camK / (camDist + rotated.z);
  return {
    x: center + rotated.x * scale,
    y: center - rotated.y * scale,
    t: depthT(rotated.z),
  };
}
