/**
 * Production-view scene, variant 1 (single operation metric card).
 *
 * A small cropped panel of the Nitro / Banana Cake Pop telemetry app: one
 * operation's metric card, the unit a production dashboard is built from. It
 * shows the `checkout` operation name with a span-kind tag, a hand-drawn
 * p99-latency sparkline that kinks up at the right edge (the regression that
 * triggered the alert), two stat numbers (p95 42 ms, errors 0.3%), and an amber
 * "Investigating" status pill.
 *
 * Self-contained Nitro-palette panel: colors come from `nitro` via inline
 * `style={{}}` (not the site cc-* tokens, which is intentional for these
 * illustrations). Static final frame only: no motion package, no hooks, no
 * animation. Every SVG id is prefixed "observe-v1-".
 */
import type { CSSProperties } from "react";

import { nitro } from "@/src/components/home/act2/variants/nitroTheme";

interface ObserveVariant1Props {
  readonly className?: string;
}

// p99 latency sample (ms), a flat baseline that kinks sharply up at the tail —
// the regression the on-call engineer is investigating.
const P99_SERIES = [
  38, 40, 39, 41, 40, 42, 41, 43, 42, 44, 48, 57, 71, 86,
] as const;

// Sparkline viewBox; rendered with preserveAspectRatio="none" so it stretches
// to the card width while strokes stay crisp via non-scaling-stroke.
const SPARK_W = 240;
const SPARK_H = 44;
const SPARK_PAD_Y = 4;

function buildSparkPaths(values: readonly number[]) {
  const n = values.length;
  const min = Math.min(...values);
  const max = Math.max(...values);
  const top = SPARK_PAD_Y;
  const bottom = SPARK_H - SPARK_PAD_Y;
  const xOf = (i: number) => (i / (n - 1)) * SPARK_W;
  const yOf = (v: number) =>
    max === min
      ? (top + bottom) / 2
      : bottom - ((v - min) / (max - min)) * (bottom - top);

  const pts = values.map((v, i) => [xOf(i), yOf(v)] as const);
  const line = pts
    .map(([x, y], i) => `${i === 0 ? "M" : "L"}${x} ${y}`)
    .join(" ");
  const last = pts[pts.length - 1];
  const area = `${line} L${last[0]} ${bottom} L${pts[0][0]} ${bottom} Z`;
  return { line, area, last };
}

function StatBlock({
  label,
  value,
  unit,
  tone,
}: {
  readonly label: string;
  readonly value: string;
  readonly unit?: string;
  readonly tone?: "normal" | "error";
}) {
  return (
    <div style={{ display: "flex", flexDirection: "column", gap: 3 }}>
      <span
        style={{
          fontFamily: nitro.font,
          fontSize: 10,
          letterSpacing: "0.04em",
          textTransform: "uppercase",
          color: nitro.textSecondary,
        }}
      >
        {label}
      </span>
      <span
        style={{
          fontFamily: nitro.mono,
          fontSize: 18,
          fontWeight: 600,
          lineHeight: 1,
          color: tone === "error" ? nitro.errorText : nitro.textStrong,
          fontVariantNumeric: "tabular-nums",
        }}
      >
        {value}
        {unit && (
          <span
            style={{
              fontSize: 11,
              fontWeight: 400,
              color: nitro.textSecondary,
              marginLeft: 2,
            }}
          >
            {unit}
          </span>
        )}
      </span>
    </div>
  );
}

