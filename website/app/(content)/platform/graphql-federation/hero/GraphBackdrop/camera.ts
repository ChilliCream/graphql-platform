// A minimal perspective camera, the same idea as
// products/fusion/hero/Tokamak/geometry.ts (never imported from here):
// a world point with a real depth, a focal length and a perspective
// divide. The slab has no rotation, so this only needs the divide, not
// Tokamak's pitch tilt.
export interface Vec3 {
  readonly x: number;
  readonly y: number;
  /** Distance from the camera's near plane; 0 = nearest the camera. */
  readonly z: number;
}

export interface Camera {
  readonly originX: number;
  readonly originY: number;
  readonly focal: number;
  /** Distance from the camera to the z = 0 plane. */
  readonly dist: number;
}

export interface Projected {
  readonly x: number;
  readonly y: number;
  /** Screen px per world unit at this depth; larger = nearer the camera. */
  readonly scale: number;
  readonly depth: number;
}

export function makeCamera(
  originX: number,
  originY: number,
  focal: number,
  dist: number,
): Camera {
  return { originX, originY, focal, dist };
}

export function project(p: Vec3, cam: Camera): Projected {
  const depth = cam.dist + p.z;
  const scale = cam.focal / depth;
  return {
    x: cam.originX + p.x * scale,
    y: cam.originY + p.y * scale,
    scale,
    depth,
  };
}

/**
 * Screen px per world unit at depth `z` -- the forward half of `project`'s
 * math, with no (x, y). Used to size the world-space sampling volume (how
 * wide a world slice at this depth needs to be to cover the screen) --
 * never to place a node at a chosen screen position, which would be the
 * inverse of `project` and is not used anywhere in this folder.
 */
export function scaleAtDepth(z: number, cam: Camera): number {
  return cam.focal / (cam.dist + z);
}
