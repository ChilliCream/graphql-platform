import type { Metadata } from "next";
import Link from "next/link";
import type { ReactNode } from "react";

import { OutlineButton, SolidButton } from "@/src/design-system/Button";

export const metadata: Metadata = {
  title: "Platform Hub Variations",
  description: "Preview variations for the ChilliCream Platform hub page.",
  robots: { index: false, follow: false },
};

/* -------------------------------------------------------------------------- */
/*  Manifest                                                                   */
/*  Each variation route is verified to exist on disk under                    */
/*  app/(content)/platform/preview-hub/v<n>/page.tsx. The `present` flag       */
/*  records that check; a missing variation renders a placeholder instead.    */
/* -------------------------------------------------------------------------- */

interface Variation {
  readonly v: number;
  readonly name: string;
  readonly summary: string;
  readonly route: string;
  readonly present: boolean;
}

const VARIATIONS: readonly Variation[] = [
  {
    v: 1,
    name: "Capability Map",
    summary:
      "Survey of every platform capability grouped under Build, Run, and Evolve.",
    route: "/platform/preview-hub/v1",
    present: true,
  },
  {
    v: 2,
    name: "Developer Loop Narrative",
    summary:
      "Walks the platform end to end as a single developer loop, scene by scene.",
    route: "/platform/preview-hub/v2",
    present: true,
  },
  {
    v: 3,
    name: "Product-Led Hub",
    summary:
      "Leads with Nitro as the product, with capabilities arranged around it.",
    route: "/platform/preview-hub/v3",
    present: true,
  },
  {
    v: 4,
    name: "Annotated Source",
    summary:
      "A code-walkthrough stance that reads the platform like an annotated source file, with capabilities surfaced as inline commentary.",
    route: "/platform/preview-hub/v4",
    present: true,
  },
  {
    v: 5,
    name: "The Field Guide",
    summary:
      "An editorial long-form stance that presents the platform as a field guide, with each capability written up as a guided entry.",
    route: "/platform/preview-hub/v5",
    present: true,
  },
  {
    v: 6,
    name: "House Menu",
    summary:
      "A barista-styled house menu where each capability reads as a signature drink, with tasting notes and a recommended pour.",
    route: "/platform/preview-hub/v6",
    present: true,
  },
  {
    v: 7,
    name: "Live Circuit",
    summary:
      "A motion-forward showcase that traces the platform as a live circuit, with capabilities lit up as nodes along an animated path.",
    route: "/platform/preview-hub/v7",
    present: true,
  },
  {
    v: 8,
    name: "The Spectrum Hinge",
    summary:
      "A hue-wash-band stance that hinges the platform on a violet spectrum sweep, with each capability washed in along the band.",
    route: "/platform/preview-hub/v8",
    present: true,
  },
  {
    v: 9,
    name: "Hatched Atlas",
    summary:
      "A diagonal-hatch stance that maps the platform as a hatched atlas, with each capability plotted as a cross-ruled plate.",
    route: "/platform/preview-hub/v9",
    present: true,
  },
];

/* -------------------------------------------------------------------------- */
/*  Brand spectrum: the single gradient event on this screen                  */
/* -------------------------------------------------------------------------- */

const SPECTRUM = "linear-gradient(100deg, #16b9e4, #7c92c6, #f0786a)";

/* -------------------------------------------------------------------------- */
/*  Small chrome                                                              */
/* -------------------------------------------------------------------------- */

interface EyebrowProps {
  readonly children: ReactNode;
}

function Eyebrow({ children }: EyebrowProps) {
  return (
    <p className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.22em] uppercase">
      {children}
    </p>
  );
}

interface VariationBadgeProps {
  readonly v: number;
}

function VariationBadge({ v }: VariationBadgeProps) {
  return (
    <span className="border-cc-card-border bg-cc-surface text-cc-heading flex h-9 w-9 shrink-0 items-center justify-center rounded-full border font-mono text-[0.82rem] font-semibold tabular-nums">
      v{v}
    </span>
  );
}

/* -------------------------------------------------------------------------- */
/*  Variation card                                                            */
/* -------------------------------------------------------------------------- */

