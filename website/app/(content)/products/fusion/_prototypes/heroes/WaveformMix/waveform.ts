/**
 * Geometry and waveform math for the Waveform Mix hero: an oscilloscope view
 * where five service-coloured traces converge at a summing junction into one
 * calm white composite trace.
 */

export const VIEW_W = 1600;
export const VIEW_H = 900;

/** Vertical centre shared by the composite trace and the summing junction. */
export const BASELINE_Y = VIEW_H / 2;

/** The five channel lanes, stacked between these bounds. */
export const LANE_TOP = 150;
export const LANE_BOTTOM = 750;
const LANE_COUNT = 5;
const LANE_GAP = (LANE_BOTTOM - LANE_TOP) / LANE_COUNT;

export const LANE_CENTERS: readonly number[] = Array.from(
  { length: LANE_COUNT },
  (_, i) => LANE_TOP + LANE_GAP * (i + 0.5),
);

/** The summing junction where the five channels resolve into the composite, fixed at centre so it survives the mobile crop. */
export const JUNCTION_X = VIEW_W / 2;
/** Where the flat, scrolling channel lanes begin, right of the junction lead-ins. */
export const LANE_START_X = JUNCTION_X + 200;

/** Shared scroll period, in viewBox units, so every trace tiles and moves in lockstep. */
export const PERIOD = 200;

/** Two-harmonic mix per channel; every frequency is an integer multiple of the period so each trace tiles seamlessly. */
const CHANNEL_HARMONICS: readonly {
  readonly n1: number;
  readonly n2: number;
  readonly phase: number;
}[] = [
  { n1: 2, n2: 5, phase: 0.3 },
  { n1: 3, n2: 7, phase: 1.1 },
  { n1: 1, n2: 4, phase: 2.4 },
  { n1: 4, n2: 9, phase: 0.8 },
  { n1: 2, n2: 6, phase: 3.6 },
];

const CHANNEL_AMPLITUDE = 34;
const COMPOSITE_AMPLITUDE = 28;

function shape(x: number, n1: number, n2: number, phase: number): number {
  const t = (x / PERIOD) * Math.PI * 2;
  return (
    0.65 * Math.sin(n1 * t + phase) + 0.35 * Math.sin(n2 * t + phase * 1.7)
  );
}

/** One channel's raw waveform value, roughly in [-1, 1]. */
function channelShape(index: number, x: number): number {
  const { n1, n2, phase } = CHANNEL_HARMONICS[index];
  return shape(x, n1, n2, phase);
}

/** The composite is the average of all five channels: the same signal, mixed down and calmer. */
function compositeShape(x: number): number {
  const total = CHANNEL_HARMONICS.reduce(
    (sum, { n1, n2, phase }) => sum + shape(x, n1, n2, phase),
    0,
  );
  return total / CHANNEL_HARMONICS.length;
}

function samplePath(
  valueAt: (x: number) => number,
  xStart: number,
  xEnd: number,
  step = 8,
): string {
  const points: string[] = [];
  for (let x = xStart; x <= xEnd; x += step) {
    const command = points.length === 0 ? "M" : "L";
    points.push(`${command} ${x.toFixed(1)} ${valueAt(x).toFixed(1)}`);
  }
  return points.join(" ");
}

/** One channel's flat, scrolling trace in its lane; sampled past the right edge for a seamless loop. */
export function channelPath(index: number): string {
  const centerY = LANE_CENTERS[index];
  return samplePath(
    (x) => centerY + CHANNEL_AMPLITUDE * channelShape(index, x),
    LANE_START_X,
    VIEW_W + PERIOD,
  );
}

/** The calm composite trace under the copy; sampled past the junction for a seamless loop. */
export function compositePath(): string {
  return samplePath(
    (x) => BASELINE_Y + COMPOSITE_AMPLITUDE * compositeShape(x),
    0,
    JUNCTION_X + PERIOD,
  );
}

/** Static wiring from the summing junction to where each channel's scrolling trace begins. */
export function leadInPath(index: number): string {
  const y1 = LANE_CENTERS[index];
  const midX = (JUNCTION_X + LANE_START_X) / 2;
  return `M ${JUNCTION_X} ${BASELINE_Y} C ${midX} ${BASELINE_Y}, ${midX} ${y1}, ${LANE_START_X} ${y1}`;
}
