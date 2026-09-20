import { BRAND } from "../../tokens";
import type { InstrumentLight, Tile } from "./chamber";
import { blackToRgba, hexToRgba, mixHexToRgba, whiteToRgba } from "./colors";
import { project, ringPoint, type Camera } from "./geometry";
import type { ShadedPoint } from "./plasma";
import type { ChamberRow } from "./sceneLayout";

interface Pt {
  readonly x: number;
  readonly y: number;
}

/** Half a theta segment beyond `chamber.ts`'s own worst-case row stagger
 * (`0.5 / thetaSegments`, at the smallest `thetaSegments` this scene ever
 * builds), plus headroom: `buildChamberTiles` keeps a cell by its
 * MIDPOINT theta only, so a staggered cell straddling the front/back
 * cutoff (`theta` = pi or 2*pi) can have one real corner just past that
 * cutoff and still be a kept, painted tile -- `rowFrontArc`'s own sampled
 * domain has to reach that corner too, not stop exactly at the cutoff. */
const FRONT_ARC_MARGIN = (15 * Math.PI) / 180;

/** One row's front-facing arc (`theta` = pi -> 2*pi, the same front-face
 * half `chamber.ts`'s `facingAway` back-face-culls the real tiles to,
 * widened by `FRONT_ARC_MARGIN` on both ends), densely sampled -- the raw
 * material `buildColumnSilhouette` walks to trace the hourglass's
 * outline. */
function rowFrontArc(row: ChamberRow, camera: Camera, segments: number): Pt[] {
  const pts: Pt[] = [];
  const start = Math.PI - FRONT_ARC_MARGIN;
  const span = Math.PI + 2 * FRONT_ARC_MARGIN;
  for (let k = 0; k <= segments; k++) {
    const theta = start + (k / segments) * span;
    const p = project(ringPoint(row.radius, theta, row.y, row.z), camera);
    pts.push({ x: p.x, y: p.y });
  }
  return pts;
}

/** Dense enough that the envelope `buildColumnSilhouette` builds from
 * these samples never leaves a real tile corner more than a sub-pixel
 * outside the outline (checked by `rv2-limb.cjs`'s overhang measure),
 * without a per-frame cost -- this runs once per `measure()` (mount/
 * resize), not per animation frame. */
const SILHOUETTE_ARC_SAMPLES = 720;

/**
 * The column's opaque backing, as ONE simple outline polygon of the whole
 * hourglass silhouette (never a union of separately filled cells, which
 * always leaves antialiased hairlines/gaps at shared edges -- Rule F, and
 * the user's own report of a dark half-moon/hairlines on the shipped
 * pillar).
 *
 * A row's own front arc is not a simple bulge: past its own left/right
 * projected extremum, `x(theta)` folds back inward WHILE `y(theta)` keeps
 * moving further from the row's own centre -- most visibly on the rim
 * rows, where the curvature (sagitta) is largest -- so a real tile corner
 * in that fold can sit further from the band, in SCREEN Y, than either
 * that row's own extremum or a straight chord to the next row's extremum.
 * Neither a single limb point per row nor a per-flare convex hull (the
 * first two attempts at this fix) bounds that fold correctly: a hull's
 * own "narrowest point" is the front-centre apex (screen `x` close to the
 * axis there too), not the waist, so it cannot be told apart from the
 * waist by position alone.
 *
 * The outline instead builds a screen-space ENVELOPE directly: sample
 * every row's front arc densely (`rowFrontArc`, every row at the SAME
 * theta grid), and for every screen-space scanline (`y`, rounded to the
 * pixel) that any sampled point, or any segment between two of them,
 * crosses, record the smallest and largest `x` reached there
 * (`addSegment` interpolates along a segment so no scanline in its own
 * `y`-span is skipped). Two kinds of segment feed this: a row's own
 * WITHIN-ROW consecutive samples (its fold-back, above), and, same
 * importance, BETWEEN-ROW segments joining two adjacent rows' samples at
 * the SAME theta index -- a real tile's own edge is exactly one such
 * segment, and two adjacent rows' own `y` ranges do not always overlap
 * (each is its own local hump), so a tile can cross a `y` band that
 * NEITHER row's own within-row arc ever visits; the within-row pass alone
 * leaves that band a genuine gap (found as a several-pixel-tall,
 * pixel-wide crack in the rendered base, not explained by antialiasing).
 * The polygon is the right envelope (`max x` at each `y`, top to bottom)
 * followed by the left envelope (`min x` at each `y`, bottom to top) --
 * by construction, no sampled point (and so no real tile corner, whose
 * own `(x, y)` is on one of these same segments at a coarser `theta`) can
 * fall outside it (checked directly against `buildChamberTiles`'s own
 * output: 0 corners outside at both 1440 and 375), and the band's own
 * narrow radius still pinches the waist because at the waist's own `y`
 * range only the waist row's points (and any other row's rare, narrow
 * fold-back through that same `y`) contribute to the envelope. Filling
 * this once at alpha 1 (see
 * `paintColumnLayer`) covers both the area the real (inset) tiles occupy
 * AND every seam gap between them, so nothing drawn behind the column (the
 * far arc's streaks) is ever visible through a tile or a seam, and the top
 * rim's own front edge -- where the ceiling begins -- is the polygon's own
 * top edge, always solid.
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
  rows: readonly ChamberRow[],
  camera: Camera,
  segments = SILHOUETTE_ARC_SAMPLES,
): readonly Pt[] {
  if (rows.length < 2) {
    return [];
  }
  const arcs = rows.map((row) => rowFrontArc(row, camera, segments));
  const extents = new Map<number, YExtent>();
  // Within-row segments: a row's own fold-back (see this function's doc).
  for (const arc of arcs) {
    for (let i = 0; i < arc.length - 1; i++) {
      addSegment(extents, arc[i], arc[i + 1]);
    }
  }
  // Between-row segments, same theta index on two adjacent rows (every
  // `rowFrontArc` call above uses the SAME theta grid, so `arcs[r][k]` and
  // `arcs[r + 1][k]` share theta exactly): a real tile's own vertical-ish
  // edge connects exactly these two points, and two rows can leave a `y`
  // GAP that neither row's own arc ever visits (each row's Y range is its
  // own local hump, and adjacent rows' humps don't always overlap) while a
  // tile spanning them still draws straight through that gap -- the
  // within-row segments alone miss it, so it needs its own pass.
  for (let r = 0; r < arcs.length - 1; r++) {
    const arcA = arcs[r];
    const arcB = arcs[r + 1];
    for (let k = 0; k < arcA.length; k++) {
      addSegment(extents, arcA[k], arcB[k]);
    }
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
    ctx.fillStyle = blackToRgba(1);
    ctx.fill();
    ctx.lineWidth = 3;
    ctx.strokeStyle = blackToRgba(1);
    ctx.stroke();
  }
  for (const tile of columnTiles) {
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
    paintTile(ctx, tile);
  }
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
