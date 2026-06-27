/**
 * Production-view scene, variant 2, v2 "Flow Diagrams" (distributed trace
 * waterfall).
 *
 * Re-expresses one request's distributed trace as a WATERFALL/RANK flow diagram
 * in the locked cc-* system: a pipeline of nested-span chips up top names who
 * calls whom (checkout over users-svc, billing, worker, db), then stacked thin
 * bars below place each span on the shared 0-94 ms axis so it reads where the
 * time actually goes. The single teal path traces the critical route the
 * headline names: root checkout into the slow billing gRPC hop. Billing is the
 * one span flagged coral because it genuinely fires as the bottleneck (63 of
 * 94 ms); every other node stays cream-label / grey-ink. A Stat duo footer
 * carries the two key numbers.
 *
 * Borrowed content (exact, from the v1 nitro variant): checkout 0-94 ms (root,
 * GraphQL), users-svc 8-18 (10 ms, REST), billing 18-81 (63 ms, gRPC, the
 * bottleneck), db 81-94 (13 ms, SELECT). A worker hop is shown in the call
 * pipeline to mirror the ServiceTopology node set.
 *
 * Server component: no "use client", no hooks, no animation, settled final
 * frame. Inline SVG id prefixed "v2-observe-2-". cc-* palette only.
 */

interface ObserveVariant2Props {
  readonly className?: string;
}

const cc = {
  surface: "#0c1322",
  cardBorder: "rgba(245, 241, 234, 0.12)",
  inkFaint: "rgba(245, 241, 234, 0.16)",
  ink: "#a1a3af",
  inkDim: "rgba(245, 241, 234, 0.62)",
  navLabel: "#62748e",
  heading: "#f5f0ea",
  accent: "#5eead4",
  coral: "#f0786a",
  heads: '"Josefin Sans", Futura, sans-serif',
} as const;

const TOTAL_MS = 94;

interface TraceSpan {
  readonly id: string;
  readonly label: string;
  readonly start: number;
  readonly end: number;
  // critical: the single traced teal path (root and the slow hop the time hides
  // behind). bottleneck: the one genuinely-firing span, drawn coral.
  readonly critical: boolean;
  readonly bottleneck: boolean;
}

// Nested durations summing to the 94 ms critical path.
const SPANS: readonly TraceSpan[] = [
  {
    id: "checkout",
    label: "checkout",
    start: 0,
    end: 94,
    critical: true,
    bottleneck: false,
  },
  {
    id: "users",
    label: "users-svc",
    start: 8,
    end: 18,
    critical: false,
    bottleneck: false,
  },
  {
    id: "billing",
    label: "billing",
    start: 18,
    end: 81,
    critical: true,
    bottleneck: true,
  },
  {
    id: "db",
    label: "db",
    start: 81,
    end: 94,
    critical: false,
    bottleneck: false,
  },
] as const;

function pct(ms: number): number {
  return (ms / TOTAL_MS) * 100;
}

export function ObserveVariant2({ className }: ObserveVariant2Props) {
  const idp = "v2-observe-2-";

  return (
    <div
      aria-hidden="true"
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
          distributed trace
        </p>

        {/* Call pipeline: nested-span chips naming who calls whom. The teal path
            runs from the root checkout to the slow billing hop. */}
        <div className="mt-4 flex flex-wrap items-center justify-center gap-x-1 gap-y-1.5">
          <Chip label="checkout" accent />
          <PipeArrow accent />
          <Chip label="users-svc" />
          <PipeArrow />
          <Chip label="billing" status="coral" />
          <PipeArrow />
          <Chip label="worker" />
          <PipeArrow />
          <Chip label="db" />
        </div>

        {/* Waterfall: each span placed on the shared 0-94 ms axis. The teal bars
            are the critical path; billing reads coral as the firing bottleneck. */}
        <div className="border-cc-card-border mt-4 space-y-1.5 border-t pt-4">
          {SPANS.map((span) => {
            const left = pct(span.start);
            const width = Math.max(2, pct(span.end - span.start));
            const traced = span.critical || span.bottleneck;
            const barColor = span.bottleneck
              ? cc.coral
              : span.critical
                ? cc.accent
                : cc.ink;

            return (
              <div key={span.id} className="flex items-center gap-2">
                <span
                  className="w-16 shrink-0 text-right font-mono text-[0.55rem]"
                  style={{ color: span.bottleneck ? cc.coral : cc.inkDim }}
                >
                  {span.label}
                </span>
                <span
                  className="relative h-2 flex-1 overflow-hidden rounded-full"
                  style={{ background: cc.surface }}
                >
                  <span
                    className="absolute top-0 h-full rounded-full"
                    style={{
                      left: `${left}%`,
                      width: `${width}%`,
                      background: barColor,
                      opacity: traced ? 0.85 : 0.45,
                    }}
                  />
                </span>
                <span
                  className="w-9 shrink-0 text-right font-mono text-[0.55rem]"
                  style={{ color: span.bottleneck ? cc.coral : cc.inkDim }}
                >
                  {span.end - span.start}ms
                </span>
              </div>
            );
          })}
        </div>

        {/* Two key numbers: total wall time and where most of it actually went. */}
        <div className="border-cc-card-border mt-4 grid grid-cols-2 gap-4 border-t pt-4">
          <Stat figure="94ms" label="root checkout" />
          <Stat figure="63ms" label="billing gRPC" accent />
        </div>
      </div>

      {/* Inline-svg marker def (id prefixed per contract). Zero-size, no chrome. */}
      <svg width="0" height="0" className="absolute" aria-hidden="true">
        <defs>
          <marker
            id={`${idp}arrowhead`}
            viewBox="0 0 10 10"
            refX="8"
            refY="5"
            markerWidth="5"
            markerHeight="5"
            orient="auto-start-reverse"
          >
            <path
              d="M0,0 L8,5 L0,10"
              fill="none"
              stroke={cc.inkFaint}
              strokeWidth="1"
            />
          </marker>
        </defs>
      </svg>
    </div>
  );
}

function Chip({
  label,
  accent = false,
  status,
}: {
  readonly label: string;
  readonly accent?: boolean;
  readonly status?: "coral";
}) {
  const borderColor =
    status === "coral" ? cc.coral : accent ? cc.accent : cc.cardBorder;
  const textColor = status === "coral" ? cc.coral : accent ? cc.accent : cc.ink;

  return (
    <span
      className="rounded-lg border px-2.5 py-1.5 font-mono text-[0.65rem] whitespace-nowrap"
      style={{ background: cc.surface, borderColor, color: textColor }}
    >
      {label}
    </span>
  );
}

function PipeArrow({ accent = false }: { readonly accent?: boolean }) {
  return (
    <span
      className="px-0.5 text-sm"
      style={{ color: accent ? cc.accent : cc.inkFaint }}
    >
      &rarr;
    </span>
  );
}

function Stat({
  figure,
  label,
  accent = false,
}: {
  readonly figure: string;
  readonly label: string;
  readonly accent?: boolean;
}) {
  return (
    <div>
      <p
        className="text-h4 leading-none font-semibold"
        style={{ fontFamily: cc.heads, color: accent ? cc.accent : cc.heading }}
      >
        {figure}
      </p>
      <p className="mt-1.5 text-xs" style={{ color: cc.inkDim }}>
        {label}
      </p>
    </div>
  );
}
