import { SERVICE_SPECTRUM } from "../../spectrum";

/**
 * Pure particle-path math for the Particle Stream hero: five service-coloured
 * lanes curve in from the right edge to one shared attractor, then continue
 * left as a single river. No canvas or DOM calls live here; `index.tsx`
 * drives the canvas with the values these functions compute.
 */

export const LANE_COUNT = SERVICE_SPECTRUM.length;
export const PARTICLES_PER_LANE = 170;

/** Fraction of a particle's cycle spent in the coloured stream phase; the rest is the white river. */
const STREAM_FRACTION = 0.56;

export interface Point {
  readonly x: number;
  readonly y: number;
}

export interface Layout {
  readonly width: number;
  readonly height: number;
  readonly attractor: Point;
}

export interface Particle {
  readonly lane: number;
  readonly base: number;
  readonly speed: number;
  readonly lateralOffset: number;
  readonly lateralAmp: number;
  readonly phaseOffset: number;
  readonly radius: number;
}

export interface ParticleFrame {
  readonly x: number;
  readonly y: number;
  readonly alpha: number;
  /** 0 keeps the lane colour, 1 is fully white. */
  readonly colorMix: number;
}

export type Rgb = readonly [number, number, number];

/** Deterministic PRNG so the field spread looks the same on every render. */
export function mulberry32(seed: number): () => number {
  let a = seed;
  return () => {
    a |= 0;
    a = (a + 0x6d2b79f5) | 0;
    let t = Math.imul(a ^ (a >>> 15), 1 | a);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

/** The attractor sits right of and below the copy column, at a fixed fraction of the hero's size. */
export function computeLayout(width: number, height: number): Layout {
  return {
    width,
    height,
    attractor: { x: width * 0.66, y: height * 0.74 },
  };
}

export function createParticles(seed: number): Particle[] {
  const rng = mulberry32(seed);
  const particles: Particle[] = [];
  for (let lane = 0; lane < LANE_COUNT; lane++) {
    for (let i = 0; i < PARTICLES_PER_LANE; i++) {
      particles.push({
        lane,
        base: (i + rng() * 0.7) / PARTICLES_PER_LANE,
        speed: 0.045 + rng() * 0.02,
        lateralOffset: (rng() - 0.5) * 2,
        lateralAmp: 4 + rng() * 9,
        phaseOffset: rng() * Math.PI * 2,
        radius: 1 + rng() * 1.5,
      });
    }
  }
  return particles;
}

function laneSpawnY(lane: number, layout: Layout): number {
  const top = layout.height * 0.14;
  const bottom = layout.height * 0.86;
  const step = LANE_COUNT > 1 ? (bottom - top) / (LANE_COUNT - 1) : 0;
  return top + step * lane;
}

function quadBezier(p0: Point, p1: Point, p2: Point, t: number): Point {
  const mt = 1 - t;
  return {
    x: mt * mt * p0.x + 2 * mt * t * p1.x + t * t * p2.x,
    y: mt * mt * p0.y + 2 * mt * t * p1.y + t * t * p2.y,
  };
}

function smoothstep(edge0: number, edge1: number, x: number): number {
  const t = Math.min(1, Math.max(0, (x - edge0) / (edge1 - edge0)));
  return t * t * (3 - 2 * t);
}

/**
 * A particle's position at `time` seconds, 0 being the rest frame. Below
 * `STREAM_FRACTION` it rides its lane's curve toward the attractor and its
 * colour blends to white near the end; above it, it rides the shared river
 * curve out past the left edge as white.
 */
export function particlePosition(
  particle: Particle,
  time: number,
  layout: Layout,
): ParticleFrame {
  const cycle = (particle.base + time * particle.speed) % 1;

  if (cycle < STREAM_FRACTION) {
    const t = cycle / STREAM_FRACTION;
    const spawn: Point = {
      x: layout.width * 1.08,
      y: laneSpawnY(particle.lane, layout),
    };
    const control: Point = {
      x: spawn.x + (layout.attractor.x - spawn.x) * 0.62,
      y: spawn.y + particle.lateralOffset * 6,
    };
    const wobble =
      Math.sin(time * 1.4 + particle.phaseOffset) *
      particle.lateralAmp *
      (1 - t);
    const base = quadBezier(spawn, control, layout.attractor, t);
    return {
      x: base.x,
      y: base.y + wobble,
      alpha: smoothstep(0, 0.06, t),
      colorMix: smoothstep(0.74, 1, t),
    };
  }

  const t = (cycle - STREAM_FRACTION) / (1 - STREAM_FRACTION);
  const exit: Point = {
    x: -layout.width * 0.08,
    y: layout.attractor.y + layout.height * 0.07,
  };
  const control: Point = {
    x: layout.attractor.x + (exit.x - layout.attractor.x) * 0.5,
    y: layout.attractor.y - layout.height * 0.05,
  };
  const wobble =
    Math.sin(time * 1.1 + particle.phaseOffset) * particle.lateralAmp * t;
  const base = quadBezier(layout.attractor, control, exit, t);
  return {
    x: base.x,
    y: base.y + particle.lateralOffset * 8 * t + wobble,
    alpha: 1 - smoothstep(0.82, 1, t),
    colorMix: 1,
  };
}

/** Parses a `#rrggbb` spectrum stop into an RGB triple for canvas blending. */
export function hexToRgb(hex: string): Rgb {
  const value = Number.parseInt(hex.replace("#", ""), 16);
  return [(value >> 16) & 255, (value >> 8) & 255, value & 255];
}

export function mixRgb(a: Rgb, b: Rgb, t: number): Rgb {
  const clamped = Math.min(1, Math.max(0, t));
  return [
    Math.round(a[0] + (b[0] - a[0]) * clamped),
    Math.round(a[1] + (b[1] - a[1]) * clamped),
    Math.round(a[2] + (b[2] - a[2]) * clamped),
  ];
}
