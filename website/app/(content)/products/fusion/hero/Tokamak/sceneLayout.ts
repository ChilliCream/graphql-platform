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
  /**
   * The `wallRows` index range (`[start, end)`) `buildInstrumentLights`
   * draws its candidate rows from: the wall's own frozen rhythm rows, never
   * the rim/bridge rows spliced onto either end (so inserting or removing
   * bridge rows -- e.g. this ticket's junction fix -- cannot change which
   * rows the light scatter can land on, or its `mulberry32` draw sequence).
   * `sideBySide`: `base.slice(2, 17)`'s own index range within `wallRows`.
   * `mobile`/`stacked`: unchanged from the prior `rows.slice(2, rows.length
   * - 2)` behaviour, since those wall rows are not junction-spliced.
   */
  readonly wallLightRowRange: readonly [number, number];
  /**
   * hc-0-gar ruling (i): the TOP junction's own small row chain -- frozen
   * wall row 16, then the `buildBridgeRows` sequence, then the column's own
   * top rim (`wallRows`' own `base.slice(2,17)[14]`/bridge/`columnRows`
   * tail, already spliced into `wallRows` above) -- for `chamber.ts`'s
   * culling exception (`buildChamberTiles(..., "wall", 1, "near")`),
   * building ONLY the near-facing half of this small range so it can be
   * stamped OVER the column layer (`index.tsx`'s dedicated bridge layer),
   * the way the reference's ceiling folds down in front of the pillar.
   * Never overlaps `wallLightRowRange`'s own frozen index range. Empty on
   * `mobile`/`stacked`: their column and wall are fully independent
   * constructs (no bridge splice), and neither rim is ever in frame there
   * (confirmed at 375/768) -- no near-facing exception needed.
   */
  readonly topJunctionRows: readonly ChamberRow[];
  /** Same as `topJunctionRows`, mirrored at the BOTTOM junction: the column's own bottom rim, the bridge sequence, then frozen wall row 2. */
  readonly bottomJunctionRows: readonly ChamberRow[];
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
 * hc-0-gar: bridges a FROZEN wall row to the column's own rim with a short
 * SEQUENCE of rows -- replacing the single, oversized bridging row
 * (`SIDE_TOP_ROW17`/`SIDE_BOTTOM_ROW1`, about 8x its neighbours) that read
 * as a smooth, untextured "buffer" between the ceiling/floor grid and the
 * pillar's rim (user report, ticket comment 364). `from`/`to`'s `y`, `z`
 * and `radius` are interpolated independently along a monotone power
 * curve (continuing the hourglass flare into the ceiling/floor rather
 * than jumping in one step): `t = (i / steps) ^ power`, `i = 1..steps-1`
 * (`to` itself is never included -- the caller already has it, either the
 * rim row or the next frozen wall row). Both endpoints land exactly on
 * `from`/`to` regardless of the powers, so this introduces no kink/step at
 * either end by construction, same as `buildColumnRows`.
 *
 * `powerYZ` (for `y`/`z` together) and `powerR` (for `radius`) are
 * independent: the wall row and the rim are NOT two points on the wall's
 * own natural taper curve (the rim's own `y` is far smaller than the
 * frozen row's -- the wall's rows flare OUTWARD in `y` toward its edge,
 * while the rim sits back in near the column's axis), so a single shared
 * power cannot keep every consecutive drawn-tile-height ratio (from the
 * wall's own frozen reference pair, through the bridge, to the column's
 * own rim-adjacent pair) inside the 1.3x rhythm bar -- verified directly
 * against the real projection, a joint 2D/step grid search over
 * 1280/1440/1920 (`test-results/gar-bridge-search.cjs`), not assumed.
 */
function buildBridgeRows(
  from: ChamberRow,
  to: ChamberRow,
  steps: number,
  powerYZ: number,
  powerR: number,
): ChamberRow[] {
  const rows: ChamberRow[] = [];
  for (let i = 1; i < steps; i++) {
    const tYZ = Math.pow(i / steps, powerYZ);
    const tR = Math.pow(i / steps, powerR);
    rows.push({
      y: from.y + (to.y - from.y) * tYZ,
      z: from.z + (to.z - from.z) * tYZ,
      radius: from.radius + (to.radius - from.radius) * tR,
    });
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

/**
 * `copyRect.right` is `null` only when the `data-hero-copy` block isn't
 * found yet (never observed in practice, since `measure()` runs after
 * mount) -- matches the same-width plateau the real measurement produces
 * below the `sm:px-12` container's own `max-w-6xl` cap. `computeLayout`
 * only ever calls this from the `sideBySide` branch (`w >=
 * SIDE_BY_SIDE_BREAKPOINT`, 1280), where the 1152-wide container always has
 * real margin on both sides -- hc-0-540 housekeeping (planner comment 287):
 * removed the `w <= 1152 ? 0 : ...` branch this used to guard with, dead at
 * every width this function is actually called at (hc-0-wkf review comment
 * 285's minor note 4).
 */
function fallbackZoneRight(w: number): number {
  const contentLeft = (w - 1152) / 2;
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
    // 8c545c43e1 values for rows 2-16 (planner comment 327/345/346): pixel
    // parity outside the column's bbox and the two bridging rows, never
    // re-tuned. `base` is the baseline's full 19-row array; its two
    // outermost rows (0 and 18, the wall's own literal "innermost
    // ceiling/floor ring") are replaced below by the column's own rim
    // rows, and `base[1]`/`base[17]` (the bridging rows) are ALSO replaced
    // -- planner ruling comment 345/346 (option B on the flare): only
    // these two rows may move (radius, z, y-spacing) to meet a larger rim
    // able to clear the F1 bar against the column's own rim-adjacent pair,
    // keeping the wall's tile-size progression, seam width, fastener
    // rhythm and painter (the substituted rows are still plain
    // `ChamberRow`s consumed by the same frozen `buildChamberTiles`/wall
    // painter -- nothing about the rendering pipeline changes, only these
    // two rows' own position).
    const base = buildTaperedRows(1500 * s, 132 * s, 780 * s, 920 * s, 2.2);
    const SIDE_WALL_SEGMENTS = 56;

    // The column's own waist, unchanged since checkpoint 1 (planner
    // comment 345: "the waist (about 79 canonical) ... stay as at
    // checkpoint 1" -- verifier 2's waist-narrowing item is rejected).
    const SIDE_COLUMN_WAIST = 79;

    // BOTTOM rim (canonical units, `*s` scales them like the rest of this
    // branch) -- OPTION B (planner ruling, ticket comment 334, orchestrator
    // relay 335, reaffirmed 345/346 "bottom rim per the earlier option B" --
    // superseding the interim "front arc in canvas" reading of comment
    // 333): the bottom rim's front arc MAY sit below the canvas at
    // side-by-side widths, exactly as the wall's own floor already is.
    // These three constants are UNCHANGED from the accepted option B fix
    // (ticket comment 338, commit ed62382170) -- hc-0-b8z's user ruling
    // ("the bottom of the pillar looks great", ticket comment 426) keeps
    // this rim's own look "exactly as it is" and mirrors it to build the
    // TOP rim below, never the reverse. Declared first so the TOP block can
    // reference these values directly instead of duplicating them.
    const SIDE_BOTTOM_RIM_Y = -270;
    const SIDE_BOTTOM_RIM_Z = 230;
    const SIDE_BOTTOM_RIM_R = 236;
    // BOTTOM bridging ROW(S) (hc-0-gar, replaces the single FROZEN
    // `base[1]` this ticket's fix removes): a `buildBridgeRows` sequence
    // from the bottom rim to row 2 (still frozen). PLANNER RULING (i)
    // (comments 384/385/386, "both junctions"): re-tuned against the NEAR
    // arc (rim -> bridge -> row 2) via `chamber.ts`'s `faceOverride:
    // "near"` exception (`TokamakLayout.bottomJunctionRows`), composited
    // over the column so the floor folds up into its bottom.
    //
    // This near-facing chain is NEVER visible at any side-by-side width:
    // the bottom rim's own front arc (this chain's very FIRST point)
    // already projects at y >= 1068px on a <=900px-tall canvas (verified
    // directly against the real bundle -- the option B acceptance this
    // file's module doc already documents), and every row further from the
    // rim (the bridge steps, then row 2) projects further still past the
    // bottom edge, so no near-facing bottom tile -- regardless of its own
    // size -- ever paints a visible pixel; a ring-wide median could
    // therefore never mask a hidden front-arc blow-up here the way it once
    // did at the (now-withdrawn) in-canvas top dome. `steps = 2` (one
    // inserted row) is therefore kept: `test-results/
    // gar-bridge-near-search.cjs`'s own best candidate clears the
    // (moot but still checked) ratio bar with wide margin, worst
    // consecutive ratio 1.001-1.007 across 1280/1440/1920.
    const SIDE_BOTTOM_BRIDGE_STEPS = 2;
    const SIDE_BOTTOM_BRIDGE_POWER_YZ = 1.15;
    const SIDE_BOTTOM_BRIDGE_POWER_R = 0.95;
    // TOP rim: hc-0-b8z, user ruling (ticket description, planner comment
    // 426) -- "the pillar top should look exactly like the bottom of the
    // pillar (no plinth)". WITHDRAWN: the flared-dome top rim (`Y=140,
    // Z=420, R=320`, wqa's numeric flare target, kept fully inside the
    // canvas below the header) and the "top rim inside the canvas" rule
    // that produced it -- both explicitly superseded by the ruling. This
    // rim is now the EXACT MIRROR of `SIDE_BOTTOM_RIM_*` above about the
    // band's `y = 0`: same `z`, same `r`, `y` negated (`buildColumnRows`
    // reads `ySpanTop`/`rimRadiusTop`/`zSpreadTop` as magnitudes, so
    // matching the bottom's own magnitudes here makes the column's
    // world-space hourglass profile literally symmetric, `kTop === kBottom`).
    // The camera's tilt makes the SCREEN projection of a world-mirrored
    // point asymmetric (verified directly: at 1440 this rim's own front
    // arc projects to y approx -130, i.e. off-canvas ABOVE, the same way
    // the bottom rim's front arc projects off-canvas BELOW at y approx
    // 1072) -- the ruling explicitly allows this ("the top rim's front arc
    // may sit above the canvas exactly as the bottom rim's sits below
    // it"), so no in-canvas dome/cap renders at the top at any width.
    const SIDE_TOP_RIM_Y = -SIDE_BOTTOM_RIM_Y;
    const SIDE_TOP_RIM_Z = SIDE_BOTTOM_RIM_Z;
    const SIDE_TOP_RIM_R = SIDE_BOTTOM_RIM_R;
    // TOP bridging ROWS: UNLIKE the bottom's own `steps = 2`, this junction
    // is NOT mirroring a chain that is always fully off-canvas. Verified
    // directly (`test-results/b8z-wall16.cjs`, not checked in): frozen wall
    // row 16 (`base[16]`, `y` approx 607, `z` approx 557 canonical) already
    // has NEAR-facing tiles at wide theta (away from the front pole, near
    // `chamber.ts`'s own away/near culling boundary) that project close to
    // the canvas' own top edge REGARDLESS of what the junction's other
    // endpoint is -- a grazing-angle silhouette effect of the frozen row's
    // own position under this camera's tilt, not something the rim/bridge
    // choice controls. `SIDE_BOTTOM_BRIDGE_STEPS = 2` is safe for the
    // bottom because the bottom rim (and therefore its ENTIRE chain,
    // `columnRows[0]` included) is confirmed off-canvas at every sampled
    // point; the top's chain has one endpoint (`base[16]`) that is NOT
    // off-canvas at every theta, so a single oversized bridge row there
    // reads as an untextured "buffer" tile again (the exact defect
    // hc-0-gar was built to remove) instead of a graduated fold. Re-ran
    // hc-0-gar's own front-arc search methodology
    // (`test-results/b8z-bridge-front-search.cjs`, a copy of `gar-bridge-
    // front-search.cjs` retargeted at this rim) against the NEW mirrored
    // rim: `steps = 8` is the smallest step count whose front-arc tile
    // heights stay under 300px (the wall's own established scale) while
    // every consecutive ratio clears [0.77, 1.3] (worst 1.086 at
    // `powerYZ = powerR = 1.1`, jointly at 1280/1440/1920) -- chosen over
    // `steps = 7` (the bare minimum feasible count) for a small margin.
    const SIDE_TOP_BRIDGE_STEPS = 8;
    const SIDE_TOP_BRIDGE_POWER_YZ = 1.1;
    const SIDE_TOP_BRIDGE_POWER_R = 1.1;

    // Hourglass: waist at the band, flaring independently to each chosen
    // rim point above -- the column's own row 0/18 land exactly on those
    // rim points (same y/z/radius `buildColumnRows` always gives its
    // endpoints), so substituting them in for the wall's own rows 0/18
    // below introduces no kink/step by construction.
    // `rowsPerSide`/`spacingPower` (verifier 2 item 3, `test-results/
    // wqa-flare-search2.cjs`): near-linear (1.2, close to the wall's own
    // row-index spacing) and 9 rows/side -- unchanged by hc-0-b8z's rim
    // mirroring above (the shared `spec.spacingPower`/`rowsPerSide` apply to
    // both sides identically already, and 9 rows/side is at least as smooth
    // at the now-SMALLER, bottom-matched top rim as it was at the larger,
    // withdrawn dome rim it was originally tuned against -- verified below,
    // no scalloping at either junction).
    const columnRows = buildColumnRows({
      waistRadius: SIDE_COLUMN_WAIST * s,
      rimRadiusTop: SIDE_TOP_RIM_R * s,
      rimRadiusBottom: SIDE_BOTTOM_RIM_R * s,
      ySpanTop: SIDE_TOP_RIM_Y * s,
      ySpanBottom: -SIDE_BOTTOM_RIM_Y * s,
      zSpreadTop: SIDE_TOP_RIM_Z * s,
      zSpreadBottom: SIDE_BOTTOM_RIM_Z * s,
      spacingPower: 1.2,
      rowsPerSide: 9,
    });
    // The desktop substitution (comments 328-330, un-frozen bridging rows
    // per 345/346, sequenced per hc-0-gar): the column's rim rows REPLACE
    // the wall's own two far-ring rows (`base[0]`/`base[18]`); the NEW
    // bridge SEQUENCES above replace `base[1]`/`base[17]` (rows 2-16,
    // `base.slice(2, 17)`, stay the untouched, frozen baseline wall rows
    // -- pixel parity). Built from the already-`*s`-scaled rows (`base`,
    // `columnRows`), not canonical ones: `buildBridgeRows`' own
    // interpolation is affine in each field (`from + (to - from) * t`), so
    // scaling both endpoints by `s` first and interpolating is identical
    // to interpolating canonical rows and then scaling by `s`.
    const bottomBridge = buildBridgeRows(
      columnRows[0],
      base[2],
      SIDE_BOTTOM_BRIDGE_STEPS,
      SIDE_BOTTOM_BRIDGE_POWER_YZ,
      SIDE_BOTTOM_BRIDGE_POWER_R,
    );
    // hc-0-b8z: interpolated from the RIM toward wall row 16 -- the SAME
    // `from`/`to` direction `bottomBridge` above uses (rim -> wall), not
    // wall -> rim -- then reversed into wall-to-rim order for splicing into
    // `wallRows` below. `buildBridgeRows`' own `t = (i/steps)^power` is
    // measured from `from`, so interpolating from the wall (the old
    // approach) instead of from the rim would NOT produce a mirror image of
    // `bottomBridge` even with identical `steps`/powers/endpoints -- the
    // fractional step positions would fall at different world points on
    // each side of the band. Interpolating from the same endpoint
    // (`SIDE_TOP_RIM_*` mirrors `SIDE_BOTTOM_RIM_*` exactly, see above) and
    // reversing makes `topBridge[i]` the exact `y`-negation of
    // `bottomBridge[i]` at every step, for any `steps`/`powerYZ`/`powerR`.
    const topBridge = [
      ...buildBridgeRows(
        columnRows[columnRows.length - 1],
        base[16],
        SIDE_TOP_BRIDGE_STEPS,
        SIDE_TOP_BRIDGE_POWER_YZ,
        SIDE_TOP_BRIDGE_POWER_R,
      ),
    ].reverse();
    const wallRows: ChamberRow[] = [
      columnRows[0],
      ...bottomBridge,
      ...base.slice(2, 17),
      ...topBridge,
      columnRows[columnRows.length - 1],
    ];
    // hc-0-gar ruling (i): the small near-facing-only chains (see
    // `TokamakLayout.topJunctionRows`'s own doc) -- built straight from the
    // same `base[16]`/`topBridge`/`columnRows` values already spliced into
    // `wallRows` above, so a rim/row/bridge change here can never drift out
    // of sync with the far-facing wall build.
    const topJunctionRows: ChamberRow[] = [
      base[16],
      ...topBridge,
      columnRows[columnRows.length - 1],
    ];
    const bottomJunctionRows: ChamberRow[] = [
      columnRows[0],
      ...bottomBridge,
      base[2],
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
      // `base.slice(2, 17)`'s own index range within `wallRows` (see
      // `TokamakLayout.wallLightRowRange`): fixed regardless of the bridge
      // sequences' own row counts on either side.
      wallLightRowRange: [2, 17],
      topJunctionRows,
      bottomJunctionRows,
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
      // Unchanged from the prior `rows.slice(2, rows.length - 2)` reach:
      // these wall rows are not junction-spliced.
      wallLightRowRange: [2, wallRows.length - 2],
      // No bridge splice on this branch (checkpoint 3, ruling 329/330): the
      // column's own band hourglass rims sit off canvas, never in frame at
      // 768 (confirmed) -- no near-facing junction exception applies.
      topJunctionRows: [],
      bottomJunctionRows: [],
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
    // Unchanged from the prior `rows.slice(2, rows.length - 2)` reach:
    // these wall rows are not junction-spliced.
    wallLightRowRange: [2, wallRows.length - 2],
    // Same reasoning as `stacked` above: no bridge splice, rims never in
    // frame at 375 (confirmed).
    topJunctionRows: [],
    bottomJunctionRows: [],
    torus,
    artLeft: 0,
    artTop,
  };
}

/**
 * hc-0-540 housekeeping (planner comment 287): the side-by-side right-limb
 * containment criterion, ANALYTIC from the layout (the ring's own torus
 * surface, projected at its near side and base scale -- the same basis
 * `index.tsx`'s own `ringWidthPx` uses) rather than a rendered-pixel probe.
 * hc-0-wkf's own review (comment 285) found the pixel-measured right limb
 * exceeding `w - 16` by about 15px at every side-by-side width, identically
 * at 1440's own already-shipped, unchanged-by-that-ticket construction: not
 * a regression, but the rendered ring's stray-streak population (pushed out
 * to 1.3-2.2x the tube radius, `plasma.ts`'s `tubeScale`) and its bloom
 * halo, both bright enough to cross a 25%-luminance threshold well past the
 * analytic tube surface -- by design (the "hazy outer streaks" look), not a
 * containment bug. This gives a stable, geometry-only bound to check `w -
 * 16` against instead: the ring's own analytic surface, excluding the
 * strays/bloom that pixel-probe past it. `camera.originX` is the projection
 * origin (band axis on-screen), `(R + a) * baseScale` its analytic radius
 * at the near side, matching `ringWidthPx`'s own `(R + a) * baseScale * 2`
 * full-width basis.
 */
export function analyticRingRightLimb(layout: TokamakLayout): number {
  return (
    layout.camera.originX +
    (layout.torus.R + layout.torus.a) * layout.camera.baseScale
  );
}
