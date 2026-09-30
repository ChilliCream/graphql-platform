import type { CSSProperties } from "react";

import { token } from "@/src/nitro";

export const VB = 300;
export const CX = VB / 2;
export const CY = VB / 2;

export const BEZEL_WIDTH = 2;
export const BEZEL_R = CX - BEZEL_WIDTH / 2;

export const NUMERAL_FONT_PX = 11;
const NUMERAL_GAP_PX = 2;
const NUMERAL_ADVANCE = 0.6;

// Pixels from a scale's inner edge to the numeral centre, clearing the glyph box at any angle.
export function numeralInset(angle: number, label: string): number {
  const rad = (angle * Math.PI) / 180;
  const halfWidth = (label.length * NUMERAL_FONT_PX * NUMERAL_ADVANCE) / 2;
  const halfHeight = NUMERAL_FONT_PX / 2;
  return (
    NUMERAL_GAP_PX +
    Math.abs(Math.cos(rad)) * halfWidth +
    Math.abs(Math.sin(rad)) * halfHeight
  );
}

interface Circle {
  readonly x: number;
  readonly y: number;
  readonly r: number;
}

export interface Arc {
  readonly start: number;
  readonly end: number;
}

interface Slot {
  readonly left: string;
  readonly top: string;
  readonly size: string;
}

export type ChartDial = "latency";

interface Layout {
  readonly aspect: string;
  readonly chart: Readonly<Record<ChartDial, string>>;
  readonly latency: Slot;
  readonly centre: Slot;
  readonly pressure: Slot;
  readonly latencyScale: Arc;
  readonly pressureArcs: { readonly cpu: Arc; readonly mem: Arc };
  readonly pressureShift: { readonly x: number; readonly y: number };
}

// Share of a side dial's diameter that sits hidden behind the centre dial.
const OVERLAP = 0.2;
const LENS_MARGIN_DEG = 12;
const ARC_GAP_DEG = 5;

const ROW_SIDE_RATIO = 0.66;
const ROW_DROP = 0.7;
const STACK_CENTRE_WIDTH = 0.9;
const STACK_SIDE_WIDTH = 0.49;

const CHART_MIN_PX = 40;
const CHART_MAX_PX = 114;
const CHART_CHORDS = [
  { share: 0.46, offset: 21.8 },
  { share: 0.64, offset: 61.2 },
] as const;

const deg = (rad: number) => (rad * 180) / Math.PI;
const pct = (n: number) => `${Math.round(n * 1e5) / 1e3}%`;
const ratio = (n: number) => String(Math.round(n * 1e5) / 1e5);

// The widest chord the latency face holds under its readout.
function latencyChartWidth(): string {
  const [low, high] = CHART_CHORDS.map(
    ({ share, offset }) => `${Math.round(share * 1e5) / 1e3}cqw - ${offset}px`,
  );
  return `clamp(${CHART_MIN_PX}px, max(${low}, ${high}), ${CHART_MAX_PX}px)`;
}

function slot(c: Circle, width: number, height: number): Slot {
  return {
    left: pct((c.x - c.r) / width),
    top: pct((c.y - c.r) / height),
    size: pct((2 * c.r) / width),
  };
}

function build(
  width: number,
  height: number,
  centre: Circle,
  left: Circle,
): Layout {
  const right: Circle = { ...left, x: width - left.x };
  const dx = centre.x - left.x;
  const dy = left.y - centre.y;
  const d = Math.hypot(dx, dy);
  const theta = deg(Math.atan2(dy, dx));
  const half = deg(
    Math.acos(
      (d * d + left.r * left.r - centre.r * centre.r) / (2 * d * left.r),
    ),
  );
  const latencyScale: Arc = {
    start: theta - half - LENS_MARGIN_DEG,
    end: theta + half + LENS_MARGIN_DEG - 360,
  };
  const outerStart = 180 - latencyScale.start;
  const outerEnd = 180 - latencyScale.end;
  const mid = (outerStart + outerEnd) / 2;
  return {
    aspect: ratio(width / height),
    chart: { latency: latencyChartWidth() },
    latency: slot(left, width, height),
    centre: slot(centre, width, height),
    pressure: slot(right, width, height),
    latencyScale,
    pressureArcs: {
      mem: { start: outerStart, end: mid - ARC_GAP_DEG / 2 },
      cpu: { start: outerEnd, end: mid + ARC_GAP_DEG / 2 },
    },
    pressureShift: {
      x: Math.cos((theta * Math.PI) / 180),
      y: Math.sin((theta * Math.PI) / 180),
    },
  };
}

