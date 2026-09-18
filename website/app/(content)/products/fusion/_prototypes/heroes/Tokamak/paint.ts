import { BRAND } from "../../../tokens";
import type { InstrumentLight, Tile } from "./chamber";
import { hexToRgba, mixHexToRgba, whiteToRgba } from "./colors";
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

/**
 * Same as `strokeShadedPath` but for a colour `hexToRgba`/`mixHexToRgba`
 * cannot build (white has no `BRAND` hex): `toRgba` is `whiteToRgba` or
 * `warmWhiteToRgba` from `./colors` (white-hot centres), called with the
 * same per-segment alpha `strokeShadedPath` computes from `hexToRgba`.
 */
export function strokeShadedPathRgba(
  ctx: CanvasRenderingContext2D,
  pts: readonly ShadedPoint[],
  toRgba: (alpha: number) => string,
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
    ctx.strokeStyle = toRgba(Math.min(1, baseAlpha * near));
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
 * One tile's specular-gradient face, inset seam gap and (on large tiles)
 * fastener dots -- the shared paint code for both the wall layer and the
 * column layer (hc-0-wrc.3 comment 206: the column is now its own cached
 * layer, stamped over the far arc and under the near arc every frame, so it
 * needs the exact same tile rendering the wall always had, just painted
 * into a separate, transparent-background canvas).
 */
function paintTile(ctx: CanvasRenderingContext2D, tile: Tile): void {
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
      ctx.fillStyle = whiteToRgba(0.3 * tile.shade + 0.06);
      ctx.beginPath();
      ctx.arc(d.x - r * 0.2, d.y - r * 0.2, r * 0.5, 0, Math.PI * 2);
      ctx.fill();
    }
  }
}

/**
 * Static WALL layer: the outer vessel's tiled walls (specular-gradient
 * trapezoids, dark inset seams, fastener dots), the instrument lights and
 * the vignette. Painted once on mount and again on resize into its own
 * bottom-most canvas -- nothing ever needs to draw behind it, so unlike the
 * column it is never re-stamped per frame (hc-0-wrc.3 comment 206: the
 * column moved to its own cached layer so the plasma's far arc can draw
 * between the wall and the column and the near arc on top of the column;
 * baking both into one "chamber" canvas, as before, could never let the
 * far arc's orbiting streaks sit behind a static image).
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
export function paintWall(
  ctx: CanvasRenderingContext2D,
  w: number,
  h: number,
  wallTiles: readonly Tile[],
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

  for (const tile of wallTiles) {
    paintTile(ctx, tile);
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

/**
 * Static COLUMN layer: the central column's own tiles, painted into a
 * transparent-background canvas (never touching navy, ambient wash,
 * instrument lights or the vignette, which are the wall's job) so it can be
 * stamped with `drawImage` on top of the far arc and under the near arc
 * every live frame (hc-0-wrc.3 comment 206) -- real occlusion from actual
 * tile geometry and draw order, not a destination-out mask or a dimming
 * factor. Painted once on mount and again on resize, same as the wall; the
 * per-frame cost is one cheap `drawImage`, not a re-paint of the tiles.
 *
 * Each tile gets an opaque navy backing, in its own polygon, BEFORE its
 * usual low-alpha specular gradient: the wall gets its opaque backing once,
 * for the whole canvas, from the navy `fillRect` under all its tiles, but
 * the column has no such backdrop of its own on this transparent canvas --
 * without it, `tile.poly`'s low-alpha gradient blended `'source-over'`
 * straight onto the bright far-arc streaks underneath (stamped into the
 * live canvas first) reads as a pale translucent veil, not a solid tile
 * standing in front of them. The seam gaps between tiles (`insetQuad`'s own
 * inset) get no backing, so they stay genuinely transparent -- the far arc
 * still shows faintly through the seams, exactly as "seams are gaps".
 */
