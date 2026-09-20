import { BRAND } from "../../tokens";
import type { InstrumentLight, Tile } from "./chamber";
import { blackToRgba, hexToRgba, mixHexToRgba, whiteToRgba } from "./colors";
import type { ShadedPoint } from "./plasma";

interface Pt {
  readonly x: number;
  readonly y: number;
}

/**
 * The column's opaque backing, as ONE simple outline polygon of the whole
 * hourglass silhouette (never a union of separately filled cells, which
 * always leaves antialiased hairlines/gaps at shared edges -- Rule F, and
 * the user's own report of a dark half-moon/hairlines on the shipped
 * pillar).
 *
 * Built directly from the DRAWN column tiles' own `rawPoly` (the un-inset
 * corner quads `buildChamberTiles` computes before the seam gap is cut
 * in): the exact same kept, `depth > 1`, near-facing tiles the column
 * layer actually paints, not a separately sampled row arc. Checkpoint 1's
 * F1 transfer ("the base must be the exact union of the tile polygons")
 * is then true by construction -- there is no approximating envelope or
 * margin left to overshoot the tiles (a stray fringe/crescent) or fall
 * short of them (a hairline gap): every tile's own 4 edges are fed into
 * the same scanline min/max-x envelope `buildColumnSilhouette` used to
 * build from the row arcs, so two neighbouring tiles' shared edge (and
 * any row-to-row edge) still closes the scanline gaps between separately
 * sampled points the same way the row-arc version's own doc described,
 * just anchored to the real tile geometry instead of a widened arc
 * sample.
 *
 * Filling this once at alpha 1 (see `paintColumnLayer`) covers both the
 * area the real (inset) tiles occupy AND every seam gap between them, so
 * nothing drawn behind the column (the far arc's streaks) is ever visible
 * through a tile or a seam, and the top rim's own front edge -- where the
 * ceiling begins -- is the polygon's own top edge, always solid.
 */

interface YExtent {
  min: number;
  max: number;
}

function extendExtent(map: Map<number, YExtent>, y: number, x: number): void {
  const key = Math.round(y);
  const cur = map.get(key);
  if (!cur) {
    map.set(key, { min: x, max: x });
  } else if (x < cur.min) {
    cur.min = x;
  } else if (x > cur.max) {
    cur.max = x;
  }
}

/** Extends `map`'s per-scanline min/max `x` with both of `a`/`b` AND every
 * integer `y` strictly between them, linearly interpolated along the `a`
 * -> `b` segment -- so a row's own consecutive dense samples never leave a
 * scanline gap between them for the envelope to miss. */
function addSegment(map: Map<number, YExtent>, a: Pt, b: Pt): void {
  extendExtent(map, a.y, a.x);
  extendExtent(map, b.y, b.x);
  const y0 = Math.round(Math.min(a.y, b.y));
  const y1 = Math.round(Math.max(a.y, b.y));
  if (y1 <= y0 + 1) {
    return;
  }
  const dy = b.y - a.y;
  for (let y = y0 + 1; y < y1; y++) {
    const t = dy !== 0 ? (y - a.y) / dy : 0;
    extendExtent(map, y, a.x + (b.x - a.x) * t);
  }
}

export function buildColumnSilhouette(
  columnTiles: readonly Tile[],
): readonly Pt[] {
  if (columnTiles.length === 0) {
    return [];
  }
  const extents = new Map<number, YExtent>();
  // Every drawn tile's own 4 edges (its true, un-inset corners): a
  // neighbouring tile's matching edge contributes the same segment (up to
  // floating-point noise), so the union naturally closes without a
  // separate "between-row" pass -- unlike the row-arc version, these
  // segments already ARE real tile edges, not samples approximating them.
  for (const tile of columnTiles) {
    const [c0, c1, c2, c3] = tile.rawPoly;
    addSegment(extents, c0, c1);
    addSegment(extents, c1, c2);
    addSegment(extents, c2, c3);
    addSegment(extents, c3, c0);
  }
  const ys = [...extents.keys()].sort((a, b) => a - b);
  if (ys.length === 0) {
    return [];
  }
  const path: Pt[] = [];
  for (const y of ys) {
    path.push({ x: extents.get(y)!.max, y });
  }
  for (let i = ys.length - 1; i >= 0; i--) {
    const y = ys[i];
    path.push({ x: extents.get(y)!.min, y });
  }
  return path;
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
 * `columnSilhouette` (see `buildColumnSilhouette` above) is ONE simple
 * outline polygon of the whole hourglass -- not a union of separately
 * filled cells, which always leaves antialiased hairlines at shared edges
 * -- filled ONCE, at alpha 1, with the local `blackToRgba` helper (a user
 * ruling for this one element: the README's "black only encodes
 * transparency" convention is overridden here). The same path is then
 * stroked once, also opaque black, at a 3px width: `buildColumnSilhouette`
 * builds its envelope from a `y`-rounded-to-the-pixel scanline map (see its
 * own doc), so the fill's own edge can land a sub-pixel outside where a
 * real tile corner sits purely from that pixel rounding (corner-
 * containment probed directly against `buildChamberTiles`'s own output:
 * 0 corners outside the FILLED polygon at both 1440 and 375 once the
 * envelope's between-row pass is included) -- the stroke (still the SAME
 * path, so this stays one opaque shape, never a second fill that could
 * itself leave a seam) closes that rounding with margin, without widening
 * the silhouette enough to show past the tiles as exposed black (G3).
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
export function paintColumnLayer(
  ctx: CanvasRenderingContext2D,
  w: number,
  h: number,
  columnSilhouette: readonly Pt[],
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
  if (columnSilhouette.length >= 3) {
    ctx.beginPath();
    ctx.moveTo(columnSilhouette[0].x, columnSilhouette[0].y);
    for (let i = 1; i < columnSilhouette.length; i++) {
      ctx.lineTo(columnSilhouette[i].x, columnSilhouette[i].y);
    }
    ctx.closePath();
    // A vertical gradient, not a flat fill: checkpoint 3's mobile/stacked
    // continuous scene reads the column as low-alpha structure away from
    // the band (`Tile.bandFade`'s own doc), and the opaque base has to
    // fade the same way its tiles do, or a solid black silhouette would
    // show through every faded tile's seams and gaps. `sideBySide` tiles
    // never carry a `bandFade` below 1 (`bandFadeFloor` defaults to 1
    // there), so this reduces to the pre-ticket flat opaque fill for that
    // mode -- both stops land on alpha 1.
    let minY = columnSilhouette[0].y;
    let maxY = columnSilhouette[0].y;
    for (const p of columnSilhouette) {
      if (p.y < minY) minY = p.y;
      if (p.y > maxY) maxY = p.y;
    }
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
