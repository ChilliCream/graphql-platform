import Link from "next/link";
import { Fragment } from "react";

import { RevealOnScroll } from "@/src/components/RevealOnScroll";

/**
 * Observability section, take v2 "See where time is lost".
 *
 * One all-visible hero: a distributed-trace span waterfall for a single
 * `checkout` request on a cc-* card. A root span (the GraphQL operation, 318 ms)
 * sits at the top, then nested, indented child spans for a REST users service, a
 * gRPC billing call, a background job worker, and the database, each drawn as a
 * horizontal duration bar on one shared time axis with its duration in mono on
 * the right. The slow hop, billing (gRPC), is the wide coral bar that owns the
 * timeline at 201 ms of 318. A coral hairline ties that span as the critical
 * path and labels the lost time directly under the bar.
 *
 * The spans cross GraphQL, REST, gRPC, and a job to show this is
 * OpenTelemetry-native tracing for any .NET service, with the GraphQL operation
 * as one first-class span among equals. Static React Server Component: no hooks,
 * no client APIs. Dark cc-* palette; teal is the signature, status colors are
 * used as data and rationed (green healthy, coral the one slow hop). Every inline
 * SVG is decorative and every figure is present as text. Svg ids are prefixed
 * "obs-v2-".
 */

const TOTAL_MS = 318;

type SpanTone = "root" | "healthy" | "slow";

interface TraceSpan {
  readonly name: string;
  /** Transport / span-kind, shown so the trace reads service-agnostic. */
  readonly kind: string;
  /** Nesting depth in the trace tree; controls indentation. */
  readonly depth: number;
  /** Span start offset from the request start, in milliseconds. */
  readonly start: number;
  /** Span duration in milliseconds. */
  readonly dur: number;
  readonly tone: SpanTone;
}

// Locked sample: one checkout request, 318 ms end to end. Billing (gRPC) is the
// long pole at 201 ms and is the only span flagged slow; everything else is calm.
const SPANS: readonly TraceSpan[] = [
  {
    name: "checkout",
    kind: "GraphQL",
    depth: 0,
    start: 0,
    dur: 318,
    tone: "root",
  },
  {
    name: "users-svc",
    kind: "REST",
    depth: 1,
    start: 8,
    dur: 34,
    tone: "healthy",
  },
  {
    name: "billing",
    kind: "gRPC",
    depth: 1,
    start: 46,
    dur: 201,
    tone: "slow",
  },
  {
    name: "worker",
    kind: "job",
    depth: 1,
    start: 252,
    dur: 58,
    tone: "healthy",
  },
  {
    name: "orders-db",
    kind: "DB",
    depth: 2,
    start: 258,
    dur: 44,
    tone: "healthy",
  },
];

const SLOW = SPANS.find((span) => span.tone === "slow") ?? SPANS[2];
const SLOW_LEFT = (SLOW.start / TOTAL_MS) * 100;
const SLOW_RIGHT = ((SLOW.start + SLOW.dur) / TOTAL_MS) * 100;
const SLOW_CENTER = (SLOW_LEFT + SLOW_RIGHT) / 2;

/** Locked status hues, used only for inline SVG fills and strokes. */
const HEX = {
  accent: "#5eead4",
  healthy: "#34d399",
  slow: "#f0786a",
} as const;

/** Time-axis ticks, aligned to the 25 / 50 / 75 gridlines. */
const TICKS: readonly {
  readonly pct: number;
  readonly label: string;
  readonly smOnly?: boolean;
}[] = [
  { pct: 0, label: "0" },
  { pct: 25, label: "80", smOnly: true },
  { pct: 50, label: "159" },
  { pct: 75, label: "239", smOnly: true },
  { pct: 100, label: "318 ms" },
];

/** Shared 3-column grid so the labels, bars, and durations stay aligned. */
const GRID =
  "grid grid-cols-[5.25rem_minmax(0,1fr)_3rem] gap-x-2 sm:grid-cols-[10rem_minmax(0,1fr)_3.75rem] sm:gap-x-3";

