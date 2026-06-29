import { RevealOnScroll } from "@/src/components/RevealOnScroll";

/**
 * Observability landing take V3: a symptom-to-cause path, all visible at once.
 *
 * The left tile is the symptom: a `checkout` p99 sparkline holding a calm teal
 * baseline, then kinking up across a dashed SLO line into a coral breach at
 * 318 ms, flagged with an amber Investigating pill. A teal "same trace"
 * connector leads to the right tile, the cause: a span waterfall whose
 * `billing.Charge` gRPC hop is lit coral as the one degrading span, carrying its
 * +180 ms cost. The picture moves from the spike to the exact hop in the same
 * trace, so debugging starts from evidence.
 *
 * Static server component (no hooks, no client APIs): the sparkline geometry is
 * computed once at import. Teal is the calm accent; amber and coral are rationed
 * to encode real status. Every svg id is prefixed "v3-observe-".
 */

const ID = "v3-observe-";

const MONO =
  'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace';

// Palette echoing the cc-* tokens, for inline SVG fills and strokes.
const C = {
  page: "#0b0f1a",
  accent: "#5eead4",
  coral: "#f0786a",
  slo: "rgba(245, 241, 234, 0.30)",
  navLabel: "#62748e",
} as const;

// p99 latency sample (ms): a flat baseline near 155 ms that bends sharply up and
// breaches the 250 ms SLO, peaking at 318 ms (the value on the tile).
const SERIES: readonly number[] = [
  152, 158, 150, 161, 155, 163, 157, 168, 162, 178, 205, 248, 292, 318,
];

// Fixed value domain so the SLO line sits at a stable height regardless of data.
const DOMAIN_MIN = 120;
const DOMAIN_MAX = 340;
const SLO_VALUE = 250;

// Plot rectangle inside the 280 x 84 viewBox.
const PLOT = { left: 10, right: 270, top: 8, bottom: 74 } as const;

type Point = readonly [number, number];

/** Builds the split sparkline geometry: calm baseline below the SLO, coral tail above. */
function buildSloSparkline() {
  const n = SERIES.length;
  const round = (v: number) => Math.round(v * 10) / 10;
  const xOf = (i: number) =>
    PLOT.left + (i / (n - 1)) * (PLOT.right - PLOT.left);
  const yOf = (v: number) =>
    PLOT.bottom -
    ((v - DOMAIN_MIN) / (DOMAIN_MAX - DOMAIN_MIN)) * (PLOT.bottom - PLOT.top);

  const pts: Point[] = SERIES.map((v, i) => [xOf(i), yOf(v)]);
  const sloY = yOf(SLO_VALUE);

  // Single upward crossing: split the curve where it passes the SLO value.
  const crossIndex = SERIES.findIndex((v) => v > SLO_VALUE);
  const prev = crossIndex - 1;
  const t = (SLO_VALUE - SERIES[prev]) / (SERIES[crossIndex] - SERIES[prev]);
  const crossX = xOf(prev) + t * (xOf(crossIndex) - xOf(prev));
  const cross: Point = [crossX, sloY];

  const below: Point[] = [...pts.slice(0, crossIndex), cross];
  const above: Point[] = [cross, ...pts.slice(crossIndex)];

  const toLine = (p: readonly Point[]) =>
    p
      .map(([x, y], i) => `${i === 0 ? "M" : "L"}${round(x)} ${round(y)}`)
      .join(" ");
  const toArea = (p: readonly Point[]) =>
    `${toLine(p)} L${round(p[p.length - 1][0])} ${PLOT.bottom} L${round(p[0][0])} ${PLOT.bottom} Z`;

  return {
    belowLine: toLine(below),
    aboveLine: toLine(above),
    belowArea: toArea(below),
    aboveArea: toArea(above),
    sloY: round(sloY),
    cross: [round(cross[0]), round(cross[1])] as const,
    last: [round(pts[n - 1][0]), round(pts[n - 1][1])] as const,
  };
}

const SPARK = buildSloSparkline();

interface Span {
  readonly name: string;
  /** Bar offset and width as a percentage of the request timeline. */
  readonly left: number;
  readonly width: number;
  readonly root?: boolean;
  /** The one degrading hop, lit coral and carrying its cost. */
  readonly cause?: boolean;
  readonly delta?: string;
}

// The checkout span waterfall: the billing gRPC hop runs +180 ms and is the cause.
const SPANS: readonly Span[] = [
  { name: "checkout", left: 0, width: 100, root: true },
  { name: "users-svc", left: 4, width: 11 },
  { name: "catalog", left: 15, width: 13 },
  {
    name: "billing.Charge",
    left: 29,
    width: 57,
    cause: true,
    delta: "+180 ms",
  },
  { name: "orders", left: 88, width: 7 },
];

