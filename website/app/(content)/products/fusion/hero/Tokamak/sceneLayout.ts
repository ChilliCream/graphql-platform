import { makeCamera, project, torusPoint, type Camera } from "./geometry";

export interface ChamberRow {
  readonly y: number;
  readonly z: number;
  readonly radius: number;
}

export interface TorusParams {
  readonly R: number;
  readonly a: number;
  readonly y: number;
  readonly z: number;
}

export type TokamakMode = "mobile" | "stacked" | "sideBySide";

export interface TokamakLayout {
  readonly w: number;
  readonly h: number;
  readonly mode: TokamakMode;
  readonly camera: Camera;
  /**
   * The central column: a near-constant-radius cylinder of tiles running
   * most of the frame's height, sharing the torus' axis.
   */
  readonly columnRows: readonly ChamberRow[];
  readonly columnThetaSegments: number;
  /**
   * The outer vessel wall: wide around the plasma's height (so it wraps
   * past the frame's left/right edges, the way the near wall of a tube
   * wraps around someone standing inside it) and narrowing back down to
   * meet the column's radius above and below the band. The viewer never
   * sees the vessel's outside silhouette: wall + column together cover the
   * frame, darkening toward the edges only through the vignette, never
   * through bare ground.
   */
  readonly wallRows: readonly ChamberRow[];
  readonly wallThetaSegments: number;
  readonly torus: TorusParams;
  /** `stacked`/`sideBySide`: the x the copy-clear zone ends at (0 off `mobile`). */
  readonly artLeft: number;
  /** `mobile`/`stacked`: the y the copy-clear zone ends at (0 on `sideBySide`), i.e. `copyRect.bottom + 24`. */
  readonly artTop: number;
}

/** The hero copy block's own rendered rect (see `index.tsx`'s `measureCopyRect`), relative to the section. */
export interface CopyRect {
  /** The copy's own rightmost real content (the teaser paragraph, not the wider `xl:max-w-2xl` block it sits in -- see `measureCopyRect`). */
  readonly right: number;
  /** The copy's own bottom (the button row's bottom, not the block's own bottom padding). */
  readonly bottom: number;
}

/** Below this width, `computeLayout` never reads `copyRect` -- also used by `index.tsx`'s remeasure guard to avoid a font-load reflow forcing a rebuild that would come out identical. */
export const MOBILE_BREAKPOINT = 768;
/** `md`/`lg`: STACKED reuses the mobile (below-the-copy) construction, refit to the measured band. */
const SIDE_BY_SIDE_BREAKPOINT = 1280;
const ROWS_PER_SIDE = 9;

/**
 * Rows from the near/bottom rim to the far/top rim (and the mirrored rows
 * below), radius easing from `waistRadius` at the centre row (`t = 0`) to
 * `edgeRadius` at the outermost rows (`t = +-1`), with `zSpread` pushing the
 * edge rows away from the camera so they compress toward a far wall instead
 * of holding one flat depth. Used for both the narrow column (waist small,
 * edge a little larger) and the wide wall (waist huge so it wraps past the
 * frame edges, edge small so it narrows back down to meet the column).
 */
function buildTaperedRows(
  waistRadius: number,
  edgeRadius: number,
  ySpread: number,
  zSpread: number,
  power: number,
): ChamberRow[] {
  const rows: ChamberRow[] = [];
  for (let i = -ROWS_PER_SIDE; i <= ROWS_PER_SIDE; i++) {
    const t = i / ROWS_PER_SIDE;
    const at = Math.abs(t);
    const radius =
      waistRadius + (edgeRadius - waistRadius) * Math.pow(at, power);
    rows.push({ y: t * ySpread, z: at * at * zSpread, radius });
  }
  return rows;
}

function clamp(value: number, min: number, max: number): number {
  return Math.min(max, Math.max(min, value));
}

