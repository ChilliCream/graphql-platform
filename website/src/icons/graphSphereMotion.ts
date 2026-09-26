import {
  SPHERE_EDGES,
  SPHERE_RADIUS_PX,
  SPHERE_VERTICES,
  VIEWBOX,
} from "./graphSphereGeometry";

export const ROTATION_PERIOD_MS = 75_000;

const DOT_COUNT = 4;
const DOT_HOPS = 3;
const DOT_HOP_MS = 4200;
const DOT_HOP_STAGGER_MS = 650;
const DOT_SEED_STRIDE = Math.floor(SPHERE_VERTICES.length / DOT_COUNT);

export const DOT_RADIUS = 2.4;

const BREATHE_PERIOD_MS = 5200;
const BREATHE_AMPLITUDE = 0.06;

function buildAdjacency(): readonly (readonly number[])[] {
  const adjacency: number[][] = Array.from(
    { length: SPHERE_VERTICES.length },
    () => [],
  );
  for (const [a, b] of SPHERE_EDGES) {
    adjacency[a].push(b);
    adjacency[b].push(a);
  }
  for (const list of adjacency) list.sort((a, b) => a - b);
  return adjacency;
}

const ADJACENCY = buildAdjacency();

function walkPath(start: number, hops: number): readonly number[] {
  const path = [start];
  let prev = -1;
  let current = start;
  for (let i = 0; i < hops; i++) {
    const neighbors = ADJACENCY[current];
    const next = neighbors.find((n) => n !== prev) ?? neighbors[0];
    path.push(next);
    prev = current;
    current = next;
  }
  return path;
}

interface DotPath {
  readonly path: readonly number[];
  readonly hopMs: number;
}

export const DOT_PATHS: readonly DotPath[] = Array.from(
  { length: DOT_COUNT },
  (_, i) => ({
    path: walkPath(i * DOT_SEED_STRIDE, DOT_HOPS),
    hopMs: DOT_HOP_MS + i * DOT_HOP_STAGGER_MS,
  }),
);

interface DotFrame {
  readonly fromVertex: number;
  readonly toVertex: number;
  readonly frac: number;
  readonly envelope: number;
}

export function dotFrame(dot: DotPath, elapsedMs: number): DotFrame {
  const hops = dot.path.length - 1;
  const cycleMs = dot.hopMs * hops * 2;
  const s = (elapsedMs % cycleMs) / dot.hopMs;
  const forward = s < hops;
  const u = forward ? s : s - hops;
  const leg = Math.min(hops - 1, Math.floor(u));
  const frac = u - leg;
  const fromIndex = forward ? leg : hops - leg;
  const toIndex = forward ? leg + 1 : hops - leg - 1;
  return {
    fromVertex: dot.path[fromIndex],
    toVertex: dot.path[toIndex],
    frac,
    envelope: Math.sin(Math.PI * frac),
  };
}

export function breatheScale(phaseOffset: number, elapsedMs: number): number {
  return (
    1 +
    BREATHE_AMPLITUDE *
      Math.sin((2 * Math.PI * elapsedMs) / BREATHE_PERIOD_MS + phaseOffset)
  );
}

const CENTER = VIEWBOX / 2;
const GOLDEN_ANGLE = 2.399963229728653;

// Displacements ramp in from zero over this window, so t=0 stays static.
const MOTION_RAMP_MS = 4000;

const DRIFT_RADIAL_PERIOD_A_MS = 9000;
const DRIFT_RADIAL_PERIOD_B_MS = 13000;
const DRIFT_TANGENTIAL_PERIOD_A_MS = 11000;
const DRIFT_TANGENTIAL_PERIOD_B_MS = 15500;
const DRIFT_RADIAL_AMPLITUDE_A = 0.022;
const DRIFT_RADIAL_AMPLITUDE_B = 0.013;
const DRIFT_TANGENTIAL_AMPLITUDE_A = 0.011;
const DRIFT_TANGENTIAL_AMPLITUDE_B = 0.006;

export const PULSE_DURATION_MS = 650;
export const MAX_CONCURRENT_PULSES = 2;

