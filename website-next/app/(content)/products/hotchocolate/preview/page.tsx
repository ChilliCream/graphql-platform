import type { Metadata } from "next";
import NextLink from "next/link";

export const metadata: Metadata = {
  title: "Hot Chocolate Page Variations",
  description:
    "Preview variations for the ChilliCream Hot Chocolate product page.",
  robots: { index: false, follow: false },
};

/* -------------------------------------------------------------------------- */
/*  Manifest                                                                   */
/*  Each variation is verified to exist under                                  */
/*  app/(content)/products/hotchocolate/preview/v<n>/page.tsx. `present`       */
/*  records that check; a missing variation renders a disabled placeholder.    */
/* -------------------------------------------------------------------------- */

interface Variation {
  readonly slug: "v1" | "v2" | "v3" | "v4" | "v5" | "v6" | "v7" | "v8" | "v9";
  readonly stance: string;
  readonly tagline: string;
  readonly summary: string;
  readonly angle: string;
  readonly accent: string;
  readonly present: boolean;
}

const VARIATIONS: readonly Variation[] = [
  {
    slug: "v1",
    stance: "Code-First Feature Catalogue",
    tagline: "Everything Hot Chocolate ships, laid out as a working surface.",
    summary:
      "A reference shelf of features, attributes, and APIs for the .NET developer who wants to scan the box before they pick it up.",
    angle: "Catalogue",
    accent: "#16b9e4",
    present: true,
  },
  {
    slug: "v2",
    stance: "Story-Led Build Loop",
    tagline: "Walk the build, see Hot Chocolate move under the request.",
    summary:
      "The page reads like a session at the keyboard, schema first, then resolvers, then the loop tightening into production posture.",
    angle: "Narrative",
    accent: "#7c92c6",
    present: true,
  },
  {
    slug: "v3",
    stance: "Why-.NET Comparison",
    tagline: "Where a .NET GraphQL stack changes the math.",
    summary:
      "A side-by-side argument for choosing Hot Chocolate on .NET, framed against the trade-offs of other GraphQL ecosystems.",
    angle: "Comparison",
    accent: "#f0786a",
    present: true,
  },
  {
    slug: "v4",
    stance: "Reference Manual",
    tagline: "Hot Chocolate laid out as a dense, scannable manual.",
    summary:
      "A reference-manual surface, tight rows of capabilities, attributes, and APIs packed for the reader who wants the whole catalogue at a glance.",
    angle: "Dense catalog",
    accent: "#5eead4",
    present: true,
  },
  {
    slug: "v5",
    stance: "The Honest Diff",
    tagline: "A side-by-side look at where Hot Chocolate lands vs. the rest.",
    summary:
      "An honest side-by-side, Hot Chocolate against the alternatives row for row, with the trade-offs shown plainly instead of papered over.",
    angle: "Side by side",
    accent: "#5eead4",
    present: true,
  },
  {
    slug: "v6",
    stance: "House Blend",
    tagline: "Hot Chocolate served the way the house pours it.",
    summary:
      "A barista-style take, the surface is poured slowly, each capability framed as part of the daily blend the team actually ships with.",
    angle: "Barista",
    accent: "#5eead4",
    present: true,
  },
  {
    slug: "v7",
    stance: "Compiler Pulse",
    tagline: "Hot Chocolate as a live pulse, the schema moving as you read.",
    summary:
      "A motion-led showcase, the page reads like the compiler at work, types flowing into resolvers, requests pulsing through the runtime.",
    angle: "Motion showcase",
    accent: "#5eead4",
    present: true,
  },
  {
    slug: "v8",
    stance: "The Hot Chocolate Funnies",
    tagline: "Hot Chocolate told as a comic strip, panel by panel.",
    summary:
      "A comic-strip surface, each capability lands as its own panel with speech bubbles and gutters, the build story read like a Sunday strip.",
    angle: "Comic strip",
    accent: "var(--color-cc-accent)",
    present: true,
  },
  {
    slug: "v9",
    stance: "The Field Journal",
    tagline: "Hot Chocolate kept as a working field journal.",
    summary:
      "A field-journal surface, observations, sketches, and margin notes logged by hand as the schema and runtime are charted entry by entry.",
    angle: "Field journal",
    accent: "var(--color-cc-accent)",
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

export default function HotChocolatePreviewIndexPage() {
  return (
    <div className="py-6">
      <header className="relative">
        <p className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.22em] uppercase">
          Products / Hot Chocolate / Preview
        </p>
        <h1 className="font-heading text-cc-heading text-hero mt-4 max-w-3xl font-semibold tracking-tight">
          Nine stances on the same{" "}
          <span
            className="bg-clip-text text-transparent"
            style={{ backgroundImage: SPECTRUM }}
          >
            Hot Chocolate
          </span>{" "}
          product page.
        </h1>
        <p className="lead text-cc-ink mt-6 max-w-2xl">
          Internal preview, not indexed. Each variation reframes the same
          surface around a different reader, the cataloguer, the builder, the
          architect comparing platforms, the manual reader, the honest
          comparator, the barista pouring the house blend, the motion showcase
          pulsing with the compiler, the comic strip read panel by panel, and
          the field journal logged entry by entry.
        </p>

        <dl className="text-cc-ink-dim mt-10 grid gap-4 font-mono text-[0.72rem] tracking-tight sm:grid-cols-3">
          <div className="border-cc-card-border bg-cc-card-bg/40 rounded-lg border p-3">
            <dt className="text-cc-nav-label text-[0.6rem] tracking-[0.22em] uppercase">
              Track
            </dt>
            <dd className="text-cc-heading mt-1">Hot Chocolate</dd>
          </div>
          <div className="border-cc-card-border bg-cc-card-bg/40 rounded-lg border p-3">
            <dt className="text-cc-nav-label text-[0.6rem] tracking-[0.22em] uppercase">
              Variations
            </dt>
            <dd className="text-cc-heading mt-1 tabular-nums">
              {VARIATIONS.length.toString().padStart(2, "0")}
            </dd>
          </div>
          <div className="border-cc-card-border bg-cc-card-bg/40 rounded-lg border p-3">
            <dt className="text-cc-nav-label text-[0.6rem] tracking-[0.22em] uppercase">
              Status
            </dt>
            <dd className="text-cc-success mt-1">noindex, preview only</dd>
          </div>
        </dl>
      </header>

      <div
        aria-hidden="true"
        className="mt-12 h-px w-full"
        style={{
          background:
            "linear-gradient(90deg, transparent, rgba(94,234,212,0.35), transparent)",
        }}
      />

      <section
        aria-label="Hot Chocolate page variations"
        className="mt-12 grid gap-6 md:grid-cols-2 lg:grid-cols-3"
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

      <footer className="text-cc-ink-dim mt-14 flex flex-wrap items-center justify-between gap-3 font-mono text-[0.65rem] tracking-[0.18em] uppercase">
        <span>Hot Chocolate / Page Lab</span>
        <span>{VARIATIONS.length} variations live</span>
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
      <div className="border-cc-card-border bg-cc-card-bg/40 flex flex-col overflow-hidden rounded-2xl border border-dashed p-6">
        <div className="flex items-center justify-between">
          <span className="border-cc-card-border text-cc-nav-label inline-flex h-9 items-center rounded-full border border-dashed px-3 font-mono text-[0.7rem] tabular-nums">
            {variation.slug.toUpperCase()}
          </span>
          <span className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.22em] uppercase">
            {counter}
          </span>
        </div>
        <h2 className="font-heading text-cc-ink-dim text-h4 mt-6 font-semibold tracking-tight">
          {variation.stance}
        </h2>
        <p className="text-cc-ink-dim mt-3 text-[0.9rem] leading-relaxed">
          {variation.summary}
        </p>
        <span className="text-cc-nav-label mt-auto pt-6 font-mono text-[0.65rem] tracking-[0.18em] uppercase">
          Not generated
        </span>
      </div>
    );
  }

  return (
    <NextLink
      href={`/products/hotchocolate/preview/${variation.slug}`}
      className="group border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover relative flex flex-col overflow-hidden rounded-2xl border p-6 no-underline transition-colors"
    >
      <span
        aria-hidden="true"
        className="absolute inset-x-0 top-0 h-px opacity-70"
        style={{
          background: `linear-gradient(90deg, transparent, ${variation.accent}, transparent)`,
        }}
      />
      <div className="flex items-center justify-between">
        <span
          className="border-cc-card-border bg-cc-surface text-cc-heading inline-flex h-9 items-center rounded-full border px-3 font-mono text-[0.7rem] font-semibold tabular-nums"
          style={{ boxShadow: `inset 0 0 0 1px ${variation.accent}33` }}
        >
          {variation.slug.toUpperCase()}
        </span>
        <span className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.22em] uppercase">
          {counter}
        </span>
      </div>

      <VariationGlyph slug={variation.slug} accent={variation.accent} />

      <p
        className="text-cc-nav-label mt-1 font-mono text-[0.62rem] tracking-[0.22em] uppercase"
        style={{ color: variation.accent }}
      >
        {variation.angle}
      </p>
      <h2 className="font-heading text-cc-heading group-hover:text-cc-accent text-h4 mt-3 font-semibold tracking-tight transition-colors">
        {variation.stance}
      </h2>
      <p className="text-cc-ink mt-3 text-[0.95rem] leading-relaxed">
        {variation.tagline}
      </p>
      <p className="text-cc-ink-dim mt-3 text-[0.85rem] leading-relaxed">
        {variation.summary}
      </p>

      <div className="border-cc-card-border mt-6 flex items-center justify-between border-t pt-4">
        <span className="text-cc-ink-dim font-mono text-[0.66rem] tracking-tight">
          /products/hotchocolate/preview/{variation.slug}
        </span>
        <span className="text-cc-accent group-hover:text-cc-heading inline-flex items-center gap-2 font-mono text-[0.7rem] tracking-[0.18em] uppercase transition-colors">
          Open
          <ArrowGlyph />
        </span>
      </div>
    </NextLink>
  );
}