export function ObserveVariant1({ className }: ObserveVariant1Props) {
  const { line, area, last } = buildSparkPaths(P99_SERIES);

  const cardStyle: CSSProperties = {
    background: nitro.bg,
    border: `1px solid ${nitro.border}`,
    borderRadius: nitro.radius,
    padding: 14,
    fontFamily: nitro.font,
    fontSize: 12,
    color: nitro.text,
    lineHeight: 1.4,
    boxSizing: "border-box",
  };

  return (
    <div
      className={className}
      style={{ width: "100%", maxWidth: 320, marginInline: "auto" }}
    >
      <div style={cardStyle}>
        {/* Header: operation name + span-kind tag on the left, status pill right. */}
        <div
          style={{
            display: "flex",
            alignItems: "center",
            justifyContent: "space-between",
            gap: 10,
            marginBottom: 12,
          }}
        >
          <div
            style={{
              display: "flex",
              alignItems: "center",
              gap: 7,
              minWidth: 0,
            }}
          >
            <span
              aria-hidden="true"
              style={{
                width: 7,
                height: 7,
                borderRadius: "50%",
                background: nitro.warning,
                flexShrink: 0,
              }}
            />
            <span
              style={{
                fontFamily: nitro.mono,
                fontSize: 13,
                fontWeight: 600,
                color: nitro.textStrong,
                whiteSpace: "nowrap",
                overflow: "hidden",
                textOverflow: "ellipsis",
              }}
            >
              checkout
            </span>
            <span
              style={{
                fontFamily: nitro.mono,
                fontSize: 9,
                lineHeight: 1.3,
                padding: "1px 4px",
                color: nitro.textSecondary,
                border: `1px solid ${nitro.border}`,
                borderRadius: 3,
                whiteSpace: "nowrap",
                flexShrink: 0,
              }}
            >
              query
            </span>
          </div>
          <span
            style={{
              display: "inline-flex",
              alignItems: "center",
              fontFamily: nitro.font,
              fontSize: 10,
              fontWeight: 600,
              color: nitro.warning,
              padding: "2px 8px",
              borderRadius: 999,
              border: `1px solid ${nitro.warning}`,
              background: "rgba(255, 201, 20, 0.12)",
              whiteSpace: "nowrap",
              flexShrink: 0,
            }}
          >
            Investigating
          </span>
        </div>

        {/* p99 latency sparkline, kinking up at the tail. */}
        <div style={{ marginBottom: 12 }}>
          <div
            style={{
              display: "flex",
              alignItems: "baseline",
              justifyContent: "space-between",
              marginBottom: 5,
            }}
          >
            <span
              style={{
                fontFamily: nitro.font,
                fontSize: 10,
                letterSpacing: "0.04em",
                textTransform: "uppercase",
                color: nitro.textSecondary,
              }}
            >
              p99 latency
            </span>
            <span
              style={{
                fontFamily: nitro.mono,
                fontSize: 11,
                fontWeight: 600,
                color: nitro.errorText,
                fontVariantNumeric: "tabular-nums",
              }}
            >
              86 ms
            </span>
          </div>
          <div style={{ height: SPARK_H, width: "100%" }}>
            <svg
              viewBox={`0 0 ${SPARK_W} ${SPARK_H}`}
              preserveAspectRatio="none"
              width="100%"
              height="100%"
              style={{ display: "block", overflow: "visible" }}
              role="img"
              aria-label="p99 latency sparkline, flat around 40 ms then climbing sharply to 86 ms"
            >
              <defs>
                <linearGradient
                  id="observe-v1-spark-fill"
                  x1="0"
                  y1="0"
                  x2="0"
                  y2="1"
                >
                  <stop offset="0%" stopColor={nitro.cP99} stopOpacity={0.22} />
                  <stop offset="100%" stopColor={nitro.cP99} stopOpacity={0} />
                </linearGradient>
              </defs>
              <path d={area} fill="url(#observe-v1-spark-fill)" stroke="none" />
              <path
                d={line}
                fill="none"
                stroke={nitro.cP99}
                strokeWidth={1.5}
                strokeLinecap="round"
                strokeLinejoin="round"
                vectorEffect="non-scaling-stroke"
              />
              {/* Marker on the tail point that kinks up. */}
              <circle
                cx={last[0]}
                cy={last[1]}
                r={2.5}
                fill={nitro.errorText}
                vectorEffect="non-scaling-stroke"
              />
            </svg>
          </div>
        </div>

        {/* Two stat numbers, divided from the chart by a hairline. */}
        <div
          style={{
            display: "flex",
            gap: 24,
            paddingTop: 11,
            borderTop: `1px solid ${nitro.border}`,
          }}
        >
          <StatBlock label="p95" value="42" unit="ms" />
          <StatBlock label="errors" value="0.3" unit="%" tone="error" />
        </div>
      </div>
    </div>
  );
}
