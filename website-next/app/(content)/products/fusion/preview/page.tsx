import type { Metadata } from "next";
import NextLink from "next/link";

export const metadata: Metadata = {
  title: "Fusion Page Variations",
  description: "Preview variations for the ChilliCream Fusion product page.",
  robots: { index: false, follow: false },
};

/* -------------------------------------------------------------------------- */
/*  Manifest                                                                   */
/*  Each variation is verified to exist under                                  */
/*  app/(content)/products/fusion/preview/v<n>/page.tsx. `present` records     */
/*  that check; a missing variation renders a disabled placeholder card.       */
/* -------------------------------------------------------------------------- */

interface Variation {
  readonly slug: "v1" | "v2" | "v3" | "v4" | "v5" | "v6" | "v7" | "v8" | "v9";
  readonly stance: string;
  readonly family: string;
  readonly tagline: string;
  readonly summary: string;
  readonly angle: string;
  readonly accent: string;
  readonly present: boolean;
}

const VARIATIONS: readonly Variation[] = [
  {
    slug: "v1",
    stance: "Composition-First Catalogue",
    family: "catalogue",
    tagline:
      "Every subgraph, directive, and composition rule Fusion ships, on one surface.",
    summary:
      "A reference shelf for platform engineers shopping Fusion as a composition tool, planning-time checks, Apollo Federation spec coverage, and the self-run gateway, laid out for a careful read.",
    angle: "Catalogue",
    accent: "#16b9e4",
    present: true,
  },
  {
    slug: "v2",
    stance: "Query-Plan Story",
    family: "narrative",
    tagline:
      "Follow a single request as Fusion plans, fetches, and stitches it back together.",
    summary:
      "The page walks one query from gateway intake through the composed plan and parallel subgraph fetches, showing where Fusion makes the decisions that keep a federated graph honest.",
    angle: "Narrative",
    accent: "#7c92c6",
    present: true,
  },
  {
    slug: "v3",
    stance: ".NET-Native-Gateway Positioning",
    family: "positioning",
    tagline:
      "A federation gateway you run on your own .NET runtime, not a hosted hop.",
    summary:
      "Framed for architects weighing where the gateway lives. Fusion is a .NET process in your cluster, with composition validated at planning time and operations that match the rest of your stack.",
    angle: "Positioning",
    accent: "#f0786a",
    present: true,
  },
  {
    slug: "v4",
    stance: "The Long Read",
    family: "centered-essay",
    tagline:
      "Fusion as a single, centered essay you read top to bottom, no sidebars.",
    summary:
      "A centered narrative column that walks composition, planning, and the .NET-native gateway as one continuous read, generous measure, quiet rhythm, and headings that mark sections rather than compete for attention.",
    angle: "Centered narrative",
    accent: "var(--cc-accent)",
    present: true,
  },
  {
    slug: "v5",
    stance: "Reference Manual",
    family: "sidebar-toc",
    tagline:
      "A sticky table of contents on the side, Fusion treated as documentation.",
    summary:
      "A sidebar table of contents anchors the page while the main column lays out composition rules, planning checks, and gateway operations as numbered, scannable reference sections, built for engineers who jump in by anchor.",
    angle: "Sidebar TOC",
    accent: "#22d3ee",
    present: true,
  },
  {
    slug: "v6",
    stance: "House Blend Gateway",
    family: "barista",
    tagline:
      "Fusion served as a house blend, composition, planning, and gateway poured into one cup.",
    summary:
      "A barista take on the federation gateway, the subgraphs are beans, composition is the blend, planning is the pour, and the .NET-native gateway is the cup it lands in, warm copy that still names every moving part.",
    angle: "Barista",
    accent: "var(--cc-accent)",
    present: true,
  },
  {
    slug: "v7",
    stance: "Plan in Motion",
    family: "motion-showcase",
    tagline:
      "Watch composition fold into a plan and the gateway dispatch it, in motion.",
    summary:
      "A motion-led showcase where the query plan animates from composed schema to parallel subgraph fetches and back, leaning on choreography to make the gateway's job legible at a glance.",
    angle: "Motion showcase",
    accent: "var(--cc-accent)",
    present: true,
  },
  {
    slug: "v8",
    stance: "Ridge Walk",
    family: "mountain-range",
    tagline:
      "Fusion as a ridge walk, each peak a stage of composition, planning, and the gateway in turn.",
    summary:
      "A mountain-range traverse where the page climbs from subgraph foothills through the composition saddle to the planning summit, then descends into the .NET-native gateway, every elevation marking one decision the federation gateway owns.",
    angle: "Mountain range",
    accent: "var(--cc-accent)",
    present: true,
  },
  {
    slug: "v9",
    stance: "Composition Calendar",
    family: "calendar-grid",
    tagline:
      "Fusion laid out as a calendar grid, composition, planning, and gateway booked cell by cell.",
    summary:
      "A calendar-grid layout that slots subgraph composition, planning-time checks, and gateway operations into a scannable month of cells, treating the federation surface as a schedule the reader can plan against at a glance.",
    angle: "Calendar grid",
    accent: "var(--cc-accent)",
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

export default function FusionPreviewIndexPage() {
  return (
    <div className="py-6">
      <header className="relative">
        <p className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.22em] uppercase">
          Products / Fusion / Preview
        </p>
        <h1 className="font-heading text-cc-heading text-hero mt-4 max-w-3xl font-semibold tracking-tight">
          Nine stances on the same{" "}
          <span
            className="bg-clip-text text-transparent"
            style={{ backgroundImage: SPECTRUM }}
          >
            Fusion
          </span>{" "}
          product page.
        </h1>
        <p className="lead text-cc-ink mt-6 max-w-2xl">
          Internal preview, not indexed. Each variation reframes the same
          surface around a different reader, the cataloguer scanning the feature
          set, the engineer following a query plan, and the architect deciding
          where the gateway runs.
        </p>

        <dl className="text-cc-ink-dim mt-10 grid gap-4 font-mono text-[0.72rem] tracking-tight sm:grid-cols-3">
          <div className="border-cc-card-border bg-cc-card-bg/40 rounded-lg border p-3">
            <dt className="text-cc-nav-label text-[0.6rem] tracking-[0.22em] uppercase">
              Track
            </dt>
            <dd className="text-cc-heading mt-1">Fusion</dd>
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
        aria-label="Fusion page variations"
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
        <span>Fusion / Page Lab</span>
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
      href={`/products/fusion/preview/${variation.slug}`}
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
          /products/fusion/preview/{variation.slug}
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
    // Catalogue: a grid of composed subgraph cells, one highlighted.
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
    // Query plan: a root node fanning into parallel subgraph fetches, then merging.
    return (
      <svg
        aria-hidden="true"
        viewBox="0 0 64 40"
        className="mt-6 h-10 w-16"
        fill="none"
      >
        <path
          d="M8 20 L24 8 M8 20 L24 20 M8 20 L24 32 M24 8 L48 20 M24 20 L48 20 M24 32 L48 20 M48 20 L60 20"
          stroke={accent}
          strokeOpacity="0.55"
          strokeWidth="1.2"
          strokeLinecap="round"
        />
        <circle cx="8" cy="20" r="2.6" fill={accent} />
        <circle
          cx="24"
          cy="8"
          r="2.2"
          stroke={accent}
          strokeWidth="1.2"
          fill="none"
        />
        <circle
          cx="24"
          cy="20"
          r="2.2"
          stroke={accent}
          strokeWidth="1.2"
          fill="none"
        />
        <circle
          cx="24"
          cy="32"
          r="2.2"
          stroke={accent}
          strokeWidth="1.2"
          fill="none"
        />
        <circle cx="48" cy="20" r="2.6" fill={accent} />
        <circle
          cx="60"
          cy="20"
          r="2.2"
          stroke={accent}
          strokeWidth="1.2"
          fill="none"
        />
      </svg>
    );
  }

  if (slug === "v3") {
    // .NET-native gateway: a single gateway box bridging subgraphs to clients.
    return (
      <svg
        aria-hidden="true"
        viewBox="0 0 64 40"
        className="mt-6 h-10 w-16"
        fill="none"
      >
        <rect
          x="22"
          y="14"
          width="20"
          height="12"
          rx="2"
          stroke={accent}
          strokeWidth="1.4"
        />
        <path
          d="M22 20 L8 20 M42 20 L56 20"
          stroke={accent}
          strokeOpacity="0.55"
          strokeWidth="1.2"
          strokeLinecap="round"
        />
        <rect
          x="2"
          y="6"
          width="6"
          height="6"
          rx="1"
          stroke={accent}
          strokeOpacity="0.6"
        />
        <rect
          x="2"
          y="17"
          width="6"
          height="6"
          rx="1"
          stroke={accent}
          strokeOpacity="0.6"
        />
        <rect
          x="2"
          y="28"
          width="6"
          height="6"
          rx="1"
          stroke={accent}
          strokeOpacity="0.6"
        />
        <rect
          x="56"
          y="11"
          width="6"
          height="6"
          rx="1"
          stroke={accent}
          strokeOpacity="0.6"
        />
        <rect
          x="56"
          y="22"
          width="6"
          height="6"
          rx="1"
          stroke={accent}
          strokeOpacity="0.6"
        />
      </svg>
    );
  }

  if (slug === "v4") {
    // The Long Read: a single centered column of justified text lines.
    return (
      <svg
        aria-hidden="true"
        viewBox="0 0 64 40"
        className="mt-6 h-10 w-16"
        fill="none"
      >
        <line
          x1="22"
          y1="6"
          x2="42"
          y2="6"
          stroke={accent}
          strokeWidth="1.4"
          strokeLinecap="round"
        />
        <line
          x1="18"
          y1="13"
          x2="46"
          y2="13"
          stroke={accent}
          strokeOpacity="0.55"
          strokeWidth="1"
          strokeLinecap="round"
        />
        <line
          x1="18"
          y1="18"
          x2="46"
          y2="18"
          stroke={accent}
          strokeOpacity="0.55"
          strokeWidth="1"
          strokeLinecap="round"
        />
        <line
          x1="18"
          y1="23"
          x2="46"
          y2="23"
          stroke={accent}
          strokeOpacity="0.55"
          strokeWidth="1"
          strokeLinecap="round"
        />
        <line
          x1="18"
          y1="28"
          x2="40"
          y2="28"
          stroke={accent}
          strokeOpacity="0.55"
          strokeWidth="1"
          strokeLinecap="round"
        />
        <line
          x1="26"
          y1="35"
          x2="38"
          y2="35"
          stroke={accent}
          strokeOpacity="0.4"
          strokeWidth="1"
          strokeLinecap="round"
        />
      </svg>
    );
  }

  if (slug === "v5") {
    // Reference Manual: sidebar TOC ticks on the left, body lines on the right.
    return (
      <svg
        aria-hidden="true"
        viewBox="0 0 64 40"
        className="mt-6 h-10 w-16"
        fill="none"
      >
        <line
          x1="20"
          y1="4"
          x2="20"
          y2="36"
          stroke={accent}
          strokeOpacity="0.35"
          strokeWidth="1"
        />
        <line
          x1="4"
          y1="8"
          x2="14"
          y2="8"
          stroke={accent}
          strokeWidth="1.4"
          strokeLinecap="round"
        />
        <line
          x1="4"
          y1="15"
          x2="16"
          y2="15"
          stroke={accent}
          strokeOpacity="0.55"
          strokeWidth="1"
          strokeLinecap="round"
        />
        <line
          x1="4"
          y1="22"
          x2="12"
          y2="22"
          stroke={accent}
          strokeOpacity="0.55"
          strokeWidth="1"
          strokeLinecap="round"
        />
        <line
          x1="4"
          y1="29"
          x2="16"
          y2="29"
          stroke={accent}
          strokeOpacity="0.55"
          strokeWidth="1"
          strokeLinecap="round"
        />
        <line
          x1="26"
          y1="8"
          x2="60"
          y2="8"
          stroke={accent}
          strokeWidth="1.2"
          strokeLinecap="round"
        />
        <line
          x1="26"
          y1="15"
          x2="58"
          y2="15"
          stroke={accent}
          strokeOpacity="0.55"
          strokeWidth="1"
          strokeLinecap="round"
        />
        <line
          x1="26"
          y1="22"
          x2="60"
          y2="22"
          stroke={accent}
          strokeOpacity="0.55"
          strokeWidth="1"
          strokeLinecap="round"
        />
        <line
          x1="26"
          y1="29"
          x2="52"
          y2="29"
          stroke={accent}
          strokeOpacity="0.55"
          strokeWidth="1"
          strokeLinecap="round"
        />
      </svg>
    );
  }

  if (slug === "v6") {
    // House Blend Gateway: a cup with a rising steam curl, beans at the base.
    return (
      <svg
        aria-hidden="true"
        viewBox="0 0 64 40"
        className="mt-6 h-10 w-16"
        fill="none"
      >
        <path
          d="M18 18 L18 30 Q18 34 22 34 L38 34 Q42 34 42 30 L42 18 Z"
          stroke={accent}
          strokeWidth="1.4"
        />
        <path
          d="M42 21 Q48 21 48 25 Q48 29 42 29"
          stroke={accent}
          strokeOpacity="0.6"
          strokeWidth="1.2"
        />
        <path
          d="M24 14 Q26 10 24 6 M30 14 Q32 10 30 6 M36 14 Q38 10 36 6"
          stroke={accent}
          strokeOpacity="0.55"
          strokeWidth="1.2"
          strokeLinecap="round"
        />
        <ellipse
          cx="26"
          cy="37"
          rx="2.4"
          ry="1.4"
          stroke={accent}
          strokeOpacity="0.6"
          strokeWidth="1"
        />
        <ellipse
          cx="34"
          cy="37"
          rx="2.4"
          ry="1.4"
          stroke={accent}
          strokeOpacity="0.6"
          strokeWidth="1"
        />
      </svg>
    );
  }

  if (slug === "v7") {
    // Plan in Motion: a flowing motion arc with phase dots tracing the path.
    return (
      <svg
        aria-hidden="true"
        viewBox="0 0 64 40"
        className="mt-6 h-10 w-16"
        fill="none"
      >
        <path
          d="M2 30 Q16 6 32 20 T62 14"
          stroke={accent}
          strokeOpacity="0.55"
          strokeWidth="1.4"
          strokeLinecap="round"
        />
        <circle cx="6" cy="27" r="1.6" fill={accent} fillOpacity="0.35" />
        <circle cx="18" cy="14" r="1.8" fill={accent} fillOpacity="0.55" />
        <circle cx="32" cy="20" r="2.2" fill={accent} />
        <circle cx="46" cy="22" r="1.8" fill={accent} fillOpacity="0.55" />
        <circle cx="58" cy="15" r="1.6" fill={accent} fillOpacity="0.35" />
        <path
          d="M52 11 L58 15 L54 19"
          stroke={accent}
          strokeWidth="1.2"
          strokeLinecap="round"
          strokeLinejoin="round"
        />
      </svg>
    );
  }

  if (slug === "v8") {
    // Ridge Walk: a mountain range of staged peaks with a summit marker.
    return (
      <svg
        aria-hidden="true"
        viewBox="0 0 64 40"
        className="mt-6 h-10 w-16"
        fill="none"
      >
        <path
          d="M2 34 L14 22 L22 28 L34 10 L44 24 L52 18 L62 30"
          stroke={accent}
          strokeWidth="1.4"
          strokeLinecap="round"
          strokeLinejoin="round"
        />
        <path
          d="M2 38 L62 38"
          stroke={accent}
          strokeOpacity="0.35"
          strokeWidth="1"
          strokeLinecap="round"
        />
        <path
          d="M34 10 L37 4 L40 10"
          stroke={accent}
          strokeOpacity="0.6"
          strokeWidth="1.2"
          strokeLinecap="round"
          strokeLinejoin="round"
        />
        <circle cx="34" cy="10" r="2" fill={accent} />
        <circle cx="14" cy="22" r="1.6" fill={accent} fillOpacity="0.55" />
        <circle cx="52" cy="18" r="1.6" fill={accent} fillOpacity="0.55" />
      </svg>
    );
  }

  // Composition Calendar: a grid of booked cells with one day marked.
  return (
    <svg
      aria-hidden="true"
      viewBox="0 0 64 40"
      className="mt-6 h-10 w-16"
      fill="none"
    >
      <rect
        x="6"
        y="6"
        width="52"
        height="30"
        rx="2"
        stroke={accent}
        strokeWidth="1.4"
      />
      <line
        x1="6"
        y1="14"
        x2="58"
        y2="14"
        stroke={accent}
        strokeOpacity="0.5"
        strokeWidth="1"
      />
      <line
        x1="19"
        y1="14"
        x2="19"
        y2="36"
        stroke={accent}
        strokeOpacity="0.35"
        strokeWidth="1"
      />
      <line
        x1="32"
        y1="14"
        x2="32"
        y2="36"
        stroke={accent}
        strokeOpacity="0.35"
        strokeWidth="1"
      />
      <line
        x1="45"
        y1="14"
        x2="45"
        y2="36"
        stroke={accent}
        strokeOpacity="0.35"
        strokeWidth="1"
      />
      <line
        x1="6"
        y1="25"
        x2="58"
        y2="25"
        stroke={accent}
        strokeOpacity="0.35"
        strokeWidth="1"
      />
      <line
        x1="14"
        y1="2"
        x2="14"
        y2="9"
        stroke={accent}
        strokeOpacity="0.6"
        strokeWidth="1.2"
        strokeLinecap="round"
      />
      <line
        x1="50"
        y1="2"
        x2="50"
        y2="9"
        stroke={accent}
        strokeOpacity="0.6"
        strokeWidth="1.2"
        strokeLinecap="round"
      />
      <rect x="33" y="26" width="11" height="9" rx="1" fill={accent} />
    </svg>
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
