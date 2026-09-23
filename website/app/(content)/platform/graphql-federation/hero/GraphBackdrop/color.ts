// One local helper turning a palette hex string (or the words "white" /
// "black") into an rgba() string, so the folder itself holds no hex
// literals: every colour value flows through here from hero/palette.ts.
export function rgba(color: string, alpha: number): string {
  if (color === "white") {
    return `rgba(255,255,255,${alpha})`;
  }
  if (color === "black") {
    return `rgba(0,0,0,${alpha})`;
  }
  const hex = color.replace("#", "");
  const r = parseInt(hex.slice(0, 2), 16);
  const g = parseInt(hex.slice(2, 4), 16);
  const b = parseInt(hex.slice(4, 6), 16);
  return `rgba(${r},${g},${b},${alpha})`;
}

export const WHITE = "white";