/** Symptom tile: the p99 sparkline breaching its SLO line. */
function SymptomCard() {
  return (
    <div className="border-cc-card-border bg-cc-card-bg/60 flex-1 rounded-2xl border p-5 backdrop-blur-sm">
      <div className="flex items-center justify-between gap-3">
        <span className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.18em] uppercase">
          Symptom
        </span>
        <span className="border-cc-status-investigating/40 text-cc-status-investigating bg-cc-status-investigating/10 inline-flex shrink-0 items-center gap-1.5 rounded-full border px-2.5 py-0.5 font-mono text-[0.6rem] font-medium whitespace-nowrap">
          <span className="bg-cc-status-investigating size-1.5 rounded-full" />
          Investigating
        </span>
      </div>

      <div className="mt-3 flex items-center gap-2">
        <span className="text-cc-heading font-mono text-sm font-semibold">
          checkout
        </span>
        <span className="border-cc-card-border text-cc-nav-label rounded border px-1.5 py-0.5 font-mono text-[0.6rem] tracking-[0.04em]">
          query
        </span>
      </div>

      <div className="mt-4 flex items-baseline justify-between gap-3">
        <span className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.12em] uppercase">
          p99 latency
        </span>
        <span className="text-cc-status-firing font-mono text-base font-semibold tabular-nums">
          318 ms
        </span>
      </div>

      <div className="mt-2.5">
        <svg
          viewBox="0 0 280 84"
          width="100%"
          role="img"
          aria-label="p99 latency holds near 155 ms, then climbs sharply across the 250 ms SLO line to 318 ms in breach"
          style={{ display: "block", overflow: "visible" }}
        >
          <defs>
            <linearGradient
              id={`${ID}calm-fill`}
              x1="0"
              y1={PLOT.top}
              x2="0"
              y2={PLOT.bottom}
              gradientUnits="userSpaceOnUse"
            >
              <stop offset="0" stopColor={C.accent} stopOpacity="0.14" />
              <stop offset="1" stopColor={C.accent} stopOpacity="0" />
            </linearGradient>
            <linearGradient
              id={`${ID}breach-fill`}
              x1="0"
              y1={PLOT.top}
              x2="0"
              y2={PLOT.bottom}
              gradientUnits="userSpaceOnUse"
            >
              <stop offset="0" stopColor={C.coral} stopOpacity="0.26" />
              <stop offset="1" stopColor={C.coral} stopOpacity="0" />
            </linearGradient>
          </defs>

          {/* Area washes under the calm and breaching parts of the curve. */}
          <path d={SPARK.belowArea} fill={`url(#${ID}calm-fill)`} />
          <path d={SPARK.aboveArea} fill={`url(#${ID}breach-fill)`} />

          {/* Dashed SLO threshold line + its label. */}
          <line
            x1={PLOT.left - 2}
            y1={SPARK.sloY}
            x2={PLOT.right + 2}
            y2={SPARK.sloY}
            stroke={C.slo}
            strokeWidth="1"
            strokeDasharray="3 3"
            vectorEffect="non-scaling-stroke"
          />
          <text
            x={PLOT.left}
            y={SPARK.sloY - 5}
            fontFamily={MONO}
            fontSize="8"
            letterSpacing="0.06em"
            fill={C.navLabel}
          >
            SLO 250 ms
          </text>

          {/* Calm baseline (teal), then the breaching tail (coral). */}
          <path
            d={SPARK.belowLine}
            fill="none"
            stroke={C.accent}
            strokeWidth="1.5"
            strokeLinecap="round"
            strokeLinejoin="round"
            vectorEffect="non-scaling-stroke"
          />
          <path
            d={SPARK.aboveLine}
            fill="none"
            stroke={C.coral}
            strokeWidth="1.7"
            strokeLinecap="round"
            strokeLinejoin="round"
            vectorEffect="non-scaling-stroke"
          />

          {/* Coral ring marking the exact crossing on the SLO line. */}
          <circle
            cx={SPARK.cross[0]}
            cy={SPARK.cross[1]}
            r="2.4"
            fill={C.page}
            stroke={C.coral}
            strokeWidth="1.4"
            vectorEffect="non-scaling-stroke"
          />

          {/* Static halo + solid dot on the breaching peak. */}
          <circle
            cx={SPARK.last[0]}
            cy={SPARK.last[1]}
            r="6"
            fill={C.coral}
            fillOpacity="0.16"
          />
          <circle
            cx={SPARK.last[0]}
            cy={SPARK.last[1]}
            r="2.8"
            fill={C.coral}
          />
        </svg>
      </div>

      <div className="border-cc-card-border mt-4 flex items-center justify-between border-t pt-3">
        <span className="text-cc-ink-dim font-mono text-[0.62rem]">
          last 30 min
        </span>
        <span className="text-cc-status-firing font-mono text-[0.62rem]">
          +68 ms over SLO
        </span>
      </div>
    </div>
  );
}

