/**
 * "Production view" scene, concept 2 ("Distributed trace waterfall"),
 * v3 "Signal & Metrics" (dark cc-* panel).
 *
 * Re-expresses the v2 nitro trace 4b1c8f2a waterfall in v3's locked metrics
 * strategy with layout D: a signal strip on top, a cream stat-duo footer under a
 * solid divider. The signal strip is the trace itself - one checkout request as
 * a compact span waterfall on a shared 0-94ms axis (checkout GraphQL root over
 * users-svc REST, billing gRPC, db SELECT), each span a thin bar positioned by
 * start and sized by duration. The trace bars are the single teal signal (the
 * ScrollScenes ServiceTopology read); the slow billing gRPC hop is the one
 * genuine status element, recolored coral with a BOTTLENECK tag, since 63 of the
 * 94ms path lives in that one downstream call. The stat-duo footer makes the
 * measurable result the headline: two cream font-heading figures, 94 ms total
 * and 63 ms in billing. A dashed-divider caption reads the interpretation.
 *
 * Content is faithful to the v2 ObserveVariant2: trace 4b1c8f2a, total 94ms;
 * checkout 0-94 (root, GraphQL), users-svc 8-18 (10ms, REST), billing 18-81
 * (63ms, gRPC, the bottleneck), db 81-94 (13ms, SELECT). Only the visual
 * language changes to the v3 dark metrics panel.
 *
 * Static settled frame: no animation, no motion, no hooks, no "use client".
 * Server component, aria-hidden root. Every inline SVG id is prefixed
 * "v3-observe-2-". Local cc palette mirrors the cc-* tokens exactly.
 */
import type { CSSProperties } from "react";

interface ObserveVariant2Props {
  readonly className?: string;
}

/* v3 "Signal & Metrics" strict cc-* dark palette. Teal is the only decorative
 * hue and is bound to the trace signal (the span bars); coral is rationed to the
 * single slow billing hop, the one genuine status element. */
const cc = {
  surface: "#0c1322",
  cardBg: "rgba(12,19,34,0.55)",
  cardBorder: "rgba(245,241,234,0.12)",
  inkFaint: "rgba(245,241,234,0.16)",
  heading: "#f5f0ea",
  ink: "#a1a3af",
  inkDim: "rgba(245,241,234,0.62)",
  navLabel: "#62748e",
  accent: "#5eead4",
  coral: "#f0786a",
} as const;

const MONO =
  'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace';
const HEADING = '"Josefin Sans", Futura, sans-serif';

const TOTAL_MS = 94;

interface TraceSpan {
  readonly id: string;
  readonly label: string;
  readonly start: number;
  readonly end: number;
  readonly depth: number;
  /** The single slow hop on the critical path (the one status element). */
  readonly slow: boolean;
}

/* Locked nested durations summing to the 94ms critical path. */
const SPANS: readonly TraceSpan[] = [
  {
    id: "checkout",
    label: "checkout",
    start: 0,
    end: 94,
    depth: 0,
    slow: false,
  },
  { id: "users", label: "users-svc", start: 8, end: 18, depth: 1, slow: false },
  { id: "billing", label: "billing", start: 18, end: 81, depth: 1, slow: true },
  { id: "db", label: "db", start: 81, end: 94, depth: 1, slow: false },
] as const;

