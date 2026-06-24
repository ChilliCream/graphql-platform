import type { Metadata } from "next";
import Link from "next/link";
import type { ReactNode } from "react";

import { OutlineButton, SolidButton } from "@/src/design-system/Button";

export const metadata: Metadata = {
  title: "About Page Variations",
  description: "Preview variations for the ChilliCream About page.",
  robots: { index: false, follow: false },
};

/* -------------------------------------------------------------------------- */
/*  Stance manifest                                                            */
/*  Each target route is verified on disk at                                   */
/*  app/(content)/about/preview/v<n>/page.tsx. A missing take falls back to a  */
/*  placeholder card instead of a dead link.                                   */
/* -------------------------------------------------------------------------- */

interface Stance {
  readonly v: number;
  readonly name: string;
  readonly tagline: string;
  readonly route: string;
  readonly present: boolean;
}

const STANCES: readonly Stance[] = [
  {
    v: 1,
    name: "Mission + Products",
    tagline:
      "Lead with what we build and why, then walk the product surface end to end.",
    route: "/about/preview/v1",
    present: true,
  },
  {
    v: 2,
    name: "Open-Source-First",
    tagline:
      "Frame ChilliCream through the open-source story behind Hot Chocolate and the wider stack.",
    route: "/about/preview/v2",
    present: true,
  },
  {
    v: 3,
    name: "Customer Outcomes Narrative",
    tagline:
      "Anchor the page in the outcomes teams ship on the platform, with company context underneath.",
    route: "/about/preview/v3",
    present: true,
  },
  {
    v: 4,
    name: "Six Steps to a Platform",
    tagline:
      "Walk the company story as a numbered sequence (six steps), each rung building on the last.",
    route: "/about/preview/v4",
    present: true,
  },
  {
    v: 5,
    name: "Constellation Diagram",
    tagline:
      "Treat the About page as a visual hero: a constellation diagram maps how the pieces connect at a glance.",
    route: "/about/preview/v5",
    present: true,
  },
  {
    v: 6,
    name: "House Blend",
    tagline:
      "Pour the company story like a barista flight: the same beans, brewed five different ways for five different palates.",
    route: "/about/preview/v6",
    present: true,
  },
  {
    v: 7,
    name: "Constellation",
    tagline:
      "Run the page as a kinetic showcase, where each chapter glides into view as a tightly choreographed motion set.",
    route: "/about/preview/v7",
    present: true,
  },
  {
    v: 8,
    name: "The Platform Deck",
    tagline:
      "Deal the company story as a card deck (concept: card-deck), each face a self-contained chapter the reader can fan out and shuffle through.",
    route: "/about/preview/v8",
    present: true,
  },
  {
    v: 9,
    name: "Specimen Cabinet",
    tagline:
      "Lay the pieces out like a curated specimen grid (concept: specimen-grid), every product and value pinned and labelled for close inspection.",
    route: "/about/preview/v9",
    present: true,
  },
];

/* -------------------------------------------------------------------------- */
/*  Brand spectrum: a single gradient event for the whole screen               */
/* -------------------------------------------------------------------------- */

const SPECTRUM = "linear-gradient(100deg, #16b9e4, #7c92c6, #f0786a)";

/* -------------------------------------------------------------------------- */
/*  Small chrome                                                               */
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

interface StanceMarkerProps {
  readonly v: number;
  readonly muted?: boolean;
}

/**
 * The take number rendered as a mono coin. Two visual states: live (solid
 * surface) and muted (dashed) for placeholder rows.
 */
function StanceMarker({ v, muted = false }: StanceMarkerProps) {
  if (muted) {
    return (
      <span className="border-cc-card-border text-cc-nav-label flex h-9 w-9 shrink-0 items-center justify-center rounded-full border border-dashed font-mono text-[0.82rem] tabular-nums">
        {v}
      </span>
    );
  }
  return (
    <span className="border-cc-card-border bg-cc-surface text-cc-heading flex h-9 w-9 shrink-0 items-center justify-center rounded-full border font-mono text-[0.82rem] font-semibold tabular-nums">
      {v}
    </span>
  );
}

/* -------------------------------------------------------------------------- */
/*  Stance card                                                                */
/*  A tall ledger column rather than a wide stock card: stance number up top,  */
/*  stance name dominant in the middle, route + arrow pinned at the foot.      */
/* -------------------------------------------------------------------------- */

