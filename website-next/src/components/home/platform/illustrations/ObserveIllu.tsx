interface ObserveIlluProps {
  readonly className?: string;
}

/**
 * Section-grade "production view" illustration: one checkout request rendered as
 * a distributed-trace span waterfall on a shared 0-318 ms axis.
 *
 * A teal GraphQL root span (checkout) spans the full timeline; four indented
 * child hops hang off a faint tree connector beneath it. Three stay muted (a REST
 * call, a background job, a db read); the one WIDE bar that visibly owns the
 * timeline is the billing gRPC hop, lit coral with a soft halo. Mono per-hop
 * timings run down the right edge, with the coral 201 ms reading loudest.
 *
 * Delivers "see exactly which hop is slow": the eye lands on the single coral bar
 * eating 201 of 318 ms without reading a chart. cc-* dark palette only, coral
 * encodes the real bottleneck status, teal marks the GraphQL root, everything
 * else stays muted. Static, no motion. Every inline SVG id is prefixed
 * "illu-observe-".
 */
const CREAM = "#f5f0ea";
const LABEL = "rgba(245,241,234,0.70)";
const DIM = "rgba(245,241,234,0.62)";
const EYEBROW = "#62748e";
const BORDER = "rgba(245,241,234,0.12)";
const FAINT = "rgba(245,241,234,0.10)";
const TEAL = "#5eead4";
const CORAL = "#f0786a";
const MUTED_BAR = "rgba(245,241,234,0.20)";
const MUTED_DOT = "rgba(245,241,234,0.34)";
const MONO =
  "ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, 'Liberation Mono', monospace";
const HEAD = "'Josefin Sans', Futura, sans-serif";

type SpanKind = "root" | "slow" | "muted";

interface Span {
  readonly name: string;
  readonly tag: string;
  readonly tagX: number;
  readonly start: number;
  readonly end: number;
  readonly ms: string | null;
  readonly kind: SpanKind;
}

const TOTAL_MS = 318;

// Locked critical path: 14 + 32 + 201 + 42 + 29 = 318 ms, the billing gRPC hop
// owning 201 of them (63% of the request). The root timing lives in the header,
// so the root row omits a right-edge value.
const SPANS: readonly Span[] = [
  {
    name: "checkout",
    tag: "GraphQL",
    tagX: 102,
    start: 0,
    end: 318,
    ms: null,
    kind: "root",
  },
  {
    name: "users-svc",
    tag: "REST",
    tagX: 116,
    start: 14,
    end: 46,
    ms: "32 ms",
    kind: "muted",
  },
  {
    name: "billing",
    tag: "gRPC",
    tagX: 104,
    start: 46,
    end: 247,
    ms: "201 ms",
    kind: "slow",
  },
  {
    name: "worker",
    tag: "job",
    tagX: 97,
    start: 247,
    end: 289,
    ms: "42 ms",
    kind: "muted",
  },
  {
    name: "db",
    tag: "SQL",
    tagX: 72,
    start: 289,
    end: 318,
    ms: "29 ms",
    kind: "muted",
  },
];

// Geometry: track [T0, T1] maps the 0..TOTAL_MS axis; rows step by ROW_PITCH.
const T0 = 160;
const T1 = 354;
const ROW_TOP = 92;
const ROW_PITCH = 33;
const BAR_H = 11;
const AXIS_TOP = 80;
const AXIS_BOTTOM = 236;
const RIGHT_EDGE = 420;

function trackX(ms: number): number {
  return T0 + (ms / TOTAL_MS) * (T1 - T0);
}

function barFill(kind: SpanKind): string {
  if (kind === "root") {
    return "url(#illu-observe-teal)";
  }
  if (kind === "slow") {
    return "url(#illu-observe-coral)";
  }
  return MUTED_BAR;
}

function nameFill(kind: SpanKind): string {
  if (kind === "root") {
    return CREAM;
  }
  if (kind === "slow") {
    return CORAL;
  }
  return LABEL;
}

