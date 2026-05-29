const OSS_PRODUCTS = [
  "Hot Chocolate",
  "Mocha",
  "Strawberry Shake",
  "Fusion (OSS)",
];

// The "open source belt": MIT-licensed core stack, no account required.
export function OssStrip() {
  return (
    <section className="py-12">
      <div className="grid items-center gap-8 rounded-2xl border border-[var(--cc-card-border)] bg-[var(--cc-card-bg)] p-8 backdrop-blur-sm lg:grid-cols-[minmax(0,1fr)_auto]">
        <div className="flex flex-col gap-4">
          <div className="flex flex-wrap gap-2">
            <span className="rounded-lg border border-fuchsia-400/40 bg-fuchsia-500/10 px-2.5 py-1.5 font-mono text-[11px] uppercase tracking-[0.08em] text-[var(--cc-ink)]">
              Free forever
            </span>
            {OSS_PRODUCTS.map((label) => (
              <span
                key={label}
                className="rounded-lg border border-[var(--cc-card-border)] bg-white/[0.025] px-2.5 py-1.5 font-mono text-[11px] uppercase tracking-[0.08em] text-[var(--cc-ink)]"
              >
                {label}
              </span>
            ))}
          </div>
          <p className="max-w-[60ch] text-base leading-relaxed text-[var(--cc-ink-dim)]">
            <strong className="font-medium text-[var(--cc-ink)]">
              MIT-licensed.
            </strong>{" "}
            No account needed. No upsell. Build, ship, and scale a production
            GraphQL platform on the OSS stack alone.
          </p>
        </div>
        <div
          aria-label="Install Hot Chocolate from NuGet"
          className="inline-flex min-w-[300px] items-center gap-3.5 rounded-xl border border-[var(--cc-card-border)] bg-white/[0.03] px-4 py-3.5 font-mono text-[13px] text-[var(--cc-ink)]"
        >
          <span className="text-fuchsia-400">$</span>
          <span>
            <span>dotnet add package </span>
            <span className="text-amber-300">HotChocolate</span>
          </span>
        </div>
      </div>
    </section>
  );
}
