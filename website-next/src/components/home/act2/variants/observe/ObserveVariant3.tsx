import type { CSSProperties } from "react";
import {
  nitro,
  NITRO_IMPACT_STOPS,
} from "@/src/components/home/act2/variants/nitroTheme";

/**
 * Observe scene, variant 3 — "impact mini-table".
 *
 * A small, static slice of the Nitro Insights panel: the four most-impactful
 * GraphQL operations of an EShops storefront, ranked top-down by impact. Each
 * row carries the operation name, a tiny impact bar tinted along the teal→amber
 * impact ramp, and a single status dot reading the dominant response class for
 * that operation (2xx healthy, 4xx client error, 5xx server error). The
 * checkout mutation sits at #1 and is the only 5xx row, so the eye lands there.
 *
 * Cropped to just the ranked list under one thin title row, no table chrome, no
 * tabs, no toolbar. This is the settled final frame: no motion package, no
 * hooks, no "use client". Color is rationed as data on the Nitro palette: the
 * impact bars carry the gradient, the status dots carry intent, everything else
 * is muted Nitro text on the `nitro.surface` card inside a `nitro.border`
 * hairline with `nitro.radius` corners, so it reads as a real product slice.
 *
 * Self-contained for bare gallery rendering: all helpers are local and every
 * inline SVG id is prefixed "observe-v3-".
 */

interface ObserveVariant3Props {
  readonly className?: string;
}

/** Dominant response class for an operation, drives the status dot color. */
type StatusClass = "2xx" | "4xx" | "5xx";

interface ImpactRow {
  readonly rank: number;
  readonly operation: string;
  /** 0..100, drives bar width and gradient tint. */
  readonly impact: number;
  readonly status: StatusClass;
}

// Locked EShops sample: checkout #1 is the only failing (5xx) operation, the
// cart update is degraded (4xx), the rest are healthy (2xx). Impact descends.
const ROWS: readonly ImpactRow[] = [
  { rank: 1, operation: "checkout", impact: 92, status: "5xx" },
  { rank: 2, operation: "updateCart", impact: 64, status: "4xx" },
  { rank: 3, operation: "productPage", impact: 41, status: "2xx" },
  { rank: 4, operation: "searchCatalog", impact: 27, status: "2xx" },
];

const STATUS_COLOR: Record<StatusClass, string> = {
  "2xx": nitro.successText,
  "4xx": nitro.warning,
  "5xx": nitro.errorText,
};

/** Parse a `#rrggbb` hex into an [r, g, b] triple. */
function hexToRgb(value: string): readonly [number, number, number] {
  const v = value.replace("#", "");
  return [
    parseInt(v.slice(0, 2), 16),
    parseInt(v.slice(2, 4), 16),
    parseInt(v.slice(4, 6), 16),
  ];
}

/** Interpolate the teal→lime→amber impact ramp at `t` in 0..1. */
function impactColor(t: number): string {
  const stops = NITRO_IMPACT_STOPS;
  const x = Math.min(Math.max(t, 0), 1) * (stops.length - 1);
  const i = Math.min(Math.floor(x), stops.length - 2);
  const f = x - i;
  const a = hexToRgb(stops[i]);
  const b = hexToRgb(stops[i + 1]);
  const mix = (m: number, n: number) => Math.round(m + (n - m) * f);
  return `rgb(${mix(a[0], b[0])}, ${mix(a[1], b[1])}, ${mix(a[2], b[2])})`;
}

export function ObserveVariant3({ className }: ObserveVariant3Props) {
  const mono: CSSProperties = {
    fontFamily: nitro.mono,
    fontVariantNumeric: "tabular-nums",
  };

  return (
    <div
      className={className}
      style={{ width: "100%", maxWidth: 320, marginInline: "auto" }}
    >
      <div
        role="img"
        aria-label="Nitro impact ranking of four EShops operations. Ranked by impact: checkout 92 with a 5xx server error status, updateCart 64 with a 4xx client error status, productPage 41 healthy 2xx, and searchCatalog 27 healthy 2xx."
        style={{
          border: `1px solid ${nitro.border}`,
          borderRadius: nitro.radius,
          background: nitro.surface,
          overflow: "hidden",
          boxShadow: "0 1px 3px rgba(2, 6, 16, 0.6)",
        }}
      >
        {/* Single thin title row: panel label + scope hint, no tabs. */}
        <div
          style={{
            display: "flex",
            alignItems: "baseline",
            justifyContent: "space-between",
            padding: "9px 12px",
            borderBottom: `1px solid ${nitro.border}`,
          }}
        >
          <span
            style={{
              fontFamily: nitro.font,
              fontSize: 11.5,
              fontWeight: 600,
              color: nitro.textStrong,
            }}
          >
            Top operations by impact
          </span>
          <span style={{ ...mono, fontSize: 10, color: nitro.textDim }}>
            24h
          </span>
        </div>

        {/* Ranked rows: rank · name · impact bar · status dot. */}
        <div style={{ padding: "6px 0" }}>
          {ROWS.map((row) => {
            const fill = impactColor(row.impact / 100);
            const dot = STATUS_COLOR[row.status];
            return (
              <div
                key={row.operation}
                style={{
                  display: "grid",
                  gridTemplateColumns: "14px 1fr 84px 14px",
                  alignItems: "center",
                  gap: 9,
                  padding: "7px 12px",
                }}
              >
                {/* Rank index. */}
                <span
                  style={{
                    ...mono,
                    fontSize: 10.5,
                    color: nitro.textDim,
                    textAlign: "right",
                  }}
                >
                  {row.rank}
                </span>

                {/* Operation name; the #1 checkout row reads strongest. */}
                <span
                  style={{
                    fontFamily: nitro.mono,
                    fontSize: 12,
                    color: row.rank === 1 ? nitro.textStrong : nitro.text,
                    fontWeight: row.rank === 1 ? 600 : 400,
                    overflow: "hidden",
                    textOverflow: "ellipsis",
                    whiteSpace: "nowrap",
                  }}
                >
                  {row.operation}
                </span>

                {/* Tiny impact bar: track + gradient-tinted fill. */}
                <span
                  aria-hidden="true"
                  style={{
                    position: "relative",
                    height: 6,
                    width: "100%",
                    background: nitro.grid,
                    borderRadius: 999,
                    overflow: "hidden",
                  }}
                >
                  <span
                    style={{
                      position: "absolute",
                      inset: 0,
                      width: `${row.impact}%`,
                      background: fill,
                      borderRadius: 999,
                    }}
                  />
                </span>

                {/* Status dot for the dominant response class. */}
                <svg
                  width={14}
                  height={14}
                  viewBox="0 0 14 14"
                  role="img"
                  aria-label={`${row.status} status`}
                  style={{ display: "block" }}
                >
                  <circle
                    cx={7}
                    cy={7}
                    r={6}
                    fill={`${dot}26`}
                    stroke={dot}
                    strokeWidth={1}
                  />
                  <circle cx={7} cy={7} r={2.4} fill={dot} />
                </svg>
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
}
