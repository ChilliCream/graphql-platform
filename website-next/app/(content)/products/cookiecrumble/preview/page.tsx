import type { Metadata } from "next";
import NextLink from "next/link";

export const metadata: Metadata = {
  title: "Cookie Crumble Page Variations",
  description:
    "Preview variations for the ChilliCream Cookie Crumble product page.",
  robots: { index: false, follow: false },
};

/* -------------------------------------------------------------------------- */
/*  Manifest                                                                   */
/*  Each variation is verified to exist under                                  */
/*  app/(content)/products/cookiecrumble/preview/v<n>/page.tsx. `present`      */
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
    stance: "Test-Author Catalogue",
    tagline: "Every snapshot surface Cookie Crumble ships, laid out flat.",
    summary:
      "A reference shelf of APIs, formatters, and runner support for the .NET test author who wants to scan the toolkit before adopting it.",
    angle: "Catalogue",
    accent: "#16b9e4",
    present: true,
  },
  {
    slug: "v2",
    stance: "Three-Snapshot-Styles Story",
    tagline: "Inline, file, and Markdown snapshots, told as one workflow.",
    summary:
      "The page walks the three snapshot styles in turn, so the reader sees when to reach for each one and how they compose in a real suite.",
    angle: "Narrative",
    accent: "#7c92c6",
    present: true,
  },
  {
    slug: "v3",
    stance: "Built-for-Hot-Chocolate Positioning",
    tagline: "Snapshot testing made by the team behind Hot Chocolate.",
    summary:
      "An argument for Cookie Crumble framed around its native formatters for IExecutionResult and GraphQLHttpResponse, and the suite that proves them.",
    angle: "Positioning",
    accent: "#f0786a",
    present: true,
  },
  {
    slug: "v4",
    stance: "The Snapshot Dispatch",
    tagline: "Snapshot testing read as a long-form editorial dispatch.",
    summary:
      "Cookie Crumble set as a long-form dispatch, with numbered section rules, a dropcap lede, pull quotes, and body columns that walk the test author through the toolkit at a reader's pace.",
    angle: "Editorial long-form",
    accent: "#5eead4",
    present: true,
  },
  {
    slug: "v5",
    stance: "The Six-Step Crumb Trail",
    tagline: "The library walked end to end, one numbered step at a time.",
    summary:
      "A six-step trail through Cookie Crumble, from hero to GraphQL-aware formatters, the three snapshot shapes, the __mismatch__ workflow, runner support, and who ships it, each step hung off a single vertical rule.",
    angle: "Numbered step",
    accent: "#5eead4",
    present: true,
  },
  {
    slug: "v6",
    stance: "Tasting Notes",
    tagline: "Snapshot testing served with the language of a barista's bar.",
    summary:
      "Cookie Crumble laid out as a tasting card, each snapshot style described with body, finish, and pairing notes, so the test author can taste the toolkit before committing a suite.",
    angle: "Barista",
    accent: "#5eead4",
    present: true,
  },
  {
    slug: "v7",
    stance: "Mismatch Reel",
    tagline: "The __mismatch__ workflow shown as a frame-by-frame motion reel.",
    summary:
      "A motion showcase that walks the diff, the __mismatch__ folder, and the accept step as a strip of frames, so the reader sees the snapshot workflow play out instead of reading it.",
    angle: "Motion showcase",
    accent: "#5eead4",
    present: true,
  },
  {
    slug: "v8",
    stance: "Crumb Cards",
    tagline: "The toolkit dealt as a hand of cards that open on touch.",
    summary:
      "Each snapshot capability sits on its own crumb card, compact at rest and expanding on hover to reveal the API, the formatter, and the runner note, so the test author opens only the cards that matter to the suite at hand.",
    angle: "Hover expand cards",
    accent: "var(--color-cc-accent)",
    present: true,
  },
  {
    slug: "v9",
    stance: "Heartbeat of the Suite",
    tagline: "Cookie Crumble read as the steady pulse under a test run.",
    summary:
      "A vital-signs trace where each snapshot style, the __mismatch__ accept step, and the GraphQL formatters land as a pulse marker along one heartbeat line, framing the toolkit as the signal that keeps a ChilliCream suite alive.",
    angle: "Pulse markers",
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

export default function CookieCrumblePreviewIndexPage() {
  return (
    <div className="py-6">
      <header className="relative">
        <p className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.22em] uppercase">
          Products / Cookie Crumble / Preview
        </p>
        <h1 className="font-heading text-cc-heading text-hero mt-4 max-w-3xl font-semibold tracking-tight">
          Nine stances on the same{" "}
          <span
            className="bg-clip-text text-transparent"
            style={{ backgroundImage: SPECTRUM }}
          >
            Cookie Crumble
          </span>{" "}
          product page.
        </h1>
        <p className="lead text-cc-ink mt-6 max-w-2xl">
          Internal preview, not indexed. Each variation reframes the same
          snapshot-testing surface around a different reader, from the test
          author scanning APIs to the editor reading a long-form dispatch and
          the typographer holding a strict Swiss grid.
        </p>

        <dl className="text-cc-ink-dim mt-10 grid gap-4 font-mono text-[0.72rem] tracking-tight sm:grid-cols-3">
          <div className="border-cc-card-border bg-cc-card-bg/40 rounded-lg border p-3">
            <dt className="text-cc-nav-label text-[0.6rem] tracking-[0.22em] uppercase">
              Track
            </dt>
            <dd className="text-cc-heading mt-1">Cookie Crumble</dd>
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
        aria-label="Cookie Crumble page variations"
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
        <span>Cookie Crumble / Page Lab</span>
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
      href={`/products/cookiecrumble/preview/${variation.slug}`}
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
          /products/cookiecrumble/preview/{variation.slug}
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
    // Catalogue: a stacked grid of API cells.
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
    // Three-snapshot-styles story: three stepped panels along a baseline.
    return (
      <svg
        aria-hidden="true"
        viewBox="0 0 64 40"
        className="mt-6 h-10 w-16"
        fill="none"
      >
        <line
          x1="2"
          y1="34"
          x2="62"
          y2="34"
          stroke={accent}
          strokeOpacity="0.4"
        />
        <rect
          x="3"
          y="22"
          width="16"
          height="12"
          rx="1.5"
          stroke={accent}
          strokeOpacity="0.6"
        />
        <rect
          x="24"
          y="14"
          width="16"
          height="20"
          rx="1.5"
          stroke={accent}
          strokeWidth="1.4"
        />
        <rect
          x="45"
          y="6"
          width="16"
          height="28"
          rx="1.5"
          stroke={accent}
          strokeOpacity="0.6"
        />
        <circle cx="11" cy="28" r="1.4" fill={accent} />
        <circle cx="32" cy="24" r="1.4" fill={accent} />
        <circle cx="53" cy="20" r="1.4" fill={accent} />
      </svg>
    );
  }

  if (slug === "v3") {
    // Built-for-Hot-Chocolate: a cookie node beside a Hot Chocolate mug glyph,
    // linked by a short tether to signal the native pairing.
    return (
      <svg
        aria-hidden="true"
        viewBox="0 0 64 40"
        className="mt-6 h-10 w-16"
        fill="none"
      >
        <circle
          cx="14"
          cy="20"
          r="9"
          stroke={accent}
          strokeWidth="1.4"
          fill="none"
        />
        <circle cx="11" cy="17" r="1.3" fill={accent} fillOpacity="0.85" />
        <circle cx="17" cy="20" r="1.1" fill={accent} fillOpacity="0.7" />
        <circle cx="13" cy="23" r="1.1" fill={accent} fillOpacity="0.7" />
        <line
          x1="23"
          y1="20"
          x2="37"
          y2="20"
          stroke={accent}
          strokeOpacity="0.5"
          strokeDasharray="2 2"
        />
        <rect
          x="38"
          y="13"
          width="16"
          height="16"
          rx="2"
          stroke={accent}
          strokeWidth="1.4"
        />
        <path
          d="M54 17 H58 V25 H54"
          stroke={accent}
          strokeOpacity="0.6"
          strokeWidth="1.2"
        />
        <path
          d="M42 11 C 42 8, 44 8, 44 11"
          stroke={accent}
          strokeOpacity="0.55"
          strokeLinecap="round"
        />
        <path
          d="M47 11 C 47 8, 49 8, 49 11"
          stroke={accent}
          strokeOpacity="0.55"
          strokeLinecap="round"
        />
      </svg>
    );
  }

  if (slug === "v4") {
    // Editorial: a stacked masthead rule above two columns of body lines,
    // evoking a quarterly print spread.
    return (
      <svg
        aria-hidden="true"
        viewBox="0 0 64 40"
        className="mt-6 h-10 w-16"
        fill="none"
      >
        <line x1="2" y1="6" x2="62" y2="6" stroke={accent} strokeWidth="1.4" />
        <line
          x1="2"
          y1="10"
          x2="40"
          y2="10"
          stroke={accent}
          strokeOpacity="0.5"
        />
        <line
          x1="2"
          y1="18"
          x2="28"
          y2="18"
          stroke={accent}
          strokeOpacity="0.6"
        />
        <line
          x1="2"
          y1="22"
          x2="28"
          y2="22"
          stroke={accent}
          strokeOpacity="0.6"
        />
        <line
          x1="2"
          y1="26"
          x2="28"
          y2="26"
          stroke={accent}
          strokeOpacity="0.6"
        />
        <line
          x1="2"
          y1="30"
          x2="22"
          y2="30"
          stroke={accent}
          strokeOpacity="0.6"
        />
        <line
          x1="34"
          y1="18"
          x2="62"
          y2="18"
          stroke={accent}
          strokeOpacity="0.6"
        />
        <line
          x1="34"
          y1="22"
          x2="62"
          y2="22"
          stroke={accent}
          strokeOpacity="0.6"
        />
        <line
          x1="34"
          y1="26"
          x2="62"
          y2="26"
          stroke={accent}
          strokeOpacity="0.6"
        />
        <line
          x1="34"
          y1="30"
          x2="54"
          y2="30"
          stroke={accent}
          strokeOpacity="0.6"
        />
      </svg>
    );
  }

  if (slug === "v5") {
    // Numbered step: numbered section marker beside a strict grid with a single
    // cherry accent rule.
    return (
      <svg
        aria-hidden="true"
        viewBox="0 0 64 40"
        className="mt-6 h-10 w-16"
        fill="none"
      >
        <text
          x="2"
          y="14"
          fill={accent}
          fontFamily="monospace"
          fontSize="7"
          fontWeight="600"
        >
          01
        </text>
        <line
          x1="2"
          y1="18"
          x2="14"
          y2="18"
          stroke={accent}
          strokeWidth="1.6"
        />
        <line
          x1="20"
          y1="6"
          x2="20"
          y2="34"
          stroke={accent}
          strokeOpacity="0.25"
        />
        <line
          x1="32"
          y1="6"
          x2="32"
          y2="34"
          stroke={accent}
          strokeOpacity="0.25"
        />
        <line
          x1="44"
          y1="6"
          x2="44"
          y2="34"
          stroke={accent}
          strokeOpacity="0.25"
        />
        <line
          x1="56"
          y1="6"
          x2="56"
          y2="34"
          stroke={accent}
          strokeOpacity="0.25"
        />
        <rect
          x="22"
          y="10"
          width="38"
          height="14"
          stroke={accent}
          strokeOpacity="0.55"
        />
        <line
          x1="22"
          y1="30"
          x2="60"
          y2="30"
          stroke={accent}
          strokeWidth="1.4"
        />
      </svg>
    );
  }

  if (slug === "v6") {
    // Barista tasting notes: a cup silhouette beside three tasting marks, body,
    // finish, and pairing, each pinned to a short rule.
    return (
      <svg
        aria-hidden="true"
        viewBox="0 0 64 40"
        className="mt-6 h-10 w-16"
        fill="none"
      >
        <path
          d="M4 12 H22 V26 C22 30, 18 32, 13 32 C8 32, 4 30, 4 26 Z"
          stroke={accent}
          strokeWidth="1.4"
        />
        <path
          d="M22 16 C 26 16, 26 24, 22 24"
          stroke={accent}
          strokeOpacity="0.6"
          strokeWidth="1.2"
        />
        <path
          d="M9 8 C 9 5, 11 5, 11 8"
          stroke={accent}
          strokeOpacity="0.55"
          strokeLinecap="round"
        />
        <path
          d="M14 8 C 14 5, 16 5, 16 8"
          stroke={accent}
          strokeOpacity="0.55"
          strokeLinecap="round"
        />
        <line
          x1="32"
          y1="12"
          x2="36"
          y2="12"
          stroke={accent}
          strokeWidth="1.4"
        />
        <line
          x1="38"
          y1="12"
          x2="60"
          y2="12"
          stroke={accent}
          strokeOpacity="0.55"
        />
        <line
          x1="32"
          y1="20"
          x2="36"
          y2="20"
          stroke={accent}
          strokeWidth="1.4"
        />
        <line
          x1="38"
          y1="20"
          x2="56"
          y2="20"
          stroke={accent}
          strokeOpacity="0.55"
        />
        <line
          x1="32"
          y1="28"
          x2="36"
          y2="28"
          stroke={accent}
          strokeWidth="1.4"
        />
        <line
          x1="38"
          y1="28"
          x2="52"
          y2="28"
          stroke={accent}
          strokeOpacity="0.55"
        />
      </svg>
    );
  }

  if (slug === "v8") {
    // Hover-expand cards: a fanned hand of crumb cards, one lifted and widened
    // to signal the card that opens on touch.
    return (
      <svg
        aria-hidden="true"
        viewBox="0 0 64 40"
        className="mt-6 h-10 w-16"
        fill="none"
      >
        <rect
          x="4"
          y="12"
          width="14"
          height="22"
          rx="2"
          stroke={accent}
          strokeOpacity="0.5"
        />
        <rect
          x="20"
          y="10"
          width="14"
          height="24"
          rx="2"
          stroke={accent}
          strokeOpacity="0.6"
        />
        <rect
          x="36"
          y="4"
          width="24"
          height="30"
          rx="2.5"
          stroke={accent}
          strokeWidth="1.4"
        />
        <line
          x1="40"
          y1="11"
          x2="56"
          y2="11"
          stroke={accent}
          strokeOpacity="0.7"
        />
        <line
          x1="40"
          y1="17"
          x2="52"
          y2="17"
          stroke={accent}
          strokeOpacity="0.5"
        />
        <circle cx="48" cy="26" r="1.6" fill={accent} fillOpacity="0.8" />
        <circle cx="44" cy="23" r="1.1" fill={accent} fillOpacity="0.6" />
        <circle cx="52" cy="24" r="1.1" fill={accent} fillOpacity="0.6" />
      </svg>
    );
  }

  if (slug === "v9") {
    // Pulse markers: a single heartbeat trace with marker dots pinned at each
    // peak, framing the toolkit as the suite's vital signs.
    return (
      <svg
        aria-hidden="true"
        viewBox="0 0 64 40"
        className="mt-6 h-10 w-16"
        fill="none"
      >
        <path
          d="M2 20 H14 L18 20 L22 8 L27 32 L31 20 L42 20 L46 12 L50 26 L53 20 H62"
          stroke={accent}
          strokeWidth="1.4"
          strokeLinecap="round"
          strokeLinejoin="round"
        />
        <circle cx="22" cy="8" r="1.6" fill={accent} />
        <circle cx="27" cy="32" r="1.6" fill={accent} fillOpacity="0.7" />
        <circle cx="46" cy="12" r="1.6" fill={accent} />
        <circle cx="2" cy="20" r="1.2" fill={accent} fillOpacity="0.5" />
        <circle cx="62" cy="20" r="1.2" fill={accent} fillOpacity="0.5" />
      </svg>
    );
  }

  // Motion showcase: a strip of four film frames sliding into a playhead, the
  // mismatch reel rendered as a filmstrip.
  return (
    <svg
      aria-hidden="true"
      viewBox="0 0 64 40"
      className="mt-6 h-10 w-16"
      fill="none"
    >
      <line x1="2" y1="8" x2="62" y2="8" stroke={accent} strokeOpacity="0.35" />
      <line
        x1="2"
        y1="32"
        x2="62"
        y2="32"
        stroke={accent}
        strokeOpacity="0.35"
      />
      <rect
        x="3"
        y="12"
        width="12"
        height="16"
        rx="1.5"
        stroke={accent}
        strokeOpacity="0.55"
      />
      <rect
        x="17"
        y="12"
        width="12"
        height="16"
        rx="1.5"
        stroke={accent}
        strokeOpacity="0.7"
      />
      <rect
        x="31"
        y="12"
        width="12"
        height="16"
        rx="1.5"
        stroke={accent}
        strokeWidth="1.4"
      />
      <rect
        x="45"
        y="12"
        width="12"
        height="16"
        rx="1.5"
        stroke={accent}
        strokeOpacity="0.55"
      />
      <circle cx="9" cy="6" r="0.9" fill={accent} fillOpacity="0.55" />
      <circle cx="23" cy="6" r="0.9" fill={accent} fillOpacity="0.55" />
      <circle cx="37" cy="6" r="0.9" fill={accent} fillOpacity="0.55" />
      <circle cx="51" cy="6" r="0.9" fill={accent} fillOpacity="0.55" />
      <circle cx="9" cy="34" r="0.9" fill={accent} fillOpacity="0.55" />
      <circle cx="23" cy="34" r="0.9" fill={accent} fillOpacity="0.55" />
      <circle cx="37" cy="34" r="0.9" fill={accent} fillOpacity="0.55" />
      <circle cx="51" cy="34" r="0.9" fill={accent} fillOpacity="0.55" />
      <line x1="37" y1="10" x2="37" y2="30" stroke={accent} strokeWidth="1.4" />
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
