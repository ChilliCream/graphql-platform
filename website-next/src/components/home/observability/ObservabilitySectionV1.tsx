import Link from "next/link";

import { RevealOnScroll } from "@/src/components/RevealOnScroll";

/**
 * Observability section, take v1 "Operations ranked by impact".
 *
 * One all-visible hero: an operations table on a cc-* card. A header row
 * (operation, p95, error rate, throughput, impact) sits above five rows of real
 * production figures, ordered by an impact score rather than raw call count. The
 * checkout operation is pinned at #1 and flagged firing (coral); the field steps
 * down through one degrading row (amber) to three calm healthy rows (green). The
 * busiest operation by throughput (GET /products, 8.6k req/min) sits last, and
 * the slowest by p95 (a 6.40s background job) ranks low, so the row that hurts
 * most is the one at the top.
 *
 * The operations span GraphQL, gRPC, REST, and a background job to show this is
 * OpenTelemetry-native observability for any .NET service, with GraphQL as one
 * first-class citizen among equals. Static React Server Component: no hooks, no
 * client APIs. Dark cc-* palette; status colors are used as data and rationed.
 * Every inline SVG is decorative; all figures are present as text. Svg-related
 * literals are prefixed "obs-v1-".
 */

type Status = "firing" | "investigating" | "healthy";

/** Locked status hues, used only for inline SVG fills and strokes. */
const STATUS_HEX: Record<Status, string> = {
  firing: "#f0786a",
  investigating: "#fbbf24",
  healthy: "#34d399",
};

/** Faint track behind every impact bar. */
const TRACK = "rgba(245, 241, 234, 0.1)";

/** Text color per status, as full Tailwind class strings so they are emitted. */
const STATUS_TEXT_CLASS: Record<Status, string> = {
  firing: "text-cc-status-firing",
  investigating: "text-cc-status-investigating",
  healthy: "text-cc-status-healthy",
};

/** Pill chrome per status, used only for the two non-healthy rows. */
const STATUS_PILL_CLASS: Record<Status, string> = {
  firing:
    "border-cc-status-firing/40 bg-cc-status-firing/10 text-cc-status-firing",
  investigating:
    "border-cc-status-investigating/40 bg-cc-status-investigating/10 text-cc-status-investigating",
  healthy:
    "border-cc-status-healthy/40 bg-cc-status-healthy/10 text-cc-status-healthy",
};

interface OperationRow {
  readonly rank: number;
  readonly name: string;
  /** Transport / span-kind, shown so the table reads as service-agnostic. */
  readonly kind: string;
  readonly p95: string;
  readonly errorRate: string;
  /** Throughput in requests per minute. */
  readonly throughput: string;
  /** Impact score, 0-100, the value the table is ordered by. */
  readonly impact: number;
  readonly status: Status;
  /** Status word for the pill; only set on the non-healthy rows. */
  readonly statusLabel?: string;
}

// Locked sample, ordered by impact descending. checkout is the only firing op
// and ranks #1; the busiest op by throughput (GET /products) and the slowest by
// p95 (reindex-catalog) both rank low, so impact, not call count, sets the order.
const OPERATIONS: readonly OperationRow[] = [
  {
    rank: 1,
    name: "checkout",
    kind: "graphql",
    p95: "1.84s",
    errorRate: "4.2%",
    throughput: "1.2k",
    impact: 98,
    status: "firing",
    statusLabel: "Firing",
  },
  {
    rank: 2,
    name: "Billing.Charge",
    kind: "grpc",
    p95: "940ms",
    errorRate: "1.1%",
    throughput: "320",
    impact: 71,
    status: "investigating",
    statusLabel: "Degrading",
  },
  {
    rank: 3,
    name: "POST /orders",
    kind: "rest",
    p95: "280ms",
    errorRate: "0.3%",
    throughput: "2.4k",
    impact: 46,
    status: "healthy",
  },
  {
    rank: 4,
    name: "reindex-catalog",
    kind: "job",
    p95: "6.40s",
    errorRate: "0.0%",
    throughput: "18",
    impact: 24,
    status: "healthy",
  },
  {
    rank: 5,
    name: "GET /products",
    kind: "rest",
    p95: "64ms",
    errorRate: "0.0%",
    throughput: "8.6k",
    impact: 11,
    status: "healthy",
  },
];

/** Shared 5-column grid so the header labels and every data row stay aligned. */
const GRID =
  "grid grid-cols-[minmax(0,1fr)_4.5rem_4.5rem_4.75rem_8.5rem] items-center gap-x-4";

