import {
  BURST_END,
  CALM_END,
  CORE_COUNT,
  CPU_CORE_BURST,
  CPU_CORE_CALM,
  CPU_TOTAL_BURST,
  CYCLE_MS,
  LATENCY_BAND,
  MEMORY_BURST,
  MEMORY_CALM,
  MEM_FALL_LAG_MS,
  MEM_RISE_LAG_MS,
  HISTORY_POINTS,
  HISTORY_STEP_MS,
  NET_IN_BURST,
  NET_IN_CALM,
  NET_OUT_BURST,
  NET_OUT_CALM,
  RAMP_END,
  OPS_BURST,
  OPS_CALM,
} from "./data";

export interface Telemetry {
  readonly ops: number;
  readonly cores: readonly number[];
  readonly cpuTotal: number;
  readonly memory: number;
  readonly netIn: number;
  readonly netOut: number;
  readonly netInHistory: readonly number[];
  readonly netOutHistory: readonly number[];
  readonly latency: number;
  readonly latencyHistory: readonly number[];
}

const clamp = (x: number, lo: number, hi: number) =>
  x < lo ? lo : x > hi ? hi : x;
const lerp = (a: number, b: number, t: number) => a + (b - a) * t;
const smoothstep = (x: number) => {
  const c = clamp(x, 0, 1);
  return c * c * (3 - 2 * c);
};
const wrap = (x: number) => ((x % CYCLE_MS) + CYCLE_MS) % CYCLE_MS;

// Four incommensurate-frequency sines summed to a smooth wander with no single visible period.
function noise(tRaw: number, seed: number): number {
  const a = Math.sin(tRaw * 0.00083 + seed * 12.9898);
  const b = Math.sin(tRaw * 0.00151 + seed * 4.1414 + 1.7);
  const c = Math.sin(tRaw * 0.00266 + seed * 7.233 + 3.1);
  const d = Math.sin(tRaw * 0.00427 + seed * 2.399 + 5.2);
  return a * 0.45 + b * 0.28 + c * 0.17 + d * 0.1;
}

function bandValue(
  tRaw: number,
  seed: number,
  calm: readonly [number, number],
  burst: readonly [number, number],
  envelope: number,
): number {
  const mid = lerp(
    (calm[0] + calm[1]) / 2,
    (burst[0] + burst[1]) / 2,
    envelope,
  );
  const half = lerp(
    (calm[1] - calm[0]) / 2,
    (burst[1] - burst[0]) / 2,
    envelope,
  );
  const raw = mid + noise(tRaw, seed) * half * 0.92;
  return clamp(raw, Math.min(calm[0], burst[0]), Math.max(calm[1], burst[1]));
}

function fastEnvelope(t: number): number {
  if (t < CALM_END) return 0;
  if (t < RAMP_END) return smoothstep((t - CALM_END) / (RAMP_END - CALM_END));
  if (t < BURST_END) return 1;
  return 1 - smoothstep((t - BURST_END) / (CYCLE_MS - BURST_END));
}

function memoryEnvelope(t: number): number {
  const riseStart = CALM_END + MEM_RISE_LAG_MS;
  const riseEnd = RAMP_END;
  const fallStart = BURST_END + MEM_FALL_LAG_MS;
  if (t < riseStart) return 0;
  if (t < riseEnd) return smoothstep((t - riseStart) / (riseEnd - riseStart));
  if (t < fallStart) return 1;
  return 1 - smoothstep((t - fallStart) / (CYCLE_MS - fallStart));
}

function phaseClamp(
  raw: number,
  calm: readonly [number, number],
  burst: readonly [number, number],
  pureCalm: boolean,
  pureBurst: boolean,
): number {
  if (pureBurst) return clamp(raw, burst[0], burst[1]);
  if (pureCalm) return clamp(raw, calm[0], calm[1]);
  return raw;
}

function netAt(
  elapsedMs: number,
  seed: number,
  calm: readonly [number, number],
  burst: readonly [number, number],
): number {
  const t = wrap(elapsedMs);
  const env = fastEnvelope(t);
  const raw = bandValue(elapsedMs, seed, calm, burst, env);
  return phaseClamp(
    raw,
    calm,
    burst,
    t < CALM_END,
    t >= RAMP_END && t < BURST_END,
  );
}

