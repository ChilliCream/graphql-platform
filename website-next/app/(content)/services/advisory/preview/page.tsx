import type { Metadata } from "next";
import Link from "next/link";

import { OutlineButton, SolidButton } from "@/src/design-system/Button";

export const metadata: Metadata = {
  title: "Advisory Page Variations",
  description: "Preview variations for the ChilliCream Advisory page.",
  robots: { index: false, follow: false },
  keywords: [
    "advisory preview",
    "engagement tiers",
    "outcome patterns",
    "book a conversation",
    "internal review",
  ],
  openGraph: {
    title: "Advisory Page Variations",
    description: "Preview variations for the ChilliCream Advisory page.",
  },
};

/* -------------------------------------------------------------------------- */
/*  Variation manifest                                                         */
/*  Each target page is verified on disk at                                    */
/*  app/(content)/services/advisory/preview/v<n>/page.tsx. Any missing take    */
/*  renders a dashed placeholder slot rather than a dead link.                 */
/* -------------------------------------------------------------------------- */

interface Variation {
  readonly slug: "v1" | "v2" | "v3" | "v4" | "v5" | "v6" | "v7" | "v8" | "v9";
  readonly stance: string;
  readonly summary: string;
  readonly angle: string;
  readonly accent: string;
  readonly present: boolean;
}

const VARIATIONS: readonly Variation[] = [
  {
    slug: "v1",
    stance: "Engagement Tiers",
    summary:
      "A priced ladder of advisory packages, from a single architecture review up to an embedded retainer.",
    angle: "Sales-shaped",
    accent: "#16b9e4",
    present: true,
  },
  {
    slug: "v2",
    stance: "Outcome Patterns",
    summary:
      "Leads with the situations teams hire us into, then maps each one to the shape of advisory that fits.",
    angle: "Situation-shaped",
    accent: "#7c92c6",
    present: true,
  },
  {
    slug: "v3",
    stance: "Book a Conversation",
    summary:
      "Strips the page to a single human invitation, with the work scoped together once we are on a call.",
    angle: "Conversation-shaped",
    accent: "#f0786a",
    present: true,
  },
  {
    slug: "v4",
    stance: "Reference Sheet",
    summary:
      "A dense catalog of advisory engagements, laid out as a single reference sheet so the offer can be scanned at a glance.",
    angle: "Dense catalog",
    accent: "var(--cc-accent)",
    present: true,
  },
  {
    slug: "v5",
    stance: "The Ledger",
    summary:
      "A side-by-side ledger that pairs each advisory engagement with what we do, what you get, and when it fits.",
    angle: "Side by side",
    accent: "var(--cc-accent)",
    present: true,
  },
  {
    slug: "v6",
    stance: "Behind The Bar",
    summary:
      "Frames advisory as a barista counter, with each engagement served as an item on the menu and the workshop in plain view.",
    angle: "Barista",
    accent: "var(--cc-accent)",
    present: true,
  },
  {
    slug: "v7",
    stance: "First Hour, Live",
    summary:
      "Plays back the first hour of an advisory engagement as a live, scripted motion showcase so the work is visible before booking.",
    angle: "Motion showcase",
    accent: "var(--cc-accent)",
    present: true,
  },
  {
    slug: "v8",
    stance: "Single Stroke Advisory",
    summary:
      "Draws the whole advisory path in one continuous line, so the arc from first call to handover reads as a single unbroken stroke.",
    angle: "Path draw once",
    accent: "var(--cc-accent)",
    present: true,
  },
  {
    slug: "v9",
    stance: "Dictated, Not Drafted",
    summary:
      "Types the advisory brief onto the hero as if spoken aloud, framing the offer as a dictated note rather than a polished pitch.",
    angle: "Type on hero",
    accent: "var(--cc-accent)",
    present: true,
  },
];

/* -------------------------------------------------------------------------- */
/*  Brand spectrum: one gradient event per screen                              */
/* -------------------------------------------------------------------------- */

const SPECTRUM = "linear-gradient(100deg, #16b9e4, #7c92c6, #f0786a)";

/* -------------------------------------------------------------------------- */
/*  Page                                                                       */
/*  Layout stance: a left-rail header sits beside a vertical timeline of the   */
/*  stances. Each variation is one rung; the rail itself carries the           */
/*  spectrum so the page reads as a single comparison strip rather than a set  */
/*  of detached cards.                                                         */
/* -------------------------------------------------------------------------- */

