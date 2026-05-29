import { PricingCalculator } from "./PricingCalculator";

export function PricingHero() {
  return (
    <section className="py-16 text-center sm:py-20">
      <div className="mx-auto max-w-3xl">
        <div className="mb-4 font-mono text-xs font-semibold uppercase tracking-widest text-[var(--cc-ink-dim)]">
          Pricing
        </div>
        <h1 className="text-5xl font-semibold leading-tight tracking-tight text-[var(--cc-ink)] sm:text-6xl">
          Pricing for{" "}
          <span className="bg-gradient-to-r from-sky-400 to-fuchsia-500 bg-clip-text text-transparent">
            humans and agents.
          </span>
        </h1>
        <p className="mx-auto mt-6 max-w-xl text-base text-[var(--cc-ink-dim)] sm:text-lg">
          Open source, all the way up. Pay only for the parts you don&apos;t
          want to run.
        </p>
      </div>

      <div className="mt-10">
        <PricingCalculator />
      </div>
    </section>
  );
}
