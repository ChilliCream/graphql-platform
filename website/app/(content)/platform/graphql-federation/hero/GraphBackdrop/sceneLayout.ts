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
  if (mode === "portrait") {
    // Shifted down so the gateway's coral core clears the copy block
    // (rendered above it) instead of sitting dead-centre behind the text.
    const originY = h / 2 + h * 0.12;
    const camera = makeCamera(originX, originY, Math.max(w, h) * 0.66, 6.8, 6);
    return { camera, mode };
  }
  const originY = h / 2 + h * 0.27;
  const camera = makeCamera(originX, originY, Math.max(w, h) * 0.6, 8.5, 9);
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
