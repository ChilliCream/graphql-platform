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
  readonly rows: readonly ChamberRow[];
  readonly torus: TorusParams;
  readonly thetaSegments: number;
  /** Desktop: the x the copy-clear zone ends at (0 off mobile). */
  readonly artLeft: number;
  /** Mobile: the y the copy-clear zone ends at (0 on desktop), i.e. `ButtonRow.bottom + 24`. */
  readonly artTop: number;
}

const MOBILE_BREAKPOINT = 768;
const ROWS_PER_SIDE = 6;

/**
 * Chamber rows from the near/bottom rim to the far/top rim (and the
 * mirrored rows below), the column's radius profile tapering to
 * `waistRadius` at the torus height and flaring to `outerRadius` away from
 * it, with `zSpread` pushing the flared rows away from the camera so the
 * rows compress toward a far wall each direction instead of holding one
 * flat radius.
 */
function buildRows(
  waistRadius: number,
  outerRadius: number,
  zSpread: number,
  ySpread: number,
  power: number,
): ChamberRow[] {
  const rows: ChamberRow[] = [];
  for (let i = -ROWS_PER_SIDE; i <= ROWS_PER_SIDE; i++) {
    const t = i / ROWS_PER_SIDE;
    const at = Math.abs(t);
    const radius =
      waistRadius + (outerRadius - waistRadius) * Math.pow(at, power);
    rows.push({ y: t * ySpread, z: at * at * zSpread, radius });
  }
  return rows;
}

/**
 * Chamber and plasma geometry per viewport size (ticket hc-0-wrc.3, Rule L).
 * Every number below is a chosen target, not a literal from the ticket's
 * Concept section (superseded): the copy-clear zone is measured by rendered
 * extents (h1 text rect, paragraph box, each button's own rect, each +24px),
 * and the ring/streaks/filament/bloom clear it by construction.
 *
 * Desktop (w >= 768): the zone's right edge is the paragraph's,
 * `artLeft = w/2 + 72` (792 at 1440). The column/ring sit at `x = 0.775w`
 * (1116 at 1440, inside the ruled 74-78% band); the torus major radius (128)
 * plus tube radius (30) plus a bloom margin project to well right of
 * `artLeft` at the camera's base scale (verified by the copy-clear probe).
 * The chamber's tiles may extend left under the copy as low-alpha structure
 * -- the scrims keep that region dark, only the glowing torus must clear the
 * zone (planner ruling on hc-0-wrc.3 comment 142/160).
 *
 * Mobile (w < 768): the whole scene sits in the band below
 * `ButtonRow.bottom + 24`, `artTop = h * 0.77` (about 609 of a 792px
 * section, matching v11's mobile rule). Camera and torus are scaled down so
 * the ring and its bloom fit the remaining band height.
 */
export function computeLayout(w: number, h: number): TokamakLayout {
  const mobile = w < MOBILE_BREAKPOINT;

  if (!mobile) {
    const artLeft = w / 2 + 72;
    const originX = w * 0.775;
    const originY = h * 0.47;
    const camera = makeCamera(originX, originY, 760, 640, 13);
    const rows = buildRows(118, 300, 760, 460, 1.35);
    const torus: TorusParams = { R: 128, a: 30, y: 0, z: 0 };
    return {
      w,
      h,
      mobile,
      camera,
      rows,
      torus,
      thetaSegments: 40,
      artLeft,
      artTop: 0,
    };
  }

  const artTop = h * 0.77;
  const bandCenterY = artTop + (h - artTop) / 2;
  const originX = w * 0.5;
  const camera = makeCamera(originX, bandCenterY, 300, 340, 15);
  const rows = buildRows(46, 150, 340, 220, 1.35);
  const torus: TorusParams = { R: 50, a: 12, y: 0, z: 0 };
  return {
    w,
    h,
    mobile,
    camera,
    rows,
    torus,
    thetaSegments: 34,
    artLeft: 0,
    artTop,
  };
}
