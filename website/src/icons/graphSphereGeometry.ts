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
  return { x: x1, y: y1, z: z1 };
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

function scaleAtZ(z: number): number {
  return CAM_K / (CAM_DIST + z);
}

function depthT(z: number): number {
  return (1 - z) / 2;
}

interface Projected {
  readonly x: number;
  readonly y: number;
  readonly t: number;
}

export function projectVertex(index: number, theta: number): Projected {
  const rotated = tilt(rotateY(SPHERE_VERTICES[index], theta));
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
