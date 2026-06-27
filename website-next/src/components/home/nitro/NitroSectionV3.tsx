import Link from "next/link";

import { RevealOnScroll } from "@/src/components/RevealOnScroll";
import { NitroMonitoringReel } from "@/src/nitro";

interface StatCaptionProps {
  readonly label: string;
  readonly value: string;
  readonly unit: string;
}

// Static instrument readouts that flank the live dashboard. They label what the
// panel is watching; the animated reel is the live surface.
const STATS: readonly StatCaptionProps[] = [
  { label: "p95 latency", value: "142", unit: "ms" },
  { label: "Error rate", value: "0.42", unit: "%" },
  { label: "Throughput", value: "3.1k", unit: "rps" },
];

/** One mono-labelled gauge readout in the side rail. */
function StatCaption({ label, value, unit }: StatCaptionProps) {
  return (
    <div className="border-cc-card-border bg-cc-card-bg rounded-xl border px-4 py-3">
      <div className="flex items-center gap-2">
        <span
          aria-hidden="true"
          className="bg-cc-accent inline-block h-1.5 w-1.5 rounded-full"
        />
        <span className="text-cc-nav-label font-mono text-[10px] tracking-[0.18em] uppercase">
          {label}
        </span>
      </div>
      <div className="text-cc-heading font-heading mt-1.5 flex items-baseline gap-1">
        <span className="text-xl tabular-nums sm:text-2xl">{value}</span>
        <span className="text-cc-ink-dim text-xs">{unit}</span>
      </div>
    </div>
  );
}

/**
 * App-window chrome around the live monitoring reel: a title bar carrying a
 * status dot, the environment label, and a "live" tag, with a teal radial glow
 * pooled behind the frame. The reel paints edge to edge under the bar so it
 * reads as the running Nitro app.
 */
function LiveDashboard() {
  return (
    <div className="relative min-w-0">
      <div
        aria-hidden="true"
        className="absolute -inset-x-6 -inset-y-5 -z-10 rounded-[2.5rem] opacity-50 blur-3xl"
        style={{
          background:
            "radial-gradient(55% 55% at 50% 35%, rgba(94,234,212,0.16), transparent 70%)",
        }}
      />
      <div className="border-cc-card-border bg-cc-surface overflow-hidden rounded-2xl border shadow-2xl shadow-black/50 sm:rounded-3xl">
        <div className="border-cc-card-border flex items-center gap-2 border-b px-4 py-2.5">
          <span
            aria-hidden="true"
            className="bg-cc-accent inline-block h-2 w-2 rounded-full"
          />
          <span className="text-cc-nav-label font-mono text-[10px] tracking-[0.18em] uppercase">
            Nitro / production
          </span>
          <span className="text-cc-accent bg-cc-accent/10 ml-auto rounded px-1.5 py-0.5 font-mono text-[10px] tracking-[0.16em] uppercase">
            live
          </span>
        </div>
        <NitroMonitoringReel />
      </div>
    </div>
  );
}

/**
 * Nitro section, take "Gauges on". A single large live monitoring dashboard is
 * the hero: the Nitro app reel runs inside app-window chrome, flanked on large
 * screens by a slim rail of instrument readouts. Structurally this is one
 * breathing telemetry panel, distinct from the tabbed reel and surface-grid
 * takes. Everything is visible at once, nothing hides behind interaction.
 */
export function NitroSectionV3() {
  return (
    <section className="mx-auto max-w-6xl px-5 pt-16 sm:px-12 sm:pt-24">
      <RevealOnScroll>
        <div className="max-w-3xl">
          <p className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
            Nitro
          </p>
          <h2 className="font-heading text-cc-heading text-h3 sm:text-h2 mt-5 leading-[1.1] font-semibold text-balance">
            Your platform, with the gauges on.
          </h2>
          <p className="text-cc-ink mt-6 max-w-3xl text-base text-pretty sm:text-lg">
            Live latency, throughput, traces, and schema-change signals: the
            instrument panel for everything your backend is doing right now.
          </p>
          <Link
            href="/products/nitro"
            className="group text-cc-accent hover:text-cc-accent-hover mt-6 inline-flex items-center gap-1.5 text-sm font-medium transition-colors"
          >
            Open Nitro
            <span
              aria-hidden="true"
              className="transition-transform group-hover:translate-x-0.5"
            >
              &rarr;
            </span>
          </Link>
        </div>

        <div className="mt-12 grid gap-6 sm:mt-16 lg:grid-cols-[minmax(0,11rem)_minmax(0,1fr)] lg:items-center lg:gap-8">
          <div className="grid grid-cols-3 gap-3 lg:grid-cols-1 lg:gap-4">
            {STATS.map((stat) => (
              <StatCaption
                key={stat.label}
                label={stat.label}
                value={stat.value}
                unit={stat.unit}
              />
            ))}
          </div>
          <LiveDashboard />
        </div>
      </RevealOnScroll>
    </section>
  );
}
