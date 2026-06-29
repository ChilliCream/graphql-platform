import type { ReactNode } from "react";
import Link from "next/link";

import { RevealOnScroll } from "@/src/components/RevealOnScroll";
import { NitroReel } from "@/src/nitro";

// Brand spectrum gradient, used exactly once on this section (the frame hairline).
const SPECTRUM = "linear-gradient(90deg, #16b9e4, #7c92c6, #f0786a)";

interface Facet {
  readonly label: string;
  readonly description: string;
  readonly icon: ReactNode;
}

/** Single window glyph: one app, everything switching inside it (take 1). */
function WindowGlyph() {
  return (
    <svg viewBox="0 0 24 24" fill="none" aria-hidden="true" className="h-4 w-4">
      <rect
        x="3"
        y="5"
        width="18"
        height="14"
        rx="2"
        stroke="currentColor"
        strokeWidth="1.5"
      />
      <path d="M3 9h18" stroke="currentColor" strokeWidth="1.5" />
      <circle cx="6" cy="7" r="0.6" fill="currentColor" />
    </svg>
  );
}

/** Four-surface grid glyph: GraphQL IDE, telemetry, registry, Fusion (take 2). */
function SurfacesGlyph() {
  return (
    <svg viewBox="0 0 24 24" fill="none" aria-hidden="true" className="h-4 w-4">
      <rect
        x="4"
        y="4"
        width="6.5"
        height="6.5"
        rx="1.2"
        stroke="currentColor"
        strokeWidth="1.5"
      />
      <rect
        x="13.5"
        y="4"
        width="6.5"
        height="6.5"
        rx="1.2"
        stroke="currentColor"
        strokeWidth="1.5"
      />
      <rect
        x="4"
        y="13.5"
        width="6.5"
        height="6.5"
        rx="1.2"
        stroke="currentColor"
        strokeWidth="1.5"
      />
      <rect
        x="13.5"
        y="13.5"
        width="6.5"
        height="6.5"
        rx="1.2"
        stroke="currentColor"
        strokeWidth="1.5"
      />
    </svg>
  );
}

/** Gauge glyph: the live instrument panel reading right now (take 3). */
function GaugeGlyph() {
  return (
    <svg viewBox="0 0 24 24" fill="none" aria-hidden="true" className="h-4 w-4">
      <path
        d="M4 17a8 8 0 0 1 16 0"
        stroke="currentColor"
        strokeWidth="1.5"
        strokeLinecap="round"
      />
      <path
        d="M12 17l4.5-4"
        stroke="currentColor"
        strokeWidth="1.5"
        strokeLinecap="round"
      />
      <circle cx="12" cy="17" r="1.3" fill="currentColor" />
    </svg>
  );
}

// The three takes, compacted into one labeled point each.
const FACETS: readonly Facet[] = [
  {
    label: "One window",
    description:
      "Every surface lives in one app, cycling Author, Observe, Diagnose, Schema, and Fusion on its own.",
    icon: <WindowGlyph />,
  },
  {
    label: "Every surface",
    description:
      "A GraphQL IDE, a telemetry dashboard, a schema and client registry, and a Fusion plan viewer.",
    icon: <SurfacesGlyph />,
  },
  {
    label: "Gauges on",
    description:
      "Live latency, throughput, traces, and schema-change signals as your backend runs.",
    icon: <GaugeGlyph />,
  },
];

/**
 * Combined Nitro landing section.
 *
 * Compacts the three Nitro takes (single window, surface grid, live gauges) into
 * one section: a shared header, one framed app-window holding the auto-cycling
 * five-tab `NitroReel`, and a tight three-up row of labeled points that carry
 * each take's idea. `NitroReel` is the only animated element and renders as a
 * client island from "@/src/nitro"; the section itself is a server component.
 */
export function CombinedNitro() {
  return (
    <section className="mx-auto max-w-7xl px-5 pt-16 sm:px-12 sm:pt-24">
      <RevealOnScroll>
        {/* shared header */}
        <div className="mx-auto max-w-3xl text-center">
          <p className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
            Nitro
          </p>
          <h2 className="font-heading text-cc-heading text-h3 sm:text-h2 mt-5 leading-[1.1] font-semibold text-balance">
            The platform, with wheels attached.
          </h2>
          <p className="text-cc-ink mx-auto mt-6 max-w-3xl text-base text-pretty sm:text-lg">
            Nitro is the app you drive the platform from: the GraphQL IDE,
            telemetry, schema and client registry, and Fusion management, all in
            one place.
          </p>
          <Link
            href="/products/nitro"
            className="group text-cc-accent hover:text-cc-accent-hover mt-6 inline-flex items-center gap-1.5 text-sm font-medium transition-colors"
          >
            Learn more
            <span
              aria-hidden="true"
              className="transition-transform group-hover:translate-x-0.5"
            >
              &rarr;
            </span>
          </Link>
        </div>

        {/* one framed app window: the auto-cycling five-tab reel */}
        <div className="relative mx-auto mt-12 max-w-4xl sm:mt-14">
          {/* soft teal radial glow behind the frame */}
          <div
            aria-hidden="true"
            className="pointer-events-none absolute -inset-x-6 -inset-y-8 -z-10 rounded-[2.5rem] opacity-50 blur-3xl"
            style={{
              background:
                "radial-gradient(55% 55% at 50% 35%, rgba(94,234,212,0.18), transparent 70%)",
            }}
          />

          <div className="border-cc-card-border bg-cc-surface overflow-hidden rounded-2xl border shadow-[0_40px_120px_-50px_rgba(94,234,212,0.4)] sm:rounded-3xl">
            {/* brand-spectrum hairline (used once on this section) */}
            <div
              aria-hidden="true"
              className="h-px w-full"
              style={{ background: SPECTRUM }}
            />

            {/* title bar */}
            <div className="border-cc-card-border flex items-center gap-2 border-b px-4 py-2.5 sm:px-5">
              <span className="bg-cc-accent inline-block h-2 w-2 animate-pulse rounded-full" />
              <span className="text-cc-nav-label font-mono text-[10px] tracking-[0.18em] uppercase">
                Nitro / production
              </span>
              <span className="border-cc-card-border text-cc-ink-dim ml-auto inline-flex items-center rounded-full border px-2 py-0.5 font-mono text-[10px] tracking-[0.16em] uppercase">
                live
              </span>
            </div>

            <NitroReel className="w-full" />
          </div>
        </div>

        {/* three compacted takes, one labeled point each */}
        <div className="mx-auto mt-10 grid max-w-4xl grid-cols-1 gap-px overflow-hidden rounded-2xl sm:grid-cols-3 sm:gap-0">
          {FACETS.map((facet, index) => (
            <div
              key={facet.label}
              className={
                index > 0 ? "sm:border-cc-card-border sm:border-l" : undefined
              }
            >
              <div className="flex h-full flex-col gap-2.5 px-1 py-3 sm:px-5">
                <div className="flex items-center gap-2">
                  <span className="text-cc-accent">{facet.icon}</span>
                  <span className="text-cc-heading font-mono text-[11px] tracking-[0.18em] uppercase">
                    {facet.label}
                  </span>
                </div>
                <p className="text-cc-ink-dim text-sm text-pretty">
                  {facet.description}
                </p>
              </div>
            </div>
          ))}
        </div>
      </RevealOnScroll>
    </section>
  );
}