/** The "same trace" link from symptom to cause: down on small screens, right from lg up. */
function TraceConnector() {
  return (
    <div className="flex shrink-0 flex-col items-center justify-center gap-2 py-1 lg:py-0">
      <span className="border-cc-accent/40 text-cc-accent bg-cc-accent/5 rounded-full border px-2.5 py-0.5 font-mono text-[0.58rem] tracking-[0.12em] whitespace-nowrap uppercase">
        same trace
      </span>
      <span aria-hidden="true" className="text-cc-accent lg:hidden">
        <svg width="16" height="22" viewBox="0 0 16 22" fill="none">
          <path
            d="M8 1V15"
            stroke="currentColor"
            strokeWidth="1.5"
            strokeLinecap="round"
          />
          <path
            d="M3 11.5L8 17L13 11.5"
            stroke="currentColor"
            strokeWidth="1.5"
            strokeLinecap="round"
            strokeLinejoin="round"
          />
        </svg>
      </span>
      <span aria-hidden="true" className="text-cc-accent hidden lg:inline">
        <svg width="36" height="16" viewBox="0 0 36 16" fill="none">
          <path
            d="M1 8H28"
            stroke="currentColor"
            strokeWidth="1.5"
            strokeLinecap="round"
          />
          <path
            d="M24 3L30 8L24 13"
            stroke="currentColor"
            strokeWidth="1.5"
            strokeLinecap="round"
            strokeLinejoin="round"
          />
        </svg>
      </span>
    </div>
  );
}

/** Cause tile: the span waterfall with the degrading billing gRPC hop lit coral. */
function CauseCard() {
  return (
    <div className="border-cc-card-border bg-cc-card-bg/60 flex-1 rounded-2xl border p-5 backdrop-blur-sm">
      <div className="flex items-center justify-between gap-3">
        <span className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.18em] uppercase">
          Cause
        </span>
        <span className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.04em]">
          trace 4b1c8f2a
        </span>
      </div>

      <div
        role="img"
        aria-label="Span waterfall for the checkout request. The billing.Charge gRPC hop runs 180 ms longer than the others and is the degrading span."
        className="mt-4 space-y-2"
      >
        {SPANS.map((span) => (
          <div key={span.name} className="flex items-center gap-2.5">
            <span
              className={[
                "w-[5.5rem] shrink-0 truncate text-right font-mono text-[0.6rem]",
                span.cause
                  ? "text-cc-status-firing"
                  : span.root
                    ? "text-cc-ink"
                    : "text-cc-ink-dim",
              ].join(" ")}
            >
              {span.name}
            </span>
            <span className="bg-cc-surface relative h-2.5 flex-1 overflow-hidden rounded-full">
              <span
                className={[
                  "absolute top-0 h-full rounded-full",
                  span.cause
                    ? "bg-cc-status-firing"
                    : span.root
                      ? "bg-cc-accent/25"
                      : "bg-cc-accent/70",
                ].join(" ")}
                style={{ left: `${span.left}%`, width: `${span.width}%` }}
              />
            </span>
            <span
              className={[
                "w-14 shrink-0 text-right font-mono text-[0.6rem] tabular-nums",
                span.cause ? "text-cc-status-firing" : "text-transparent",
              ].join(" ")}
            >
              {span.delta ?? ""}
            </span>
          </div>
        ))}
      </div>

      <div className="border-cc-card-border mt-4 flex items-center justify-between border-t pt-3">
        <span className="text-cc-status-firing font-mono text-[0.62rem]">
          billing gRPC, +180 ms
        </span>
        <span className="text-cc-ink-dim font-mono text-[0.62rem]">
          slowest hop
        </span>
      </div>
    </div>
  );
}

/**
 * Observability section, take V3: OpenTelemetry-native visibility for any .NET
 * service shown as one symptom-to-cause path, from a p99 breaching its SLO to
 * the single degrading hop in the same trace.
 */
export function ObservabilitySectionV3() {
  return (
    <section className="mx-auto max-w-7xl px-5 pt-16 sm:px-12 sm:pt-24">
      <RevealOnScroll>
        <div className="max-w-3xl">
          <p className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
            Observability
          </p>
          <h2 className="font-heading text-cc-heading text-h3 sm:text-h2 mt-5 leading-[1.1] font-semibold text-balance">
            From symptom to cause.
          </h2>
          <p className="text-cc-ink mt-6 max-w-3xl text-base text-pretty sm:text-lg">
            Start at the symptom and follow it to the cause in the same trace: a
            p99 climbing past its SLO, down to the one degrading hop. Less time
            guessing what broke.
          </p>
        </div>

        <div className="mt-10 flex flex-col items-stretch gap-3 sm:mt-12 lg:flex-row lg:gap-5">
          <SymptomCard />
          <TraceConnector />
          <CauseCard />
        </div>

        <div className="mt-8">
          <a
            href="/platform/observability"
            className="text-cc-accent hover:text-cc-accent-hover inline-flex items-center gap-1.5 text-sm font-medium transition-colors"
          >
            Learn more
            <span aria-hidden="true">&rarr;</span>
          </a>
        </div>
      </RevealOnScroll>
    </section>
  );
}
