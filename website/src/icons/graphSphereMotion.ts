import { SPHERE_GRAPH, VIEWBOX } from "./graphSphereGeometry";
import type { GraphGeometry } from "./graphSphereGeometry";

export const ROTATION_PERIOD_MS = 75_000;

const DOT_COUNT = 4;
const DOT_HOPS = 3;
const DOT_HOP_MS = 4200;
const DOT_HOP_STAGGER_MS = 650;
export const DOT_RADIUS = 2.4;

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

export interface DotPath {
  readonly path: readonly number[];
  readonly hopMs: number;
}

export interface DotFrame {
  readonly fromVertex: number;
  readonly toVertex: number;
  readonly frac: number;
  readonly envelope: number;
}

export interface PulseCandidate {
  readonly vertex: number;
  readonly age: number;
}

export interface NodeMotion {
  readonly dx: number;
  readonly dy: number;
  readonly brightness: number;
  readonly ringScale: number;
  readonly ringAlpha: number;
}

export interface GraphMotion {
  readonly dotPaths: readonly DotPath[];
  readonly dotFrame: (dot: DotPath, elapsedMs: number) => DotFrame;
  readonly pulseCandidates: (elapsedMs: number) => readonly PulseCandidate[];
  readonly nodeMotion: (
    vertexIndex: number,
    elapsedMs: number,
    x: number,
    y: number,
    pulseAge: number | undefined,
  ) => NodeMotion;
}

function smoothstep01(t: number): number {
  const c = Math.min(1, Math.max(0, t));
  return c * c * (3 - 2 * c);
}

function motionRamp(elapsedMs: number): number {
  return smoothstep01(elapsedMs / MOTION_RAMP_MS);
}

function buildAdjacency(graph: GraphGeometry): readonly (readonly number[])[] {
  const adjacency: number[][] = Array.from(
    { length: graph.vertices.length },
    () => [],
  );
  for (const [a, b] of graph.edges) {
    adjacency[a].push(b);
    adjacency[b].push(a);
  }
  for (const list of adjacency) {
    list.sort((a, b) => a - b);
  }
  return adjacency;
}

function walkPath(
  adjacency: readonly (readonly number[])[],
  start: number,
  hops: number,
): readonly number[] {
  const path = [start];
  let prev = -1;
  let current = start;
  for (let i = 0; i < hops; i++) {
    const neighbors = adjacency[current];
    const next = neighbors.find((n) => n !== prev) ?? neighbors[0];
    path.push(next);
    prev = current;
    current = next;
  }
  return path;
}

export function createGraphMotion(graph: GraphGeometry): GraphMotion {
  const adjacency = buildAdjacency(graph);
  const seedStride = Math.floor(graph.vertices.length / DOT_COUNT);
  const dotPaths: readonly DotPath[] = Array.from(
    { length: DOT_COUNT },
    (_, i) => ({
      path: walkPath(adjacency, i * seedStride, DOT_HOPS),
      hopMs: DOT_HOP_MS + i * DOT_HOP_STAGGER_MS,
    }),
  );

  function dotFrame(dot: DotPath, elapsedMs: number): DotFrame {
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

  function pulseCandidates(elapsedMs: number): readonly PulseCandidate[] {
    const active = dotPaths
      .map((dot) => {
        const frame = dotFrame(dot, elapsedMs);
        return { vertex: frame.fromVertex, age: frame.frac * dot.hopMs };
      })
      .filter((candidate) => candidate.age < PULSE_DURATION_MS);
    active.sort((a, b) => a.age - b.age);
    return active.slice(0, MAX_CONCURRENT_PULSES);
  }

  function nodeMotion(
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
      graph.radiusPx *
      (DRIFT_RADIAL_AMPLITUDE_A *
        Math.sin((2 * Math.PI * elapsedMs) / DRIFT_RADIAL_PERIOD_A_MS + phase) +
        DRIFT_RADIAL_AMPLITUDE_B *
          Math.sin(
            (2 * Math.PI * elapsedMs) / DRIFT_RADIAL_PERIOD_B_MS +
              phase * 1.7 +
              1.1,
          ));
    const tangential =
      graph.radiusPx *
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
        graph.radiusPx *
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

  return { dotPaths, dotFrame, pulseCandidates, nodeMotion };
}

const SPHERE_MOTION = createGraphMotion(SPHERE_GRAPH);
export const DOT_PATHS = SPHERE_MOTION.dotPaths;
export const dotFrame = SPHERE_MOTION.dotFrame;
export const pulseCandidates = SPHERE_MOTION.pulseCandidates;
export const nodeMotion = SPHERE_MOTION.nodeMotion;