const BUMP_AMPLITUDE = 0.011;
const BUMP_OSCILLATIONS = 2;
const BUMP_DAMPING = 4.5;
const BRIGHTNESS_MAX = 0.18;
const BRIGHTNESS_DECAY = 4;
const RING_MAX_SCALE = 3.2;
const RING_ALPHA_MAX = 0.45;

function smoothstep01(t: number): number {
  const c = Math.min(1, Math.max(0, t));
  return c * c * (3 - 2 * c);
}

function motionRamp(elapsedMs: number): number {
  return smoothstep01(elapsedMs / MOTION_RAMP_MS);
}

interface PulseCandidate {
  readonly vertex: number;
  readonly age: number;
}

// The node a dot last reached, and how long ago (dotFrame's per-leg fraction resets to zero on arrival).
export function pulseCandidates(elapsedMs: number): readonly PulseCandidate[] {
  const active = DOT_PATHS.map((dot) => {
    const frame = dotFrame(dot, elapsedMs);
    return { vertex: frame.fromVertex, age: frame.frac * dot.hopMs };
  }).filter((c) => c.age < PULSE_DURATION_MS);
  active.sort((a, b) => a.age - b.age);
  return active.slice(0, MAX_CONCURRENT_PULSES);
}

export interface NodeMotion {
  readonly dx: number;
  readonly dy: number;
  readonly brightness: number;
  readonly ringScale: number;
  readonly ringAlpha: number;
}

export function nodeMotion(
  vertexIndex: number,
  elapsedMs: number,
  x: number,
  y: number,
  pulseAge: number | undefined,
): NodeMotion {
  const fromCenterX = x - CENTER;
  const fromCenterY = y - CENTER;
  const dist = Math.hypot(fromCenterX, fromCenterY) || 1;
  const ux = fromCenterX / dist;
  const uy = fromCenterY / dist;
  const tx = -uy;
  const ty = ux;
  const ramp = motionRamp(elapsedMs);
  const phase = vertexIndex * GOLDEN_ANGLE;

  const radial =
    SPHERE_RADIUS_PX *
    (DRIFT_RADIAL_AMPLITUDE_A *
      Math.sin((2 * Math.PI * elapsedMs) / DRIFT_RADIAL_PERIOD_A_MS + phase) +
      DRIFT_RADIAL_AMPLITUDE_B *
        Math.sin(
          (2 * Math.PI * elapsedMs) / DRIFT_RADIAL_PERIOD_B_MS +
            phase * 1.7 +
            1.1,
        ));
  const tangential =
    SPHERE_RADIUS_PX *
    (DRIFT_TANGENTIAL_AMPLITUDE_A *
      Math.sin(
        (2 * Math.PI * elapsedMs) / DRIFT_TANGENTIAL_PERIOD_A_MS +
          phase * 0.6 +
          2.3,
      ) +
      DRIFT_TANGENTIAL_AMPLITUDE_B *
        Math.sin(
          (2 * Math.PI * elapsedMs) / DRIFT_TANGENTIAL_PERIOD_B_MS +
            phase * 2.1,
        ));

  let dx = (radial * ux + tangential * tx) * ramp;
  let dy = (radial * uy + tangential * ty) * ramp;
  let brightness = 0;
  let ringScale = 1;
  let ringAlpha = 0;

  if (pulseAge !== undefined) {
    const p = Math.min(1, Math.max(0, pulseAge / PULSE_DURATION_MS));
    const bump =
      SPHERE_RADIUS_PX *
      BUMP_AMPLITUDE *
      Math.exp(-BUMP_DAMPING * p) *
      Math.sin(2 * Math.PI * BUMP_OSCILLATIONS * p) *
      ramp;
    dx += bump * ux;
    dy += bump * uy;
    brightness = BRIGHTNESS_MAX * Math.exp(-BRIGHTNESS_DECAY * p) * ramp;
    const ease = 1 - (1 - p) ** 3;
    ringScale = 1 + (RING_MAX_SCALE - 1) * ease;
    ringAlpha = RING_ALPHA_MAX * (1 - p) * (1 - p) * ramp;
  }

  return { dx, dy, brightness, ringScale, ringAlpha };
}

export function prefersReducedMotion(): boolean {
  return (
    typeof window !== "undefined" &&
    window.matchMedia("(prefers-reduced-motion: reduce)").matches
  );
}
