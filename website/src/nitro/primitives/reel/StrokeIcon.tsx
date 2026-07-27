import type { CSSProperties } from "react";

interface StrokeIconProps {
  readonly d: string;
  readonly size?: number;
  readonly strokeWidth?: number;
  readonly fill?: string;
  readonly color?: string;
  readonly className?: string;
  readonly style?: CSSProperties;
}

export function StrokeIcon({
  d,
  size = 14,
  strokeWidth = 1.5,
  fill = "none",
  color,
  className,
  style,
}: StrokeIconProps) {
  return (
    <svg
      width={size}
      height={size}
      viewBox="0 0 24 24"
      fill={fill}
      stroke={color ?? "currentColor"}
      strokeWidth={strokeWidth}
      strokeLinecap="round"
      strokeLinejoin="round"
      className={className}
      style={{ flex: "0 0 auto", ...style }}
    >
      <path d={d} />
    </svg>
  );
}
