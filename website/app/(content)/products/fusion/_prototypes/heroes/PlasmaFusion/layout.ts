export interface Point {
  readonly x: number;
  readonly y: number;
}

export interface PlasmaLayout {
  readonly w: number;
  readonly h: number;
  readonly mobile: boolean;
  /** Shared sphere radius, in CSS px. */
  readonly radius: number;
  readonly sphereA: Point;
  readonly sphereB: Point;
  /** Where the two spheres touch: the white-hot fusion seam. */
  readonly core: Point;
  /** Y of the thin full-width beam; always `core.y`. */
  readonly beamY: number;
  /**
   * Desktop: the x the copy-clear zone ends at (0 off mobile). The live and
   * static layers are feathered out to the left of this, never drawn with a
   * destination-out mask over geometry that itself still overlaps the copy.
   */
  readonly artLeft: number;
  /**
   * Mobile: the y the copy-clear zone ends at (0 on desktop), i.e.
   * `ButtonRow.bottom + 24`. The scene is feathered out above this.
   */
  readonly artTop: number;
}

const MOBILE_BREAKPOINT = 768;

function clamp(value: number, min: number, max: number): number {
  return Math.max(min, Math.min(max, value));
}

/**
 * Sphere/core/beam geometry for a given viewport size, per planner rulings 1
 * and 2 on ticket hc-0-wrc.2 (comments 141, 152, 154): the copy-clear zone is
 * measured by rendered extents (the h1's text rect, the paragraph box, each
 * button's own rect, each +24px), not by eyeballed percentages, and is met
 * by moving/shrinking the geometry itself, never by masking stale geometry.
 *
 * Desktop (w >= `MOBILE_BREAKPOINT`): the zone's right edge is the
 * paragraph's (`xl:max-w-2xl` column) plus its 24px margin, `artLeft = w/2 +
 * 72` (792 at 1440). The sphere radius is derived from the remaining width
 * so sphere B still clears the viewport's right edge, clamped to ruling 2's
 * ~150-180px (about 162 at 1440). Sphere A's left edge sits at `artLeft`,
 * sphere B sits inside the right edge, and the core (where they touch) lands
 * at their midpoint, about 77% of the width -- inside the ruling's 74-78%
 * band.
 *
 * Mobile (w < `MOBILE_BREAKPOINT`): the scene lives in the band below
 * `ButtonRow.bottom + 24`, expressed as `artTop = h * 0.77` (about 609 of a
 * 792px section). The radius fits the remaining band height (about 75px),
 * and the core/spheres sit centred in that band (about 0.885h). Sphere B is
 * kept inside the right viewport edge and sphere A is placed so the two
 * spheres are tangent at the core.
 */
export function computeLayout(w: number, h: number): PlasmaLayout {
  const mobile = w < MOBILE_BREAKPOINT;

  if (!mobile) {
    const artLeft = w / 2 + 72;
    const radius = clamp((w - artLeft) / 4, 150, 180);
    const sphereAX = artLeft + radius;
    const sphereBX = w - radius * 1.05;
    const coreX = (sphereAX + sphereBX) / 2;
    const y = h * 0.52;
    return {
      w,
      h,
      mobile,
      radius,
      sphereA: { x: sphereAX, y },
      sphereB: { x: sphereBX, y },
      core: { x: coreX, y },
      beamY: y,
      artLeft,
      artTop: 0,
    };
  }

  const artTop = h * 0.77;
  const radius = clamp((h - artTop) * 0.41, 60, 95);
  const y = (artTop + h) / 2;
  const sphereBX = Math.min(w * 0.66 + radius, w - radius * 1.05);
  const sphereAX = sphereBX - radius * 2;
  const coreX = sphereBX - radius;
  return {
    w,
    h,
    mobile,
    radius,
    sphereA: { x: sphereAX, y },
    sphereB: { x: sphereBX, y },
    core: { x: coreX, y },
    beamY: y,
    artLeft: 0,
    artTop,
  };
}