export function ObservabilitySectionV2() {
  return (
    <section className="mx-auto max-w-6xl px-5 pt-16 sm:px-12 sm:pt-24">
      <RevealOnScroll>
        {/* heading block */}
        <div className="max-w-3xl">
          <p className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
            Observability
          </p>
          <h2 className="font-heading text-cc-heading text-h3 sm:text-h2 mt-5 leading-[1.1] font-semibold text-balance">
            See where time is lost.
          </h2>
          <p className="text-cc-ink mt-6 max-w-3xl text-base text-pretty sm:text-lg">
            Every request becomes one distributed trace across your services,
            GraphQL, REST, gRPC, and the jobs behind them, so when something is
            slow you see exactly where the time went, hop by hop.
          </p>
          <Link
            href="/platform/observability"
            className="text-cc-accent hover:text-cc-accent-hover mt-6 inline-flex items-center gap-1.5 text-sm font-medium transition-colors"
          >
            Learn more
            <span aria-hidden="true">&rarr;</span>
          </Link>
        </div>

        {/* hero: distributed-trace span waterfall for one request */}
        <div className="border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover mt-10 rounded-3xl border p-5 backdrop-blur-sm transition-colors sm:p-8">
          <p className="sr-only">
            Distributed trace for one checkout request, total 318 milliseconds.
            Spans: users-svc over REST 34 ms, billing over gRPC 201 ms (the slow
            hop on the critical path), worker job 58 ms, orders-db 44 ms.
          </p>

          {/* card header strip */}
          <div className="flex flex-wrap items-center justify-between gap-3">
            <div className="flex items-center gap-2.5">
              <span className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.18em] uppercase">
                Distributed trace
              </span>
              <span className="text-cc-ink-dim font-mono text-[0.6rem] tracking-[0.04em]">
                checkout &middot; 7f3a4b2c
              </span>
            </div>
            <span className="border-cc-status-firing/40 bg-cc-status-firing/10 text-cc-status-firing inline-flex items-center gap-1.5 rounded-full border px-2.5 py-0.5 font-mono text-[0.6rem] font-medium whitespace-nowrap">
              <span
                aria-hidden="true"
                className="bg-cc-status-firing size-1.5 rounded-full"
              />
              1 slow span
            </span>
          </div>

          {/* waterfall */}
          <div className="mt-6">
            {/* time-axis ruler */}
            <div className={GRID}>
              <div className="h-5" />
              <div className="relative h-5 min-w-0">
                <PlotGrid />
                {TICKS.map((tick) => (
                  <span
                    key={tick.pct}
                    className={`text-cc-nav-label absolute top-0 font-mono text-[0.58rem] tabular-nums ${
                      tick.smOnly ? "hidden sm:block" : ""
                    }`}
                    style={{
                      left: `${tick.pct}%`,
                      transform:
                        tick.pct === 0
                          ? "none"
                          : tick.pct === 100
                            ? "translateX(-100%)"
                            : "translateX(-50%)",
                    }}
                  >
                    {tick.label}
                  </span>
                ))}
              </div>
              <div className="h-5" />
            </div>

            {/* span rows, with the critical-path tie injected under the slow hop */}
            {SPANS.map((span) => (
              <Fragment key={span.name}>
                <div className={GRID}>
                  <SpanLabel span={span} />
                  <div className="relative h-10 min-w-0">
                    <PlotGrid />
                    {span.tone === "slow" ? (
                      <span
                        aria-hidden="true"
                        className="absolute top-1/2 h-3 -translate-y-1/2 rounded-full shadow-[0_0_18px_3px_rgba(240,120,106,0.4)]"
                        style={{
                          left: `${(span.start / TOTAL_MS) * 100}%`,
                          width: `${(span.dur / TOTAL_MS) * 100}%`,
                        }}
                      />
                    ) : null}
                    <div className="absolute inset-x-0 top-1/2 -translate-y-1/2">
                      <SpanBar span={span} />
                    </div>
                  </div>
                  <SpanDuration span={span} />
                </div>

                {span.tone === "slow" ? <CriticalPathRow /> : null}
              </Fragment>
            ))}
          </div>

          {/* footer: plain summary + the status legend */}
          <div className="border-cc-card-border mt-6 flex flex-wrap items-center justify-between gap-x-6 gap-y-3 border-t pt-4">
            <p className="text-cc-ink-dim text-sm text-pretty">
              One request, every hop on a single timeline, with the time
              accounted for across every service.
            </p>
            <div className="flex items-center gap-4">
              <Legend swatch="bg-cc-accent" label="operation" />
              <Legend swatch="bg-cc-status-healthy" label="healthy" />
              <Legend swatch="bg-cc-status-firing" label="slow" />
            </div>
          </div>
        </div>
      </RevealOnScroll>
    </section>
  );
}

/** Three faint vertical gridlines, drawn behind every plot cell so the time
 * axis reads as continuous from the ruler down through the bars. */
function PlotGrid() {
  return (
    <div aria-hidden="true" className="pointer-events-none absolute inset-0">
      {[25, 50, 75].map((pct) => (
        <div
          key={pct}
          className="bg-cc-card-border absolute inset-y-0 w-px"
          style={{ left: `${pct}%` }}
        />
      ))}
    </div>
  );
}

