import { BRAND } from "../../tokens";
import type { InstrumentLight, Tile } from "./chamber";
import { blackToRgba, hexToRgba, mixHexToRgba, whiteToRgba } from "./colors";
import type { ShadedPoint } from "./plasma";

interface Pt {
  readonly x: number;
  readonly y: number;
}

/**
 * The column's opaque backing, as the TRUE UNION of the drawn tiles' own
 * quads (hc-0-wqa fix 2, review 3's F1 transfer: "the base = the exact
 * union of the drawn tile polygons"), not a single scanline min/max-x
 * envelope polygon (that approach's own doc previously claimed this same
 * exactness, but the flare route's much larger radius jump per row
 * produces many more foreshortened, near-grazing tiles -- for those, a
 * quad's own bounding-box `y`-range can cross a scanline `y` where the
 * quad's true (non-rectangular, skewed) footprint does not actually reach
 * that `x`; pooling ALL tiles' scanline extents together then bridges
 * clean across a real gap between two unrelated tiles/limb clusters,
 * exposing flat black -- rv3-fringe.cjs's `left` edge read up to 89px at
 * 1440, on the isolated column-only canvas, real for the reasons above,
 * not a plasma-masking artifact).
 *
 * Returns each drawn column tile's own `rawPoly` (the un-inset corner
 * quad, before the seam gap is cut in) directly: `paintColumnLayer` draws
 * every one of these quads into ONE path and fills it once with the
 * canvas' default "nonzero" winding rule, which unions overlapping and
 * adjacent quads correctly (a point covered by any quad reads as inside,
 * with no double-fill artifact since it is a single `fill()` call) and,
 * unlike a scanline envelope, can never bridge across a real gap between
 * two quads that do not actually touch. Two neighbouring tiles' shared
 * edge (and the un-inset seam gap between them) is still covered because
 * `rawPoly` is pre-inset, exactly as before.
 */
export function buildColumnSilhouette(
  columnTiles: readonly Tile[],
): readonly (readonly [Pt, Pt, Pt, Pt])[] {
  return columnTiles.map((tile) => tile.rawPoly);
}

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
 * vector edge. Only used for the plasma cache, which is baked once per
 * `measure()`, so the cost never lands on a live frame.
 *
 * `paint` draws every halo path UNFILTERED into a same-size, same-transform
 * offscreen canvas (one `ctx.filter`-free stroke per path, cheap), and this
 * then composites that whole canvas onto `ctx` ONCE under a single
 * `ctx.filter = 'blur(...)'` pass. Setting `ctx.filter` before hundreds of
 * individual filtered strokes is dramatically slower than rasterising them
 * unfiltered first and blurring the whole result in one filtered
 * `drawImage`.
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
 * column layer, which is its own cached layer stamped over the far arc and
 * under the near arc every frame, so it needs the exact same tile rendering
 * the wall uses, just painted into a separate, transparent-background
 * canvas.
 */