export function ObserveIllu({ className }: ObserveIlluProps) {
  const lastChildY = ROW_TOP + (SPANS.length - 1) * ROW_PITCH;

  return (
    <svg
      className={["mx-auto w-full", className ?? ""].join(" ")}
      viewBox="0 0 448 272"
      fill="none"
      aria-hidden="true"
    >
      <defs>
        <linearGradient
          id="illu-observe-card"
          x1="0"
          y1="0"
          x2="0"
          y2="272"
          gradientUnits="userSpaceOnUse"
        >
          <stop offset="0" stopColor="#0d1526" />
          <stop offset="1" stopColor="#0a0f1b" />
        </linearGradient>
        <linearGradient
          id="illu-observe-teal"
          x1={T0}
          y1="0"
          x2={T1}
          y2="0"
          gradientUnits="userSpaceOnUse"
        >
          <stop offset="0" stopColor="#5eead4" />
          <stop offset="1" stopColor="#2dd4bf" />
        </linearGradient>
        <linearGradient id="illu-observe-coral" x1="0" y1="0" x2="1" y2="0">
          <stop offset="0" stopColor="#f0786a" />
          <stop offset="1" stopColor="#f8978a" />
        </linearGradient>
        <filter
          id="illu-observe-glow"
          x="-30%"
          y="-120%"
          width="160%"
          height="340%"
        >
          <feGaussianBlur stdDeviation="4" />
        </filter>
      </defs>

      {/* Card surface. */}
      <rect
        x="1"
        y="1"
        width="446"
        height="270"
        rx="18"
        fill="url(#illu-observe-card)"
        stroke={BORDER}
        strokeWidth="1"
      />

      {/* Header: trace eyebrow on the left, total wall time as the headline stat. */}
      <text
        x="28"
        y="34"
        fontFamily={MONO}
        fontSize="10.5"
        letterSpacing="1.6"
        fill={EYEBROW}
      >
        trace &middot; checkout
      </text>
      <text
        x={RIGHT_EDGE}
        y="26"
        textAnchor="end"
        fontFamily={MONO}
        fontSize="7"
        letterSpacing="1.4"
        fill={EYEBROW}
      >
        TOTAL WALL TIME
      </text>
      <text
        x={RIGHT_EDGE}
        y="49"
        textAnchor="end"
        fontFamily={HEAD}
        fontSize="22"
        fontWeight="600"
        fill={CREAM}
      >
        318 ms
      </text>

      {/* Header rule. */}
      <line x1="28" y1="64" x2={RIGHT_EDGE} y2="64" stroke={BORDER} />

      {/* Axis scale: the shared 0..318 ms timeline. */}
      <text
        x={T0}
        y="76"
        fontFamily={MONO}
        fontSize="7.5"
        fill={EYEBROW}
        letterSpacing="0.5"
      >
        0 ms
      </text>
      <text
        x={T1}
        y="76"
        textAnchor="end"
        fontFamily={MONO}
        fontSize="7.5"
        fill={EYEBROW}
        letterSpacing="0.5"
      >
        318 ms
      </text>

      {/* Timeline frame: solid start rule, dashed end-of-trace rule. */}
      <line
        x1={T0}
        y1={AXIS_TOP}
        x2={T0}
        y2={AXIS_BOTTOM}
        stroke={FAINT}
        strokeWidth="1"
      />
      <line
        x1={T1}
        y1={AXIS_TOP}
        x2={T1}
        y2={AXIS_BOTTOM}
        stroke={FAINT}
        strokeWidth="1"
        strokeDasharray="2 3"
      />

      {/* Tree connector: the child hops hang off the root span. */}
      <path
        d={`M30 ${ROW_TOP + 8} V ${lastChildY}`}
        stroke={FAINT}
        strokeWidth="1"
      />
      {SPANS.filter((s) => s.kind !== "root").map((s, i) => {
        const y = ROW_TOP + (i + 1) * ROW_PITCH;
        return (
          <line
            key={`illu-observe-stub-${s.name}`}
            x1="30"
            y1={y}
            x2="44"
            y2={y}
            stroke={FAINT}
            strokeWidth="1"
          />
        );
      })}

      {SPANS.map((span, i) => {
        const cy = ROW_TOP + i * ROW_PITCH;
        const x = trackX(span.start);
        const w = Math.max(3, trackX(span.end) - x);
        const isRoot = span.kind === "root";
        const isSlow = span.kind === "slow";
        const dotColor = isRoot ? TEAL : isSlow ? CORAL : MUTED_DOT;

        return (
          <g key={span.name}>
            {/* Span dot, indented one level for child hops. */}
            <circle
              cx={isRoot ? 30 : 44}
              cy={cy}
              r={isRoot ? 3.6 : 2.8}
              fill={dotColor}
            />

            {/* Span name. */}
            <text
              x={isRoot ? 40 : 52}
              y={cy}
              dy="0.32em"
              fontFamily={MONO}
              fontSize={isRoot ? 11.5 : 10.5}
              fontWeight={isRoot || isSlow ? 600 : 400}
              fill={nameFill(span.kind)}
            >
              {span.name}
            </text>

            {/* Transport tag. */}
            <text
              x={span.tagX}
              y={cy}
              dy="0.32em"
              fontFamily={MONO}
              fontSize="8.5"
              fill={EYEBROW}
            >
              {span.tag}
            </text>

            {/* Coral halo behind the bottleneck bar. */}
            {isSlow && (
              <rect
                x={x - 5}
                y={cy - 10}
                width={w + 10}
                height={20}
                rx={7}
                fill={CORAL}
                opacity={0.32}
                filter="url(#illu-observe-glow)"
              />
            )}

            {/* The bar, positioned by start and sized by duration. */}
            <rect
              x={x}
              y={cy - BAR_H / 2}
              width={w}
              height={BAR_H}
              rx={3}
              fill={barFill(span.kind)}
              opacity={isRoot ? 0.92 : 1}
              stroke={isSlow ? CORAL : "none"}
              strokeWidth={isSlow ? 1 : 0}
            />

            {/* Per-hop timing, right-aligned down the edge. */}
            {span.ms !== null && (
              <text
                x={RIGHT_EDGE}
                y={cy}
                dy="0.32em"
                textAnchor="end"
                fontFamily={MONO}
                fontSize={isSlow ? 12.5 : 10.5}
                fontWeight={isSlow ? 600 : 400}
                fill={isSlow ? CORAL : DIM}
              >
                {span.ms}
              </text>
            )}
          </g>
        );
      })}

      {/* Punchline rule and caption. */}
      <line
        x1="28"
        y1="248"
        x2={RIGHT_EDGE}
        y2="248"
        stroke={BORDER}
        strokeDasharray="2 3"
      />
      <text
        x="224"
        y="263"
        textAnchor="middle"
        fontFamily={MONO}
        fontSize="10.5"
        fill={DIM}
      >
        slowest hop <tspan fill={EYEBROW}>&rarr;</tspan>{" "}
        <tspan fill={CORAL} fontWeight="600">
          billing (gRPC)
        </tspan>{" "}
        <tspan fill={CORAL} fontWeight="600">
          201 ms
        </tspan>{" "}
        of 318 ms
      </text>
    </svg>
  );
}
