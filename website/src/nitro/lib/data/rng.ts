export type Rng = () => number;

export function mulberry32(seed: number): Rng {
  let a = seed >>> 0;
  return () => {
    a |= 0;
    a = (a + 0x6d2b79f5) | 0;
    let t = Math.imul(a ^ (a >>> 15), 1 | a);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

export const uniform = (r: Rng, lo: number, hi: number) => lo + r() * (hi - lo);

export const gaussian = (r: Rng) => (r() + r() + r() - 1.5) * (1 / 0.866);

export const normal = (r: Rng, mean: number, sd: number) =>
  mean + gaussian(r) * sd;

export const pick = <T>(r: Rng, xs: readonly T[]): T =>
  xs[Math.floor(r() * xs.length)];
