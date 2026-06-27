interface ObserveVariant2Props {
  readonly className?: string;
}

/**
 * v6 "Production view" hook, variant 2: the span-waterfall bottleneck.
 *
 * Bespoke, one-off illustration (no shared v6 theme): one request's distributed
 * trace as a vertical span waterfall on a shared 0-318 ms axis. A teal GraphQL
 * root span (checkout) spans the full width at the top; four indented child hops
 * hang off a faint tree connector beneath it. Three are muted (a REST call, a
 * background job, a db read); the one WIDE bar that visibly owns the timeline is
 * the `billing.Charge()` gRPC hop, lit coral with a soft halo. Mono per-span
 * timings run down the right edge, with the coral `201 ms` reading loudest.
 *
 * Delivers "See exactly which hop is slow.": the eye lands on the single coral
 * bar eating 201 of 318 ms without reading a chart.
 *
 * cc-* dark palette only; coral encodes the real bottleneck status, teal marks
 * the GraphQL root, everything else stays muted. Static final frame, no motion,
 * no hooks. Every inline SVG id is prefixed "v6-observe-2-".
 */
const TEAL = "#5eead4";
const CORAL = "#f0786a";
const HEADING = "#f5f0ea";
const DIM = "rgba(245,241,234,0.62)";
const MUTED_BAR = "rgba(245,241,234,0.24)";
const MUTED_DOT = "rgba(245,241,234,0.34)";
const FAINT = "rgba(245,241,234,0.12)";
const MONO =
  "ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, 'Liberation Mono', monospace";

interface Span {
  readonly name: string;
  readonly start: number;
  readonly end: number;
  readonly ms: string;
  readonly root: boolean;
  readonly slow: boolean;
}

const TOTAL_MS = 318;

// Locked critical path: 14 + 32 + 201 + 42 + 29 = 318 ms, the billing gRPC hop
// owning 201 of them (63% of the request).
const SPANS: readonly Span[] = [
  {
    name: "checkout",
    start: 0,
    end: 318,
    ms: "318 ms",
    root: true,
    slow: false,
  },
  {
    name: "users.Get()",
    start: 14,
    end: 46,
    ms: "32 ms",
    root: false,
    slow: false,
  },
  {
    name: "billing.Charge()",
    start: 46,
    end: 247,
    ms: "201 ms",
    root: false,
    slow: true,
  },
  {
    name: "inventory.Hold()",
    start: 247,
    end: 289,
    ms: "42 ms",
    root: false,
    slow: false,
  },
  {
    name: "orders.db",
    start: 289,
    end: 318,
    ms: "29 ms",
    root: false,
    slow: false,
  },
];

// Geometry: track [T0, T1] maps the 0..TOTAL_MS axis; rows step by ROW_PITCH.
const T0 = 130;
const T1 = 268;
const ROW_TOP = 20;
const ROW_PITCH = 30;
const BAR_H = 10;

function trackX(ms: number): number {
  return T0 + (ms / TOTAL_MS) * (T1 - T0);
}