export function ObservabilitySectionV1() {
  return (
    <section className="mx-auto max-w-7xl px-5 pt-16 sm:px-12 sm:pt-24">
      <RevealOnScroll>
        {/* heading block */}
        <div className="max-w-3xl">
          <p className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
            Observability
          </p>
          <h2 className="font-heading text-cc-heading text-h3 sm:text-h2 mt-5 leading-[1.1] font-semibold text-balance">
            Fix the right thing first.
          </h2>
          <p className="text-cc-ink mt-6 max-w-3xl text-base text-pretty sm:text-lg">
            Nitro turns your traffic into per-operation metrics and ranks them
            by impact, not by call count, so the operation that is actually
            hurting the system sits at the top. You fix the right thing first
            instead of chasing the loudest error.
          </p>
          <Link
            href="/platform/observability"
            className="text-cc-accent hover:text-cc-accent-hover mt-6 inline-flex items-center gap-1.5 text-sm font-medium transition-colors"
          >
            Learn more
            <span aria-hidden="true">&rarr;</span>
          </Link>
        </div>

        {/* hero: operations ranked by impact */}
        <div className="border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover mt-10 rounded-3xl border p-5 backdrop-blur-sm transition-colors sm:p-8">
          {/* card header strip */}
          <div className="flex items-center justify-between gap-3">
            <span className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.18em] uppercase">
              Operations by impact
            </span>
            <span className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.04em]">
              last 24h
            </span>
          </div>

          {/* desktop table */}
          <div className="mt-5 hidden lg:block">
            <div
              className={`${GRID} text-cc-nav-label border-cc-card-border border-b px-4 pb-2.5 font-mono text-[0.58rem] tracking-[0.1em] uppercase`}
            >
              <span>Operation</span>
              <span className="text-right">p95</span>
              <span className="text-right">Err rate</span>
              <span className="text-right">Req/min</span>
              <span>Impact</span>
            </div>

            {OPERATIONS.map((row) => {
              const pinned = row.rank === 1;
              return (
                <div
                  key={row.name}
                  className={`${GRID} px-4 py-3.5 ${
                    row.rank > 1 ? "border-cc-card-border/60 border-t" : ""
                  } ${pinned ? "bg-cc-status-firing/5" : ""}`}
                  style={
                    pinned
                      ? { boxShadow: `inset 2px 0 0 ${STATUS_HEX.firing}` }
                      : undefined
                  }
                >
                  <div className="flex min-w-0 items-center gap-2.5">
                    <RankTag rank={row.rank} />
                    <StatusDot status={row.status} />
                    <span
                      className={`truncate font-mono text-sm ${
                        pinned ? "text-cc-heading font-semibold" : "text-cc-ink"
                      }`}
                    >
                      {row.name}
                    </span>
                    <KindTag kind={row.kind} />
                    {row.statusLabel ? (
                      <span className="ml-auto">
                        <StatusPill
                          status={row.status}
                          label={row.statusLabel}
                        />
                      </span>
                    ) : null}
                  </div>
                  <Figure value={row.p95} />
                  <Figure value={row.errorRate} />
                  <Figure value={row.throughput} />
                  <ImpactCell status={row.status} impact={row.impact} />
                </div>
              );
            })}
          </div>

          {/* mobile / tablet stack */}
          <div className="mt-5 space-y-2.5 lg:hidden">
            {OPERATIONS.map((row) => {
              const pinned = row.rank === 1;
              return (
                <div
                  key={row.name}
                  className={`rounded-xl border p-4 ${
                    pinned
                      ? "border-cc-status-firing/30 bg-cc-status-firing/5"
                      : "border-cc-card-border bg-cc-surface/40"
                  }`}
                  style={
                    pinned
                      ? { boxShadow: `inset 2px 0 0 ${STATUS_HEX.firing}` }
                      : undefined
                  }
                >
                  <div className="flex items-center gap-2.5">
                    <RankTag rank={row.rank} />
                    <StatusDot status={row.status} />
                    <span
                      className={`min-w-0 flex-1 truncate font-mono text-sm ${
                        pinned ? "text-cc-heading font-semibold" : "text-cc-ink"
                      }`}
                    >
                      {row.name}
                    </span>
                    <KindTag kind={row.kind} />
                    {row.statusLabel ? (
                      <StatusPill status={row.status} label={row.statusLabel} />
                    ) : null}
                  </div>

                  <div className="mt-3.5 grid grid-cols-3 gap-3">
                    <Stat label="p95" value={row.p95} />
                    <Stat label="Err rate" value={row.errorRate} />
                    <Stat label="Req/min" value={row.throughput} />
                  </div>

                  <div className="mt-3.5 flex items-center gap-3">
                    <span className="text-cc-nav-label shrink-0 font-mono text-[0.55rem] tracking-[0.1em] uppercase">
                      Impact
                    </span>
                    <span className="min-w-0 flex-1">
                      <ImpactBar status={row.status} impact={row.impact} />
                    </span>
                    <span
                      className={`${STATUS_TEXT_CLASS[row.status]} w-6 text-right font-mono text-sm font-semibold tabular-nums`}
                    >
                      {row.impact}
                    </span>
                  </div>
                </div>
              );
            })}
          </div>

          <p className="text-cc-ink-dim border-cc-card-border mt-6 border-t pt-4 text-sm text-pretty">
            Impact weighs p95 latency, error rate, and how much of the system
            depends on the operation, so the call with the most traffic is not
            always the one to fix.
          </p>
        </div>
      </RevealOnScroll>
    </section>
  );
}

