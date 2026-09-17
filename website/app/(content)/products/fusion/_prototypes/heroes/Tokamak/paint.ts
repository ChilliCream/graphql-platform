import { SERVICE_SPECTRUM } from "../../spectrum";
import { BRAND } from "../../../tokens";
import type { InstrumentLight, Tile } from "./chamber";
import { hexToRgba } from "./colors";
import type { ShadedPoint } from "./plasma";

/**
 * Strokes a projected, per-point-shaded path (a streak arc, the helix, a
 * spiral) as a short run of segments, each segment's width and alpha scaled
 * by its own near/far factor -- the far side of the torus draws dimmer and
 * thinner than the near side purely from the projection's `scale`, never a
 * hand-set 2D annulus fade.
 */
export function strokeShadedPath(
  ctx: CanvasRenderingContext2D,
  pts: readonly ShadedPoint[],
  colorHex: string,
  baseWidth: number,
  baseAlpha: number,
): void {
  if (pts.length < 2) {
    return;
  }
  for (let i = 0; i < pts.length - 1; i++) {
    const a = pts[i];
    const b = pts[i + 1];
    const near = (a.near + b.near) / 2;
    ctx.beginPath();
    ctx.moveTo(a.x, a.y);
    ctx.lineTo(b.x, b.y);
    ctx.strokeStyle = hexToRgba(colorHex, Math.min(1, baseAlpha * near));
    ctx.lineWidth = Math.max(0.4, baseWidth * near);
    ctx.stroke();
  }
}

/** Same as `strokeShadedPath` but with a fixed `rgba(...)` colour string (white-hot centres). */
export function strokeShadedPathRgba(
  ctx: CanvasRenderingContext2D,
  pts: readonly ShadedPoint[],
  rgb: readonly [number, number, number],
  baseWidth: number,
  baseAlpha: number,
): void {
  if (pts.length < 2) {
    return;
  }
  for (let i = 0; i < pts.length - 1; i++) {
    const a = pts[i];
    const b = pts[i + 1];
    const near = (a.near + b.near) / 2;
    ctx.beginPath();
    ctx.moveTo(a.x, a.y);
    ctx.lineTo(b.x, b.y);
    ctx.strokeStyle = `rgba(${rgb[0]},${rgb[1]},${rgb[2]},${Math.min(1, baseAlpha * near)})`;
    ctx.lineWidth = Math.max(0.4, baseWidth * near);
    ctx.stroke();
  }
}

/**
 * Static chamber layer: the tiled walls (specular-gradient trapezoids, dark
 * inset seams, fastener dots on large-enough tiles), the instrument lights
 * and the vignette. Painted once on mount and again on resize; the plasma
 * cache and the live layer draw on top of it every frame without ever
 * repainting this canvas.
 */
export function paintChamber(
  ctx: CanvasRenderingContext2D,
  w: number,
  h: number,
  tiles: readonly Tile[],
  lights: readonly InstrumentLight[],
): void {
  ctx.clearRect(0, 0, w, h);
  ctx.globalCompositeOperation = "source-over";
  ctx.fillStyle = hexToRgba(BRAND.navy, 1);
  ctx.fillRect(0, 0, w, h);

  for (const tile of tiles) {
    const grad = ctx.createLinearGradient(
      tile.hi.x,
      tile.hi.y,
      tile.lo.x,
      tile.lo.y,
    );
    const base = tile.shade;
    grad.addColorStop(
      0,
      hexToRgba(BRAND.slate, Math.min(0.92, base * 0.6 + 0.42)),
    );
    grad.addColorStop(
      0.45,
      hexToRgba(BRAND.slate, Math.min(0.78, base * 0.5 + 0.3)),
    );
    grad.addColorStop(
      1,
      hexToRgba(BRAND.slate, Math.max(0.26, base * 0.34 + 0.12)),
    );
    ctx.fillStyle = grad;
    ctx.beginPath();
    ctx.moveTo(tile.poly[0].x, tile.poly[0].y);
    ctx.lineTo(tile.poly[1].x, tile.poly[1].y);
    ctx.lineTo(tile.poly[2].x, tile.poly[2].y);
    ctx.lineTo(tile.poly[3].x, tile.poly[3].y);
    ctx.closePath();
    ctx.fill();

    if (tile.fastener) {
      const d0 = {
        x: (tile.poly[0].x + tile.poly[1].x) / 2,
        y: (tile.poly[0].y + tile.poly[1].y) / 2,
      };
      const d1 = {
        x: (tile.poly[2].x + tile.poly[3].x) / 2,
        y: (tile.poly[2].y + tile.poly[3].y) / 2,
      };
      const r = Math.max(1.4, tile.size * 0.08);
      for (const d of [d0, d1]) {
        ctx.fillStyle = hexToRgba(BRAND.navy, 0.75);
        ctx.beginPath();
        ctx.arc(d.x, d.y + r * 0.3, r, 0, Math.PI * 2);
        ctx.fill();
        ctx.fillStyle = `rgba(255,255,255,${0.5 * tile.shade + 0.1})`;
        ctx.beginPath();
        ctx.arc(d.x - r * 0.2, d.y - r * 0.2, r * 0.55, 0, Math.PI * 2);
        ctx.fill();
      }
    }
  }

  ctx.shadowBlur = 7;
  ctx.shadowColor = hexToRgba(BRAND.cyan, 0.9);
  ctx.fillStyle = hexToRgba(BRAND.cyan, 0.75);
  for (const light of lights) {
    ctx.beginPath();
    ctx.arc(light.x, light.y, light.r, 0, Math.PI * 2);
    ctx.fill();
  }
  ctx.shadowBlur = 0;

  const vignette = ctx.createRadialGradient(
    w / 2,
    h / 2,
    Math.min(w, h) * 0.28,
    w / 2,
    h / 2,
    Math.max(w, h) * 0.78,
  );
  vignette.addColorStop(0, hexToRgba(BRAND.navy, 0));
  vignette.addColorStop(1, hexToRgba(BRAND.navy, 0.6));
  ctx.fillStyle = vignette;
  ctx.fillRect(0, 0, w, h);
}

/**
 * Cached plasma layer: the dense static majority of streaks (hundreds) plus
 * the five service-colour spirals, baked once per `measure()` with
 * `lighter` compositing so their glow adds instead of covering. Re-rendered
 * only on resize, never per frame -- the live layer draws the small
 * orbiting subset, the helix and the breathing bloom on top of this every
 * frame instead.
 */
export function paintPlasmaCache(
  ctx: CanvasRenderingContext2D,
  w: number,
  h: number,
  staticStreakPaths: readonly ShadedPoint[][],
  spiralPaths: readonly ShadedPoint[][],
): void {
  ctx.clearRect(0, 0, w, h);
  ctx.lineCap = "round";
  ctx.globalCompositeOperation = "lighter";

  for (let i = 0; i < spiralPaths.length; i++) {
    const spectrum = SERVICE_SPECTRUM[i % SERVICE_SPECTRUM.length];
    strokeShadedPath(ctx, spiralPaths[i], spectrum.color, 1.1, 0.16);
  }

  for (const path of staticStreakPaths) {
    strokeShadedPath(ctx, path, BRAND.coral, 2.6, 0.16);
  }
  for (const path of staticStreakPaths) {
    strokeShadedPathRgba(ctx, path, [255, 255, 255], 0.9, 0.5);
  }

  ctx.globalCompositeOperation = "source-over";
}
