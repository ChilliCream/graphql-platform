import { OutlineButton, SolidButton } from "@/src/design-system/Button";

import { ComposeScene } from "./ComposeScene";
import { CYAN, TEAL } from "./palette";

export function Hero() {
  return (
    <section className="bg-cc-bg relative flex min-h-[92svh] items-center overflow-hidden">
      <div className="relative z-10 mx-auto w-full max-w-6xl px-5 py-20 sm:px-12">
        <div className="grid grid-cols-1 items-center gap-14 lg:grid-cols-12 lg:gap-10">
          <div className="lg:col-span-5">
            <div className="flex items-center gap-3">
              <span
                aria-hidden="true"
                className="h-px w-12 rounded-full"
                style={{
                  background: `linear-gradient(90deg, ${TEAL}, transparent)`,
                }}
              />
              <span className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
                Fusion · GraphQL Federation for .NET
              </span>
            </div>
            <h1 className="font-heading text-cc-heading text-h3 sm:text-h2 mt-6 text-balance">
              Many teams. Many services.{" "}
              <span
                className="bg-clip-text text-transparent"
                style={{
                  backgroundImage: `linear-gradient(90deg, ${TEAL}, ${CYAN})`,
                }}
              >
                One API.
              </span>
            </h1>
            <p className="text-cc-ink mt-6 text-base sm:text-lg">
              Each team publishes its own schema and ships on its own cadence.
              Fusion composes them into one graph: a single Product carries what
              every team knows about it, joined by identity, checked before it
              deploys.
            </p>
            <div className="mt-9 flex flex-wrap gap-3">
              <SolidButton href="/get-started">Start for Free</SolidButton>
              <OutlineButton href="/docs/fusion">Read the Docs</OutlineButton>
            </div>
            <div className="text-cc-nav-label mt-12 flex items-center gap-3 font-mono text-[0.65rem] tracking-[0.24em] uppercase">
              <span aria-hidden="true" className="bg-cc-card-border h-px w-8" />
              Apollo Federation · Composite Schemas · One Gateway
            </div>
          </div>
          <div className="lg:col-span-7">
            <ComposeScene />
          </div>
        </div>
      </div>
    </section>
  );
}
