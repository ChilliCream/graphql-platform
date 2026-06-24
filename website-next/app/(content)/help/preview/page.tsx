import type { Metadata } from "next";
import NextLink from "next/link";

export const metadata: Metadata = {
  title: "Help Page Variations",
  description: "Preview variations for the ChilliCream Help page.",
  robots: { index: false, follow: false },
};

/* -------------------------------------------------------------------------- */
/*  Manifest                                                                   */
/*  Each variation is verified to exist under                                  */
/*  app/(content)/help/preview/v<n>/page.tsx. `present` records that check;    */
/*  a missing variation renders a disabled placeholder card.                   */
/* -------------------------------------------------------------------------- */

interface Variation {
  readonly slug: "v1" | "v2" | "v3" | "v4" | "v5" | "v6" | "v7" | "v8" | "v9";
  readonly stance: string;
  readonly summary: string;
  readonly posture: string;
  readonly accent: string;
  readonly present: boolean;
}

const VARIATIONS: readonly Variation[] = [
  {
    slug: "v1",
    stance: "Tiered Resource Hub",
    summary:
      "Help framed as a tiered shelf, from community to paid support, each layer earning the next.",
    posture: "Layered",
    accent: "#16b9e4",
    present: true,
  },
  {
    slug: "v2",
    stance: "Self-Serve First",
    summary:
      "Help that hands the keys to the reader first, with search, docs, and recipes before any contact form.",
    posture: "Hands-on",
    accent: "#7c92c6",
    present: true,
  },
  {
    slug: "v3",
    stance: "Decision-Tree / Quick Paths",
    summary:
      "Help as a routing surface, answering one question (what kind of help) and dispatching to the right path.",
    posture: "Routing",
    accent: "#f0786a",
    present: true,
  },
  {
    slug: "v4",
    stance: "The Compass",
    summary:
      "Help led by a visual hero, orienting readers with a single bearing before any path or list appears.",
    posture: "Visual hero",
    accent: "#22d3ee",
    present: true,
  },
  {
    slug: "v5",
    stance: "The Long Read",
    summary:
      "Help as a centered narrative, one calm column that walks the reader through every option in sequence.",
    posture: "Centered narrative",
    accent: "#5eead4",
    present: true,
  },
  {
    slug: "v6",
    stance: "The Help Bar",
    summary:
      "Help served at a bar, three brews lined up by depth (community, consultancy, plan) so the reader picks a pour.",
    posture: "Barista bar",
    accent: "#5eead4",
    present: true,
  },
  {
    slug: "v7",
    stance: "Routing Tree",
    summary:
      "Help as a live routing tree, a question travels down branches and settles on the tier that fits its urgency.",
    posture: "Motion showcase",
    accent: "#5eead4",
    present: true,
  },
  {
    slug: "v8",
    stance: "The Triage Schematic",
    summary:
      "Help drawn as an annotated diagram, each support tier labeled in place so the reader reads the whole map before choosing.",
    posture: "Annotated diagram",
    accent: "#5eead4",
    present: true,
  },
  {
    slug: "v9",
    stance: "Help Console",
    summary:
      "Help typed out as terminal prose, prompts and responses scrolling past so the reader follows the answer like a session log.",
    posture: "Terminal prose",
    accent: "#5eead4",
    present: true,
  },
];

/* -------------------------------------------------------------------------- */
/*  Brand spectrum: the single gradient event on this screen                   */
/* -------------------------------------------------------------------------- */

const SPECTRUM = "linear-gradient(100deg, #16b9e4, #7c92c6, #f0786a)";

/* -------------------------------------------------------------------------- */
/*  Page                                                                       */
/* -------------------------------------------------------------------------- */

