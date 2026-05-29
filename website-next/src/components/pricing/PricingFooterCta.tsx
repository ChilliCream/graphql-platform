import Link from "next/link";

// Final ask. Distinct from the Enterprise band above: this one points the
// self-serve reader at the docs and the install line, not at sales.
export function PricingFooterCta() {
  return (
    <section className="relative overflow-hidden py-20 text-center">
      <div
        aria-hidden
        className="pointer-events-none absolute bottom-0 right-0 -z-10 h-[420px] w-[420px] translate-x-1/3 translate-y-1/3 rounded-full bg-fuchsia-500/15 blur-3xl"
      />
      <div className="relative mx-auto max-w-2xl">
        <div className="mb-3 font-mono text-xs font-semibold uppercase tracking-widest text-[var(--cc-ink-dim)]">
          Ready when you are
        </div>
        <h2 className="text-4xl font-semibold leading-tight tracking-tight text-[var(--cc-ink)] sm:text-5xl">
          Start free. Scale when you need to.
        </h2>
        <p className="mx-auto mt-5 max-w-xl text-base text-[var(--cc-ink-dim)] sm:text-lg">
          Every Nitro tier ships with hard limits, budget alerts, and the same
          OSS engine underneath. No lock-in, no surprise invoices.
        </p>

        <div
          aria-label="Install Hot Chocolate from NuGet"
          className="mx-auto mt-7 inline-flex items-center gap-3 rounded-xl border border-fuchsia-400/30 bg-white/[0.02] px-4 py-3 font-mono text-[13px] text-[var(--cc-ink)]"
        >
          <span className="text-fuchsia-400">$</span>
          <span>dotnet add package </span>
          <span className="text-amber-300">HotChocolate</span>
        </div>

        <div className="mt-7 flex flex-wrap items-center justify-center gap-4">
          <Link
            href="https://nitro.chillicream.com"
            target="_blank"
            rel="noopener noreferrer"
            className="inline-flex items-center justify-center rounded-full bg-[var(--cc-ink)] px-7 py-3 text-sm font-medium text-[#0c1322] no-underline transition-colors hover:bg-white"
          >
            Start free →
          </Link>
          <Link
            href="/docs"
            className="border-b border-[var(--cc-card-border)] pb-0.5 text-sm text-[var(--cc-ink-dim)] no-underline transition-colors hover:border-[var(--cc-ink)] hover:text-[var(--cc-ink)]"
          >
            Read the docs →
          </Link>
        </div>
      </div>
    </section>
  );
}
