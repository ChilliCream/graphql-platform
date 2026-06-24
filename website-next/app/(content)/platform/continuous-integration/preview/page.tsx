import type { Metadata } from "next";
import Link from "next/link";
import type { ReactNode } from "react";

import { OutlineButton, SolidButton } from "@/src/design-system/Button";

export const metadata: Metadata = {
  title: "Continuous Integration Variations",
  description:
    "Preview variations for the ChilliCream Platform / Continuous Integration page.",
  robots: { index: false, follow: false },
};

/* -------------------------------------------------------------------------- */
/*  Manifest                                                                  */
/*  Each variation route is verified to exist on disk under                   */
/*  app/(content)/platform/continuous-integration/preview/v<n>/page.tsx.      */
/*  The `present` flag records that check; a missing variation renders a     */
/*  placeholder.                                                              */
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
    name: "Pipeline Story",
    summary:
      "Walks a single commit through compose, validate, and publish as one continuous CI narrative.",
    route: "/platform/continuous-integration/preview/v1",
    present: true,
  },
  {
    v: 2,
    name: "Safety Net",
    summary:
      "Leads with the breaking-change checks that gate every merge and protect published clients.",
    route: "/platform/continuous-integration/preview/v2",
    present: true,
  },
  {
    v: 3,
    name: "CLI-First Engineering",
    summary:
      "Centres the nitro command surface: scriptable, reproducible, and the same on laptop or runner.",
    route: "/platform/continuous-integration/preview/v3",
    present: true,
  },
  {
    v: 4,
    name: "The Registry Dispatch",
    summary:
      "Editorial long-form that reads each CI run as a registry dispatch, with schema diffs as the lede.",
    route: "/platform/continuous-integration/preview/v4",
    present: true,
  },
  {
    v: 5,
    name: "Field Manual",
    summary:
      "Numbered, step-by-step field manual that walks compose, check, and publish in disciplined order.",
    route: "/platform/continuous-integration/preview/v5",
    present: true,
  },
  {
    v: 6,
    name: "Bean to Cup",
    summary:
      "Frames CI as a barista's craft: compose the blend, taste the diff, and pour each release with care.",
    route: "/platform/continuous-integration/preview/v6",
    present: true,
  },
  {
    v: 7,
    name: "Pipeline Pulse",
    summary:
      "Motion-led showcase where the pipeline beats in time with every commit, surfacing CI as live rhythm.",
    route: "/platform/continuous-integration/preview/v7",
    present: true,
  },
  {
    v: 8,
    name: "Registry Wall",
    summary:
      "A mosaic of registry tiles that rearranges as schemas land, reading CI as one shifting wall of state.",
    route: "/platform/continuous-integration/preview/v8",
    present: true,
  },
  {
    v: 9,
    name: "Registry Codex",
    summary:
      "Chapter-by-chapter codex that bookmarks each CI run as a registry entry, paging through compose to publish.",
    route: "/platform/continuous-integration/preview/v9",
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

export default function ContinuousIntegrationChooserPage() {
  return (
    <div className="flex flex-col gap-16 py-6">
      <header>
        <Eyebrow>Internal · continuous integration page review</Eyebrow>
        <h1 className="font-heading text-h1 text-cc-heading mt-5 font-semibold tracking-tight">
          Continuous Integration{" "}
          <span
            className="bg-clip-text text-transparent"
            style={{ backgroundImage: SPECTRUM }}
          >
            Variations
          </span>
        </h1>
        <p className="text-cc-ink mt-6 max-w-2xl text-[1.1rem] leading-relaxed">
          Nine stances for the Platform / Continuous Integration page. Open
          each, compare them side by side, and pick the one that frames the CI
          story best.
        </p>
        <ul className="text-cc-ink-dim mt-8 flex flex-wrap gap-x-6 gap-y-2 font-mono text-[0.72rem] tracking-tight">
          <li>v1 · Pipeline Story</li>
          <li>v2 · Safety Net</li>
          <li>v3 · CLI-First Engineering</li>
          <li>v4 · The Registry Dispatch</li>
          <li>v5 · Field Manual</li>
          <li>v6 · Bean to Cup</li>
          <li>v7 · Pipeline Pulse</li>
          <li>v8 · Registry Wall</li>
          <li>v9 · Registry Codex</li>
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
