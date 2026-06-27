/**
 * Production-view scene, variant 1, v2 "Flow Diagrams".
 *
 * Re-expresses the v1 Nitro operation-detail tile (the `checkout` operation
 * caught mid-spike) as an on-brand flow diagram in the locked v2 system: the
 * ScrollScenes Chip / Arrow / Stat / thin-teal-bar vocabulary on a single
 * cc-card surface. Topology is WATERFALL/RANK: the operation chip flows into a
 * stack of latency windows drawn as thin bars (the ServiceTopology pattern),
 * the single teal-traced path being p99 climbing past the SLO line; an amber
 * Investigating gate marks the firing window; a Stat duo footer carries the two
 * headline numbers.
 *
 * Content matches v1: operation `checkout`, span-kind `query`, "Investigating"
 * status, p99 climbing to 86 ms past a 60 ms SLO, p95 42 ms, errors 0.3%.
 *
 * Server component: no "use client", no hooks, no handlers, no animation. The
 * single SLO marker is an inline SVG with ids prefixed "v2-observe-1-".
 */

interface ObserveVariant1Props {
  readonly className?: string;
}

// p99 latency by time window (ms): flat around 40 ms, then kinking up to 86 ms,
// the regression on-call is investigating. The 60 ms SLO sits between them.
const SLO_MS = 60;
const BAR_MAX_MS = 90;

interface LatencyWindow {
  readonly label: string;
  readonly ms: number;
  readonly firing: boolean;
}

// The single traced path: p99 over the last four windows, breaching SLO in -1m.
const WINDOWS: readonly LatencyWindow[] = [
  { label: "-4m", ms: 41, firing: false },
  { label: "-3m", ms: 44, firing: false },
  { label: "-2m", ms: 57, firing: false },
  { label: "-1m", ms: 86, firing: true },
];

const sloPct = (SLO_MS / BAR_MAX_MS) * 100;

function Chip({
  children,
  active = false,
}: {
  readonly children: string;
  readonly active?: boolean;
}) {
  return (
    <span
      className={[
        "rounded-lg border px-2.5 py-1.5 font-mono text-[0.65rem] whitespace-nowrap",
        active
          ? "border-cc-accent/60 text-cc-accent bg-cc-surface"
          : "border-cc-card-border text-cc-ink bg-cc-surface",
      ].join(" ")}
    >
      {children}
    </span>
  );
}

function Arrow() {
  return (
    <span aria-hidden="true" className="text-cc-ink-faint px-0.5 text-sm">
      &rarr;
    </span>
  );
}

function Stat({
  figure,
  label,
  tone = "normal",
}: {
  readonly figure: string;
  readonly label: string;
  readonly tone?: "normal" | "accent";
}) {
  return (
    <div>
      <p
        className={[
          "font-heading text-h4 leading-none font-semibold",
          tone === "accent" ? "text-cc-accent" : "text-cc-heading",
        ].join(" ")}
      >
        {figure}
      </p>
      <p className="text-cc-ink-dim mt-1.5 text-xs">{label}</p>
    </div>
  );
}

export function ObserveVariant1({ className }: ObserveVariant1Props) {
  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      aria-hidden="true"
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
          operation health
        </p>

        {/* Operation flows into its p99 signal, the one traced path. */}
        <div className="mt-3 flex flex-wrap items-center justify-center gap-1.5">
          <Chip active>checkout</Chip>
          <Chip>query</Chip>
          <Arrow />
          <Chip>p99 latency</Chip>
        </div>

        {/* Waterfall of latency windows; the firing window crosses the SLO. */}
        <div className="border-cc-card-border mt-4 border-t pt-4">
          <div className="text-cc-nav-label flex items-baseline justify-between font-mono text-[0.55rem] tracking-[0.12em] uppercase">
            <span>p99 by window</span>
            <span>slo {SLO_MS}ms</span>
          </div>

          <div className="mt-3 space-y-1.5">
            {WINDOWS.map((w) => {
              const pct = Math.min((w.ms / BAR_MAX_MS) * 100, 100);
              return (
                <div key={w.label} className="flex items-center gap-2">
                  <span className="text-cc-ink-dim w-10 shrink-0 text-right font-mono text-[0.55rem]">
                    {w.label}
                  </span>
                  <span className="bg-cc-surface relative h-2 flex-1 overflow-hidden rounded-full">
                    <span
                      className={[
                        "absolute top-0 left-0 h-full rounded-full",
                        w.firing
                          ? "bg-cc-accent opacity-90"
                          : "bg-cc-accent opacity-40",
                      ].join(" ")}
                      style={{ width: `${pct}%` }}
                    />
                    {/* Dashed SLO threshold, fixed inside the 0..max track,
                        drawn over the fill so a breach stays visible. */}
                    <span
                      aria-hidden="true"
                      className="absolute top-0 h-full"
                      style={{
                        left: `${sloPct}%`,
                        width: 0,
                        borderLeft: "1px dashed #f0786a",
                        opacity: 0.8,
                      }}
                    />
                  </span>
                  <span
                    className="w-10 shrink-0 font-mono text-[0.55rem]"
                    style={{
                      color: w.firing ? "#5eead4" : "rgba(245,241,234,0.62)",
                    }}
                  >
                    {w.ms}ms
                  </span>
                </div>
              );
            })}
          </div>
        </div>

        {/* The firing window opens an amber Investigating gate on the path. */}
        <div className="mt-4 flex items-center justify-center gap-1.5">
          <Chip>-1m</Chip>
          <Arrow />
          <span
            className="inline-flex items-center gap-1.5 rounded-lg border px-2.5 py-1.5 font-mono text-[0.65rem] whitespace-nowrap"
            style={{ borderColor: "#fbbf24", color: "#fbbf24" }}
          >
            <span
              aria-hidden="true"
              className="inline-block size-1.5 rounded-full"
              style={{ background: "#fbbf24" }}
            />
            investigating
          </span>
        </div>

        {/* Stat duo footer: the two headline numbers at a glance. */}
        <div className="border-cc-card-border mt-4 grid grid-cols-2 gap-4 border-t pt-4">
          <Stat figure="86ms" label="p99, past slo" tone="accent" />
          <Stat figure="0.3%" label="error rate" />
        </div>

        <div className="border-cc-ink-faint mt-4 border-t border-dashed pt-3">
          <p className="text-cc-ink-dim text-center text-xs">
            one operation&apos;s health at a glance
          </p>
        </div>
      </div>
    </div>
  );
}
