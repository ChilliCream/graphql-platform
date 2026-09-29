const toRad = (deg: number) => (deg * Math.PI) / 180;

// Collapses server/client sin/cos ULP drift before it reaches JSX attributes.
const round = (n: number) => Math.round(n * 1000) / 1000;

export function polarPoint(
  cx: number,
  cy: number,
  r: number,
  angleDeg: number,
): readonly [number, number] {
  const rad = toRad(angleDeg);
  return [round(cx + r * Math.cos(rad)), round(cy - r * Math.sin(rad))];
}

export function gaugeArcPath(
  cx: number,
  cy: number,
  r: number,
  startAngle: number,
  endAngle: number,
): string {
  const [sx, sy] = polarPoint(cx, cy, r, startAngle);
  const [ex, ey] = polarPoint(cx, cy, r, endAngle);
  const large = Math.abs(startAngle - endAngle) > 180 ? 1 : 0;
  // Ascending angle sweeps counter-clockwise on screen, so it needs the opposite flag.
  const sweep = endAngle > startAngle ? 0 : 1;
  return `M ${sx} ${sy} A ${r} ${r} 0 ${large} ${sweep} ${ex} ${ey}`;
}

export function sweepAngle(
  fraction: number,
  startAngle: number,
  endAngle: number,
): number {
  return startAngle + fraction * (endAngle - startAngle);
}

export function logFraction(value: number, min: number, max: number): number {
  const clamped = Math.min(max, Math.max(min, value));
  return Math.log(clamped / min) / Math.log(max / min);
}

export function polarCss(
  angleDeg: number,
  rFraction: number,
  offsetPx: number,
): { readonly left: string; readonly top: string } {
  const rad = toRad(angleDeg);
  const cos = Math.cos(rad);
  const sin = Math.sin(rad);
  return {
    left: `calc(${round(50 + cos * rFraction * 100)}% + ${round(cos * offsetPx)}px)`,
    top: `calc(${round(50 - sin * rFraction * 100)}% + ${round(-sin * offsetPx)}px)`,
  };
}
