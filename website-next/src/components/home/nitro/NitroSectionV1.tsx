import Link from "next/link";

import { RevealOnScroll } from "@/src/components/RevealOnScroll";
import { NitroReel } from "@/src/nitro";

/**
 * Nitro section, take v1 "One window".
 *
 * The marquee take: a single framed app window holds the full 5-tab Nitro reel
 * (Author / Observe / Diagnose / Schema / Fusion), which auto-cycles on its own.
 * A centered heading block sits above the window. The window wears a thin title
 * bar (teal status dot, a mono "Nitro / production" label, a faint "live" tag)
 * over a soft teal radial glow, so the "one window, everything in it" claim
 * plays without any extra scaffolding. The reel is the only animated element and
 * is rendered as a client island from "@/src/nitro"; the section itself is a
 * plain server component. The brand spectrum appears once, as the hairline along
 * the top edge of the frame.
 */
export function NitroSectionV1() {
  return (
    <section className="mx-auto max-w-6xl px-5 pt-16 sm:px-12 sm:pt-24">
      <RevealOnScroll>
        {/* heading block */}
        <div className="mx-auto max-w-3xl text-center">
          <p className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
            Nitro
          </p>
          <h2 className="font-heading text-cc-heading text-h3 sm:text-h2 mt-5 leading-[1.1] font-semibold text-balance">
            The platform, with wheels attached.
          </h2>
          <p className="text-cc-ink mx-auto mt-6 max-w-3xl text-base text-pretty sm:text-lg">
            Hot Chocolate, Fusion, and Mocha are the engine. Nitro is the app
            you drive them from: author, observe, diagnose, and ship against
            your APIs, all in one place.
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

        {/* one big app window: the auto-cycling 5-tab reel */}
        <div className="relative mx-auto mt-12 max-w-4xl sm:mt-16">
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
            {/* brand-spectrum hairline (used once on the page) */}
            <div
              aria-hidden="true"
              className="h-px w-full"
              style={{
                background: "linear-gradient(90deg, #16b9e4, #7c92c6, #f0786a)",
              }}
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

          <p className="text-cc-ink-dim mt-5 text-center text-sm text-pretty">
            One window: author, observe, diagnose, schema, and Fusion, switching
            tabs on its own.
          </p>
        </div>
      </RevealOnScroll>
    </section>
  );
}