/**
 * The central column's own row profile: an HOURGLASS, narrowest at the
 * waist (the plasma band's own height, world y = 0 = `torus.y`, kept at
 * row index `floor(len/2)` -- both the per-point far/near split and the
 * opaque base read that row for the column's radius at the band) and
 * flaring to `rimMultiplier` (1.6-2.0) times the waist radius at both
 * rims, per `r(y) = r_waist + k * (y - y_band)^2`. `ySpanTop`/
 * `ySpanBottom` are the world-y distance from the waist to each rim,
 * solved independently (not mirrored) because the camera's tilt and each
 * row's own near-side reference point (`theta = 3*PI/2`, the same
 * front-face convention `chamber.ts`'s `facingAway` uses) do not project
 * symmetrically around the camera's aim -- a single shared span either
 * overshoots one rim past the frame or leaves the other short of it.
 *
 * Row placement compresses toward the rims: `spacingPower` (kept < 1, the
 * opposite of `buildTaperedRows`' own `power` exponent on the RADIUS)
 * applied to `|t|^spacingPower` on the row's own y makes each row step
 * shrink, in projected screen space, as it nears a rim -- verified
 * empirically against the real projection, not assumed from the world-y
 * spacing alone -- so the trapezoid tiles read denser approaching the rim
 * the way a globe's latitude rings compress toward its poles, instead of
 * a handful of oversized slabs.
 */
interface ColumnRowSpec {
  readonly waistRadius: number;
  readonly rimMultiplier: number;
  readonly ySpanTop: number;
  readonly ySpanBottom: number;
  readonly zSpread: number;
  readonly spacingPower: number;
}

function buildColumnRows(spec: ColumnRowSpec): ChamberRow[] {
  const {
    waistRadius,
    rimMultiplier,
    ySpanTop,
    ySpanBottom,
    zSpread,
    spacingPower,
  } = spec;
  // r(rim) = waistRadius * rimMultiplier at y = +-ySpan, so k is solved
  // per side from that boundary condition rather than picked by hand.
  const kTop = (waistRadius * (rimMultiplier - 1)) / (ySpanTop * ySpanTop);
  const kBottom =
    (waistRadius * (rimMultiplier - 1)) / (ySpanBottom * ySpanBottom);
  const rows: ChamberRow[] = [];
  for (let i = -ROWS_PER_SIDE; i <= ROWS_PER_SIDE; i++) {
    const t = i / ROWS_PER_SIDE;
    const at = Math.abs(t);
    const ySpan = i < 0 ? ySpanBottom : ySpanTop;
    const k = i < 0 ? kBottom : kTop;
    const y = Math.sign(t) * Math.pow(at, spacingPower) * ySpan;
    const radius = waistRadius + k * y * y;
    rows.push({ y, z: at * at * zSpread, radius });
  }
  return rows;
}

/**
 * The torus' own projected width/height in px at `baseScale = 1`
 * (`focal === dist`), at the near side (`theta = -PI/2`, the convention
 * `isFarSide`/`bandCenter` in `index.tsx` use) -- `originX`/`originY` don't
 * matter (only the offsets from them), so the probe camera is centred at
 * the origin.
 */
function ringExtentsAtBaseScale1(
  R: number,
  a: number,
  dist: number,
  tiltDeg: number,
): { widthPx: number; heightPx: number } {
  const cam = makeCamera(0, 0, dist, dist, tiltDeg);
  const top = project(torusPoint(R, a, -Math.PI / 2, Math.PI / 2, 0, 0), cam);
  const bottom = project(
    torusPoint(R, a, -Math.PI / 2, -Math.PI / 2, 0, 0),
    cam,
  );
  return { widthPx: 2 * (R + a), heightPx: Math.abs(bottom.y - top.y) };
}