export function ObserveVariant2({ className }: ObserveVariant2Props) {
  const lastChildY = ROW_TOP + (SPANS.length - 1) * ROW_PITCH;

  return (
    <div
      className={[
        "border-cc-card-border bg-cc-card-bg/60 mx-auto w-full max-w-[340px] rounded-2xl border p-5 backdrop-blur-sm select-none",
        className ?? "",
      ].join(" ")}
    >
      {/* Header: trace eyebrow on the left, total wall time on the right. */}
      <div className="flex items-baseline justify-between gap-3">
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
          trace &middot; checkout
        </p>
        <p className="font-mono text-[0.62rem]">
          <span className="text-cc-ink-dim">total </span>
          <span className="text-cc-heading font-semibold">318 ms</span>
        </p>
      </div>

      <svg
        className="mt-3 block w-full"
        viewBox="0 0 330 158"
        fill="none"
        role="img"
        aria-label="Distributed-trace waterfall for the checkout request: a GraphQL root span over 318 ms with four nested hops, the billing.Charge() gRPC hop lit coral as the 201 ms bottleneck."
      >
        <defs>
          <filter
            id="v6-observe-2-glow"
            x="-40%"
            y="-80%"
            width="180%"
            height="260%"
          >
            <feGaussianBlur stdDeviation="3.2" />
          </filter>
        </defs>

        {/* Timeline frame: faint start rule and a dashed end-of-trace rule. */}
        <line
          id="v6-observe-2-axis-start"
          x1={T0}
          y1={10}
          x2={T0}
          y2={lastChildY + 12}
          stroke={FAINT}
          strokeWidth={1}
        />
        <line
          id="v6-observe-2-axis-end"
          x1={T1}
          y1={10}
          x2={T1}
          y2={lastChildY + 12}
          stroke={FAINT}
          strokeWidth={1}
          strokeDasharray="2 3"
        />

        {/* Tree connector: the children hang off the root span. */}
        <path
          d={`M10 ${ROW_TOP + 6} V ${lastChildY}`}
          stroke={FAINT}
          strokeWidth={1}
        />
        {SPANS.filter((s) => !s.root).map((_, i) => {
          const y = ROW_TOP + (i + 1) * ROW_PITCH;
          return (
            <line
              key={`v6-observe-2-stub-${i}`}
              x1={10}
              y1={y}
              x2={19}
              y2={y}
              stroke={FAINT}
              strokeWidth={1}
            />
          );
        })}

        {SPANS.map((span, i) => {
          const cy = ROW_TOP + i * ROW_PITCH;
          const x = trackX(span.start);
          const w = Math.max(2, trackX(span.end) - x);
          const barColor = span.root ? TEAL : span.slow ? CORAL : MUTED_BAR;
          const dotColor = span.root ? TEAL : span.slow ? CORAL : MUTED_DOT;
          const labelColor = span.root ? HEADING : span.slow ? CORAL : DIM;
          const timeColor = span.root ? HEADING : span.slow ? CORAL : DIM;

          return (
            <g key={span.name}>
              {/* Span dot, indented one level for child hops. */}
              <circle
                cx={span.root ? 10 : 22}
                cy={cy}
                r={span.root ? 3 : 2.6}
                fill={dotColor}
              />

              {/* Span name. */}
              <text
                x={span.root ? 20 : 32}
                y={cy}
                dy="0.32em"
                fontFamily={MONO}
                fontSize={span.root ? 10.5 : 9.5}
                fontWeight={span.slow || span.root ? 600 : 400}
                fill={labelColor}
              >
                {span.name}
              </text>

              {/* Coral halo behind the bottleneck bar. */}
              {span.slow && (
                <rect
                  x={x - 4}
                  y={cy - 9}
                  width={w + 8}
                  height={18}
                  rx={6}
                  fill={CORAL}
                  opacity={0.28}
                  filter="url(#v6-observe-2-glow)"
                />
              )}

              {/* The bar, positioned by start and sized by duration. */}
              <rect
                x={x}
                y={cy - BAR_H / 2}
                width={w}
                height={BAR_H}
                rx={3}
                fill={barColor}
                opacity={span.slow ? 1 : span.root ? 0.85 : 1}
                stroke={span.slow ? CORAL : "none"}
                strokeWidth={span.slow ? 1 : 0}
              />

              {/* Per-span timing, right-aligned down the edge. */}
              <text
                x={322}
                y={cy}
                dy="0.32em"
                textAnchor="end"
                fontFamily={MONO}
                fontSize={10}
                fontWeight={span.slow || span.root ? 600 : 400}
                fill={timeColor}
              >
                {span.ms}
              </text>
            </g>
          );
        })}
      </svg>

      {/* Punchline: tie the coral bar to the gRPC hop and its share of the time. */}
      <p className="text-cc-ink-dim mt-3 text-center font-mono text-[0.72rem] tracking-[0.02em]">
        slowest hop{" "}
        <span aria-hidden="true" className="text-cc-ink-faint">
          &rarr;
        </span>{" "}
        <span className="text-cc-status-firing">
          gRPC billing.Charge() 201 ms
        </span>
      </p>
    </div>
  );
}
