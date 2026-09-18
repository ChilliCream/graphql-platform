import { makeCamera, type Camera } from "./geometry";

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

export interface TokamakLayout {
  readonly w: number;
  readonly h: number;
  readonly mobile: boolean;
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
   * meet the column's radius above and below the band -- planner ruling
   * hc-0-wrc.3 comment 187 item 1. The viewer never sees the vessel's
   * outside silhouette: wall + column together cover the frame, darkening
   * toward the edges only through the vignette, never through bare ground.
   */
  readonly wallRows: readonly ChamberRow[];
  readonly wallThetaSegments: number;
  readonly torus: TorusParams;
  /** Desktop: the x the copy-clear zone ends at (0 off mobile). */
  readonly artLeft: number;
  /** Mobile: the y the copy-clear zone ends at (0 on desktop), i.e. `ButtonRow.bottom + 24`. */
  readonly artTop: number;
}

const MOBILE_BREAKPOINT = 768;
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

/**
 * Chamber and plasma geometry per viewport size (ticket hc-0-wrc.3, Rule L,
 * and the planner's re-composition ruling on comment 187/188). Every number
 * below is a chosen target, not a literal from the ticket's Concept section
 * (superseded): the copy-clear zone is measured by rendered extents (h1
 * text rect, paragraph box, each button's own rect, each +24px), and the
 * ring/streaks/filament/bloom clear it by construction.
 *
 * The camera sits close to the geometry (`dist` small relative to the
 * wall's own radius) so it reads as being INSIDE the vessel: the wall row
 * at the plasma's height (`waistRadius` below) is wide enough that its near
 * face, at the camera's own depth, projects past both the left and right
 * frame edges -- the viewer is surrounded, not looking at an object from
 * outside. Column rows share the same axis, at a much smaller, near-constant
 * radius, and their own `ySpread` carries them past the top/bottom frame
 * edges too, so nothing shows the vessel's outside silhouette against bare
 * navy.
 *
 * Desktop (w >= 768): the zone's right edge is the paragraph's,
 * `artLeft = w/2 + 72` (792 at 1440). The column/ring sit at `x = 0.775w`
 * (1116 at 1440, inside the ruled 74-78% band); the chamber's tiles may
 * extend left under the copy as low-alpha structure -- the scrims keep that
 * region dark, only the glowing torus must clear the zone (planner ruling
 * on hc-0-wrc.3 comment 142/160).
 *
 * Mobile (w < 768): the whole scene sits in the band below
 * `ButtonRow.bottom + 24`, `artTop = h * 0.77` (about 609 of a 792px
 * section, matching v11's mobile rule).
 *
 * The ring reads small and washed at the old scale (hc-0-wrc.7): `focal`
 * alone is raised well past the desktop-derived starting point, `dist` and
 * `tilt` untouched. Raising only `focal` is a pure zoom -- `scale =
 * focal/depth` grows by the same factor for every projected point (chamber
 * rows, torus, streaks alike), so the column, the wall and the band all
 * grow together and keep the exact "inside the vessel" perspective the
 * `dist`/`tilt` pair already produces (their ratio to the world-space row
 * radii is what makes the wall wrap past the frame edges and the column
 * read as a cylinder -- untouched by a focal-only change). `nearFactor`
 * (`scale / camera.baseScale`) is a ratio of two focal-proportional
 * quantities, so the near/far dimming envelope is unaffected too. At
 * `focal = 535` the rendered ring spans ~87% of a 375px viewport (target
 * 85-90%, measured with `r3-band375.cjs`'s `bandX` extent), up from ~41% at
 * the old `focal = 250`.
 */
export function computeLayout(w: number, h: number): TokamakLayout {
  const mobile = w < MOBILE_BREAKPOINT;

  if (!mobile) {
    const artLeft = w / 2 + 72;
    const originX = w * 0.775;
    const originY = h * 0.5;
    const camera = makeCamera(originX, originY, 650, 300, 8);
    const columnRows = buildTaperedRows(92, 132, 700, 260, 1.6);
    const wallRows = buildTaperedRows(700, 132, 780, 920, 1.1);
    // R/a shrunk from 130/34 (hc-0-wrc.3 review 2, F2): the near-tube band
    // ran to 0.36 of the column's visible height (over the 0.35 ceiling)
    // and its right limb projected past the 1440 frame edge. `a` is a
    // touch below the planner's ~26 guidance -- at 26 the band (now denser
    // and blurred per F3) still measured ~0.37 of the column's visible
    // height; 21 lands the measured ratio at ~0.32, comfortably under 0.35.
    const torus: TorusParams = { R: 105, a: 21, y: 0, z: 0 };
    return {
      w,
      h,
      mobile,
      camera,
      columnRows,
      // Reduced from 28 (review 2, F1): fewer, wider segments so a half-
      // segment row stagger visibly breaks up the seam alignment instead of
      // the stagger itself being smaller than the segment it is meant to
      // offset.
      columnThetaSegments: 18,
      wallRows,
      wallThetaSegments: 56,
      torus,
      artLeft,
      artTop: 0,
    };
  }

  const artTop = h * 0.77;
  const bandCenterY = artTop + (h - artTop) / 2;
  const originX = w * 0.5;
  const camera = makeCamera(originX, bandCenterY, 480, 160, 10);
  const columnRows = buildTaperedRows(36, 52, 275, 100, 1.6);
  const wallRows = buildTaperedRows(280, 52, 300, 350, 1.1);
  // Scaled proportionally to the desktop R/a change above (105/26, same
  // ~0.39/0.38 ratio the original 51/13 kept against the original 130/34).
  const torus: TorusParams = { R: 41, a: 8, y: 0, z: 0 };
  return {
    w,
    h,
    mobile,
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
