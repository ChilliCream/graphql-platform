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
 * flaring to `rimRadiusTop`/`rimRadiusBottom` at each rim, per
 * `r(y) = r_waist + k * (y - y_band)^2`. `ySpanTop`/`ySpanBottom` are the
 * world-y distance from the waist to each rim, solved independently (not
 * mirrored) because the camera's tilt and each row's own near-side
 * reference point (`theta = 3*PI/2`, the same front-face convention
 * `chamber.ts`'s `facingAway` uses) do not project symmetrically around
 * the camera's aim -- a single shared span either overshoots one rim past
 * the frame or leaves the other short of it.
 *
 * `rimRadiusTop`/`rimRadiusBottom`, `ySpanTop`/`ySpanBottom` and `zSpread`
 * are always the WALL's own edge row values at each call site (see
 * `computeLayout`): the column's last row on each side is therefore the
 * exact same point -- same y, same z, same radius -- as the wall's
 * innermost ceiling/floor row, not merely a nearby approximation, so the
 * projected outline has no kink and no step at the rim by construction
 * (two rows that are the same point project to the same pixel).
 *
 * Row placement compresses toward the rims: `spacingPower` (kept < 1, the
 * opposite of `buildTaperedRows`' own `power` exponent on the RADIUS)
 * applied to `|t|^spacingPower` on the row's own y makes each row step
 * shrink, in projected screen space, as it nears a rim -- verified
 * empirically against the real projection, not assumed from the world-y
 * spacing alone -- so the trapezoid tiles read denser approaching the rim
 * the way a globe's latitude rings compress toward its poles, instead of
 * a handful of oversized slabs. `spacingPower` only reshapes the INTERIOR
 * rows; both endpoints (`t = 0` and `t = +-1`) land at `ySpan` exactly
 * regardless of it, so it never moves the shared rim point.
 */
interface ColumnRowSpec {
  readonly waistRadius: number;
  readonly rimRadiusTop: number;
  readonly rimRadiusBottom: number;
  readonly ySpanTop: number;
  readonly ySpanBottom: number;
  readonly zSpread: number;
  readonly spacingPower: number;
}

function buildColumnRows(spec: ColumnRowSpec): ChamberRow[] {
  const {
    waistRadius,
    rimRadiusTop,
    rimRadiusBottom,
    ySpanTop,
    ySpanBottom,
    zSpread,
    spacingPower,
  } = spec;
  // r(rim) = rimRadiusTop/Bottom at y = +-ySpan, so k is solved per side
  // from that boundary condition rather than picked by hand.
  const kTop = (rimRadiusTop - waistRadius) / (ySpanTop * ySpanTop);
  const kBottom = (rimRadiusBottom - waistRadius) / (ySpanBottom * ySpanBottom);
  const rows: ChamberRow[] = [];
  for (let i = -ROWS_PER_SIDE; i <= ROWS_PER_SIDE; i++) {
    const t = i / ROWS_PER_SIDE;
    const at = Math.abs(t);
    const ySpan = i < 0 ? ySpanBottom : ySpanTop;
    const k = i < 0 ? kBottom : kTop;
    const y = Math.sign(t) * Math.pow(at, spacingPower) * ySpan;
    const radius = waistRadius + k * y * y;
    // Same `z` formula as `buildTaperedRows` (`at * at * zSpread`), using
    // the UNCOMPRESSED `at`, not the spacing-compressed `y` above -- at
    // `at = 1` this lands on exactly `zSpread`, the wall's own rim `z`.
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
 * `mobile` and `stacked` share one wall edge row (`MOBILE_WALL_*`, also fed
 * to `buildTaperedRows` for `wallRows` in both branches below) AND one
 * column row profile: the column's own waist (`MOBILE_COLUMN_WAIST`) and
 * its rims are that SAME wall edge row -- same y, z and radius as the
 * wall's innermost ceiling/floor ring, per `buildColumnRows`'s own doc --
 * so the column literally continues the wall's taper down to the waist
 * instead of capping it with an unrelated, independently-sized cylinder.
 * `stacked`'s own camera keeps the same `dist`/`tiltDeg` as `mobile` and
 * only solves a different `focal`, so the same row profile carries over.
 */
const MOBILE_COLUMN_WAIST = 36;
const MOBILE_WALL_WAIST_RADIUS = 650;
const MOBILE_WALL_EDGE_RADIUS = 400;
// Down from the pre-ticket 900: `mobile`'s (and `stacked`'s, same `dist`)
// camera has `dist = 160`, and `project`'s own `depth = y*sinTilt + dist`
// (tilt 10deg, `z = 0` throughout this branch) lands at just 3.7 world
// units at `y = -900` -- effectively AT the camera plane, so `project`'s
// `Math.max(depth, 1)` clamp sends `scale` to a huge, near-singular value.
// `buildChamberTiles` quietly culls any tile touching that (its own
// `depth <= 1` check), so the pre-ticket WALL likely never actually drew
// much there; but `buildColumnSilhouette`'s envelope has no such culling
// (`project` never errors, only clamps), so sharing this row as the
// column's own rim (checkpoint 1) turned that latent singularity into a
// huge, black, rectangular envelope -- the reported "boxy rectangle"
// under the mobile/stacked ring. 440 keeps depth well clear (83.6 world
// units at the bottom rim, `probe-mobile-depth.cjs`, not checked in) at
// both `mobile`'s and `stacked`'s shared `dist`/`tiltDeg`.
const MOBILE_WALL_Y_SPREAD = 440;
const MOBILE_WALL_Z_SPREAD = 0;
const MOBILE_WALL_POWER = 1.6;

/**
 * `a` kept at the pre-ticket tube thickness; `R` solved so `R - a` equals
 * the column's own waist radius exactly (`MOBILE_COLUMN_WAIST` above) --
 * the collar ring's inner edge then sits exactly on the waist, gap 0,
 * well inside the user ruling's "at most 0.05 R" (D(1)). `dist`/`tiltDeg`
 * stay the mobile construction's own values, keeping the same "inside the
 * vessel" perspective.
 */
const STACKED_TORUS_A = 5;
const STACKED_TORUS: TorusParams = {
  R: MOBILE_COLUMN_WAIST + STACKED_TORUS_A,
  a: STACKED_TORUS_A,
  y: 0,
  z: 0,
};
const STACKED_DIST = 160;
const STACKED_TILT_DEG = 10;

function buildMobileColumnRows(): ChamberRow[] {
  return buildColumnRows({
    waistRadius: MOBILE_COLUMN_WAIST,
    rimRadiusTop: MOBILE_WALL_EDGE_RADIUS,
    rimRadiusBottom: MOBILE_WALL_EDGE_RADIUS,
    ySpanTop: MOBILE_WALL_Y_SPREAD,
    ySpanBottom: MOBILE_WALL_Y_SPREAD,
    zSpread: MOBILE_WALL_Z_SPREAD,
    spacingPower: 0.62,
  });
}

function buildMobileWallRows(): ChamberRow[] {
  return buildTaperedRows(
    MOBILE_WALL_WAIST_RADIUS,
    MOBILE_WALL_EDGE_RADIUS,
    MOBILE_WALL_Y_SPREAD,
    MOBILE_WALL_Z_SPREAD,
    MOBILE_WALL_POWER,
  );
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
    // The wall's own inner ceiling/floor row (the column's rims, see
    // `buildColumnRows`'s own doc). All three retuned down from the
    // pre-ticket values (780/920/132): the pre-ticket wall's own zSpread
    // (920, almost 3x `dist`) is fine for the WALL alone (its radius stays
    // near 1500 for almost the whole row range, only dropping to 132 at
    // the very last row, so the huge z barely interacts with a small
    // radius until that one row); shared as the COLUMN's own rim too,
    // where every row's radius sits near 92-260 throughout, the same z
    // swing (subtracted from a nearly-constant small radius, every row,
    // not just the last) makes the front-arc's projected Y fold back on
    // itself between rows -- confirmed against the real bundled
    // `project`/`ringPoint` (`probe-rows.cjs`/`probe-zpower.cjs`, not
    // checked in): the pre-ticket wall values produced row-to-row
    // reversals of hundreds of px and a visible black band across the
    // waist in the rendered column-only capture. This (100/150/170)
    // keeps every reversal under 15px (`probe-search2.cjs`, not checked
    // in) while still landing both rims inside the canvas, below the
    // header, at 1280/1440/1920 (`h = 792`, the section's real rendered
    // height): top rim front-y 143-183px, bottom rim front-y 571-631px.
    const SIDE_WALL_EDGE_RADIUS = 170;
    const SIDE_WALL_Y_SPREAD = 100;
    const SIDE_WALL_Z_SPREAD = 150;
    const SIDE_COLUMN_WAIST = 92;
    // Hourglass: waist 92*s at the band, flaring to the wall's own inner
    // row (`SIDE_WALL_EDGE_RADIUS*s` at `+-SIDE_WALL_Y_SPREAD*s`) at both
    // rims -- the column's last row on each side is that exact wall row
    // (same y/z/radius), so the projected outline has no kink and no step
    // at the junction.
    const columnRows = buildColumnRows({
      waistRadius: SIDE_COLUMN_WAIST * s,
      rimRadiusTop: SIDE_WALL_EDGE_RADIUS * s,
      rimRadiusBottom: SIDE_WALL_EDGE_RADIUS * s,
      ySpanTop: SIDE_WALL_Y_SPREAD * s,
      ySpanBottom: SIDE_WALL_Y_SPREAD * s,
      zSpread: SIDE_WALL_Z_SPREAD * s,
      spacingPower: 0.62,
    });
    // A large waist radius and a high taper power keep each row's radius
    // near `waistRadius` for most of the row range, so every row projects
    // past the frame's left/right edges instead of only the one row at the
    // plasma's height -- otherwise a visible cliff opens between painted
    // wall and bare navy above/below the band.
    const wallRows = buildTaperedRows(
      1500 * s,
      SIDE_WALL_EDGE_RADIUS * s,
      SIDE_WALL_Y_SPREAD * s,
      SIDE_WALL_Z_SPREAD * s,
      2.2,
    );
    // R - a = the column's own waist radius exactly (D(1)'s "gap of at
    // most 0.05 R", landed at 0 here), `a` kept at the pre-ticket tube
    // thickness (21) so the band's thickness/exposure is unchanged; both
    // scaled by `s` like the rest of this branch's geometry so the ring
    // still hugs the waist at every side-by-side width, not only at 1440.
    const SIDE_TORUS_A = 21;
    const torus: TorusParams = {
      R: (SIDE_COLUMN_WAIST + SIDE_TORUS_A) * s,
      a: SIDE_TORUS_A * s,
      y: 0,
      z: 0,
    };
    return {
      w,
      h,
      mode: "sideBySide",
      camera,
      columnRows,
      // Shared with the wall (row spacing, seam width and fastener rhythm
      // all read from the same `thetaSegments` value) so the tile rhythm
      // does not jump at the rim junction.
      columnThetaSegments: 56,
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
    const wallRows = buildMobileWallRows();
    return {
      w,
      h,
      mode: "stacked",
      camera,
      columnRows,
      // Shared with the wall, same reasoning as `sideBySide` above.
      columnThetaSegments: 44,
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
  const wallRows = buildMobileWallRows();
  // R - a = the column's own waist radius exactly (D(1)), `a` kept at the
  // pre-ticket tube thickness (8) so the band's thickness/exposure is
  // unchanged.
  const MOBILE_TORUS_A = 8;
  const torus: TorusParams = {
    R: MOBILE_COLUMN_WAIST + MOBILE_TORUS_A,
    a: MOBILE_TORUS_A,
    y: 0,
    z: 0,
  };
  return {
    w,
    h,
    mode: "mobile",
    camera,
    columnRows,
    // Shared with the wall, same reasoning as `sideBySide` above.
    columnThetaSegments: 44,
    wallRows,
    wallThetaSegments: 44,
    torus,
    artLeft: 0,
    artTop,
  };
}