/** Indented span label: a tree tick for nested spans, the name, and the kind. */
function SpanLabel({ span }: { readonly span: TraceSpan }) {
  return (
    <div
      className="flex h-10 min-w-0 items-center gap-1.5 overflow-hidden"
      style={{ paddingLeft: `${span.depth * 14}px` }}
    >
      {span.depth > 0 ? (
        <span
          aria-hidden="true"
          className="border-cc-card-border mb-1.5 size-2 shrink-0 rounded-bl-[2px] border-b border-l"
        />
      ) : null}
      <span
        className={`truncate font-mono text-xs ${
          span.tone === "root" ? "text-cc-heading font-semibold" : "text-cc-ink"
        }`}
      >
        {span.name}
      </span>
      <span className="border-cc-card-border text-cc-nav-label hidden shrink-0 rounded border px-1 py-px font-mono text-[0.5rem] tracking-[0.04em] sm:inline">
        {span.kind}
      </span>
    </div>
  );
}

/** One duration bar on the shared time axis, drawn as an inline SVG rect. */
function SpanBar({ span }: { readonly span: TraceSpan }) {
  if (span.tone === "root") {
    return (
      <svg
        viewBox="0 0 100 16"
        width="100%"
        height="16"
        preserveAspectRatio="none"
        aria-hidden="true"
        className="block"
      >
        <rect
          x="0.5"
          y="0.5"
          width="99"
          height="15"
          rx="2.5"
          fill="rgba(94, 234, 212, 0.1)"
          stroke={HEX.accent}
          strokeOpacity="0.5"
          strokeWidth="1"
          vectorEffect="non-scaling-stroke"
        />
      </svg>
    );
  }

  const left = (span.start / TOTAL_MS) * 100;
  const width = (span.dur / TOTAL_MS) * 100;
  const slow = span.tone === "slow";
  const height = slow ? 12 : 10;

  return (
    <svg
      viewBox={`0 0 100 ${height}`}
      width="100%"
      height={height}
      preserveAspectRatio="none"
      aria-hidden="true"
      className="block"
    >
      <rect
        x={left}
        y="0"
        width={width}
        height={height}
        rx="2"
        fill={slow ? HEX.slow : HEX.healthy}
        fillOpacity={slow ? 1 : 0.82}
      />
    </svg>
  );
}

/** Right-aligned mono duration; the slow hop and the total read emphasized. */
function SpanDuration({ span }: { readonly span: TraceSpan }) {
  const tone =
    span.tone === "root"
      ? "text-cc-heading font-semibold"
      : span.tone === "slow"
        ? "text-cc-status-firing font-semibold"
        : "text-cc-ink-dim";

  return (
    <div
      className={`flex h-10 items-center justify-end font-mono text-[0.7rem] whitespace-nowrap tabular-nums ${tone}`}
    >
      {span.dur} ms
    </div>
  );
}

/** Critical-path tie: a coral hairline bracketing the slow hop, with the lost
 * time labeled directly beneath it. Shares the waterfall grid so the bracket
 * lands on the same time axis as the billing bar above. */
function CriticalPathRow() {
  return (
    <div className={GRID}>
      <div className="flex h-11 items-start justify-end">
        <span className="text-cc-status-firing hidden pt-0.5 font-mono text-[0.55rem] tracking-[0.1em] uppercase sm:inline">
          critical path
        </span>
      </div>
      <div className="relative h-11 min-w-0">
        <svg
          viewBox="0 0 100 8"
          width="100%"
          height="8"
          preserveAspectRatio="none"
          aria-hidden="true"
          className="absolute inset-x-0 top-0 block overflow-visible"
        >
          <path
            d={`M ${SLOW_LEFT} 0 L ${SLOW_LEFT} 5 L ${SLOW_RIGHT} 5 L ${SLOW_RIGHT} 0`}
            fill="none"
            stroke={HEX.slow}
            strokeOpacity="0.8"
            strokeWidth="1.25"
            strokeLinecap="round"
            strokeLinejoin="round"
            vectorEffect="non-scaling-stroke"
          />
          <path
            d={`M ${SLOW_CENTER} 5 L ${SLOW_CENTER} 8`}
            fill="none"
            stroke={HEX.slow}
            strokeOpacity="0.8"
            strokeWidth="1.25"
            strokeLinecap="round"
            vectorEffect="non-scaling-stroke"
          />
        </svg>
        <p className="absolute inset-x-0 top-[11px] px-1 text-center font-mono text-[0.6rem] leading-tight text-pretty">
          <span className="text-cc-status-firing font-semibold">
            201 ms of 318
          </span>
          <span className="text-cc-ink-dim"> lost in billing (gRPC)</span>
        </p>
      </div>
      <div className="h-11" />
    </div>
  );
}

/** Status legend entry: a colored swatch and its label. */
function Legend({
  swatch,
  label,
}: {
  readonly swatch: string;
  readonly label: string;
}) {
  return (
    <span className="text-cc-ink-dim inline-flex items-center gap-1.5 font-mono text-[0.6rem]">
      <span aria-hidden="true" className={`size-2 rounded-full ${swatch}`} />
      {label}
    </span>
  );
}
