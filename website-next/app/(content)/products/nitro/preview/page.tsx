import type { Metadata } from "next";
import NextLink from "next/link";

import { PageHero } from "@/src/components/PageHero";

export const metadata: Metadata = {
  title: "Nitro Page Variations",
  description:
    "The surviving design stances for the Nitro product page. Older experiments are archived.",
  robots: { index: false, follow: false },
};

interface Variation {
  readonly stop: string;
  readonly stance: string;
  readonly href: string;
  readonly description: string;
}

const VARIATIONS: readonly Variation[] = [
  {
    stop: "v3",
    stance: "Developer Deep-Dive",
    href: "/products/nitro/preview/v3",
    description:
      "Dense, technical, and hands-on, built for engineers who want the wiring exposed.",
  },
  {
    stop: "v4",
    stance: "The Verdict Ledger",
    href: "/products/nitro/preview/v4",
    description:
      "A side-by-side comparison stance with a coral accent, weighing Nitro against the alternatives row by row.",
  },
  {
    stop: "v5",
    stance: "Operator's Console",
    href: "/products/nitro/preview/v5",
    description:
      "A code-walkthrough stance on the cc-accent rail, narrating Nitro from the operator's terminal outward.",
  },
  {
    stop: "v7",
    stance: "Live Cockpit",
    href: "/products/nitro/preview/v7",
    description:
      "A motion-showcase stance on the cc-accent rail, flying Nitro through a live cockpit of moving gauges and signals.",
  },
];

export default function NitroPreviewIndexPage() {
  return (
    <>
      <PageHero
        eyebrow="Internal Preview / Not Indexed"
        title="Nitro Page Variations"
        teaser="The surviving stances for the Nitro product page, parked side by side on one line. Pick a stop to ride that variation end to end."
      />

      {/* Railway: a single spine with three stations branching off. */}
      <div className="relative mx-auto max-w-3xl pb-12">
        {/* The track: a spectrum-tinted vertical rail, used once on the screen. */}
        <div
          aria-hidden="true"
          className="absolute top-2 bottom-2 left-[15px] w-px sm:left-[19px]"
          style={{
            background:
              "linear-gradient(180deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)",
          }}
        />

        <ol className="space-y-6">
          {VARIATIONS.map((variation) => (
            <li key={variation.stop} className="relative pl-12 sm:pl-16">
              {/* Station node sitting on the rail. */}
              <span
                aria-hidden="true"
                className="border-cc-accent bg-cc-surface absolute top-6 left-0 flex size-8 items-center justify-center rounded-full border-2 sm:size-10"
              >
                <span className="text-cc-accent font-mono text-xs font-semibold tracking-widest uppercase">
                  {variation.stop}
                </span>
              </span>

              <NextLink
                href={variation.href}
                className="border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover group block rounded-2xl border p-6 no-underline backdrop-blur-sm transition-colors sm:p-8"
              >
                <div className="flex items-baseline justify-between gap-4">
                  <h2 className="text-cc-heading text-xl font-semibold tracking-tight sm:text-2xl">
                    {variation.stance}
                  </h2>
                  <span
                    aria-hidden="true"
                    className="text-cc-ink-dim group-hover:text-cc-accent shrink-0 text-2xl leading-none transition-colors"
                  >
                    &rarr;
                  </span>
                </div>
                <p className="text-cc-ink-dim mt-3 max-w-prose text-sm sm:text-base">
                  {variation.description}
                </p>
              </NextLink>
            </li>
          ))}
        </ol>
      </div>
    </>
  );
}
