/** Pure geometry for the Tachometer: a 180° gauge from angle 180° (value 0, pointing left) to 0° (max, pointing right). */

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

/** SVG arc path for the ring segment between two gauge angles (degrees, 180 → 0). */
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

/** Maps a gauge value to the needle's rotation in degrees (0 at value 0, 180 at max). */
export function needleRotation(value: number, max: number): number {
  return (value / max) * 180;
}
