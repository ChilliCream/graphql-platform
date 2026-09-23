// Camera placement per viewport size, and the dpr/backing-pixel cap. Both
// are pure functions of the canvas's own CSS size, so the layout a frame
// draws is always a direct function of that frame's measured size -- never
// a separate "is this mobile" state that could ship a wrong first paint.
import { makeCamera, type Camera } from "./camera";
import type { LayoutMode } from "./graph";

export interface Scene {
  readonly camera: Camera;
  readonly mode: LayoutMode;
}

const PORTRAIT_MAX_WIDTH = 640;
const MAX_DPR = 2;
const MAX_BACKING_PIXELS = 4_000_000;

export function modeForSize(w: number): LayoutMode {
  return w < PORTRAIT_MAX_WIDTH ? "portrait" : "landscape";
}

export function buildScene(w: number, h: number): Scene {
  const mode = modeForSize(w);
  const originX = w / 2;
  // The camera looks at the viewport's own vertical centre in both modes
  // (matching the vertically centred copy); the gateway and the cluster
  // bands carry their own world-space Y offset in graph.ts instead of the
  // whole camera being shifted, which used to push the upper cluster band
  // off the top of the frame.
  const originY = h / 2;
  if (mode === "portrait") {
    const camera = makeCamera(originX, originY, Math.max(w, h) * 0.66, 5.5, 6);
    return { camera, mode };
  }
  const camera = makeCamera(originX, originY, Math.max(w, h) * 0.6, 6.5, 9);
  return { camera, mode };
}

export function capDpr(
  cw: number,
  ch: number,
  devicePixelRatio: number,
): number {
  const base = Math.min(devicePixelRatio || 1, MAX_DPR);
  const cap = Math.sqrt(MAX_BACKING_PIXELS / Math.max(1, cw * ch));
  return Math.max(0.75, Math.min(base, cap));
}
