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
  /** `z` at the top rim (`at = 1` on the `i > 0` side). */
  readonly zSpreadTop: number;
  /**
   * `z` at the bottom rim (`at = 1` on the `i < 0` side) -- defaults to
   * `zSpreadTop` for callers (mobile/stacked) whose top and bottom rim ARE
   * the same wall edge row. `sideBySide`'s two rims are two independently
   * chosen wall rows (checkpoint 1's substitution, comments 328-330) with
   * different `z`, so the two sides need their own value: a single shared
   * `zSpread` would land one rim off its own target `z` and reopen a kink
   * at that rim.
   */
  readonly zSpreadBottom?: number;
  readonly spacingPower: number;
  /**
   * Rows per side, default `ROWS_PER_SIDE` (shared with the FROZEN wall
   * builder). `sideBySide`'s column overrides this (checkpoint 2): its two
   * rim points sit far more foreshortened than the wall's own edge row
   * used to, so the same 9-per-side count cannot keep every adjacent
   * pair's drawn tile height within 1.5x of its neighbour (verified
   * empirically against the real projection, `test-results/dbg-spacing`,
   * not checked in) -- more, smaller angular steps are needed purely for
   * the column's own row placement, independent of the wall's.
   */
  readonly rowsPerSide?: number;
}

function buildColumnRows(spec: ColumnRowSpec): ChamberRow[] {
  const {
    waistRadius,
    rimRadiusTop,
    rimRadiusBottom,
    ySpanTop,
    ySpanBottom,
    zSpreadTop,
    zSpreadBottom = zSpreadTop,
    spacingPower,
    rowsPerSide = ROWS_PER_SIDE,
  } = spec;
  // r(rim) = rimRadiusTop/Bottom at y = +-ySpan, so k is solved per side
  // from that boundary condition rather than picked by hand.
  const kTop = (rimRadiusTop - waistRadius) / (ySpanTop * ySpanTop);
  const kBottom = (rimRadiusBottom - waistRadius) / (ySpanBottom * ySpanBottom);
  const rows: ChamberRow[] = [];
  for (let i = -rowsPerSide; i <= rowsPerSide; i++) {
    const t = i / rowsPerSide;
    const at = Math.abs(t);
    const ySpan = i < 0 ? ySpanBottom : ySpanTop;
    const k = i < 0 ? kBottom : kTop;
    const zSpread = i < 0 ? zSpreadBottom : zSpreadTop;
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
const STACKED_RING_WIDTH_FRACTION = 0.67;
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
 * `mobile`'s and `stacked`'s WALL is FROZEN at its pre-ticket values
 * (checkpoint 3, ruling 329/330 -- only the mobile/stacked COLLAR relation
 * and the column's own construction change): `buildTaperedRows(650, 400,
 * 900, 0, 1.6)`, restored exactly after an earlier cycle had shrunk
 * `ySpread` to 440 to dodge a near-camera depth singularity that no longer
 * applies (see `COLUMN_BAND_*` below) -- `buildChamberTiles` already culls
 * any individual tile that touches it (`depth <= 1`), the wall painted
 * fine at 900 before, and the column's own silhouette no longer walks this
 * row at all (it is built from the column's own drawn tiles, `paint.ts`'s
 * `buildColumnSilhouette`).
 */
const MOBILE_WALL_WAIST_RADIUS = 650;
const MOBILE_WALL_EDGE_RADIUS = 400;
const MOBILE_WALL_Y_SPREAD = 900;
const MOBILE_WALL_Z_SPREAD = 0;
const MOBILE_WALL_POWER = 1.6;

/**
 * The column's own band-sized hourglass (checkpoint 3, ruling 329/330):
 * fully independent of the wall's rows -- its two rims are NOT the wall's
 * own edge row here (unlike `sideBySide`'s substitution), they simply sit
 * OFF canvas, so `rimRadiusTop`/`rimRadiusBottom` only have to stay small
 * enough that the silhouette never balloons, never match a wall value.
 *
 * `rimRadius` <= 100 and `ySpan` chosen so the front arc (`theta = 3*PI/2`,
 * `z = -radius` there since row `z` is 0 throughout this branch) never
 * nears the camera's own `depth <= 1` singularity: `depth = y*sinTilt -
 * radius*cosTilt + dist` (`dist = 160`, `tiltDeg = 10`) lands at 52.9-129.3
 * world units at both rims with these values (`test-results/dbg-mobile-
 * column`, not checked in) -- comfortably over the ticket's 40-unit floor,
 * unlike the pre-ticket-shared wall row that caused checkpoint 1's "boxy
 * rectangle".
 *
 * `spacingPower` 2 (steeper than the wall's own compression) packs rows
 * tightly through the waist and lets them spread out approaching the
 * (off-canvas) rims, landing the near-waist ROWS' own on-screen spacing in
 * the same 30-54px range the mobile wall's own back-arc rows sit at
 * (`test-results/rvv-geom.cjs`'s `wallRows`), instead of one wide gap
 * either side of the band.
 */
const COLUMN_BAND_RIM_RADIUS = 70;
const COLUMN_BAND_Y_SPAN = 220;
const COLUMN_BAND_SPACING_POWER = 2;

/**
 * `a` kept at each mode's own pre-ticket tube thickness; `R` is the value
 * that lands the RENDERED ring at the ticket's own width target (measured
 * with `rvv-ring.cjs`, not assumed) -- the column's own waist then FOLLOWS
 * `R - a` (gap 0, well inside D(1)'s "at most 0.05 R"), never the reverse
 * (ruling 330: "NO pre-ticket R/a restore"). `mobile`'s camera has a fixed
 * `focal` (unlike `stacked`'s, solved per band by `solveFocalForRing`), so
 * `R` is the only free parameter that moves its rendered ring size.
 */
const MOBILE_TORUS_A = 8;
const MOBILE_TORUS_R = 39;
const MOBILE_COLUMN_WAIST = MOBILE_TORUS_R - MOBILE_TORUS_A;

/**
 * `stacked`'s own `focal` is solved per band (`solveFocalForRing`) to hit
 * `STACKED_RING_WIDTH_FRACTION` regardless of `R` (the solve is scale-
 * invariant in `R + a`), so `R` here only has to be a reasonable tube-to-
 * column proportion, not itself search-tuned against the rendered width --
 * kept near the ticket's own starting value.
 */
const STACKED_TORUS_A = 5;
const STACKED_TORUS_R = 44;
const STACKED_COLUMN_WAIST = STACKED_TORUS_R - STACKED_TORUS_A;
const STACKED_TORUS: TorusParams = {
  R: STACKED_TORUS_R,
  a: STACKED_TORUS_A,
  y: 0,
  z: 0,
};
const STACKED_DIST = 160;
const STACKED_TILT_DEG = 10;

function buildBandColumnRows(waistRadius: number): ChamberRow[] {
  return buildColumnRows({
    waistRadius,
    rimRadiusTop: COLUMN_BAND_RIM_RADIUS,
    rimRadiusBottom: COLUMN_BAND_RIM_RADIUS,
    ySpanTop: COLUMN_BAND_Y_SPAN,
    ySpanBottom: COLUMN_BAND_Y_SPAN,
    zSpreadTop: 0,
    spacingPower: COLUMN_BAND_SPACING_POWER,
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
    // WALL row-builder parameters and painter are FROZEN at their
    // 8c545c43e1 values (planner comment 327, ratified 328/329): the wall
    // is verified by pixel parity outside the column's bbox and the
    // bridging row, never re-tuned. `base` is the baseline's full 19-row
    // array; only its two outermost rows (0 and 18, the wall's own literal
    // "innermost ceiling/floor ring" -- a tiny far ring at depth, comments
    // 328/329) are replaced below by the column's own rim rows, and
    // `base[17]`/`base[1]` (still frozen wall rows) become the bridging
    // rows into those rims.
    const base = buildTaperedRows(1500 * s, 132 * s, 780 * s, 920 * s, 2.2);
    const SIDE_WALL_SEGMENTS = 56;

    // The column's own waist, set here per planner comment 315 (about
    // 360px at 1440 -- landed at 79*s so `R - a` below gives gap 0 with
    // `a` unchanged, and the ring's own outer edge stays inside the 648
    // band with margin instead of touching the frame's right edge).
    const SIDE_COLUMN_WAIST = 79;

    // Top and bottom rim points (canonical units, `*s` scales them like
    // the rest of this branch), chosen by `test-results/wqa-rim-search.cjs`
    // against `base[17]`/`base[1]` (the bridging rows): front arc inside
    // the canvas (top rim additionally below the 72px header at 1440), no
    // `depth<=1` corner in the bridging pair, and the bridging pair's own
    // mean drawn-tile height within 15% of the reference pair
    // `base[16]->base[17]`.
    const SIDE_TOP_RIM_Y = 346;
    const SIDE_TOP_RIM_Z = 540;
    const SIDE_TOP_RIM_R = 236;
    // OPTION B (planner ruling, ticket comment 334, orchestrator relay
    // 335, superseding the interim "front arc in canvas" reading of
    // comment 333): `base[1]`'s own back arc is already off-canvas at
    // 1440 (y 801 on an `h=792` canvas), so the bottom rim's front arc is
    // allowed to sit below the canvas too -- the wall's own floor is
    // already cut by the section bottom the same way. What the ruling
    // requires instead is that the VISIBLE part of the bottom junction is
    // geometrically true: on every IN-CANVAS tile of the bridging row
    // (`base[1] -> bottom rim`, a tile counts as in-canvas if any of its 4
    // un-inset corners projects inside the canvas), mean tile height
    // within 15% of the reference pair `base[1]->base[2]`, seam gap within
    // 1px of that pair's own seam gap, and no `depth<=1` corner. Chosen by
    // `test-results/wqa-rim-search.cjs`'s extended search (dropped the
    // "front arc in canvas" bottom-rim constraint, computes the three
    // stats over in-canvas tiles only, canonical `(y, z, r)` triple
    // checked jointly at 1280/1440/1920): `R0 = 236` ties the top rim's
    // own radius exactly (`dR = 0`, the smallest reachable on the search
    // grid), keeping the hourglass symmetric-looking; `Y0`/`Z0` chosen
    // among the `dR = 0` candidates for the best worst-case margin across
    // the three widths (worst height ratio 12.4% at 1280, worst seam-gap
    // diff 0.96px at 1920, in-canvas coverage 28/28, 28/28, 17/28 of the
    // 28 drawn bridging tiles at 1280/1440/1920). Numbers reported in the
    // fixer's ticket comment.
    const SIDE_BOTTOM_RIM_Y = -270;
    const SIDE_BOTTOM_RIM_Z = 230;
    const SIDE_BOTTOM_RIM_R = 236;

    // Hourglass: waist at the band, flaring independently to each chosen
    // rim point above -- the column's own row 0/18 land exactly on those
    // rim points (same y/z/radius `buildColumnRows` always gives its
    // endpoints), so substituting them in for the wall's own rows 0/18
    // below introduces no kink/step by construction.
    // `rowsPerSide`/`spacingPower` retuned together (checkpoint 2,
    // `test-results/dbg-spacing`, not checked in) against the real
    // projected pair heights so every adjacent near-facing pair is within
    // 1.5x its neighbour end to end (max ratio 1.50 at 1440) -- 9 rows per
    // side could not clear that bar for this hourglass's own asymmetric
    // rim spans without a >2x jump somewhere near the waist.
    const columnRows = buildColumnRows({
      waistRadius: SIDE_COLUMN_WAIST * s,
      rimRadiusTop: SIDE_TOP_RIM_R * s,
      rimRadiusBottom: SIDE_BOTTOM_RIM_R * s,
      ySpanTop: SIDE_TOP_RIM_Y * s,
      ySpanBottom: -SIDE_BOTTOM_RIM_Y * s,
      zSpreadTop: SIDE_TOP_RIM_Z * s,
      zSpreadBottom: SIDE_BOTTOM_RIM_Z * s,
      spacingPower: 1.3,
      rowsPerSide: 14,
    });
    // The desktop substitution (comments 328-330): the column's rim rows
    // REPLACE the wall's own two far-ring rows (`base[0]`/`base[18]`), and
    // `base[1]`/`base[17]` bridge from the frozen wall into those rims.
    // Every other row is the untouched, frozen baseline wall row.
    const wallRows: ChamberRow[] = [
      columnRows[0],
      ...base.slice(1, 18),
      columnRows[columnRows.length - 1],
    ];
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
      columnThetaSegments: SIDE_WALL_SEGMENTS,
      wallRows,
      wallThetaSegments: SIDE_WALL_SEGMENTS,
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
    const columnRows = buildBandColumnRows(STACKED_COLUMN_WAIST);
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
  const columnRows = buildBandColumnRows(MOBILE_COLUMN_WAIST);
  // Wide rows so the mobile wall paints behind the copy band across the
  // full width, the way the desktop wall does: every row must reach both
  // the top of the section and the left/right edges, not just the row at
  // the plasma's height. `zSpread` is 0 so every row stays at the same
  // depth and reads as tiled structure under the whole band, rather than
  // only the rows nearest the plasma registering against the page.
  const wallRows = buildMobileWallRows();
  // R - a = the column's own waist radius exactly (D(1)), `a` kept at
  // mobile's own tube thickness so the band's thickness/exposure is
  // unchanged; `R` is `MOBILE_TORUS_R` (module-level, search-tuned against
  // the rendered ring width, ruling 330 -- never the pre-ticket value).
  const torus: TorusParams = {
    R: MOBILE_TORUS_R,
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
