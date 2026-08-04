import type { CSSProperties, ReactNode, Ref } from "react";

export type ChartCanvasSizing = "fill" | "fill-width" | "none";

export interface ChartCanvasProps {
  sizing?: ChartCanvasSizing;
  label?: string;
  className?: string;
  style?: CSSProperties;
  ref?: Ref<HTMLDivElement>;
  children?: ReactNode;
}

const SIZING_STYLE: Record<ChartCanvasSizing, CSSProperties> = {
  fill: { position: "relative", width: "100%", height: "100%" },
  "fill-width": { position: "relative", width: "100%" },
  none: {},
};

export function ChartCanvas({
  sizing = "fill",
  label,
  className,
  style,
  ref,
  children,
}: ChartCanvasProps) {
  return (
    <div
      ref={ref}
      className={className}
      style={{ ...SIZING_STYLE[sizing], ...style }}
      role="img"
      aria-label={label}
    >
      {children}
    </div>
  );
}
