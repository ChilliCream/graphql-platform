import { BRAND } from "../../../tokens";
import type { InstrumentLight, Tile } from "./chamber";
import { hexToRgba, mixHexToRgba } from "./colors";
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
 * A wide, low-alpha halo pass underneath the caller's normal sharp strokes,
 * so every luminous element this touches carries a glow instead of a hard
 * vector edge (README section 3, planner ruling 187 item 3). Only used for
 * the plasma cache, which is baked once per `measure()`, so the cost never
 * lands on a live frame.
 *
 * `paint` draws every halo path UNFILTERED into a same-size, same-transform
 * offscreen canvas (one `ctx.filter`-free stroke per path, cheap), and this
 * then composites that whole canvas onto `ctx` ONCE under a single
 * `ctx.filter = 'blur(...)'` pass. Setting `ctx.filter` before hundreds of
 * individual filtered strokes (the original approach) is not just slower --
 * it is catastrophically slower: a synthetic bench of 2100 strokes in this
 * same headless Chromium measured ~43.7s with `ctx.filter` set per stroke
 * versus ~29ms for one filtered `drawImage` of the same strokes rasterised
 * unfiltered first (see ticket hc-0-wrc.3 F2), which is exactly what caused
 * the two ~8.3s long tasks that blocked the RAF loop from reaching 60fps
 * until t=17s.
 */
function withBlurHalo(
  ctx: CanvasRenderingContext2D,
  w: number,
  h: number,
  blurPx: number,
  paint: (haloCtx: CanvasRenderingContext2D) => void,
): void {
  const canvas = ctx.canvas;
  const off = document.createElement("canvas");
  off.width = canvas.width;
  off.height = canvas.height;
  const offCtx = off.getContext("2d");
  if (!offCtx) {
    return;
  }
  offCtx.setTransform(ctx.getTransform());
  offCtx.lineCap = "round";
  offCtx.globalCompositeOperation = "lighter";
  paint(offCtx);

  ctx.save();
  ctx.filter = `blur(${blurPx}px)`;
  ctx.globalCompositeOperation = "lighter";
  ctx.drawImage(off, 0, 0, off.width, off.height, 0, 0, w, h);
  ctx.restore();
}

/**
 * Static chamber layer: the tiled walls (specular-gradient trapezoids, dark
 * inset seams, fastener dots on large-enough tiles), the instrument lights
 * and the vignette. Painted once on mount and again on resize; the plasma
 * cache and the live layer draw on top of it every frame without ever
 * repainting this canvas.
 *
 * Tiles are dark slate at low alpha (mean luminance well under the
 * planner's 20% ceiling outside the plasma band): `tile.shade` gives each
 * tile's own directional-light specular variation, `tile.warmth` (0 almost
 * everywhere, 1 only for the handful of tiles whose row sits right at the
 * torus) is what is allowed to brighten a tile and mix coral into it --
 * "brighter only near the plasma" (planner ruling hc-0-wrc.3 comment 187
 * item 2). Seams are never stroked: they are the gap `insetQuad` already
 * left between neighbouring tile faces.
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

  // A whisper of ambient slate wash across the whole frame, well under the
  // luminance budget on its own, so any sliver the tile geometry does not
  // quite reach still reads as the same dim vessel air rather than a hard
  // cut to bare page navy.
  const ambient = ctx.createRadialGradient(
    w * 0.62,
    h * 0.5,
    0,
    w * 0.62,
    h * 0.5,
    Math.max(w, h) * 0.75,
  );
  ambient.addColorStop(0, hexToRgba(BRAND.slate, 0.02));
  ambient.addColorStop(1, hexToRgba(BRAND.slate, 0));
  ctx.fillStyle = ambient;
  ctx.fillRect(0, 0, w, h);

  for (const tile of tiles) {
    const grad = ctx.createLinearGradient(
      tile.hi.x,
      tile.hi.y,
      tile.lo.x,
      tile.lo.y,
    );
    // Warmth is only allowed to nudge the fill alpha a little (~0.08 at
    // most) -- it must not be what makes a tile read as lit; that is
    // `tile.shade`'s job. Warmth's real effect is the coral mix below, so
    // the tiles nearest the band tint pink without ever turning pale (fix
    // 1/F1: warmth used to add up to 0.5 alpha on its own, which is what
    // painted the whole column as a pale cylinder).
    // Warmth's alpha lift and its coral mix fraction were both raised
    // (hc-0-wrc.3 review 2, F3: "no visible pink on the column between the
    // streaks") -- still capped well below `tile.shade`'s own contribution,
    // and still 0 outside the band's falloff, so the chamber's overall
    // under-20%-outside-the-band luminance budget is untouched.
    const warm = tile.warmth;
    const hiAlpha = 0.11 + tile.shade * 0.2 + warm * 0.15;
    const midAlpha = 0.08 + tile.shade * 0.14 + warm * 0.12;
    const loAlpha = 0.04 + tile.shade * 0.07 + warm * 0.08;
    grad.addColorStop(
      0,
      mixHexToRgba(BRAND.slate, BRAND.coral, warm * 0.85, hiAlpha),
    );
    grad.addColorStop(
      0.45,
      mixHexToRgba(BRAND.slate, BRAND.coral, warm * 0.7, midAlpha),
    );
    grad.addColorStop(
      1,
      mixHexToRgba(BRAND.slate, BRAND.coral, warm * 0.45, loAlpha),
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
      const r = Math.max(1.2, tile.size * 0.06);
      for (const d of [d0, d1]) {
        ctx.fillStyle = hexToRgba(BRAND.navy, 0.6);
        ctx.beginPath();
        ctx.arc(d.x, d.y + r * 0.3, r, 0, Math.PI * 2);
        ctx.fill();
        ctx.fillStyle = `rgba(255,255,255,${0.3 * tile.shade + 0.06})`;
        ctx.beginPath();
        ctx.arc(d.x - r * 0.2, d.y - r * 0.2, r * 0.5, 0, Math.PI * 2);
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
    Math.min(w, h) * 0.22,
    w / 2,
    h / 2,
    Math.max(w, h) * 0.72,
  );
  vignette.addColorStop(0, hexToRgba(BRAND.navy, 0));
  vignette.addColorStop(1, hexToRgba(BRAND.navy, 0.7));
  ctx.fillStyle = vignette;
  ctx.fillRect(0, 0, w, h);
}

/** Where the plasma band's own centre projects to, and how big it reads on screen -- passed in by `index.tsx` (computed from the layout/camera, not re-derived here) so the bake-time bloom pass can be sized and placed to match the band it is bloom-ing (hc-0-wrc.3 review 2, F3). */
export interface BandBloomTarget {
  readonly x: number;
  readonly y: number;
  /** Projected ring width in px, at the band's own depth: the wide blur pass is ~0.15 of this. */
  readonly ringWidthPx: number;
  /** Half the band's projected vertical thickness in px: the coral halo behind the column reads about 1.2x this. */
  readonly bandHalfHeightPx: number;
}