export function paintColumnLayer(
  ctx: CanvasRenderingContext2D,
  w: number,
  h: number,
  columnTiles: readonly Tile[],
): void {
  ctx.clearRect(0, 0, w, h);
  ctx.globalCompositeOperation = "source-over";
  ctx.fillStyle = hexToRgba(BRAND.navy, 0.94);
  for (const tile of columnTiles) {
    ctx.beginPath();
    ctx.moveTo(tile.poly[0].x, tile.poly[0].y);
    ctx.lineTo(tile.poly[1].x, tile.poly[1].y);
    ctx.lineTo(tile.poly[2].x, tile.poly[2].y);
    ctx.lineTo(tile.poly[3].x, tile.poly[3].y);
    ctx.closePath();
    ctx.fill();
  }
  for (const tile of columnTiles) {
    paintTile(ctx, tile);
  }
}

/**
 * Style for one cached plasma layer (the far half or the near half of the
 * band, see `paintPlasmaLayer`). `alphaMul` scales every alpha in the pass
 * -- the coral core, the white-hot centre and both halo passes together --
 * so the far half's "reduced alpha and desaturation" (hc-0-wrc.3 comment
 * 206) and the mobile density scale (review 3, F1: the 375 band is dense
 * enough at full alpha to blow out) both fall out of one number, and a
 * separate `colorHex` (`BRAND.coral` for the near half, `BRAND.coralSoft`
 * for the far half) carries the desaturation itself.
 */
export interface PlasmaLayerStyle {
  readonly colorHex: string;
  readonly alphaMul: number;
}

/**
 * One cached plasma layer: the dense static majority of streaks on one half
 * of the band (far or near, see `index.tsx`'s split by `isFarSide`), baked
 * once per `measure()` with `lighter` compositing so their glow adds
 * instead of covering. Every luminous element gets a wide, low-alpha,
 * canvas-blurred halo pass underneath its sharp core (README section 3,
 * planner ruling 187 item 3) -- affordable here because this canvas is
 * re-rendered only on resize, never per frame; the live layer draws the
 * small orbiting subset, the helix and the breathing bloom on top of this
 * every frame using a cheaper downscaled bloom pass instead (see
 * `index.tsx`).
 *
 * The two layers (far, near) are `drawImage`-d into the live canvas on
 * either side of the column layer every frame (hc-0-wrc.3 comment 206),
 * real occlusion from draw order rather than a destination-out mask or a
 * dimming factor; the column's own coral tint is a separate `'lighter'`
 * pass `index.tsx` draws right after the column so it visibly lands on the
 * tiles, not baked in here.
 *
 * Base alphas cut from 2.2px@0.15/0.7px@0.2 (hc-0-wrc.3 review 3, F1: the
 * band blew out to white-salmon over 27-29% of its own area, ceiling 15%)
 * -- individual streaks now stay readable and the halo passes carry the
 * glow instead, per the planner's ruling not to raise bloom to compensate.
 *
 * The five service-colour streams (README section 4) were dropped
 * entirely (ticket hc-0-wrc.3 F3/verifier correction): even as dash
 * trails they read as thin straight lines crossing the frame at this
 * camera distance, and the planner's own ruling permits dropping them --
 * "the picture is slate plus coral."
 */
export function paintPlasmaLayer(
  ctx: CanvasRenderingContext2D,
  w: number,
  h: number,
  paths: readonly ShadedPoint[][],
  ringWidthPx: number,
  style: PlasmaLayerStyle,
): void {
  ctx.clearRect(0, 0, w, h);
  ctx.lineCap = "round";
  ctx.globalCompositeOperation = "lighter";
  const { colorHex, alphaMul } = style;

  withBlurHalo(ctx, w, h, Math.max(1, ringWidthPx * 0.022), (haloCtx) => {
    for (const path of paths) {
      strokeShadedPath(haloCtx, path, colorHex, 3, 0.18 * alphaMul);
    }
  });

  withBlurHalo(ctx, w, h, 3.5, (haloCtx) => {
    for (const path of paths) {
      strokeShadedPath(haloCtx, path, colorHex, 2.6, 0.08 * alphaMul);
    }
  });
  for (const path of paths) {
    strokeShadedPath(ctx, path, colorHex, 2, 0.12 * alphaMul);
  }
  for (const path of paths) {
    strokeShadedPathRgba(ctx, path, whiteToRgba, 0.6, 0.07 * alphaMul);
  }

  ctx.globalCompositeOperation = "source-over";
}