function paintTile(ctx: CanvasRenderingContext2D, tile: Tile): void {
  const grad = ctx.createLinearGradient(
    tile.hi.x,
    tile.hi.y,
    tile.lo.x,
    tile.lo.y,
  );
  // Warmth is only allowed to nudge the fill alpha a little -- it must not
  // be what makes a tile read as lit; that is `tile.shade`'s job. Warmth's
  // real effect is the coral mix below, so the tiles nearest the band tint
  // pink without ever turning pale. The alpha lift and coral mix fraction
  // stay capped well below `tile.shade`'s own contribution, and are 0
  // outside the band's falloff, so the chamber's overall luminance budget
  // outside the band is untouched.
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
 * column it is never re-stamped per frame. The column is its own cached
 * layer so the plasma's far arc can draw between the wall and the column
 * and the near arc on top of the column; baking both into one "chamber"
 * canvas, as before, could never let the far arc's orbiting streaks sit
 * behind a static image.
 *
 * Tiles are dark slate at low alpha: `tile.shade` gives each tile's own
 * directional-light specular variation, `tile.warmth` (0 almost everywhere,
 * 1 only for the handful of tiles whose row sits right at the torus) is
 * what is allowed to brighten a tile and mix coral into it, so tiles read
 * brighter only near the plasma. Seams are never stroked: they are the gap
 * `insetQuad` already left between neighbouring tile faces.
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
 * Static COLUMN layer: the central column's own opaque backing plus its
 * tiles, painted into a transparent-background canvas (never touching navy,
 * ambient wash, instrument lights or the vignette, which are the wall's
 * job) so it can be stamped with `drawImage` on top of the far arc and
 * under the near arc every live frame -- real occlusion from actual tile
 * geometry and draw order, not a destination-out mask or a dimming factor.
 * Painted once on mount and again on resize, same as the wall; the
 * per-frame cost is one cheap `drawImage`, not a re-paint of the tiles.
 *
 * `columnSilhouette` (see `buildColumnSilhouette` above) is every drawn
 * column tile's own `rawPoly` quad, all moved into ONE path (one
 * `moveTo`/3x`lineTo`/`closePath` run per quad) and filled ONCE, at alpha
 * 1, with the local `blackToRgba` helper (a user ruling for this one
 * element: the README's "black only encodes transparency" convention is
 * overridden here) -- the canvas' default "nonzero" winding rule unions
 * every quad in that one `fill()` call (no double-fill artifact from
 * overlapping quads, since it is a single path/single fill, not a union of
 * SEPARATELY filled cells, which would leave antialiased hairlines at
 * shared edges). This is a TRUE union of the tile polygons (hc-0-wqa fix
 * 2, review 3's F1 transfer), not an approximating envelope: it can never
 * overshoot past the tiles (a stray fringe) or bridge across a real gap
 * between two tiles that do not touch, the way the prior scanline min/max
 * envelope could on the flare route's more foreshortened limb tiles
 * (`buildColumnSilhouette`'s own doc).
 *
 * The "nonzero" rule only unions correctly when every quad in the path
 * winds the same way (review 3's F1-new): `rawPoly`'s corner order follows
 * each tile's own row/column projection, which flips orientation (signed
 * area changes sign) for a real subset of tiles on the flare route's more
 * foreshortened limbs (40 of 504 at 1440, 34 of 396 at 375) -- where such a
 * reversed-winding quad overlapped a normally-wound neighbour, the two
 * windings summed to 0 and the "nonzero" rule left that pixel UNPAINTED, a
 * see-through hole in the base rather than a filled union. `orientQuad`
 * below reverses any quad whose signed area is negative before it is added
 * to the path, so every quad contributes the same winding direction and
 * the fill is a genuine union with no cancellation.
 *
 * The wall gets its own opaque backing once, for the whole canvas, from
 * the navy `fillRect` under all its tiles; the column has no such backdrop
 * of its own on this transparent canvas. Every column tile's own poly is
 * then filled a second time, opaque, in the wall's own navy (`BRAND.navy`
 * at alpha 1 -- the pre-ticket per-tile backing) before `paintTile` draws
 * its translucent specular gradient over it: without this, `paintTile`'s
 * low-alpha gradient blended `'source-over'` straight onto the opaque
 * BLACK base reads far darker than the pre-edit tile faces did (a flat
 * navy backing, not the far arc, is what `paintTile`'s alphas were tuned
 * against). The seam gap `insetQuad` leaves between neighbouring tile
 * faces, and the silhouette's own outer rim, are never covered by this
 * navy pass, so they stay bare black -- the far arc can no longer show
 * through a tile or a seam, the tiles keep their previous specular level,
 * and the column's own mean luminance still drops below its pre-edit
 * level because the seams/gaps are black, not lifted.
 */
/**
 * Returns `quad` with its corners in the same winding direction every
 * time, by sign of its shoelace-formula signed area: a negative area means
 * the quad winds the opposite way from the convention this picks (reverse
 * the corner order, which keeps the same four points/edges and only flips
 * which way the path traces them), a non-negative area is left as-is. Used
 * to normalise every quad `paintColumnLayer` adds to the base path so the
 * canvas' "nonzero" winding rule never cancels two overlapping,
 * oppositely-wound quads down to an unpainted hole (review 3's F1-new).
 */
function orientQuad(
  quad: readonly [Pt, Pt, Pt, Pt],
): readonly [Pt, Pt, Pt, Pt] {
  const [a, b, c, d] = quad;
  const signedArea2 =
    a.x * b.y -
    b.x * a.y +
    (b.x * c.y - c.x * b.y) +
    (c.x * d.y - d.x * c.y) +
    (d.x * a.y - a.x * d.y);
  return signedArea2 < 0 ? [a, d, c, b] : quad;
}

export function paintColumnLayer(
  ctx: CanvasRenderingContext2D,
  w: number,
  h: number,
  columnSilhouette: readonly (readonly [Pt, Pt, Pt, Pt])[],
  columnTiles: readonly Tile[],
  /**
   * `project({x: 0, y: torus.y, z: torus.z}, camera).y` -- the band's own
   * screen `y`, not the silhouette bbox's midpoint (F9): once the two
   * rims flare independently (checkpoint 1's asymmetric spans), the
   * silhouette's own vertical midpoint no longer sits at the band, so a
   * fixed `0.5` stop anchors the opaque core off-centre.
   */
  bandY: number,
): void {
  ctx.clearRect(0, 0, w, h);
  ctx.globalCompositeOperation = "source-over";
  if (columnSilhouette.length > 0) {
    ctx.beginPath();
    let minY = columnSilhouette[0][0].y;
    let maxY = columnSilhouette[0][0].y;
    for (const quad of columnSilhouette) {
      const oriented = orientQuad(quad);
      ctx.moveTo(oriented[0].x, oriented[0].y);
      ctx.lineTo(oriented[1].x, oriented[1].y);
      ctx.lineTo(oriented[2].x, oriented[2].y);
      ctx.lineTo(oriented[3].x, oriented[3].y);
      ctx.closePath();
      for (const p of quad) {
        if (p.y < minY) minY = p.y;
        if (p.y > maxY) maxY = p.y;
      }
    }
    // A vertical gradient, not a flat fill: checkpoint 3's mobile/stacked
    // continuous scene reads the column as low-alpha structure away from
    // the band (`Tile.bandFade`'s own doc), and the opaque base has to
    // fade the same way its tiles do, or a solid black silhouette would
    // show through every faded tile's seams and gaps. `sideBySide` tiles
    // never carry a `bandFade` below 1 (`bandFadeFloor` defaults to 1
    // there), so this reduces to the pre-ticket flat opaque fill for that
    // mode -- both stops land on alpha 1.
    // The two edge fades come from the FIRST and LAST row pairs' own
    // `bandFade` (F10), not a blanket minimum over every tile: with the
    // two rims flaring independently, the two ends fade at different
    // rates, and a single shared minimum would flatten that asymmetry.
    // Row-pair 0 and the highest `rowPair` index are each one rim's own
    // nearest pair; which one lands at the gradient's `minY` end (screen
    // top) vs `maxY` end depends on the camera's own projection, not row
    // index, so it is resolved here from each pair's own mean screen `y`
    // rather than assumed.
    let maxRowPair = 0;
    for (const t of columnTiles) {
      if (t.rowPair > maxRowPair) maxRowPair = t.rowPair;
    }
    const pairFade = (pair: number): { fade: number; avgY: number } => {
      let fade = 1;
      let ySum = 0;
      let n = 0;
      for (const t of columnTiles) {
        if (t.rowPair !== pair) continue;
        fade = Math.min(fade, t.bandFade);
        ySum += (t.rawPoly[0].y + t.rawPoly[2].y) / 2;
        n++;
      }
      return { fade, avgY: n > 0 ? ySum / n : minY };
    };
    const first = pairFade(0);
    const last = pairFade(maxRowPair);
    const topFade = first.avgY <= last.avgY ? first.fade : last.fade;
    const bottomFade = first.avgY <= last.avgY ? last.fade : first.fade;
    const grad = ctx.createLinearGradient(0, minY, 0, maxY);
    const span = maxY - minY || 1;
    const bandStop = Math.min(0.999, Math.max(0.001, (bandY - minY) / span));
    grad.addColorStop(0, blackToRgba(topFade));
    grad.addColorStop(bandStop, blackToRgba(1));
    grad.addColorStop(1, blackToRgba(bottomFade));
    ctx.fillStyle = grad;
    ctx.fill();
  }
  for (const tile of columnTiles) {
    ctx.globalAlpha = tile.bandFade;
    ctx.fillStyle = hexToRgba(BRAND.navy, 1);
    ctx.beginPath();
    ctx.moveTo(tile.poly[0].x, tile.poly[0].y);
    ctx.lineTo(tile.poly[1].x, tile.poly[1].y);
    ctx.lineTo(tile.poly[2].x, tile.poly[2].y);
    ctx.lineTo(tile.poly[3].x, tile.poly[3].y);
    ctx.closePath();
    ctx.fill();
  }
  for (const tile of columnTiles) {
    ctx.globalAlpha = tile.bandFade;
    paintTile(ctx, tile);
  }
  ctx.globalAlpha = 1;
}

/**
 * Style for one cached plasma layer (the far half or the near half of the
 * band, see `paintPlasmaLayer`). `alphaMul` scales every alpha in the pass
 * -- the coral core, the white-hot centre and both halo passes together --
 * so the far half's reduced alpha/desaturation and the mobile density scale
 * both fall out of one number, and a separate `colorHex` (`BRAND.coral` for
 * the near half, `BRAND.coralSoft` for the far half) carries the
 * desaturation itself.
 */
interface PlasmaLayerStyle {
  readonly colorHex: string;
  readonly alphaMul: number;
}

/**
 * One cached plasma layer: the dense static majority of streaks on one half
 * of the band (far or near, see `index.tsx`'s split by `isFarSide`), baked
 * once per `measure()` with `lighter` compositing so their glow adds
 * instead of covering. Every luminous element gets a wide, low-alpha,
 * canvas-blurred halo pass underneath its sharp core -- affordable here
 * because this canvas is re-rendered only on resize, never per frame; the
 * live layer draws the small orbiting subset, the helix and the breathing
 * bloom on top of this every frame using a cheaper downscaled bloom pass
 * instead (see `index.tsx`).
 *
 * The two layers (far, near) are `drawImage`-d into the live canvas on
 * either side of the column layer every frame: real occlusion from draw
 * order rather than a destination-out mask or a dimming factor; the
 * column's own coral tint is a separate `'lighter'` pass `index.tsx` draws
 * right after the column so it visibly lands on the tiles, not baked in
 * here.
 *
 * Base alphas are kept low so individual streaks stay readable without
 * blowing out, and the halo passes carry the glow instead of raising bloom
 * to compensate. The picture is slate plus coral: no other service-colour
 * streams are drawn here.
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