/** Rank marker; #1 reads in the heading voice, the rest stay muted. */
function RankTag({ rank }: { readonly rank: number }) {
  return (
    <span
      className={`shrink-0 font-mono text-[0.62rem] tabular-nums ${
        rank === 1 ? "text-cc-heading font-semibold" : "text-cc-nav-label"
      }`}
    >
      #{rank}
    </span>
  );
}

/** Status dot: a thin ring around a solid core, in the row's status hue. */
function StatusDot({ status }: { readonly status: Status }) {
  const color = STATUS_HEX[status];
  return (
    <svg
      width={14}
      height={14}
      viewBox="0 0 14 14"
      aria-hidden="true"
      className="shrink-0"
    >
      <circle
        cx={7}
        cy={7}
        r={5}
        fill={`${color}22`}
        stroke={color}
        strokeWidth={1}
      />
      <circle cx={7} cy={7} r={2.3} fill={color} />
    </svg>
  );
}

/** Transport tag (graphql / grpc / rest / job), so the table reads service-agnostic. */
function KindTag({ kind }: { readonly kind: string }) {
  return (
    <span className="border-cc-card-border text-cc-nav-label shrink-0 rounded border px-1.5 py-0.5 font-mono text-[0.6rem] tracking-[0.04em]">
      {kind}
    </span>
  );
}

/** Status word pill, rationed to the firing and degrading rows. */
function StatusPill({
  status,
  label,
}: {
  readonly status: Status;
  readonly label: string;
}) {
  return (
    <span
      className={`${STATUS_PILL_CLASS[status]} inline-flex shrink-0 items-center rounded-full border px-2 py-0.5 font-mono text-[0.58rem] font-medium tracking-[0.04em] whitespace-nowrap`}
    >
      {label}
    </span>
  );
}

/** Right-aligned mono figure for the numeric desktop columns. */
function Figure({ value }: { readonly value: string }) {
  return (
    <span className="text-cc-ink text-right font-mono text-sm whitespace-nowrap tabular-nums">
      {value}
    </span>
  );
}

/** Labeled stat block used in the mobile stack. */
function Stat({
  label,
  value,
}: {
  readonly label: string;
  readonly value: string;
}) {
  return (
    <div className="min-w-0">
      <p className="text-cc-nav-label font-mono text-[0.55rem] tracking-[0.1em] uppercase">
        {label}
      </p>
      <p className="text-cc-ink mt-1 font-mono text-sm tabular-nums">{value}</p>
    </div>
  );
}

/** Desktop impact column: a status-colored bar with its score on the right. */
function ImpactCell({
  status,
  impact,
}: {
  readonly status: Status;
  readonly impact: number;
}) {
  return (
    <div className="flex items-center gap-2">
      <span className="min-w-0 flex-1">
        <ImpactBar status={status} impact={impact} />
      </span>
      <span
        className={`${STATUS_TEXT_CLASS[status]} w-6 text-right font-mono text-sm font-semibold tabular-nums`}
      >
        {impact}
      </span>
    </div>
  );
}

/** Horizontal impact bar: a faint track with a status-colored fill. */
function ImpactBar({
  status,
  impact,
}: {
  readonly status: Status;
  readonly impact: number;
}) {
  return (
    <svg
      viewBox="0 0 100 6"
      width="100%"
      height={6}
      preserveAspectRatio="none"
      aria-hidden="true"
      className="block"
    >
      <rect x={0} y={0} width={100} height={6} rx={3} fill={TRACK} />
      <rect
        x={0}
        y={0}
        width={impact}
        height={6}
        rx={3}
        fill={STATUS_HEX[status]}
      />
    </svg>
  );
}