interface VariationCardProps {
  readonly variation: Variation;
}

function VariationCard({ variation }: VariationCardProps) {
  if (!variation.present) {
    return (
      <div className="border-cc-card-border bg-cc-card-bg/60 flex flex-col gap-5 rounded-xl border border-dashed p-6 backdrop-blur-sm">
        <div className="flex items-center gap-3">
          <span className="border-cc-card-border text-cc-nav-label flex h-9 w-9 shrink-0 items-center justify-center rounded-full border border-dashed font-mono text-[0.82rem] tabular-nums">
            v{variation.v}
          </span>
          <Eyebrow>Variation {variation.v}</Eyebrow>
        </div>
        <p className="text-cc-heading font-heading text-h5 font-semibold tracking-tight">
          {variation.name}
        </p>
        <p className="text-cc-ink text-[0.95rem] leading-relaxed">
          {variation.summary}
        </p>
        <p className="text-cc-nav-label font-mono text-[0.66rem] tracking-tight">
          not generated
        </p>
      </div>
    );
  }

  return (
    <Link
      href={variation.route}
      className="group border-cc-card-border bg-cc-card-bg hover:border-cc-accent flex flex-col gap-5 rounded-xl border p-6 no-underline backdrop-blur-sm transition-colors"
    >
      <div className="flex items-center gap-3">
        <VariationBadge v={variation.v} />
        <Eyebrow>Variation {variation.v}</Eyebrow>
      </div>
      <p className="text-cc-heading group-hover:text-cc-accent font-heading text-h5 font-semibold tracking-tight transition-colors">
        {variation.name}
      </p>
      <p className="text-cc-ink text-[0.95rem] leading-relaxed">
        {variation.summary}
      </p>
      <span className="text-cc-ink-dim mt-auto font-mono text-[0.66rem] tracking-tight">
        {variation.route}
      </span>
      <span className="text-cc-accent text-[0.82rem] font-medium">
        Open variation →
      </span>
    </Link>
  );
}

/* -------------------------------------------------------------------------- */
/*  Page                                                                      */
/* -------------------------------------------------------------------------- */

export default function PlatformHubChooserPage() {
  return (
    <div className="flex flex-col gap-16 py-6">
      <header>
        <Eyebrow>Internal · platform hub review</Eyebrow>
        <h1 className="font-heading text-h1 text-cc-heading mt-5 font-semibold tracking-tight">
          Platform Hub{" "}
          <span
            className="bg-clip-text text-transparent"
            style={{ backgroundImage: SPECTRUM }}
          >
            Variations
          </span>
        </h1>
        <p className="text-cc-ink mt-6 max-w-2xl text-[1.1rem] leading-relaxed">
          Nine stances for the platform hub page. Open each, compare them side
          by side, and pick the one that frames the platform best.
        </p>
        <ul className="text-cc-ink-dim mt-8 flex flex-wrap gap-x-6 gap-y-2 font-mono text-[0.72rem] tracking-tight">
          <li>v1 · Capability Map</li>
          <li>v2 · Developer Loop Narrative</li>
          <li>v3 · Product-Led Hub</li>
          <li>v4 · Annotated Source</li>
          <li>v5 · The Field Guide</li>
          <li>v6 · House Menu</li>
          <li>v7 · Live Circuit</li>
          <li>v8 · The Spectrum Hinge</li>
          <li>v9 · Hatched Atlas</li>
        </ul>
      </header>

      <section>
        <div className="grid gap-5 md:grid-cols-3">
          {VARIATIONS.map((variation) => (
            <VariationCard key={variation.v} variation={variation} />
          ))}
        </div>
      </section>

      <section className="flex flex-col items-center gap-7 py-6 text-center">
        <h2 className="font-heading text-h4 text-cc-heading max-w-2xl font-semibold tracking-tight">
          Picked a stance? The same CTA closes every variation.
        </h2>
        <div className="flex flex-wrap items-center justify-center gap-3">
          <SolidButton href="/get-started">Start for Free</SolidButton>
          <OutlineButton href="/docs">Read the Docs</OutlineButton>
        </div>
      </section>
    </div>
  );
}
