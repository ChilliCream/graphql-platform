import { logFraction } from "./gauge";

export const CALM_MS = 4000;
export const RAMP_MS = 1500;
export const BURST_MS = 5000;
export const COOLDOWN_MS = 2000;

export const CALM_END = CALM_MS;
export const RAMP_END = CALM_END + RAMP_MS;
export const BURST_END = RAMP_END + BURST_MS;
export const CYCLE_MS = BURST_END + COOLDOWN_MS;

export const ROTATE_MS = 5200;

export const MEM_RISE_LAG_MS = 350;
export const MEM_FALL_LAG_MS = 300;

export const OPS_MAX = 7000;
export const OPS_RED_START = 5500;
export const OPS_BURST_LABEL = 3000;
export const OPS_CALM: readonly [number, number] = [800, 1200];
export const OPS_BURST: readonly [number, number] = [5300, 5700];

export const CORE_COUNT = 8;
export const CPU_CORE_CALM: readonly [number, number] = [8, 20];
export const CPU_CORE_BURST: readonly [number, number] = [84, 96];
export const CPU_TOTAL_BURST: readonly [number, number] = [88, 92];

export const MEMORY_CALM: readonly [number, number] = [38, 44];
export const MEMORY_BURST: readonly [number, number] = [82, 86];

export const NET_IN_CALM: readonly [number, number] = [10, 20];
export const NET_IN_BURST: readonly [number, number] = [75, 90];
export const NET_OUT_CALM: readonly [number, number] = [8, 15];
export const NET_OUT_BURST: readonly [number, number] = [65, 80];

export const LATENCY_MIN = 1;
export const LATENCY_MAX = 40;
export const LATENCY_MAJOR_TICKS: readonly number[] = [1, 2, 5, 10, 20, 40];
export const LATENCY_MINOR_TICKS: readonly number[] = [3, 4, 6, 7, 8, 9, 30];
export const LATENCY_P50 = 7;
export const LATENCY_BAND: readonly [number, number] = [9, 13];
export const LATENCY_DOMAIN: readonly [number, number] = [7, 15];

export const latencyFraction = (ms: number): number =>
  logFraction(ms, LATENCY_MIN, LATENCY_MAX);

export const HISTORY_POINTS = 36;
export const HISTORY_STEP_MS = 160;

export function formatOps(n: number): string {
  return `${(n / 1000).toFixed(1)}K`;
}

export function formatPercent(n: number): string {
  return `${Math.round(n)}%`;
}

export function formatMs(n: number): string {
  return `${Math.round(n)}`;
}