export default function AdvisoryPreviewIndexPage() {
  const presentCount = VARIATIONS.filter((v) => v.present).length;

  return (
    <div className="flex flex-col gap-16 py-6">
      <header className="grid gap-10 lg:grid-cols-[minmax(0,5fr)_minmax(0,4fr)] lg:items-end">
        <div>
          <p className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.22em] uppercase">
            Services / Advisory / Preview
          </p>
          <h1 className="font-heading text-cc-heading text-hero mt-5 font-semibold tracking-tight">
            Nine ways to frame{" "}
            <span
              className="bg-clip-text text-transparent"
              style={{ backgroundImage: SPECTRUM }}
            >
              advisory
            </span>
            .
          </h1>
          <p className="lead text-cc-ink mt-6 max-w-xl">
            Internal preview, not indexed. Each variation answers the same brief
            (what advisory is, how to engage) from a different stance. Read them
            in order, then pick the one that matches the conversation we want
            visitors to have.
          </p>
        </div>
        <dl className="border-cc-card-border bg-cc-card-bg/60 grid grid-cols-3 gap-4 rounded-2xl border p-5 backdrop-blur-sm lg:justify-self-end">
          <ManifestStat label="variations" value={String(VARIATIONS.length)} />
          <ManifestStat label="ready" value={String(presentCount)} />
          <ManifestStat label="status" value="draft" />
        </dl>
      </header>

      <section
        aria-labelledby="variations-heading"
        className="relative grid gap-10 lg:grid-cols-[14rem_minmax(0,1fr)]"
      >
        <aside className="lg:sticky lg:top-24 lg:self-start">
          <h2
            id="variations-heading"
            className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.22em] uppercase"
          >
            Variations
          </h2>
          <ol className="text-cc-ink-dim mt-4 flex flex-col gap-2 font-mono text-[0.78rem]">
            {VARIATIONS.map((variation) => (
              <li key={variation.slug} className="flex items-center gap-3">
                <span
                  aria-hidden="true"
                  className="h-2 w-2 rounded-full"
                  style={{ background: variation.accent }}
                />
                <span className="text-cc-ink">
                  {variation.slug} · {variation.stance}
                </span>
              </li>
            ))}
          </ol>
          <p className="text-cc-ink-dim mt-6 max-w-[14rem] text-[0.85rem] leading-relaxed">
            The rail uses the brand spectrum once, top to bottom, to mark this
            as a single comparison rather than three pages.
          </p>
        </aside>

        <ol className="relative flex flex-col">
          <span
            aria-hidden="true"
            className="absolute top-2 bottom-2 left-[1.125rem] w-px opacity-70"
            style={{
              background:
                "linear-gradient(180deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)",
            }}
          />
          {VARIATIONS.map((variation, index) => (
            <li key={variation.slug} className="relative pl-12">
              <VariationCard variation={variation} index={index} />
            </li>
          ))}
        </ol>
      </section>

      <section className="border-cc-card-border flex flex-col items-center gap-6 border-t pt-12 text-center">
        <h2 className="font-heading text-h4 text-cc-heading max-w-2xl font-semibold tracking-tight">
          Pick a stance, then we wire the rest of the advisory surface to match.
        </h2>
        <div className="flex flex-wrap items-center justify-center gap-3">
          <SolidButton href="/services/advisory/preview/v1">
            Start with v1
          </SolidButton>
          <OutlineButton href="/services/advisory">
            Back to current advisory page
          </OutlineButton>
        </div>
      </section>
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  Manifest stat                                                              */
/* -------------------------------------------------------------------------- */

interface ManifestStatProps {
  readonly label: string;
  readonly value: string;
}