/**
 * Cached plasma layer: the dense static majority of streaks (hundreds),
 * baked once per `measure()` with `lighter` compositing so their glow adds
 * instead of covering. Every luminous element gets a wide, low-alpha,
 * canvas-blurred halo pass underneath its sharp core (README section 3,
 * planner ruling 187 item 3) -- affordable here because this canvas is
 * re-rendered only on resize, never per frame; the live layer draws the
 * small orbiting subset, the helix and the breathing bloom on top of this
 * every frame using a cheaper downscaled bloom pass instead (see
 * `index.tsx`).
 *
 * On top of the existing tight per-stroke halo (3.5px, under the sharp
 * cores) a second, much wider blur pass runs over the WHOLE static band at
 * once (~0.15 of the projected ring width) plus a soft coral radial halo
 * behind the column section inside the band -- both still baked once here,
 * never per frame -- so the picture reads as a lit, glowing ring instead of
 * a cloud of dashes with "almost no visible halo" (hc-0-wrc.3 review 2, F3).
 *
 * The five service-colour streams (README section 4) were dropped
 * entirely (ticket hc-0-wrc.3 F3/verifier correction): even as dash
 * trails they read as thin straight lines crossing the frame at this
 * camera distance, and the planner's own ruling permits dropping them --
 * "the picture is slate plus coral."
 */
export function paintPlasmaCache(
  ctx: CanvasRenderingContext2D,
  w: number,
  h: number,
  staticStreakPaths: readonly ShadedPoint[][],
  bloomTarget: BandBloomTarget,
): void {
  ctx.clearRect(0, 0, w, h);
  ctx.lineCap = "round";
  ctx.globalCompositeOperation = "lighter";

  const halo = ctx.createRadialGradient(
    bloomTarget.x,
    bloomTarget.y,
    0,
    bloomTarget.x,
    bloomTarget.y,
    Math.max(1, bloomTarget.bandHalfHeightPx * 0.45),
  );
  halo.addColorStop(0, hexToRgba(BRAND.coral, 0.18));
  halo.addColorStop(1, hexToRgba(BRAND.coral, 0));
  ctx.fillStyle = halo;
  ctx.beginPath();
  ctx.arc(
    bloomTarget.x,
    bloomTarget.y,
    Math.max(1, bloomTarget.bandHalfHeightPx * 0.45),
    0,
    Math.PI * 2,
  );
  ctx.fill();

  withBlurHalo(
    ctx,
    w,
    h,
    Math.max(1, bloomTarget.ringWidthPx * 0.022),
    (haloCtx) => {
      for (const path of staticStreakPaths) {
        strokeShadedPath(haloCtx, path, BRAND.coral, 3, 0.18);
      }
    },
  );

  withBlurHalo(ctx, w, h, 3.5, (haloCtx) => {
    for (const path of staticStreakPaths) {
      strokeShadedPath(haloCtx, path, BRAND.coral, 2.6, 0.08);
    }
  });
  for (const path of staticStreakPaths) {
    strokeShadedPath(ctx, path, BRAND.coral, 2.2, 0.15);
  }
  for (const path of staticStreakPaths) {
    strokeShadedPathRgba(ctx, path, [255, 255, 255], 0.7, 0.2);
  }

  ctx.globalCompositeOperation = "source-over";
}