function buildRow(): Layout {
  const rc = 0.5;
  const rs = ROW_SIDE_RATIO / 2;
  const y = rc + ROW_DROP * (rc - rs);
  const d = rc + rs - 2 * rs * OVERLAP;
  const dx = Math.sqrt(d * d - (y - rc) ** 2);
  const width = 2 * (dx + rs);
  return build(width, 1, { x: width / 2, y: rc, r: rc }, { x: rs, y, r: rs });
}

function buildStack(): Layout {
  const rc = STACK_CENTRE_WIDTH / 2;
  const rs = STACK_SIDE_WIDTH / 2;
  const d = rc + rs - 2 * rs * OVERLAP;
  const dx = 0.5 - rs;
  const y = rc + Math.sqrt(d * d - dx * dx);
  return build(1, y + rs, { x: 0.5, y: rc, r: rc }, { x: rs, y, r: rs });
}

export const CLUSTER = { row: buildRow(), stack: buildStack() } as const;

export const CHART_WIDTH_CLASS =
  "w-(--chart-stack) @min-[504px]/hud:w-(--chart-row)";

export function chartWidthVars(dial: ChartDial): CSSProperties {
  return {
    "--chart-stack": CLUSTER.stack.chart[dial],
    "--chart-row": CLUSTER.row.chart[dial],
  } as CSSProperties;
}

export const READOUT_FONT = "clamp(22px, 19.8cqw - 26px, 48px)";
// Four mono glyphs at 0.6em advance with the readout's -0.02em letter-spacing, so the chart is 110% of the number.
const NETWORK_CHART_EM = Math.round(4 * (0.6 - 0.02) * 1.1 * 1e3) / 1e3;
export const NETWORK_CHART_WIDTH = `calc(${READOUT_FONT} * ${NETWORK_CHART_EM})`;
export const READOUT_LINE_HEIGHT = 1.15;
export const CONTENT_GAP = "clamp(3px, 3cqw - 4px, 12px)";

export const ELECTRIC = "var(--color-cc-electric)";
export const DANGER = "var(--color-cc-danger)";
export const ELECTRIC_BRIGHT = `color-mix(in oklch, ${ELECTRIC} 55%, var(--color-cc-accent-hover))`;
export const ELECTRIC_DIM = `color-mix(in srgb, ${ELECTRIC} 30%, transparent)`;
export const DISC_CORE = `color-mix(in srgb, ${ELECTRIC} 12%, black)`;
export const DISC_RIM = `color-mix(in srgb, ${ELECTRIC} 48%, black)`;
export const FACE_CORE = `color-mix(in srgb, ${ELECTRIC} 25%, black)`;
export const FACE_RIM = `color-mix(in srgb-linear, ${ELECTRIC} 30%, black)`;
export const ELECTRIC_TEXT = `color-mix(in srgb, ${ELECTRIC} 70%, ${token.textStrong})`;
export const OPS_NUMERAL_COLOR = `color-mix(in srgb, ${ELECTRIC} 80%, ${token.textStrong})`;
export const LATENCY_NUMERAL_COLOR = `color-mix(in srgb, ${ELECTRIC} 30%, ${token.textStrong})`;
export const LABEL_COLOR = `color-mix(in srgb, ${token.textSecondary} 40%, ${token.textStrong})`;
export const RING_GLOW = `0 0 16px 2px color-mix(in srgb, ${ELECTRIC} 70%, transparent)`;