function ManifestStat({ label, value }: ManifestStatProps) {
  return (
    <div className="flex flex-col gap-1">
      <dt className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.18em] uppercase">
        {label}
      </dt>
      <dd className="text-cc-heading font-heading text-h5 font-semibold tabular-nums">
        {value}
      </dd>
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  Variation card                                                             */
/*  Sits to the right of the spectrum rail. The numbered node on the rail is   */
/*  rendered absolutely so the card itself stays a clean rectangle.            */
/* -------------------------------------------------------------------------- */

interface VariationCardProps {
  readonly variation: Variation;
  readonly index: number;
}

function VariationCard({ variation, index }: VariationCardProps) {
  const order = String(index + 1).padStart(2, "0");

  if (!variation.present) {
    return (
      <div className="my-3">
        <RailNode accent={variation.accent} muted />
        <div className="border-cc-card-border bg-cc-card-bg/40 grid gap-3 rounded-2xl border border-dashed p-6">
          <div className="flex items-center justify-between">
            <p className="text-cc-nav-label font-mono text-[0.62rem] tracking-[0.22em] uppercase">
              {order} · {variation.angle}
            </p>
            <span className="text-cc-nav-label font-mono text-[0.62rem] tracking-[0.18em] uppercase">
              Not generated
            </span>
          </div>
          <p className="font-heading text-cc-ink-dim text-h4 font-semibold tracking-tight">
            {variation.stance}
          </p>
          <p className="text-cc-ink-dim text-[0.95rem] leading-relaxed">
            {variation.summary}
          </p>
        </div>
      </div>
    );
  }

  return (
    <Link
      href={`/services/advisory/preview/${variation.slug}`}
      className="group border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover my-3 block rounded-2xl border p-6 no-underline transition-colors md:p-7"
    >
      <RailNode accent={variation.accent} slug={variation.slug} />
      <div className="grid gap-5 md:grid-cols-[minmax(0,1fr)_auto] md:items-start md:gap-8">
        <div>
          <div className="flex flex-wrap items-center gap-3">
            <p className="text-cc-nav-label font-mono text-[0.62rem] tracking-[0.22em] uppercase">
              {order} · {variation.angle}
            </p>
            <span aria-hidden="true" className="bg-cc-card-border h-px w-8" />
            <span
              className="font-mono text-[0.62rem] tracking-[0.18em] uppercase"
              style={{ color: variation.accent }}
            >
              {variation.slug}
            </span>
          </div>
          <h3 className="font-heading text-cc-heading group-hover:text-cc-accent text-h3 mt-4 font-semibold tracking-tight transition-colors">
            {variation.stance}
          </h3>
          <p className="text-cc-ink mt-3 max-w-2xl text-[0.98rem] leading-relaxed">
            {variation.summary}
          </p>
          <p className="text-cc-ink-dim mt-4 font-mono text-[0.66rem] tracking-tight">
            /services/advisory/preview/{variation.slug}
          </p>
        </div>
        <span className="text-cc-accent group-hover:text-cc-heading inline-flex items-center gap-2 font-mono text-[0.7rem] tracking-[0.18em] uppercase transition-colors md:self-end">
          Open variation
          <ArrowGlyph />
        </span>
      </div>
    </Link>
  );
}

/* -------------------------------------------------------------------------- */
/*  Rail node                                                                  */
/*  Numbered coin pinned over the spectrum line. Two visual states: live       */
/*  (accent ring) and muted (dashed) for placeholders.                         */
/* -------------------------------------------------------------------------- */

interface RailNodeProps {
  readonly accent: string;
  readonly slug?: string;
  readonly muted?: boolean;
}

function RailNode({ accent, slug, muted = false }: RailNodeProps) {
  const baseClass =
    "absolute top-7 left-0 flex h-9 w-9 -translate-x-[0.125rem] items-center justify-center rounded-full border font-mono text-[0.7rem] font-semibold tabular-nums";

  if (muted) {
    return (
      <span
        aria-hidden="true"
        className={`${baseClass} border-cc-card-border bg-cc-bg text-cc-ink-dim border-dashed`}
      >
        ·
      </span>
    );
  }

  return (
    <span
      aria-hidden="true"
      className={`${baseClass} border-cc-card-border bg-cc-bg text-cc-heading`}
      style={{ boxShadow: `inset 0 0 0 1px ${accent}55` }}
    >
      {slug?.toUpperCase()}
    </span>
  );
}

/* -------------------------------------------------------------------------- */
/*  Arrow glyph                                                                */
/* -------------------------------------------------------------------------- */

function ArrowGlyph() {
  return (
    <svg aria-hidden="true" viewBox="0 0 16 8" className="h-2 w-4" fill="none">
      <path
        d="M1 4 H14 M10 1 L14 4 L10 7"
        stroke="currentColor"
        strokeWidth="1"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}
