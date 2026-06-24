/**
 * Production-view scene, variant 2 (mini trace waterfall).
 *
 * A small, cropped slice of the Nitro / Banana Cake Pop telemetry app: one
 * request's distributed trace as a compact span waterfall. Four rows
 * (checkout > users-svc > billing gRPC > db) are horizontal bars on a shared
 * 0-94ms axis, each positioned by (start, duration). The slow billing gRPC hop
 * is tinted coral so the bottleneck reads at a glance. Just the spans plus a
 * single thin title row, no flyout, no header tabs, no ruler chrome.
 *
 * Locked, nested durations summing to a 94ms critical path:
 *   checkout   0-94  (root, GraphQL)
 *   users-svc  8-18  (10ms, REST)
 *   billing    18-81 (63ms, the bottleneck, gRPC)
 *   db         81-94 (13ms, SELECT)
 *
 * Self-contained Nitro-palette card: colors come from `nitro` via inline
 * `style={{}}` (not the site cc-* tokens, intentional for these illustrations).
 * Static final frame only: no motion package, no hooks, no animation. The lone
 * inline SVG id is prefixed "observe-v2-".
 */
import type { CSSProperties } from "react";
import { nitro } from "@/src/components/home/act2/variants/nitroTheme";

interface ObserveVariant2Props {
  readonly className?: string;
}

type SpanKind = "GraphQL" | "REST" | "gRPC" | "SELECT";

interface TraceSpan {
  readonly id: string;
  readonly label: string;
  readonly kind: SpanKind;
  readonly start: number;
  readonly end: number;
  readonly depth: number;
  readonly bottleneck: boolean;
}

const TOTAL_MS = 94;

const SPANS: readonly TraceSpan[] = [
  {
    id: "checkout",
    label: "checkout",
    kind: "GraphQL",
    start: 0,
    end: 94,
    depth: 0,
    bottleneck: false,
  },
  {
    id: "users",
    label: "users-svc",
    kind: "REST",
    start: 8,
    end: 18,
    depth: 1,
    bottleneck: false,
  },
  {
    id: "billing",
    label: "billing",
    kind: "gRPC",
    start: 18,
    end: 81,
    depth: 1,
    bottleneck: true,
  },
  {
    id: "db",
    label: "db",
    kind: "SELECT",
    start: 81,
    end: 94,
    depth: 1,
    bottleneck: false,
  },
] as const;

const KIND_COLOR: Record<SpanKind, string> = {
  GraphQL: nitro.active,
  REST: nitro.cThroughput,
  gRPC: nitro.error,
  SELECT: nitro.purple,
};

// Geometry. The track is a percentage axis over [0, TOTAL_MS]; row metrics are
// in px so the card reads at its natural size beside body text.
const LABEL_COL = 96; // px width of the fixed span-name column
const ROW_H = 30; // px per waterfall row
const BAR_H = 10; // px bar thickness
const BAR_TOP = 5; // px offset of the bar within its row

function pct(ms: number): number {
  return (ms / TOTAL_MS) * 100;
}