/**
 * Solves the `focal` that fits the torus' near-side band to a target pixel
 * width, capped by a target pixel height when the available band is short.
 * `project`'s `scale = focal / depth` is linear in `focal` for a fixed
 * `dist`/`tiltDeg` (every world point's `depth` is independent of `focal`),
 * so the ring's projected width and height both scale linearly with
 * `focal` too -- solving each target independently via
 * `ringExtentsAtBaseScale1` and taking the smaller resulting focal always
 * yields a ring that fits the height budget, narrower than the width
 * target only when the band is too short to hold it at that width (Rule
 * F's "fit the band height first when short").
 */
function solveFocalForRing(
  R: number,
  a: number,
  dist: number,
  tiltDeg: number,
  targetWidthPx: number,
  targetHeightPx: number,
): number {
  const ref = ringExtentsAtBaseScale1(R, a, dist, tiltDeg);
  const focalForWidth = (targetWidthPx / ref.widthPx) * dist;
  const focalForHeight = (targetHeightPx / ref.heightPx) * dist;
  return Math.min(focalForWidth, focalForHeight);
}

/**
 * The target fed to `solveFocalForRing` for the ring's analytic (tube
 * surface) width, not its own rendered width: the rendered ring also
 * carries the stray population (streaks pushed out to 1.3-2.2x the tube
 * radius, `plasma.ts`'s `tubeScale`) and the bloom halo around it, both of
 * which read at the 25%-luminance probe threshold and extend visibly
 * beyond the analytic surface. Measured against the real render, this
 * value lands the rendered ring at 70-75% of the viewport width (Rule F's
 * 70-80% target) -- raise it and re-measure with `wkf-containment.cjs`
 * (`ringWidthFrac`) rather than assuming the analytic and rendered widths
 * match.
 */
const STACKED_RING_WIDTH_FRACTION = 0.64;
/**
 * The target fed to `solveFocalForRing` for the ring's analytic height,
 * same caveat as `STACKED_RING_WIDTH_FRACTION` above: the rendered band
 * (strays plus bloom) reads taller than the analytic surface. Measured
 * against the real render, this value keeps the rendered ring's own
 * bounding box inside `[artTop, section bottom]` with margin at every
 * tested width (`wkf-containment.cjs`'s `ringTop`/`ringBottom`).
 */
const STACKED_BAND_FIT = 0.65;
/**
 * A flatter tube than the mobile torus (`R:a` about 9:1 instead of
 * 5:1, keeping `R + a` close to mobile's own 49) -- STACKED's band height
 * budget is fixed by the measured copy (typically far shorter, relative
 * to the viewport, than mobile's own band), so a thinner tube is what
 * lets a 70-80%-wide ring actually fit a short band; `dist`/`tiltDeg`
 * stay the mobile construction's own values, keeping the same "inside the
 * vessel" perspective.
 */
const STACKED_TORUS: TorusParams = { R: 44, a: 5, y: 0, z: 0 };
const STACKED_DIST = 160;
const STACKED_TILT_DEG = 10;

/**
 * `mobile` and `stacked` share one column row profile (as `buildTaperedRows`
 * did before this ticket): waist 36, rims 1.8x (64.8), spans solved
 * against `mobile`'s own fixed camera (`scratch-geom-probe6/7.mjs`, not
 * checked in) so both rims land between `artTop + 24` and the canvas
 * bottom at 375 -- `stacked`'s own camera keeps the same `dist`/`tiltDeg`
 * and only solves a different `focal`, so the same row profile carries
 * over as the same approximation `buildTaperedRows` was.
 */
function buildMobileColumnRows(): ChamberRow[] {
  return buildColumnRows({
    waistRadius: 36,
    rimMultiplier: 1.8,
    ySpanTop: 6.1,
    ySpanBottom: 30.6,
    zSpread: 5,
    spacingPower: 0.62,
  });
}

/** `copyRect.right` is `null` only when the `data-hero-copy` block isn't found yet (never observed in practice, since `measure()` runs after mount) -- matches the same-width plateau the real measurement produces below the `sm:px-12` container's own `max-w-6xl` cap. */
function fallbackZoneRight(w: number): number {
  const contentLeft = w <= 1152 ? 0 : (w - 1152) / 2;
  return contentLeft + 48 + 576 + 24;
}