interface StanceCardProps {
  readonly stance: Stance;
}

function StanceCard({ stance }: StanceCardProps) {
  if (!stance.present) {
    return (
      <div className="border-cc-card-border bg-cc-card-bg/60 flex h-full min-h-[22rem] flex-col gap-6 rounded-2xl border border-dashed p-6 backdrop-blur-sm">
        <div className="flex items-center justify-between">
          <StanceMarker v={stance.v} muted />
          <Eyebrow>v{stance.v} stance</Eyebrow>
        </div>
        <p className="text-cc-heading font-heading text-h5 font-semibold tracking-tight">
          {stance.name}
        </p>
        <p className="text-cc-ink-dim text-[0.95rem] leading-relaxed">
          {stance.tagline}
        </p>
        <p className="text-cc-nav-label mt-auto font-mono text-[0.66rem] tracking-tight">
          not generated
        </p>
      </div>
    );
  }

  return (
    <Link
      href={stance.route}
      className="group border-cc-card-border bg-cc-card-bg hover:border-cc-accent flex h-full min-h-[22rem] flex-col gap-6 rounded-2xl border p-6 no-underline backdrop-blur-sm transition-colors"
    >
      <div className="flex items-center justify-between">
        <StanceMarker v={stance.v} />
        <Eyebrow>v{stance.v} stance</Eyebrow>
      </div>
      <p className="text-cc-heading group-hover:text-cc-accent font-heading text-h5 font-semibold tracking-tight transition-colors">
        {stance.name}
      </p>
      <p className="text-cc-ink text-[0.95rem] leading-relaxed">
        {stance.tagline}
      </p>
      <div className="border-cc-card-border mt-auto flex items-center justify-between border-t pt-4">
        <span className="text-cc-ink-dim font-mono text-[0.66rem] tracking-tight">
          {stance.route}
        </span>
        <span className="text-cc-accent text-[0.82rem] font-medium">
          Open →
        </span>
      </div>
    </Link>
  );
}

/* -------------------------------------------------------------------------- */
/*  Page                                                                       */
/*  Layout stance: a wide ledger header above a three-column triptych. The     */
/*  three stances sit on the same baseline so the page reads as a comparison   */
/*  rather than a stack.                                                       */
/* -------------------------------------------------------------------------- */

export default function AboutPreviewChooserPage() {
  return (
    <div className="flex flex-col gap-16 py-6">
      <header className="border-cc-card-border grid gap-10 border-b pb-12 lg:grid-cols-[auto_1fr] lg:items-end lg:gap-16">
        <div>
          <Eyebrow>Internal · about page review</Eyebrow>
          <h1 className="font-heading text-h1 text-cc-heading mt-5 font-semibold tracking-tight">
            Nine{" "}
            <span
              className="bg-clip-text text-transparent"
              style={{ backgroundImage: SPECTRUM }}
            >
              stances
            </span>
            <br />
            on About.
          </h1>
        </div>
        <p className="text-cc-ink max-w-xl text-[1.05rem] leading-relaxed lg:justify-self-end">
          Each variation answers the same brief from a different angle. Open all
          nine, read them in sequence, and pick the stance that best fits how we
          want the company to read today.
        </p>
      </header>

      <section>
        <div className="flex flex-wrap items-baseline justify-between gap-x-6 gap-y-2">
          <Eyebrow>Variations</Eyebrow>
          <span className="text-cc-nav-label font-mono text-[0.66rem] tracking-tight">
            {STANCES.length} of {STANCES.length}
          </span>
        </div>
        <div className="mt-8 grid gap-5 md:grid-cols-3">
          {STANCES.map((stance) => (
            <StanceCard key={stance.v} stance={stance} />
          ))}
        </div>
      </section>

      <section className="border-cc-card-border flex flex-col items-center gap-7 border-t py-12 text-center">
        <h2 className="font-heading text-h4 text-cc-heading max-w-2xl font-semibold tracking-tight">
          Once a stance lands, the rest of the About page follows.
        </h2>
        <div className="flex flex-wrap items-center justify-center gap-3">
          <SolidButton href="/about/preview/v1">Start with v1</SolidButton>
          <OutlineButton href="/platform/preview">
            Back to platform chooser
          </OutlineButton>
        </div>
      </section>
    </div>
  );
}
