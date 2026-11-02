export type Pt = readonly [number, number];

export interface Polyline {
  readonly pts: readonly Pt[];
  readonly lens: readonly number[];
  readonly total: number;
}

export function measure(pts: readonly Pt[]): Polyline {
  const lens: number[] = [];
  let total = 0;
  for (let i = 0; i < pts.length - 1; i++) {
    const len = Math.hypot(
      pts[i + 1][0] - pts[i][0],
      pts[i + 1][1] - pts[i][1],
    );
    lens.push(len);
    total += len;
  }
  return { pts, lens, total };
}

export function pointAt(p: Polyline, u: number): Pt {
  const target = clamp01(u) * p.total;
  let acc = 0;
  for (let i = 0; i < p.lens.length; i++) {
    if (target <= acc + p.lens[i] || i === p.lens.length - 1) {
      const t = p.lens[i] === 0 ? 0 : (target - acc) / p.lens[i];
      const [ax, ay] = p.pts[i];
      const [bx, by] = p.pts[i + 1];
      return [ax + (bx - ax) * t, ay + (by - ay) * t];
    }
    acc += p.lens[i];
  }
  return p.pts[p.pts.length - 1];
}

export function laneD(pts: readonly Pt[]): string {
  return pts.map(([x, y], i) => `${i === 0 ? "M" : "L"}${x} ${y}`).join(" ");
}

export function clamp01(v: number): number {
  return v < 0 ? 0 : v > 1 ? 1 : v;
}

export function ramp(t: number, a: number, b: number): number {
  return clamp01((t - a) / (b - a));
}

export function easeOutCubic(u: number): number {
  return 1 - Math.pow(1 - u, 3);
}

export function easeInOutCubic(u: number): number {
  return u < 0.5 ? 4 * u * u * u : 1 - Math.pow(-2 * u + 2, 3) / 2;
}
