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
  // polarPoint negates sin for its y-up sweep, so an ascending angle draws
  // counter-clockwise on screen and needs the opposite SVG sweep flag from a
  // descending one to hug the circle instead of bowing across the chord.
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
