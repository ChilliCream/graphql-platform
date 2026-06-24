import type { Metadata } from "next";
import Link from "next/link";
import type { ReactNode } from "react";

import { OutlineButton, SolidButton } from "@/src/design-system/Button";

export const metadata: Metadata = {
  title: "Landing Page Variations",
  description:
    "Preview variations for the ChilliCream landing page. The existing app/page.tsx is untouched.",
  robots: { index: false, follow: false },
  keywords: [
    "ChilliCream",
    "landing page",
    "Hot Chocolate",
    "Nitro",
    "Fusion",
    "GraphQL",
  ],
  openGraph: {
    title: "Landing Page Variations",
    description:
      "Preview variations for the ChilliCream landing page. The existing app/page.tsx is untouched.",
  },
};

/* -------------------------------------------------------------------------- */
/*  Manifest                                                                   */
/*  Each variation route is verified to exist on disk under                    */
/*  app/(content)/landing/preview/v<n>/page.tsx. The `present` flag records    */
/*  that check; a missing variation renders a placeholder instead.            */
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
    name: "Product-Reel-First",
    summary:
      "Opens on the Nitro product reel, then layers proof and platform breadth beneath.",
    route: "/landing/preview/v1",
    present: true,
  },
  {
    v: 2,
    name: ".NET-Native Platform",
    summary:
      "Frames ChilliCream as the .NET-native GraphQL platform for Hot Chocolate teams.",
    route: "/landing/preview/v2",
    present: true,
  },
  {
    v: 3,
    name: "Outcome-Led Story",
    summary:
      "Leads with the outcomes teams ship: faster releases, safer changes, calmer on-call.",
    route: "/landing/preview/v3",
    present: true,
  },
  {
    v: 4,
    name: "The Dispatch",
    summary:
      "Editorial long-form stance: ChilliCream framed as a written dispatch with considered columns, measured pacing, and reader-grade typography.",
    route: "/landing/preview/v4",
    present: true,
  },
  {
    v: 5,
    name: "Ledger of Six",
    summary:
      "Numbered-step stance: six ordered entries that walk teams from first query to production, ledger-style and sequential.",
    route: "/landing/preview/v5",
    present: true,
  },
  {
    v: 6,
    name: "House Blend",
    summary:
      "Barista stance: ChilliCream framed as a house blend of Hot Chocolate, Nitro, and Fusion, served warm with a steady hand.",
    route: "/landing/preview/v6",
    present: true,
  },
  {
    v: 7,
    name: "Platform Pulse",
    summary:
      "Motion-showcase stance: the platform read as a live pulse of queries, schema changes, and shipped releases moving in sync.",
    route: "/landing/preview/v7",
    present: true,
  },
  {
    v: 8,
    name: "Constellation Grid",
    summary:
      "Dotgrid-hover stance: the platform plotted as a constellation of points on a fine grid that light up under the cursor.",
    route: "/landing/preview/v8",
    present: true,
  },
  {
    v: 9,
    name: "Concentric Platform",
    summary:
      "Concentric-rings stance: ChilliCream framed as nested rings radiating outward from a single GraphQL core.",
    route: "/landing/preview/v9",
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

export default function LandingChooserPage() {
  return (
    <div className="flex flex-col gap-16 py-6">
      <header>
        <Eyebrow>Internal · landing page review</Eyebrow>
        <h1 className="font-heading text-h1 text-cc-heading mt-5 font-semibold tracking-tight">
          Landing Page{" "}
          <span
            className="bg-clip-text text-transparent"
            style={{ backgroundImage: SPECTRUM }}
          >
            Variations
          </span>
        </h1>
        <p className="text-cc-ink mt-6 max-w-2xl text-[1.1rem] leading-relaxed">
          Nine stances for the public landing page. Open each, compare them side
          by side, and pick the one that frames ChilliCream best. The shipped
          landing at app/page.tsx is untouched.
        </p>
        <ul className="text-cc-ink-dim mt-8 flex flex-wrap gap-x-6 gap-y-2 font-mono text-[0.72rem] tracking-tight">
          <li>v1 · Product-Reel-First</li>
          <li>v2 · .NET-Native Platform</li>
          <li>v3 · Outcome-Led Story</li>
          <li>v4 · The Dispatch</li>
          <li>v5 · Ledger of Six</li>
          <li>v6 · House Blend</li>
          <li>v7 · Platform Pulse</li>
          <li>v8 · Constellation Grid</li>
          <li>v9 · Concentric Platform</li>
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
