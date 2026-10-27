interface Vec3 {
  readonly x: number;
  readonly y: number;
  readonly z: number;
}

export interface GraphGeometry {
  readonly vertices: readonly Vec3[];
  readonly edges: readonly (readonly [number, number])[];
  readonly radiusPx: number;
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
};

/** A fixed asymmetric faceted network for the Federation closing CTA. */
export const FACETED_GRAPH: GraphGeometry = {
  vertices: [
    { x: -0.91, y: -0.18, z: -0.28 },
    { x: -0.7, y: 0.05, z: 0.48 },
    { x: -0.62, y: 0.37, z: -0.16 },
    { x: -0.26, y: 0.78, z: 0.07 },
    { x: -0.06, y: 0.44, z: 0.54 },
    { x: 0.25, y: 0.69, z: -0.32 },
    { x: 0.56, y: 0.53, z: 0.41 },
    { x: 0.8, y: 0.31, z: -0.16 },
    { x: 0.55, y: 0.02, z: 0.51 },
    { x: 0.74, y: -0.28, z: 0.11 },
    { x: 0.09, y: -0.43, z: -0.26 },
    { x: -0.29, y: -0.31, z: 0.5 },
    { x: -0.72, y: -0.48, z: -0.46 },
    { x: -0.11, y: 0.15, z: -0.55 },
    { x: 0.31, y: 0.22, z: -0.53 },
    { x: -0.39, y: 0.12, z: 0.07 },
  ],
  edges: [
    [0, 1],
    [0, 12],
    [0, 15],
    [1, 2],
    [1, 4],
    [1, 11],
    [1, 15],
    [2, 3],
    [2, 4],
    [2, 13],
    [2, 15],
    [3, 4],
    [3, 5],
    [3, 13],
    [4, 5],
    [4, 6],
    [4, 15],
    [5, 6],
    [5, 7],
    [5, 13],
    [6, 7],
    [6, 8],
    [6, 14],
    [7, 8],
    [7, 9],
    [7, 14],
    [8, 9],
    [8, 10],
    [8, 11],
    [8, 14],
    [9, 10],
    [9, 14],
    [10, 11],
    [10, 12],
    [10, 14],
    [11, 12],
    [11, 15],
    [12, 15],
    [13, 14],
    [13, 15],
    [14, 15],
  ],
  radiusPx: 145,
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
    t: depthT(rotated.z),
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
