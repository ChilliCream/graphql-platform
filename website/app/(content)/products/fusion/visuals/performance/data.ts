export const OPS_MAX = 8000;
export const OPS_SETTLE = 5500;
export const OPS_IDLE_BAND: readonly [number, number] = [5300, 5700];

export const LATENCY_MAX = 50;
export const LATENCY_P95 = 12;
export const LATENCY_P50 = 7;

export const CPU_PRESSURE = 42;
export const MEMORY_PRESSURE = 58;
export const CPU_IDLE_BAND: readonly [number, number] = [38, 46];
export const MEMORY_IDLE_BAND: readonly [number, number] = [54, 62];

export const CACHED_DOCUMENTS = 1284;
export const CACHED_PLANS = 3410;

export function formatOps(n: number): string {
  return `${(n / 1000).toFixed(1)}K`;
}

export function formatCount(n: number): string {
  return Math.round(n).toLocaleString("en-US");
}
