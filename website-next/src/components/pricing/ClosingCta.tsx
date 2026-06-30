import { OutlineButton, SolidButton } from "@/src/design-system/Button";

/**
 * The closing call to action: a bordered band with a spectrum hairline and a
 * soft teal glow, restating the free offer and pointing to sign-up and docs.
 */
export function ClosingCta() {
  return (
    <section className="mt-24 mb-10 sm:mt-28">
      <div className="border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-3xl border p-10 text-center sm:p-16">
        <div
          aria-hidden="true"
          className="pointer-events-none absolute inset-x-0 top-0 h-px"
          style={{
            background:
              "linear-gradient(90deg, transparent, #16b9e4 30%, #7c92c6 50%, #f0786a 70%, transparent)",
          }}
        />
        <div
          aria-hidden="true"
          className="pointer-events-none absolute -top-32 left-1/2 h-64 w-[40rem] max-w-full -translate-x-1/2 opacity-50 blur-3xl"
          style={{
            background:
              "radial-gradient(50% 50% at 50% 50%, rgba(94,234,212,0.12), transparent 70%)",
          }}
        />
        <h2 className="font-heading text-cc-heading text-h3 sm:text-h2 relative font-semibold text-balance">
          Start free. Scale when you do.
        </h2>
        <p className="text-cc-ink relative mx-auto mt-5 max-w-xl text-base text-pretty sm:text-lg">
          1M operations, 2 GB of ingest, schemas and environments, and the full
          Nitro control plane, free on the shared cloud. Upgrade only when you
          outgrow it.
        </p>
        <div className="relative mt-8 flex flex-wrap items-center justify-center gap-3">
          <SolidButton href="https://nitro.chillicream.com">
            Start for free
          </SolidButton>
          <OutlineButton href="/docs">Read the docs</OutlineButton>
        </div>
        <p className="text-cc-ink-dim relative mt-6 font-mono text-xs">
          No credit card. Free forever on the shared cloud.
        </p>
      </div>
    </section>
  );
}