/** One waterfall row: indented span name, then a bar on the shared axis. */
function SpanRow({ span }: { readonly span: TraceSpan }) {
  const color = KIND_COLOR[span.kind];
  const left = pct(span.start);
  const width = Math.max(0.8, pct(span.end - span.start));
  const duration = span.end - span.start;
  // Near the far right the duration label would overflow, so anchor it inside.
  const labelInside = left + width > 82;

  return (
    <div style={{ position: "relative", height: ROW_H }}>
      {/* Fixed label column: indent by depth, span name. */}
      <div
        style={{
          position: "absolute",
          left: 0,
          top: BAR_TOP - 1,
          width: LABEL_COL,
          display: "flex",
          alignItems: "center",
          gap: 5,
          paddingLeft: span.depth * 10,
          overflow: "hidden",
        }}
      >
        <span
          aria-hidden="true"
          style={{
            width: 5,
            height: 5,
            borderRadius: "50%",
            background: color,
            flexShrink: 0,
          }}
        />
        <span
          style={{
            color: span.bottleneck ? nitro.errorText : nitro.textStrong,
            fontFamily: nitro.font,
            fontSize: 11,
            whiteSpace: "nowrap",
            overflow: "hidden",
            textOverflow: "ellipsis",
            fontWeight: span.bottleneck ? 600 : 400,
          }}
        >
          {span.label}
        </span>
      </div>

      {/* Track: occupies everything right of the label column. */}
      <div
        style={{
          position: "absolute",
          left: LABEL_COL,
          right: 0,
          top: 0,
          bottom: 0,
        }}
      >
        {/* Bar, positioned by start offset and sized by duration. */}
        <div
          style={{
            position: "absolute",
            left: `${left}%`,
            top: BAR_TOP,
            width: `${width}%`,
            height: BAR_H,
            borderRadius: 3,
            background: color,
            opacity: span.bottleneck ? 1 : 0.8,
            boxShadow: span.bottleneck ? `0 0 0 1px ${nitro.error}` : "none",
          }}
        />

        {/* Duration label, just past the bar edge (flipped inside at the right). */}
        <span
          style={{
            position: "absolute",
            top: BAR_TOP + BAR_H / 2,
            left: `${left + width}%`,
            transform: labelInside
              ? "translate(calc(-100% - 6px), -50%)"
              : "translate(6px, -50%)",
            fontFamily: nitro.mono,
            fontSize: 10,
            color: span.bottleneck ? nitro.errorText : nitro.textSecondary,
            whiteSpace: "nowrap",
          }}
        >
          {duration}ms
        </span>
      </div>
    </div>
  );
}

export function ObserveVariant2({ className }: ObserveVariant2Props) {
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
      style={{ width: "100%", maxWidth: 340, marginInline: "auto" }}
    >
      <div style={cardStyle}>
        {/* Single thin title row: trace label left, total on the right. */}
        <div
          style={{
            display: "flex",
            alignItems: "baseline",
            justifyContent: "space-between",
            gap: 10,
            marginBottom: 12,
          }}
        >
          <span
            style={{
              fontSize: 12,
              fontWeight: 600,
              color: nitro.textStrong,
            }}
          >
            Trace
            <span
              style={{
                fontFamily: nitro.mono,
                fontSize: 10,
                color: nitro.textDim,
                marginLeft: 6,
                fontWeight: 400,
              }}
            >
              4b1c8f2a
            </span>
          </span>
          <span
            style={{
              fontFamily: nitro.mono,
              fontSize: 11,
              color: nitro.textSecondary,
              whiteSpace: "nowrap",
            }}
          >
            <span style={{ color: nitro.textStrong, fontWeight: 600 }}>
              94 ms
            </span>
          </span>
        </div>

        {/* Plot: a single dashed end-gridline behind the rows, then the spans. */}
        <div style={{ position: "relative" }}>
          <svg
            viewBox="0 0 100 100"
            preserveAspectRatio="none"
            width="100%"
            height="100%"
            aria-hidden="true"
            style={{
              position: "absolute",
              left: LABEL_COL,
              right: 0,
              top: 0,
              bottom: 0,
              width: `calc(100% - ${LABEL_COL}px)`,
              pointerEvents: "none",
            }}
          >
            <line
              id="observe-v2-end-gridline"
              x1={100}
              y1={0}
              x2={100}
              y2={100}
              stroke={nitro.grid}
              strokeWidth={1}
              strokeDasharray="2 2"
              vectorEffect="non-scaling-stroke"
            />
          </svg>

          <div
            role="img"
            aria-label="Mini distributed-trace waterfall for the checkout request: 4 spans over 94 ms, with the billing gRPC hop (63 ms) tinted coral as the bottleneck."
            style={{ position: "relative" }}
          >
            {SPANS.map((span) => (
              <SpanRow key={span.id} span={span} />
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}
