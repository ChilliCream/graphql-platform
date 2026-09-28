import { token } from "@/src/nitro";

export const VB = 300;
export const CX = VB / 2;
export const CY = VB / 2;

export const BEZEL_WIDTH = 2;
export const BEZEL_R = CX - BEZEL_WIDTH / 2;
const BEZEL_INNER = BEZEL_R - BEZEL_WIDTH / 2;

export const NUMERAL_FONT_PX = 11;
const NUMERAL_GAP_PX = 2;
const NUMERAL_HALF_DIAGONAL_PX = Math.hypot(
  NUMERAL_FONT_PX * 0.62,
  NUMERAL_FONT_PX / 2,
);
export const NUMERAL_R_FRACTION = BEZEL_INNER / VB;
export const NUMERAL_OFFSET_PX = -(NUMERAL_GAP_PX + NUMERAL_HALF_DIAGONAL_PX);

const TOUCH = BEZEL_R / CX;

const SIDE_TO_CENTRE = 0.74;
const ROW_CENTRE = 1 / (SIDE_TO_CENTRE + TOUCH * (1 + SIDE_TO_CENTRE));
const ROW_SIDE = SIDE_TO_CENTRE * ROW_CENTRE;
const ROW_CENTRE_LEFT =
  ROW_SIDE / 2 + (TOUCH * (ROW_SIDE + ROW_CENTRE)) / 2 - ROW_CENTRE / 2;
const ROW_SIDE_TOP = (ROW_CENTRE - ROW_SIDE) / 2;

const STACK_CENTRE = 0.68;
const STACK_SIDE = 1 / (1 + TOUCH);
const STACK_SIDE_Y =
  STACK_CENTRE / 2 +
  Math.sqrt(
    ((TOUCH * (STACK_CENTRE + STACK_SIDE)) / 2) ** 2 -
      (0.5 - STACK_SIDE / 2) ** 2,
  );
const STACK_HEIGHT = STACK_SIDE_Y + STACK_SIDE / 2;
const STACK_SIDE_TOP = STACK_SIDE_Y - STACK_SIDE / 2;

const pct = (n: number) => `${Math.round(n * 1e5) / 1e3}%`;

interface Slot {
  readonly left: string;
  readonly top: string;
  readonly size: string;
}

function slot(left: number, top: number, size: number, height: number): Slot {
  return { left: pct(left), top: pct(top / height), size: pct(size) };
}

export const CLUSTER = {
  stackAspect: String(Math.round((1 / STACK_HEIGHT) * 1e5) / 1e5),
  rowAspect: String(Math.round((1 / ROW_CENTRE) * 1e5) / 1e5),
  latency: {
    stack: slot(0, STACK_SIDE_TOP, STACK_SIDE, STACK_HEIGHT),
    row: slot(0, ROW_SIDE_TOP, ROW_SIDE, ROW_CENTRE),
  },
  centre: {
    stack: slot((1 - STACK_CENTRE) / 2, 0, STACK_CENTRE, STACK_HEIGHT),
    row: slot(ROW_CENTRE_LEFT, 0, ROW_CENTRE, ROW_CENTRE),
  },
  pressure: {
    stack: slot(1 - STACK_SIDE, STACK_SIDE_TOP, STACK_SIDE, STACK_HEIGHT),
    row: slot(1 - ROW_SIDE, ROW_SIDE_TOP, ROW_SIDE, ROW_CENTRE),
  },
} as const;

export const ELECTRIC = "var(--color-cc-electric)";
export const ELECTRIC_BRIGHT = `color-mix(in oklch, ${ELECTRIC} 55%, var(--color-cc-accent-hover))`;
export const ELECTRIC_DIM = `color-mix(in srgb, ${ELECTRIC} 30%, transparent)`;
export const DISC_CORE = `color-mix(in srgb, ${ELECTRIC} 12%, black)`;
export const DISC_RIM = `color-mix(in srgb, ${ELECTRIC} 48%, black)`;
export const FACE_CORE = `color-mix(in srgb, ${ELECTRIC} 25%, black)`;
export const FACE_RIM = `color-mix(in srgb-linear, ${ELECTRIC} 30%, black)`;
export const BACKDROP = `color-mix(in srgb, ${ELECTRIC} 8%, black)`;
export const BACKDROP_GLOW = `color-mix(in srgb, ${ELECTRIC} 85%, transparent)`;
export const NUMERAL_COLOR = `color-mix(in srgb, ${ELECTRIC} 55%, ${token.textStrong})`;