/**
 * Chamber and plasma geometry per viewport size. Every number below is a
 * chosen target: the copy-clear zone is measured by rendered extents (h1
 * text rect, paragraph box, each button's own rect, each +24px), and the
 * ring/streaks/filament/bloom clear it by construction.
 *
 * Three modes, chosen from the viewport width and the measured
 * `data-hero-copy` block (`copyRect`, `null` only before it can be found):
 *
 * - `mobile` (w < 768): unchanged from the shipped construction.
 * - `stacked` (768 <= w < 1280): reuses the mobile construction (ring in
 *   the band below the copy, wall behind everything including the copy
 *   band) but the band itself is measured -- `artTop = copyRect.bottom +
 *   24` (fallback `h * 0.77`) -- and the camera's `focal` is solved to fit
 *   the ring to 70-80% of the width, or to the band's own height first
 *   when that's the tighter constraint (`solveFocalForRing`).
 * - `sideBySide` (w >= 1280): the desktop construction, with the art band
 *   computed from the copy instead of the fixed `w/2 + 72`/`0.775w`:
 *   `zoneRight = copyRect.right + 24`, the column centred in
 *   `[zoneRight, w]`, and the whole scene (camera focal, column/wall row
 *   radii) scaled by `s = clamp(bandWidth / 648, 0.85, 1.15)` so the ring
 *   never crosses `zoneRight` and never leaves the frame's right edge.
 *   `648` is the band this construction was tuned and reviewed at (1440,
 *   where `s` lands on exactly `1` and every number below reduces to
 *   today's shipped values -- the pixel-parity gate).
 *
 * The camera sits close to the geometry (`dist` small relative to the
 * wall's own radius) so it reads as being INSIDE the vessel: the wall row
 * at the plasma's height is wide enough that its near face projects past
 * both the left and right frame edges -- the viewer is surrounded, not
 * looking at an object from outside. Column rows share the same axis, at a
 * much smaller, near-constant radius, and their own `ySpread` carries them
 * past the top/bottom frame edges too, so nothing shows the vessel's
 * outside silhouette against bare navy.
 */
