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
}

const MOBILE_BREAKPOINT = 768;

function clamp(value: number, min: number, max: number): number {
  return Math.max(min, Math.min(max, value));
}

/**
 * Sphere/core/beam geometry for a given viewport size.
 *
 * Desktop keeps the fusion core at roughly (66%, 52%) of the hero, spheres
 * centred near (50%, 52%) and (82%, 52%) with a 200-240px radius (matches
 * `reference-plasma.jpg` at 1440x900). Below `MOBILE_BREAKPOINT` the whole
 * scene scales down (~90px vs. ~220px radius, roughly 0.4x) and the core
 * moves further down and right (80%, 86%) so its bloom clears the copy
 * column and both buttons, which fill nearly the full 375px width.
 */
export function computeLayout(w: number, h: number): PlasmaLayout {
  const mobile = w < MOBILE_BREAKPOINT;
  // Mobile pushes the core further down, and right of the stacked copy
  // column (which spans nearly the full 375px width), than the desktop
  // fractions would, so the core's bloom clears the buttons below the copy.
  const y = mobile ? h * 0.86 : h * 0.52;
  const coreX = mobile ? w * 0.8 : w * 0.66;
  const radius = mobile ? clamp(w * 0.2, 50, 90) : clamp(w * 0.153, 200, 240);

  return {
    w,
    h,
    mobile,
    radius,
    sphereA: { x: mobile ? w * 0.48 : w * 0.5, y },
    sphereB: { x: mobile ? w * 1.02 : w * 0.82, y },
    core: { x: coreX, y },
    beamY: y,
  };
}
