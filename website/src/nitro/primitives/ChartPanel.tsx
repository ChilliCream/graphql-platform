import type { CSSProperties, ReactNode } from "react";
import { motion, useTransform, type MotionValue } from "motion/react";
import { clamp } from "../lib/scale";
import { ease } from "../lib/motion";
import { token } from "../lib/tokens";
import { useChartClock } from "../lib/useInViewLoop";
import { Legend, type LegendItem } from "./Legend";

export interface ChartPanelProps {
  title: string;
  subtitle?: string;
  legend?: LegendItem[];
  action?: ReactNode;
  height?: number;
  yDomain?: [number, number];
  yTicks?: number[];
  yFormat?: (n: number) => string;
  yLog?: boolean;
  yAxisWidth?: number;
  xTicks?: string[];
  children: ReactNode;
  progress?: MotionValue<number>;
  playWindow?: [number, number];
  durationMs?: number;
  className?: string;
  style?: CSSProperties;
}

function topFrac(
  v: number,
  [min, max]: [number, number],
  log: boolean,
): number {
  if (log) {
    const lo = Math.max(min, 0.0001);
    const f =
      Math.log10(Math.max(v, lo) / lo) /
      Math.log10(Math.max(max, lo * 10) / lo);
    return clamp(1 - f, 0, 1);
  }
  return clamp(1 - (v - min) / (max - min || 1), 0, 1);
}

export function ChartPanel({
  title,
  subtitle,
  legend,
  action,
  height = 200,
  yDomain,
  yTicks,
  yFormat = (n) => `${n}`,
  yLog = false,
  yAxisWidth = 46,
  xTicks,
  children,
  progress,
  playWindow,
  durationMs,
  className,
  style,
}: ChartPanelProps) {
  const { ref, t } = useChartClock({ progress, playWindow, durationMs });
  const opacity = useTransform(t, [0, 0.18], [0, 1], {
    ease: ease.out,
    clamp: true,
  });
  const y = useTransform(t, [0, 0.18], [10, 0], {
    ease: ease.out,
    clamp: true,
  });

  const showAxes = !!(yTicks && yDomain);

  return (
    <motion.section
      ref={ref}
      className={className}
      aria-label={subtitle ? `${title} — ${subtitle}` : title}
      style={{
        background: token.card,
        border: `1px solid ${token.borderStrong}`,
        borderRadius: 5,
        padding: 11,
        opacity,
        y,
        ...style,
      }}
    >
      <div
        style={{
          display: "flex",
          alignItems: "flex-start",
          gap: 12,
          marginBottom: 8,
        }}
      >
        <div style={{ minWidth: 0 }}>
          <div
            style={{
              fontSize: 13,
              fontWeight: 600,
              color: token.textStrong,
              lineHeight: 1.2,
            }}
          >
            {title}
          </div>
          {subtitle && (
            <div
              style={{ fontSize: 11, color: token.textSecondary, marginTop: 2 }}
            >
              {subtitle}
            </div>
          )}
        </div>
        {legend && <Legend items={legend} style={{ marginLeft: "auto" }} />}
        {action && (
          <div style={{ marginLeft: legend ? 12 : "auto" }}>{action}</div>
        )}
      </div>

      <div style={{ display: "flex" }}>
        {showAxes && (
          <div
            style={{
              position: "relative",
              width: yAxisWidth,
              height,
              flex: "0 0 auto",
            }}
          >
            {yTicks!.map((v) => (
              <span
                key={v}
                style={{
                  position: "absolute",
                  right: 8,
                  top: `${topFrac(v, yDomain!, yLog) * 100}%`,
                  transform: "translateY(-50%)",
                  fontSize: 10,
                  color: token.textSecondary,
                  whiteSpace: "nowrap",
                }}
              >
                {yFormat(v)}
              </span>
            ))}
          </div>
        )}
        <div
          style={{
            position: "relative",
            flex: 1,
            height: showAxes ? height : undefined,
          }}
        >
          {showAxes &&
            yTicks!.map((v) => (
              <div
                key={v}
                style={{
                  position: "absolute",
                  left: 0,
                  right: 0,
                  top: `${topFrac(v, yDomain!, yLog) * 100}%`,
                  borderTop: `1px solid ${token.grid}`,
                  pointerEvents: "none",
                }}
              />
            ))}
          {children}
        </div>
      </div>

      {xTicks && xTicks.length > 0 && (
        <div
          style={{
            display: "flex",
            paddingLeft: showAxes ? yAxisWidth : 0,
            marginTop: 6,
          }}
        >
          {xTicks.map((tk, i) => (
            <span
              key={i}
              style={{
                flex: 1,
                fontSize: 10,
                color: token.textSecondary,
                textAlign:
                  i === 0
                    ? "left"
                    : i === xTicks.length - 1
                      ? "right"
                      : "center",
              }}
            >
              {tk}
            </span>
          ))}
        </div>
      )}
    </motion.section>
  );
}
