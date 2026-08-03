export type Pt = readonly [number, number];

export const clamp = (x: number, lo: number, hi: number) =>
  x < lo ? lo : x > hi ? hi : x;

export const lerp = (a: number, b: number, t: number) => a + (b - a) * t;

export const norm = (x: number, a: number, b: number) =>
  b === a ? 0 : clamp((x - a) / (b - a), 0, 1);

export function linScale(
  d0: number,
  d1: number,
  r0: number,
  r1: number,
): (x: number) => number {
  const m = d1 === d0 ? 0 : (r1 - r0) / (d1 - d0);
  return (x: number) => r0 + (x - d0) * m;
}

export function logScale(
  d0: number,
  d1: number,
  r0: number,
  r1: number,
  min = 0.1,
): (x: number) => number {
  const l0 = Math.log10(Math.max(d0, min));
  const l1 = Math.log10(Math.max(d1, min));
  const m = l1 === l0 ? 0 : (r1 - r0) / (l1 - l0);
  // Math.log10 can differ by a few ulps across JavaScript runtimes. Bound the
  // precision so server-rendered coordinates match during browser hydration.
  return (x: number) =>
    +(r0 + (Math.log10(Math.max(x, min)) - l0) * m).toFixed(6);
}

const f = (n: number) => (Number.isFinite(n) ? +n.toFixed(2) : 0);

export function linePath(pts: readonly Pt[]): string {
  if (!pts.length) return "";
  return pts.map(([x, y], i) => `${i ? "L" : "M"}${f(x)} ${f(y)}`).join(" ");
}

export function smoothLinePath(pts: readonly Pt[], tension = 0.5): string {
  const n = pts.length;
  if (n < 2) return linePath(pts);
  if (n === 2)
    return `M${f(pts[0][0])} ${f(pts[0][1])} L${f(pts[1][0])} ${f(pts[1][1])}`;
  const k = (1 - tension) / 6;
  let d = `M${f(pts[0][0])} ${f(pts[0][1])}`;
  for (let i = 0; i < n - 1; i++) {
    const p0 = pts[i - 1] ?? pts[i];
    const p1 = pts[i];
    const p2 = pts[i + 1];
    const p3 = pts[i + 2] ?? p2;
    const c1x = p1[0] + (p2[0] - p0[0]) * k;
    const c1y = p1[1] + (p2[1] - p0[1]) * k;
    const c2x = p2[0] - (p3[0] - p1[0]) * k;
    const c2y = p2[1] - (p3[1] - p1[1]) * k;
    d += ` C${f(c1x)} ${f(c1y)} ${f(c2x)} ${f(c2y)} ${f(p2[0])} ${f(p2[1])}`;
  }
  return d;
}

export function areaFromLine(
  linePathD: string,
  pts: readonly Pt[],
  baselineY: number,
): string {
  if (!pts.length) return "";
  const first = pts[0];
  const last = pts[pts.length - 1];
  return `${linePathD} L${f(last[0])} ${f(baselineY)} L${f(first[0])} ${f(baselineY)} Z`;
}

const hex = (h: string) => {
  const s = h.replace("#", "");
  const v =
    s.length === 3
      ? s
          .split("")
          .map((c) => c + c)
          .join("")
      : s;
  return [
    parseInt(v.slice(0, 2), 16),
    parseInt(v.slice(2, 4), 16),
    parseInt(v.slice(4, 6), 16),
  ] as const;
};

const toHex = (rgb: readonly number[]) =>
  "#" +
  rgb
    .map((c) =>
      Math.round(clamp(c, 0, 255))
        .toString(16)
        .padStart(2, "0"),
    )
    .join("");

export function lerpColor(a: string, b: string, t: number): string {
  const ca = hex(a);
  const cb = hex(b);
  return toHex([
    lerp(ca[0], cb[0], t),
    lerp(ca[1], cb[1], t),
    lerp(ca[2], cb[2], t),
  ]);
}

export function colorAt(stops: readonly string[], t: number): string {
  if (stops.length === 1) return stops[0];
  const x = clamp(t, 0, 1) * (stops.length - 1);
  const i = Math.min(Math.floor(x), stops.length - 2);
  return lerpColor(stops[i], stops[i + 1], x - i);
}

export function niceTicks(min: number, max: number, count = 4): number[] {
  if (min === max) return [min];
  const span = max - min;
  const step0 = span / count;
  const mag = Math.pow(10, Math.floor(Math.log10(step0)));
  const norms = [1, 2, 2.5, 5, 10];
  const step = (norms.find((n) => n * mag >= step0) ?? 10) * mag;
  const start = Math.ceil(min / step) * step;
  const out: number[] = [];
  for (let v = start; v <= max + step * 0.001; v += step)
    out.push(+v.toFixed(6));
  return out;
}

export function compact(n: number): string {
  const abs = Math.abs(n);
  if (abs >= 1e6) return (n / 1e6).toFixed(abs >= 1e7 ? 0 : 1) + "M";
  if (abs >= 1e3) return (n / 1e3).toFixed(abs >= 1e4 ? 0 : 1) + "k";
  return `${Math.round(n)}`;
}

export function ms(n: number): string {
  if (n >= 1000) return (n / 1000).toFixed(2) + " s";
  if (n >= 100) return Math.round(n) + " ms";
  return n.toFixed(n < 10 ? 1 : 0) + " ms";
}
