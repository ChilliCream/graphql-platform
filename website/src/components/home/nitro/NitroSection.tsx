import type { ReactNode } from "react";

import { ArrowLink } from "@/src/components/ArrowLink";
import { MockWindowChrome } from "@/src/components/MockWindowChrome";
import { PageSection } from "@/src/components/PageSection";
import { RevealOnScroll } from "@/src/components/RevealOnScroll";
import { Eyebrow } from "@/src/design-system/Eyebrow";
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

export function NitroSection() {
  return (
    <PageSection className="pt-16 sm:pt-24">
      <RevealOnScroll>
        <div className="flex max-w-3xl flex-col gap-5">
          <Eyebrow color="ink-dim">Nitro</Eyebrow>
          <h2 className="font-heading text-cc-heading text-h3 sm:text-h2 leading-[1.1] font-semibold text-balance">
            The platform, with wheels attached.
          </h2>
          <p className="text-cc-ink max-w-3xl text-base text-pretty sm:text-lg">
            A GraphQL IDE, a telemetry dashboard, a schema and client registry,
            and a Fusion query-plan viewer. All of it is the same app.
          </p>
          <ArrowLink href="/products/nitro">Learn more</ArrowLink>
        </div>

        <div className="mt-12 grid grid-cols-1 gap-6 sm:mt-16 lg:grid-cols-2">
          {SURFACES.map((surface) => (
            <a
              key={surface.caption}
              href="/products/nitro"
              aria-label={`Nitro ${surface.caption}, learn more`}
              className="block transition-transform hover:-translate-y-0.5"
            >
              <MockWindowChrome
                header={{ variant: "dots" }}
                label={`Nitro / ${surface.caption}`}
                glow={{
                  background:
                    "radial-gradient(60% 60% at 50% 35%, rgba(94,234,212,0.16), transparent 70%)",
                  inset: "-inset-3",
                  blur: "blur-2xl",
                  rounded: "rounded-[2rem]",
                }}
                rounded="rounded-xl"
              >
                {surface.visual}
              </MockWindowChrome>
            </a>
          ))}
        </div>
      </RevealOnScroll>
    </PageSection>
  );
}
