import { SolidButton } from "@/src/design-system/Button";

/** Closing band for /learn: successor of TemplatesClosing, widened from templates to learning at large. */
export function LearnClosing() {
  return (
    <section className="border-cc-card-border my-8 flex flex-col items-start justify-between gap-8 border-y py-12 sm:flex-row sm:items-center">
      <div>
        <h2 className="font-heading text-cc-heading text-h5 sm:text-h4 font-semibold">
          Build on the foundations, not from scratch.
        </h2>
        <p className="text-cc-ink-dim mt-3 max-w-2xl">
          Explore the documentation to combine Hot Chocolate, Fusion, Mocha, Strawberry Shake, and Nitro for your
          architecture.
        </p>
      </div>
      <SolidButton href="/docs">Explore the docs</SolidButton>
    </section>
  );
}
