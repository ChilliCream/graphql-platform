export interface OrbitRingSpec {
  readonly r: number;
  readonly strokeOpacity: number;
}

export const ORBIT_RINGS: readonly OrbitRingSpec[] = [
  { r: 440, strokeOpacity: 1 },
  { r: 560, strokeOpacity: 1 },
  { r: 680, strokeOpacity: 0.9 },
  { r: 800, strokeOpacity: 0.85 },
];

export interface ConnectorSpec {
  readonly key: string;
  readonly r1: number;
  readonly a1: number;
  readonly r2: number;
  readonly a2: number;
  readonly strokeOpacity: number;
}

export const CONNECTORS: readonly ConnectorSpec[] = [
  { key: "a-b", r1: 440, a1: 167, r2: 560, a2: 176, strokeOpacity: 1 },
  { key: "c-d", r1: 680, a1: 205, r2: 800, a2: 197, strokeOpacity: 0.85 },
  { key: "b-a", r1: 560, a1: 338, r2: 440, a2: 344, strokeOpacity: 1 },
  { key: "b-c", r1: 560, a1: 366, r2: 680, a2: 358, strokeOpacity: 0.9 },
];

const rad = (deg: number) => (deg * Math.PI) / 180;

export function polarPoint(
  r: number,
  angleDeg: number,
): readonly [number, number] {
  return [800 + r * Math.cos(rad(angleDeg)), 800 + r * Math.sin(rad(angleDeg))];
}

export function connectorPath(
  r1: number,
  a1: number,
  r2: number,
  a2: number,
): string {
  const t1 = rad(a1);
  const dt = rad(a2) - rad(a1);
  const dr = r2 - r1;
  const position = (u: number): [number, number] => {
    const s = u * u * (3 - 2 * u);
    const r = r1 + dr * s;
    const th = t1 + dt * u;
    return [800 + r * Math.cos(th), 800 + r * Math.sin(th)];
  };
  const derivative = (u: number): [number, number] => {
    const s = u * u * (3 - 2 * u);
    const ds = 6 * u * (1 - u);
    const r = r1 + dr * s;
    const rPrime = dr * ds;
    const th = t1 + dt * u;
    return [
      rPrime * Math.cos(th) - r * dt * Math.sin(th),
      rPrime * Math.sin(th) + r * dt * Math.cos(th),
    ];
  };
  const segments = Math.max(1, Math.ceil(Math.abs(a2 - a1) / 45));
  const h = 1 / segments;
  const f = (n: number) => n.toFixed(1);
  const [x0, y0] = position(0);
  const parts = [`M ${f(x0)} ${f(y0)}`];
  for (let i = 0; i < segments; i++) {
    const u0 = i * h;
    const u1 = (i + 1) * h;
    const [px0, py0] = position(u0);
    const [px1, py1] = position(u1);
    const [dx0, dy0] = derivative(u0);
    const [dx1, dy1] = derivative(u1);
    parts.push(
      `C ${f(px0 + (dx0 * h) / 3)} ${f(py0 + (dy0 * h) / 3)},`,
      `${f(px1 - (dx1 * h) / 3)} ${f(py1 - (dy1 * h) / 3)},`,
      `${f(px1)} ${f(py1)}`,
    );
  }
  return parts.join(" ");
}