/* -------------------------------------------------------------------------- */
/*  Glyphs                                                                     */
/* -------------------------------------------------------------------------- */

interface VariationGlyphProps {
  readonly slug: Variation["slug"];
  readonly accent: string;
}

function VariationGlyph({ slug, accent }: VariationGlyphProps) {
  if (slug === "v1") {
    // Catalogue: a stacked grid of feature cells.
    return (
      <svg
        aria-hidden="true"
        viewBox="0 0 64 40"
        className="mt-6 h-10 w-16"
        fill="none"
      >
        <rect
          x="1"
          y="1"
          width="18"
          height="11"
          rx="2"
          stroke={accent}
          strokeOpacity="0.6"
        />
        <rect
          x="23"
          y="1"
          width="18"
          height="11"
          rx="2"
          stroke={accent}
          strokeOpacity="0.6"
        />
        <rect
          x="45"
          y="1"
          width="18"
          height="11"
          rx="2"
          stroke={accent}
          strokeOpacity="0.6"
        />
        <rect
          x="1"
          y="15"
          width="18"
          height="11"
          rx="2"
          stroke={accent}
          strokeWidth="1.4"
        />
        <rect
          x="23"
          y="15"
          width="18"
          height="11"
          rx="2"
          stroke={accent}
          strokeOpacity="0.6"
        />
        <rect
          x="45"
          y="15"
          width="18"
          height="11"
          rx="2"
          stroke={accent}
          strokeOpacity="0.6"
        />
        <rect
          x="1"
          y="29"
          width="18"
          height="10"
          rx="2"
          stroke={accent}
          strokeOpacity="0.6"
        />
        <rect
          x="23"
          y="29"
          width="18"
          height="10"
          rx="2"
          stroke={accent}
          strokeOpacity="0.6"
        />
        <rect
          x="45"
          y="29"
          width="18"
          height="10"
          rx="2"
          stroke={accent}
          strokeOpacity="0.6"
        />
      </svg>
    );
  }

  if (slug === "v2") {
    // Story: a build loop, arcing line with waypoints.
    return (
      <svg
        aria-hidden="true"
        viewBox="0 0 64 40"
        className="mt-6 h-10 w-16"
        fill="none"
      >
        <path
          d="M3 30 C 12 6, 28 6, 32 20 C 36 34, 52 34, 61 12"
          stroke={accent}
          strokeWidth="1.4"
          strokeLinecap="round"
        />
        <circle cx="3" cy="30" r="2.5" fill={accent} />
        <circle
          cx="32"
          cy="20"
          r="2.5"
          stroke={accent}
          strokeWidth="1.2"
          fill="none"
        />
        <circle cx="61" cy="12" r="2.5" fill={accent} />
      </svg>
    );
  }

  if (slug === "v3") {
    // Comparison: two stacks side by side with a divider.
    return (
      <svg
        aria-hidden="true"
        viewBox="0 0 64 40"
        className="mt-6 h-10 w-16"
        fill="none"
      >
        <line
          x1="32"
          y1="2"
          x2="32"
          y2="38"
          stroke={accent}
          strokeOpacity="0.4"
          strokeDasharray="2 3"
        />
        <rect
          x="4"
          y="6"
          width="22"
          height="5"
          rx="1"
          stroke={accent}
          strokeWidth="1.3"
        />
        <rect
          x="4"
          y="15"
          width="22"
          height="5"
          rx="1"
          stroke={accent}
          strokeOpacity="0.5"
        />
        <rect
          x="4"
          y="24"
          width="22"
          height="5"
          rx="1"
          stroke={accent}
          strokeOpacity="0.5"
        />
        <rect
          x="38"
          y="6"
          width="22"
          height="5"
          rx="1"
          stroke={accent}
          strokeOpacity="0.5"
        />
        <rect
          x="38"
          y="15"
          width="14"
          height="5"
          rx="1"
          stroke={accent}
          strokeOpacity="0.5"
        />
        <rect
          x="38"
          y="24"
          width="18"
          height="5"
          rx="1"
          stroke={accent}
          strokeOpacity="0.5"
        />
      </svg>
    );
  }

  if (slug === "v4") {
    // Brutalist spec sheet: hard rules, a fixed-width data row, a unit block.
    return (
      <svg
        aria-hidden="true"
        viewBox="0 0 64 40"
        className="mt-6 h-10 w-16"
        fill="none"
      >
        <rect
          x="1.5"
          y="1.5"
          width="61"
          height="37"
          stroke={accent}
          strokeWidth="1.4"
        />
        <line
          x1="1.5"
          y1="12"
          x2="62.5"
          y2="12"
          stroke={accent}
          strokeWidth="1.2"
        />
        <line
          x1="1.5"
          y1="26"
          x2="62.5"
          y2="26"
          stroke={accent}
          strokeOpacity="0.6"
        />
        <line
          x1="42"
          y1="12"
          x2="42"
          y2="38.5"
          stroke={accent}
          strokeOpacity="0.6"
        />
        <rect x="5" y="5" width="10" height="3" fill={accent} />
        <rect
          x="5"
          y="16"
          width="22"
          height="2"
          fill={accent}
          fillOpacity="0.7"
        />
        <rect
          x="5"
          y="20"
          width="18"
          height="2"
          fill={accent}
          fillOpacity="0.5"
        />
        <rect
          x="5"
          y="30"
          width="26"
          height="2"
          fill={accent}
          fillOpacity="0.5"
        />
        <rect x="46" y="16" width="12" height="6" fill={accent} />
        <rect
          x="46"
          y="30"
          width="12"
          height="2"
          fill={accent}
          fillOpacity="0.6"
        />
      </svg>
    );
  }

  if (slug === "v5") {
    // Field notebook: hand-drawn arrow, a margin tick, a small doodle.
    return (
      <svg
        aria-hidden="true"
        viewBox="0 0 64 40"
        className="mt-6 h-10 w-16"
        fill="none"
      >
        <line
          x1="10"
          y1="3"
          x2="10"
          y2="38"
          stroke={accent}
          strokeOpacity="0.45"
          strokeDasharray="1 3"
        />
        <path
          d="M14 30 Q 22 8, 34 18 T 58 10"
          stroke={accent}
          strokeWidth="1.3"
          strokeLinecap="round"
        />
        <path
          d="M54 7 L 58 10 L 55 14"
          stroke={accent}
          strokeWidth="1.3"
          strokeLinecap="round"
          strokeLinejoin="round"
        />
        <path
          d="M16 34 q 3 -2 6 0 t 6 0 t 6 0"
          stroke={accent}
          strokeOpacity="0.55"
          strokeWidth="1"
          strokeLinecap="round"
        />
        <circle
          cx="46"
          cy="28"
          r="3.5"
          stroke={accent}
          strokeOpacity="0.65"
          strokeWidth="1.1"
        />
        <path
          d="M44 26.5 q 2 2 4 0"
          stroke={accent}
          strokeOpacity="0.65"
          strokeWidth="1"
          strokeLinecap="round"
        />
      </svg>
    );
  }

  if (slug === "v6") {
    // House blend: a cup with rising steam, the barista pour.
    return (
      <svg
        aria-hidden="true"
        viewBox="0 0 64 40"
        className="mt-6 h-10 w-16"
        fill="none"
      >
        <path
          d="M16 18 H44 V30 Q44 36, 38 36 H22 Q16 36, 16 30 Z"
          stroke={accent}
          strokeWidth="1.4"
          strokeLinejoin="round"
        />
        <path
          d="M44 21 Q52 22, 52 27 Q52 32, 44 33"
          stroke={accent}
          strokeOpacity="0.6"
          strokeWidth="1.2"
        />
        <path
          d="M22 13 q 2 -4, 0 -8"
          stroke={accent}
          strokeOpacity="0.65"
          strokeWidth="1.1"
          strokeLinecap="round"
        />
        <path
          d="M30 13 q 2 -4, 0 -8"
          stroke={accent}
          strokeOpacity="0.65"
          strokeWidth="1.1"
          strokeLinecap="round"
        />
        <path
          d="M38 13 q 2 -4, 0 -8"
          stroke={accent}
          strokeOpacity="0.65"
          strokeWidth="1.1"
          strokeLinecap="round"
        />
        <line
          x1="20"
          y1="25"
          x2="40"
          y2="25"
          stroke={accent}
          strokeOpacity="0.45"
        />
      </svg>
    );
  }

  if (slug === "v7") {
    // Compiler pulse: a waveform pulse with stacked traces.
    return (
      <svg
        aria-hidden="true"
        viewBox="0 0 64 40"
        className="mt-6 h-10 w-16"
        fill="none"
      >
        <path
          d="M2 20 H14 L18 8 L24 32 L30 14 L36 26 L42 18 H62"
          stroke={accent}
          strokeWidth="1.4"
          strokeLinecap="round"
          strokeLinejoin="round"
        />
        <path
          d="M2 30 H22 L26 24 L32 36 L38 28 H62"
          stroke={accent}
          strokeOpacity="0.45"
          strokeWidth="1.1"
          strokeLinecap="round"
          strokeLinejoin="round"
        />
        <circle cx="24" cy="32" r="2" fill={accent} />
        <circle
          cx="42"
          cy="18"
          r="2"
          stroke={accent}
          strokeWidth="1.1"
          fill="none"
        />
      </svg>
    );
  }

  if (slug === "v8") {
    // Comic strip: three gutter-separated panels, one with a speech bubble.
    return (
      <svg
        aria-hidden="true"
        viewBox="0 0 64 40"
        className="mt-6 h-10 w-16"
        fill="none"
      >
        <rect
          x="1.5"
          y="4"
          width="18"
          height="32"
          rx="1.5"
          stroke={accent}
          strokeWidth="1.3"
        />
        <rect
          x="23"
          y="4"
          width="18"
          height="32"
          rx="1.5"
          stroke={accent}
          strokeWidth="1.3"
        />
        <rect
          x="44.5"
          y="4"
          width="18"
          height="32"
          rx="1.5"
          stroke={accent}
          strokeWidth="1.3"
        />
        <path
          d="M5 11 H16 Q17 11, 17 13 V18 Q17 20, 15 20 H10 L7 23 V20 H6 Q5 20, 5 18 Z"
          stroke={accent}
          strokeOpacity="0.6"
          strokeWidth="1.1"
          strokeLinejoin="round"
        />
        <circle cx="32" cy="16" r="3.5" stroke={accent} strokeOpacity="0.6" />
        <path
          d="M27 30 L32 23 L37 30"
          stroke={accent}
          strokeOpacity="0.55"
          strokeWidth="1.1"
          strokeLinecap="round"
          strokeLinejoin="round"
        />
        <line
          x1="48"
          y1="13"
          x2="59"
          y2="13"
          stroke={accent}
          strokeOpacity="0.55"
        />
        <line
          x1="48"
          y1="20"
          x2="59"
          y2="20"
          stroke={accent}
          strokeOpacity="0.45"
        />
        <line
          x1="48"
          y1="27"
          x2="55"
          y2="27"
          stroke={accent}
          strokeOpacity="0.45"
        />
      </svg>
    );
  }

  if (slug === "v9") {
    // Field journal: a bound ledger with ruled lines, a margin, and a sketch.
    return (
      <svg
        aria-hidden="true"
        viewBox="0 0 64 40"
        className="mt-6 h-10 w-16"
        fill="none"
      >
        <rect
          x="3"
          y="2.5"
          width="58"
          height="35"
          rx="2"
          stroke={accent}
          strokeWidth="1.3"
        />
        <line
          x1="16"
          y1="2.5"
          x2="16"
          y2="37.5"
          stroke={accent}
          strokeOpacity="0.5"
        />
        <line
          x1="9.5"
          y1="2.5"
          x2="9.5"
          y2="37.5"
          stroke={accent}
          strokeOpacity="0.4"
          strokeDasharray="1 3"
        />
        <line
          x1="20"
          y1="10"
          x2="40"
          y2="10"
          stroke={accent}
          strokeOpacity="0.5"
        />
        <line
          x1="20"
          y1="17"
          x2="44"
          y2="17"
          stroke={accent}
          strokeOpacity="0.45"
        />
        <line
          x1="20"
          y1="24"
          x2="36"
          y2="24"
          stroke={accent}
          strokeOpacity="0.45"
        />
        <line
          x1="20"
          y1="31"
          x2="42"
          y2="31"
          stroke={accent}
          strokeOpacity="0.45"
        />
        <path
          d="M46 20 l4 -6 4 6 -4 6 Z"
          stroke={accent}
          strokeOpacity="0.6"
          strokeWidth="1.1"
          strokeLinejoin="round"
        />
        <circle cx="50" cy="29" r="2" fill={accent} fillOpacity="0.6" />
      </svg>
    );
  }

  return null;
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
