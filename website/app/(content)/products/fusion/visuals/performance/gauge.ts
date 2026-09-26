const toRad = (deg: number) => (deg * Math.PI) / 180;

export function polarPoint(
  cx: number,
  cy: number,
  r: number,
  angleDeg: number,
): readonly [number, number] {
  const rad = toRad(angleDeg);
  return [cx + r * Math.cos(rad), cy - r * Math.sin(rad)];
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
  return `M ${sx} ${sy} A ${r} ${r} 0 ${large} 1 ${ex} ${ey}`;
}

export function needleRotation(value: number, max: number): number {
  return (value / max) * 180;
}
