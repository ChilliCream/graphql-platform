import type { ReactNode } from "react";

import { RevealOnScroll } from "@/src/components/RevealOnScroll";
import {
  NitroCompose,
  NitroFusion,
  NitroSchema,
  NitroTrace,
} from "@/src/nitro";

interface Surface {
  readonly caption: string;
  readonly visual: ReactNode;
}

const SURFACES: readonly Surface[] = [
  {
    caption: "Author",
    visual: <NitroCompose className="block w-full" durationMs={13000} />,
  },
  {
    caption: "Observe",
    visual: <NitroTrace className="block w-full" durationMs={13000} />,
  },
  {
    caption: "Evolve",
    visual: <NitroSchema className="block w-full" durationMs={13000} />,
  },
  {
    caption: "Compose",
    visual: <NitroFusion className="block w-full" durationMs={19000} />,
  },
];

interface SurfaceCardProps {
  readonly caption: string;
  readonly children: ReactNode;
}

function SurfaceCard({ caption, children }: SurfaceCardProps) {
  return (
    <div className="relative">
      <div
        aria-hidden="true"
        className="absolute -inset-3 -z-10 rounded-[2rem] opacity-40 blur-2xl"
        style={{
          background:
            "radial-gradient(60% 60% at 50% 35%, rgba(94,234,212,0.16), transparent 70%)",
        }}
      />
      <div className="border-cc-card-border bg-cc-surface overflow-hidden rounded-xl border shadow-2xl shadow-black/40">
        <div className="border-cc-card-border flex items-center gap-2 border-b px-4 py-2.5">
          <span aria-hidden="true" className="flex items-center gap-1.5">
            <span
              className="inline-block h-2 w-2 rounded-full"
              style={{ background: "#ff5f57" }}
            />
            <span
              className="inline-block h-2 w-2 rounded-full"
              style={{ background: "#febc2e" }}
            />
            <span
              className="inline-block h-2 w-2 rounded-full"
              style={{ background: "#28c840" }}
            />
          </span>
          <span className="text-cc-ink-dim font-mono text-[10px] tracking-[0.18em] uppercase">
            Nitro / {caption}
          </span>
        </div>
        {children}
      </div>
    </div>
  );
}

export function NitroSection() {
  return (
    <section className="mx-auto max-w-7xl px-5 pt-16 sm:px-12 sm:pt-24">
      <RevealOnScroll>
        <div className="flex max-w-3xl flex-col gap-5">
          <span className="text-cc-ink-dim font-mono text-xs tracking-[0.2em] uppercase">
            Nitro
          </span>
          <h2 className="font-heading text-cc-heading text-h3 sm:text-h2 leading-[1.1] font-semibold text-balance">
            The platform, with wheels attached.
          </h2>
          <p className="text-cc-ink max-w-3xl text-base text-pretty sm:text-lg">
            A GraphQL IDE, a telemetry dashboard, a schema and client registry,
            and a Fusion query-plan viewer. All of it is the same app.
          </p>
          <a
            href="/products/nitro"
            className="text-cc-accent hover:text-cc-accent-hover group inline-flex w-fit items-center gap-2 text-sm font-medium transition-colors"
          >
            Learn more
            <span
              aria-hidden="true"
              className="transition-transform group-hover:translate-x-0.5"
            >
              &rarr;
            </span>
          </a>
        </div>

        <div className="mt-12 grid grid-cols-1 gap-6 sm:mt-16 lg:grid-cols-2">
          {SURFACES.map((surface) => (
            <a
              key={surface.caption}
              href="/products/nitro"
              aria-label={`Nitro ${surface.caption}, learn more`}
              className="block transition-transform hover:-translate-y-0.5"
            >
              <SurfaceCard caption={surface.caption}>
                {surface.visual}
              </SurfaceCard>
            </a>
          ))}
        </div>
      </RevealOnScroll>
    </section>
  );
}