export function computeLayout(
  w: number,
  h: number,
  copyRect: CopyRect | null,
): TokamakLayout {
  const mobile = w < MOBILE_BREAKPOINT;
  const sideBySide = w >= SIDE_BY_SIDE_BREAKPOINT;

  if (sideBySide) {
    const zoneRight = copyRect ? copyRect.right + 24 : fallbackZoneRight(w);
    const bandWidth = Math.max(1, w - zoneRight);
    const s = clamp(bandWidth / 648, 0.85, 1.15);
    const originX = zoneRight + bandWidth / 2;
    const originY = h * 0.5;
    const camera = makeCamera(originX, originY, 650 * s, 300, 8);
    // Hourglass: waist 92*s at the band, rims 1.8x (165.6*s) at the top
    // and bottom, spans solved (see `scratch-geom-probe4.mjs`, not
    // checked in) so the top rim's front arc lands ~12% down the 1440
    // canvas and the bottom rim ~88% down it -- both inside the top/
    // bottom 15% target with margin, all 19 rows visible.
    const columnRows = buildColumnRows({
      waistRadius: 92 * s,
      rimMultiplier: 1.8,
      ySpanTop: 74.5 * s,
      ySpanBottom: 99.7 * s,
      zSpread: 30 * s,
      spacingPower: 0.62,
    });
    // A large waist radius and a high taper power keep each row's radius
    // near `waistRadius` for most of the row range, so every row projects
    // past the frame's left/right edges instead of only the one row at the
    // plasma's height -- otherwise a visible cliff opens between painted
    // wall and bare navy above/below the band.
    const wallRows = buildTaperedRows(1500 * s, 132 * s, 780 * s, 920 * s, 2.2);
    // R/a are tuned so the near-tube band stays a modest fraction of the
    // column's visible height and its limb stays inside the frame edge;
    // unscaled by `s` -- the ring's own screen size already scales through
    // `focal` above, matching `bandWidth`'s own fraction of 648 exactly.
    const torus: TorusParams = { R: 105, a: 21, y: 0, z: 0 };
    return {
      w,
      h,
      mode: "sideBySide",
      camera,
      columnRows,
      // Fewer, wider segments so the half-segment row stagger (see
      // `buildChamberTiles`) is visibly larger than a segment, and actually
      // breaks up seam alignment instead of being swallowed by it.
      columnThetaSegments: 18,
      wallRows,
      wallThetaSegments: 56,
      torus,
      artLeft: zoneRight,
      artTop: 0,
    };
  }

  if (!mobile) {
    // STACKED: same band construction as `mobile` below (ring centred
    // under the copy, wall spanning the full width behind it), refit via
    // `focal` alone to the band this viewport's own copy actually leaves.
    const artTop = copyRect ? copyRect.bottom + 24 : h * 0.77;
    const bandHeight = Math.max(1, h - artTop);
    // Weighted a little below the band's exact midpoint: the tilted
    // camera's near-side reference point (`bandCenter()` in `index.tsx`)
    // is not exactly the ring's own visual vertical centre, and the
    // rendered extent (bloom included) sits closer to `artTop` than a
    // pure midpoint split would predict -- this keeps real margin on both
    // sides instead of the ring nearly touching `artTop`.
    const bandCenterY = artTop + bandHeight * 0.58;
    const originX = w * 0.5;
    const focal = solveFocalForRing(
      STACKED_TORUS.R,
      STACKED_TORUS.a,
      STACKED_DIST,
      STACKED_TILT_DEG,
      w * STACKED_RING_WIDTH_FRACTION,
      bandHeight * STACKED_BAND_FIT,
    );
    const camera = makeCamera(
      originX,
      bandCenterY,
      focal,
      STACKED_DIST,
      STACKED_TILT_DEG,
    );
    const columnRows = buildMobileColumnRows();
    // Wide rows so the wall paints behind the copy band across the full
    // width, the way the `sideBySide` wall does: every row must reach both
    // the top of the section and the left/right edges, not just the row at
    // the plasma's height. `zSpread` is 0 so every row stays at the same
    // depth and reads as tiled structure under the whole band, rather than
    // only the rows nearest the plasma registering against the page.
    const wallRows = buildTaperedRows(650, 400, 900, 0, 1.6);
    return {
      w,
      h,
      mode: "stacked",
      camera,
      columnRows,
      columnThetaSegments: 14,
      wallRows,
      wallThetaSegments: 44,
      torus: STACKED_TORUS,
      artLeft: 0,
      artTop,
    };
  }

  const artTop = h * 0.77;
  const bandCenterY = artTop + (h - artTop) / 2;
  const originX = w * 0.5;
  const camera = makeCamera(originX, bandCenterY, 480, 160, 10);
  const columnRows = buildMobileColumnRows();
  // Wide rows so the mobile wall paints behind the copy band across the
  // full width, the way the desktop wall does: every row must reach both
  // the top of the section and the left/right edges, not just the row at
  // the plasma's height. `zSpread` is 0 so every row stays at the same
  // depth and reads as tiled structure under the whole band, rather than
  // only the rows nearest the plasma registering against the page.
  const wallRows = buildTaperedRows(650, 400, 900, 0, 1.6);
  // R/a scaled proportionally to the desktop torus above, keeping the same
  // tube-to-major-radius ratio.
  const torus: TorusParams = { R: 41, a: 8, y: 0, z: 0 };
  return {
    w,
    h,
    mode: "mobile",
    camera,
    columnRows,
    columnThetaSegments: 14,
    wallRows,
    wallThetaSegments: 44,
    torus,
    artLeft: 0,
    artTop,
  };
}