export function ObserveVariant2({ className }: ObserveVariant2Props) {
  const idp = "v3-observe-2-";

  // Waterfall geometry, in px within the SVG viewBox.
  const W = 280;
  const labelCol = 62; // px reserved for the indented span name
  const durCol = 30; // px right gutter for the right-aligned self-time
  const trackLeft = labelCol;
  const trackRight = W - durCol;
  const trackW = trackRight - trackLeft;
  const rowH = 20;
  const barH = 7;
  const topPad = 4;

  const x = (ms: number): number => trackLeft + (ms / TOTAL_MS) * trackW;
  const H = topPad + SPANS.length * rowH + 4;

  const wrapperStyle: CSSProperties = {
    background: cc.cardBg,
    border: `1px solid ${cc.cardBorder}`,
    borderRadius: 16,
    padding: 20,
    backdropFilter: "blur(4px)",
    WebkitBackdropFilter: "blur(4px)",
    boxSizing: "border-box",
  };

  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      aria-hidden="true"
    >
      <div style={wrapperStyle}>
        {/* eyebrow row: trace id left, the one status tag (coral) right */}
        <div
          style={{
            display: "flex",
            alignItems: "center",
            justifyContent: "space-between",
            gap: 10,
          }}
        >
          <p
            style={{
              margin: 0,
              fontFamily: MONO,
              fontSize: "0.58rem",
              letterSpacing: "0.15em",
              textTransform: "uppercase",
              color: cc.navLabel,
            }}
          >
            trace 4b1c8f2a
          </p>
          <span
            style={{
              display: "inline-flex",
              alignItems: "center",
              gap: 5,
              fontFamily: MONO,
              fontSize: "0.5rem",
              letterSpacing: "0.06em",
              textTransform: "uppercase",
              color: cc.coral,
              border: `1px solid ${cc.coral}59`,
              borderRadius: 999,
              padding: "2px 7px",
            }}
          >
            <span
              style={{
                width: 5,
                height: 5,
                borderRadius: "50%",
                background: cc.coral,
              }}
            />
            bottleneck
          </span>
        </div>

        {/* signal strip: the span waterfall on a shared 0-94ms axis */}
        <div style={{ marginTop: 14 }}>
          <svg
            viewBox={`0 0 ${W} ${H}`}
            width="100%"
            role="img"
            aria-label="Distributed trace waterfall for one checkout request over 94ms: a checkout GraphQL root spanning users-svc REST 10ms, billing gRPC 63ms flagged as the bottleneck, and db SELECT 13ms; most of the time is in the billing hop."
            style={{ display: "block", overflow: "visible" }}
          >
            {/* faint dashed axis line at the 94ms end of the track */}
            <line
              id={`${idp}axis-end`}
              x1={trackRight}
              y1={topPad}
              x2={trackRight}
              y2={H - 4}
              stroke={cc.inkFaint}
              strokeWidth={1}
              strokeDasharray="2 3"
            />

            {SPANS.map((span, i) => {
              const ry = topPad + i * rowH;
              const midY = ry + rowH / 2;
              const bx = x(span.start);
              const bw = Math.max(2, x(span.end) - x(span.start));
              const dur = span.end - span.start;
              const barColor = span.slow ? cc.coral : cc.accent;
              const barOpacity = span.slow ? 0.9 : 0.62;

              return (
                <g key={`${idp}span-${span.id}`}>
                  {/* indented span name; the slow hop reads strongest */}
                  <text
                    x={span.depth * 9}
                    y={midY + 3}
                    fill={span.slow ? cc.heading : cc.ink}
                    style={{
                      fontFamily: MONO,
                      fontSize: "0.6rem",
                      fontWeight: span.slow ? 600 : 400,
                    }}
                  >
                    {span.label}
                  </text>

                  {/* track well behind the bar so short spans keep a baseline */}
                  <rect
                    x={trackLeft}
                    y={midY - barH / 2}
                    width={trackW}
                    height={barH}
                    rx={barH / 2}
                    fill={cc.surface}
                  />

                  {/* the span bar, positioned by start and sized by duration */}
                  <rect
                    x={bx}
                    y={midY - barH / 2}
                    width={bw}
                    height={barH}
                    rx={barH / 2}
                    fill={barColor}
                    opacity={barOpacity}
                  />

                  {/* per-span self-time, right-aligned in its own gutter */}
                  <text
                    x={W}
                    y={midY + 3}
                    textAnchor="end"
                    fill={span.slow ? cc.coral : cc.navLabel}
                    style={{
                      fontFamily: MONO,
                      fontSize: "0.55rem",
                      fontWeight: span.slow ? 600 : 400,
                      fontVariantNumeric: "tabular-nums",
                    }}
                  >
                    {dur}ms
                  </text>
                </g>
              );
            })}
          </svg>
        </div>

        {/* stat-duo footer: the measurable result, two cream figures */}
        <div
          style={{
            marginTop: 14,
            paddingTop: 14,
            borderTop: `1px solid ${cc.cardBorder}`,
            display: "grid",
            gridTemplateColumns: "1fr 1fr",
            columnGap: 16,
          }}
        >
          <Stat figure="94" unit="ms" caption="total path" />
          <Stat figure="63" unit="ms" caption="in billing" />
        </div>

        {/* interpretation caption under a dashed faint divider */}
        <div
          style={{
            marginTop: 14,
            paddingTop: 12,
            borderTop: `1px dashed ${cc.inkFaint}`,
          }}
        >
          <p
            style={{
              margin: 0,
              fontFamily: MONO,
              fontSize: "0.62rem",
              color: cc.inkDim,
              lineHeight: 1.4,
            }}
          >
            67% of the path is one downstream call
          </p>
        </div>
      </div>
    </div>
  );
}

function Stat({
  figure,
  unit,
  caption,
}: {
  readonly figure: string;
  readonly unit: string;
  readonly caption: string;
}) {
  return (
    <div>
      <p
        style={{
          margin: 0,
          fontFamily: HEADING,
          fontWeight: 600,
          fontSize: "1.5rem",
          lineHeight: 1,
          color: cc.heading,
          fontVariantNumeric: "tabular-nums",
        }}
      >
        {figure}
        <span
          style={{ fontFamily: MONO, fontSize: "0.9rem", color: cc.navLabel }}
        >
          {" "}
          {unit}
        </span>
      </p>
      <p
        style={{
          margin: "8px 0 0",
          fontFamily: MONO,
          fontSize: "0.7rem",
          color: cc.inkDim,
          textTransform: "lowercase",
        }}
      >
        {caption}
      </p>
    </div>
  );
}