export default function HelpPreviewIndexPage() {
  return (
    <div className="py-6">
      <header className="grid gap-8 md:grid-cols-[auto_minmax(0,1fr)] md:items-start md:gap-12">
        <div className="flex items-center gap-4 md:flex-col md:items-start md:gap-3">
          <span className="border-cc-card-border bg-cc-surface text-cc-nav-label inline-flex items-center gap-2 rounded-full border px-3 py-1 font-mono text-[0.62rem] tracking-[0.22em] uppercase">
            <span
              aria-hidden="true"
              className="inline-block h-1.5 w-1.5 rounded-full"
              style={{ background: SPECTRUM }}
            />
            Help / Preview
          </span>
          <span className="text-cc-ink-dim font-mono text-[0.66rem] tracking-[0.18em] uppercase">
            09 stances
          </span>
        </div>
        <div>
          <h1 className="font-heading text-cc-heading text-hero font-semibold tracking-tight">
            Pick the posture for the{" "}
            <span
              className="bg-clip-text text-transparent"
              style={{ backgroundImage: SPECTRUM }}
            >
              help
            </span>{" "}
            page.
          </h1>
          <p className="lead text-cc-ink mt-6 max-w-2xl">
            Internal preview, not indexed. Same material, nine stances. Each
            card opens a fully composed variation so you can read it in context.
          </p>
        </div>
      </header>

      <section
        aria-label="Variations"
        className="mt-14 grid gap-6 md:grid-cols-3"
      >
        {VARIATIONS.map((variation, index) => (
          <VariationCard
            key={variation.slug}
            variation={variation}
            index={index}
            total={VARIATIONS.length}
          />
        ))}
      </section>

      <footer className="border-cc-card-border mt-14 flex flex-wrap items-center justify-between gap-3 border-t pt-6">
        <span className="text-cc-nav-label font-mono text-[0.62rem] tracking-[0.22em] uppercase">
          Noindex, follow disabled
        </span>
        <NextLink
          href="/help"
          className="text-cc-accent hover:text-cc-heading font-mono text-[0.7rem] tracking-[0.18em] uppercase no-underline transition-colors"
        >
          Current /help
        </NextLink>
      </footer>
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  Variation card                                                             */
/* -------------------------------------------------------------------------- */

interface VariationCardProps {
  readonly variation: Variation;
  readonly index: number;
  readonly total: number;
}

function VariationCard({ variation, index, total }: VariationCardProps) {
  const counter = `${String(index + 1).padStart(2, "0")} / ${String(total).padStart(2, "0")}`;

  if (!variation.present) {
    return (
      <div className="border-cc-card-border bg-cc-card-bg/40 relative flex h-full flex-col gap-5 rounded-2xl border border-dashed p-6">
        <div className="flex items-center justify-between">
          <span className="border-cc-card-border text-cc-ink-dim inline-flex h-9 w-9 items-center justify-center rounded-full border border-dashed font-mono text-[0.72rem] font-semibold tabular-nums">
            {variation.slug.toUpperCase()}
          </span>
          <span className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.22em] uppercase">
            {counter}
          </span>
        </div>
        <div>
          <p className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.22em] uppercase">
            {variation.posture}
          </p>
          <h2 className="font-heading text-cc-ink-dim text-h4 mt-2 font-semibold tracking-tight">
            {variation.stance}
          </h2>
          <p className="text-cc-ink-dim mt-3 text-[0.92rem] leading-relaxed">
            {variation.summary}
          </p>
        </div>
        <span className="text-cc-nav-label mt-auto font-mono text-[0.62rem] tracking-[0.22em] uppercase">
          Not generated
        </span>
      </div>
    );
  }

  return (
    <NextLink
      href={`/help/preview/${variation.slug}`}
      className="group border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover relative flex h-full flex-col gap-5 overflow-hidden rounded-2xl border p-6 no-underline transition-colors"
    >
      <span
        aria-hidden="true"
        className="absolute inset-x-0 top-0 h-px"
        style={{
          background: `linear-gradient(90deg, transparent, ${variation.accent}, transparent)`,
        }}
      />
      <div className="flex items-center justify-between">
        <span
          className="border-cc-card-border bg-cc-surface text-cc-heading inline-flex h-9 w-9 items-center justify-center rounded-full border font-mono text-[0.72rem] font-semibold tabular-nums"
          style={{ boxShadow: `inset 0 0 0 1px ${variation.accent}33` }}
        >
          {variation.slug.toUpperCase()}
        </span>
        <span className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.22em] uppercase">
          {counter}
        </span>
      </div>
      <div>
        <p
          className="font-mono text-[0.6rem] tracking-[0.22em] uppercase"
          style={{ color: variation.accent }}
        >
          {variation.posture}
        </p>
        <h2 className="font-heading text-cc-heading group-hover:text-cc-accent text-h4 mt-2 font-semibold tracking-tight transition-colors">
          {variation.stance}
        </h2>
        <p className="text-cc-ink mt-3 text-[0.92rem] leading-relaxed">
          {variation.summary}
        </p>
      </div>
      <div className="mt-auto flex items-center justify-between gap-3">
        <span className="text-cc-ink-dim font-mono text-[0.64rem] tracking-tight">
          /help/preview/{variation.slug}
        </span>
        <span className="text-cc-accent group-hover:text-cc-heading inline-flex items-center gap-2 font-mono text-[0.66rem] tracking-[0.18em] uppercase transition-colors">
          Open
          <ArrowGlyph />
        </span>
      </div>
    </NextLink>
  );
}

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
