export const GAUGE_MAX = 7000;
export const RED_ZONE_START = 6000;
export const SETTLE_VALUE = 5500;
export const IDLE_BAND: readonly [number, number] = [5300, 5650];
export const CORES = 8;

export const LATENCY_MAX = 20;
export const P95_SETTLE = 12;
export const P50_MARK = 7;

export const PLAN_CACHE_HIT = 99.8;
export const FETCHES_BEFORE = 5;
export const FETCHES_AFTER = 3;

export const UPTIME_LABEL = "182d 06h";
export const REQUESTS_LABEL = "1.4B";
export const STREAM_LABEL = "@defer · @stream";

export function formatOps(n: number): string {
  return `${(n / 1000).toFixed(1)}K`;
}