function latencyAt(elapsedMs: number): number {
  const mid = (LATENCY_BAND[0] + LATENCY_BAND[1]) / 2;
  const half = (LATENCY_BAND[1] - LATENCY_BAND[0]) / 2;
  return clamp(
    mid + noise(elapsedMs, 120) * half * 0.92,
    LATENCY_BAND[0],
    LATENCY_BAND[1],
  );
}

function historyOf(
  elapsedMs: number,
  sample: (at: number) => number,
): readonly number[] {
  return Array.from({ length: HISTORY_POINTS }, (_, i) =>
    sample(elapsedMs - (HISTORY_POINTS - 1 - i) * HISTORY_STEP_MS),
  );
}

export function telemetryAt(elapsedMs: number): Telemetry {
  const t = wrap(elapsedMs);
  const env = fastEnvelope(t);
  const memEnv = memoryEnvelope(t);
  const pureCalm = t < CALM_END;
  const pureBurst = t >= RAMP_END && t < BURST_END;

  const ops = phaseClamp(
    bandValue(elapsedMs, 1, OPS_CALM, OPS_BURST, env),
    OPS_CALM,
    OPS_BURST,
    pureCalm,
    pureBurst,
  );

  const rawCores = Array.from({ length: CORE_COUNT }, (_, i) => {
    const bias = (i - (CORE_COUNT - 1) / 2) * 0.55;
    const raw =
      bandValue(elapsedMs, 10 + i * 3.7, CPU_CORE_CALM, CPU_CORE_BURST, env) +
      bias;
    return phaseClamp(raw, CPU_CORE_CALM, CPU_CORE_BURST, pureCalm, pureBurst);
  });
  const rawCoreMean = rawCores.reduce((s, v) => s + v, 0) / rawCores.length;

  const targetTotalRaw = bandValue(
    elapsedMs,
    200,
    [11, 17],
    CPU_TOTAL_BURST,
    env,
  );
  const targetTotal = pureBurst
    ? clamp(targetTotalRaw, CPU_TOTAL_BURST[0], CPU_TOTAL_BURST[1])
    : targetTotalRaw;
  const coreShift = targetTotal - rawCoreMean;
  const cores = rawCores.map((v) =>
    phaseClamp(
      v + coreShift,
      CPU_CORE_CALM,
      CPU_CORE_BURST,
      pureCalm,
      pureBurst,
    ),
  );
  const coreMean = cores.reduce((s, v) => s + v, 0) / cores.length;
  const cpuTotal = pureBurst
    ? clamp(coreMean, CPU_TOTAL_BURST[0], CPU_TOTAL_BURST[1])
    : coreMean;

  const memory = phaseClamp(
    bandValue(elapsedMs, 50, MEMORY_CALM, MEMORY_BURST, memEnv),
    MEMORY_CALM,
    MEMORY_BURST,
    pureCalm,
    pureBurst,
  );

  const sampleIn = (at: number) => netAt(at, 70, NET_IN_CALM, NET_IN_BURST);
  const sampleOut = (at: number) => netAt(at, 90, NET_OUT_CALM, NET_OUT_BURST);

  return {
    ops,
    cores,
    cpuTotal,
    memory,
    netIn: sampleIn(elapsedMs),
    netOut: sampleOut(elapsedMs),
    netInHistory: historyOf(elapsedMs, sampleIn),
    netOutHistory: historyOf(elapsedMs, sampleOut),
    latency: latencyAt(elapsedMs),
    latencyHistory: historyOf(elapsedMs, latencyAt),
  };
}

export const STATIC_BURST: Telemetry = {
  ops: 5500,
  cores: [88, 92, 85, 95, 90, 87, 93, 89],
  cpuTotal: 90,
  memory: 84,
  netIn: 82,
  netOut: 72,
  netInHistory: Array.from(
    { length: HISTORY_POINTS },
    (_, i) => 78 + ((i * 7) % 13),
  ),
  netOutHistory: Array.from(
    { length: HISTORY_POINTS },
    (_, i) => 66 + ((i * 5) % 15),
  ),
  latency: 12,
  latencyHistory: Array.from({ length: HISTORY_POINTS }, (_, i) =>
    i === HISTORY_POINTS - 1 ? 12 : 9.5 + ((i * 7) % 11) * 0.3,
  ),
};
