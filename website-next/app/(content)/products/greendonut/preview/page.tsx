import type { Metadata } from "next";
import NextLink from "next/link";

export const metadata: Metadata = {
  title: "Green Donut Page Variations",
  description:
    "Preview variations for the ChilliCream Green Donut product page.",
  robots: { index: false, follow: false },
};

/* -------------------------------------------------------------------------- */
/*  Manifest                                                                   */
/*  Each variation is verified to exist under                                  */
/*  app/(content)/products/greendonut/preview/v<n>/page.tsx. `present` records */
/*  that check; a missing variation renders a disabled placeholder.            */
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
    stance: "N+1-Killer Catalogue",
    tagline: "Every DataLoader pattern Green Donut ships, on one surface.",
    summary:
      "A reference shelf of batching, caching, and grouping primitives for the .NET developer hunting down N+1 in resolvers and services.",
    angle: "Catalogue",
    accent: "#16b9e4",
    present: true,
  },
  {
    slug: "v2",
    stance: "Before/After Walkthrough",
    tagline: "Watch a chatty resolver collapse into a single batched call.",
    summary:
      "A guided diff that walks from the naive resolver to the Green Donut DataLoader, showing the request shape and the database hits side by side.",
    angle: "Walkthrough",
    accent: "#7c92c6",
    present: true,
  },
  {
    slug: "v3",
    stance: "Inside-Hot-Chocolate Positioning",
    tagline: "How Green Donut sits inside the Hot Chocolate execution path.",
    summary:
      "The architect's view, where DataLoaders live in the request pipeline, how scopes and scheduling are wired, and where Green Donut earns its place.",
    angle: "Positioning",
    accent: "#f0786a",
    present: true,
  },
  {
    slug: "v4",
    stance: "Six Ticks To Zero",
    tagline: "Six numbered steps that take a resolver from N+1 to zero waste.",
    summary:
      "A numbered-step walkthrough that counts down the DataLoader journey, batching, caching, grouping, and scope, so the reader can see exactly where each tick collapses the round trips.",
    angle: "Numbered step",
    accent: "#5eead4",
    present: true,
  },
  {
    slug: "v5",
    stance: "The Coalescer",
    tagline: "A single visual hero of many calls coalescing into one batch.",
    summary:
      "A visual-hero layout built around one large illustration of scattered requests coalescing into a single batched call, with the DataLoader story spun out around that image.",
    angle: "Visual hero",
    accent: "#5eead4",
    present: true,
  },
  {
    slug: "v6",
    stance: "Behind the Bar",
    tagline:
      "The barista's bar, where loose orders land batched on a single tray.",
    summary:
      "A barista-counter framing of the DataLoader, where each resolver call is an order ticket and Green Donut is the bar that batches, caches, and serves the tray in one pass.",
    angle: "Barista",
    accent: "#5eead4",
    present: true,
  },
  {
    slug: "v7",
    stance: "Tick Collapse",
    tagline: "A motion showcase of round trips collapsing tick by tick.",
    summary:
      "A motion-led showcase that animates the collapse from many chatty calls to one batched round trip, with each tick of the loop folding waste into a single resolved frame.",
    angle: "Motion showcase",
    accent: "#5eead4",
    present: true,
  },
  {
    slug: "v8",
    stance: "Dewey Decimal DataLoader",
    tagline: "Every key shelved by call number, then fetched in one trip.",
    summary:
      "A library-catalog framing of the DataLoader, where each requested key is a call number, the batch is a single trip to the stacks, and the cache is the reference desk that already has the book in hand.",
    angle: "Library catalog",
    accent: "var(--color-cc-accent)",
    present: true,
  },
  {
    slug: "v9",
    stance: "The Standup",
    tagline: "Scattered resolver chatter resolved in one shared round.",
    summary:
      "A conversation-thread framing of the DataLoader, where each resolver posts its key into the thread and Green Donut answers the whole room at once, turning a wall of side chatter into a single batched reply.",
    angle: "Conversation thread",
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

export default function GreenDonutPreviewIndexPage() {
  return (
    <div className="py-6">
      <header className="relative">
        <p className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.22em] uppercase">
          Products / Green Donut / Preview
        </p>
        <h1 className="font-heading text-cc-heading text-hero mt-4 max-w-3xl font-semibold tracking-tight">
          Nine stances on the same{" "}
          <span
            className="bg-clip-text text-transparent"
            style={{ backgroundImage: SPECTRUM }}
          >
            Green Donut
          </span>{" "}
          product page.
        </h1>
        <p className="lead text-cc-ink mt-6 max-w-2xl">
          Internal preview, not indexed. Each variation reframes the same
          DataLoader story around a different reader, the cataloguer, the
          builder fighting N+1, the architect placing Green Donut inside Hot
          Chocolate, a swiss-minimal grid voice, a vivid-gradient showpiece, a
          barista bar serving tickets in one tray, and a motion showcase of
          ticks collapsing into one round trip.
        </p>

        <dl className="text-cc-ink-dim mt-10 grid gap-4 font-mono text-[0.72rem] tracking-tight sm:grid-cols-3">
          <div className="border-cc-card-border bg-cc-card-bg/40 rounded-lg border p-3">
            <dt className="text-cc-nav-label text-[0.6rem] tracking-[0.22em] uppercase">
              Track
            </dt>
            <dd className="text-cc-heading mt-1">Green Donut</dd>
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
        aria-label="Green Donut page variations"
        className="mt-12 grid gap-6 sm:grid-cols-2 lg:grid-cols-3"
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
        <span>Green Donut / Page Lab</span>
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
      href={`/products/greendonut/preview/${variation.slug}`}
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
          /products/greendonut/preview/{variation.slug}
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
    // Walkthrough: two parallel tracks collapsing into one batched call.
    return (
      <svg
        aria-hidden="true"
        viewBox="0 0 64 40"
        className="mt-6 h-10 w-16"
        fill="none"
      >
        <path
          d="M3 8 H26 M3 20 H26 M3 32 H26"
          stroke={accent}
          strokeOpacity="0.45"
          strokeWidth="1.2"
          strokeLinecap="round"
        />
        <circle cx="3" cy="8" r="2" fill={accent} fillOpacity="0.55" />
        <circle cx="3" cy="20" r="2" fill={accent} fillOpacity="0.55" />
        <circle cx="3" cy="32" r="2" fill={accent} fillOpacity="0.55" />
        <path
          d="M28 8 C 34 8, 34 20, 40 20 M28 20 H40 M28 32 C 34 32, 34 20, 40 20"
          stroke={accent}
          strokeWidth="1.3"
          strokeLinecap="round"
          fill="none"
        />
        <path
          d="M40 20 H61"
          stroke={accent}
          strokeWidth="1.6"
          strokeLinecap="round"
        />
        <circle cx="61" cy="20" r="2.6" fill={accent} />
      </svg>
    );
  }

  if (slug === "v3") {
    // Positioning: a request pipeline with the DataLoader stage highlighted.
    return (
      <svg
        aria-hidden="true"
        viewBox="0 0 64 40"
        className="mt-6 h-10 w-16"
        fill="none"
      >
        <path
          d="M3 20 H61"
          stroke={accent}
          strokeOpacity="0.35"
          strokeWidth="1"
          strokeLinecap="round"
        />
        <rect
          x="4"
          y="14"
          width="12"
          height="12"
          rx="2"
          stroke={accent}
          strokeOpacity="0.5"
        />
        <rect
          x="20"
          y="14"
          width="12"
          height="12"
          rx="2"
          stroke={accent}
          strokeOpacity="0.5"
        />
        <rect
          x="36"
          y="11"
          width="14"
          height="18"
          rx="3"
          stroke={accent}
          strokeWidth="1.6"
        />
        <circle cx="43" cy="20" r="2.4" fill={accent} />
        <rect
          x="54"
          y="14"
          width="6"
          height="12"
          rx="2"
          stroke={accent}
          strokeOpacity="0.5"
        />
      </svg>
    );
  }

  if (slug === "v4") {
    // Swiss minimal: 12 column rules with a single cherry accent column.
    return (
      <svg
        aria-hidden="true"
        viewBox="0 0 64 40"
        className="mt-6 h-10 w-16"
        fill="none"
      >
        <path
          d="M2 6 V34 M7 6 V34 M12 6 V34 M17 6 V34 M22 6 V34 M27 6 V34 M32 6 V34 M37 6 V34 M42 6 V34 M47 6 V34 M52 6 V34 M57 6 V34 M62 6 V34"
          stroke={accent}
          strokeOpacity="0.3"
          strokeWidth="0.8"
          strokeLinecap="round"
        />
        <rect x="26" y="6" width="6" height="28" rx="0.5" fill={accent} />
        <path
          d="M2 20 H62"
          stroke={accent}
          strokeOpacity="0.45"
          strokeWidth="0.8"
          strokeLinecap="round"
        />
      </svg>
    );
  }

  if (slug === "v5") {
    // Vivid gradient: an aurora arc layered over a glowing horizon.
    return (
      <svg
        aria-hidden="true"
        viewBox="0 0 64 40"
        className="mt-6 h-10 w-16"
        fill="none"
      >
        <defs>
          <linearGradient
            id="greendonut-preview-v5-aurora"
            x1="0"
            y1="0"
            x2="64"
            y2="0"
            gradientUnits="userSpaceOnUse"
          >
            <stop offset="0%" stopColor="#16b9e4" />
            <stop offset="50%" stopColor={accent} />
            <stop offset="100%" stopColor="#f0786a" />
          </linearGradient>
        </defs>
        <path
          d="M2 30 Q 16 6, 32 18 T 62 14"
          stroke="url(#greendonut-preview-v5-aurora)"
          strokeWidth="2.2"
          strokeLinecap="round"
          fill="none"
        />
        <path
          d="M2 34 H62"
          stroke="url(#greendonut-preview-v5-aurora)"
          strokeOpacity="0.55"
          strokeWidth="1.2"
          strokeLinecap="round"
        />
        <circle cx="32" cy="18" r="2.2" fill={accent} />
      </svg>
    );
  }

  if (slug === "v6") {
    // Behind the Bar: a counter line with stacked order tickets feeding one tray.
    return (
      <svg
        aria-hidden="true"
        viewBox="0 0 64 40"
        className="mt-6 h-10 w-16"
        fill="none"
      >
        <path
          d="M2 30 H62"
          stroke={accent}
          strokeOpacity="0.55"
          strokeWidth="1.4"
          strokeLinecap="round"
        />
        <rect
          x="6"
          y="6"
          width="8"
          height="11"
          rx="1"
          stroke={accent}
          strokeOpacity="0.6"
        />
        <rect
          x="17"
          y="9"
          width="8"
          height="11"
          rx="1"
          stroke={accent}
          strokeOpacity="0.6"
        />
        <rect
          x="28"
          y="6"
          width="8"
          height="11"
          rx="1"
          stroke={accent}
          strokeOpacity="0.6"
        />
        <rect
          x="42"
          y="20"
          width="18"
          height="7"
          rx="1.5"
          stroke={accent}
          strokeWidth="1.5"
        />
        <circle cx="46" cy="23.5" r="1.2" fill={accent} />
        <circle cx="51" cy="23.5" r="1.2" fill={accent} />
        <circle cx="56" cy="23.5" r="1.2" fill={accent} />
        <path
          d="M37 13 C 42 13, 42 23, 42 23"
          stroke={accent}
          strokeOpacity="0.7"
          strokeWidth="1.2"
          strokeLinecap="round"
          fill="none"
        />
      </svg>
    );
  }

  if (slug === "v7") {
    // Tick Collapse: a tick row collapsing into a single resolved frame.
    return (
      <svg
        aria-hidden="true"
        viewBox="0 0 64 40"
        className="mt-6 h-10 w-16"
        fill="none"
      >
        <path
          d="M3 10 V18 M9 10 V18 M15 10 V18 M21 10 V18 M27 10 V18 M33 10 V18"
          stroke={accent}
          strokeOpacity="0.55"
          strokeWidth="1.2"
          strokeLinecap="round"
        />
        <path
          d="M36 14 H44"
          stroke={accent}
          strokeOpacity="0.45"
          strokeWidth="1"
          strokeLinecap="round"
        />
        <path
          d="M40 11 L44 14 L40 17"
          stroke={accent}
          strokeOpacity="0.6"
          strokeWidth="1"
          strokeLinecap="round"
          strokeLinejoin="round"
          fill="none"
        />
        <rect
          x="46"
          y="8"
          width="14"
          height="12"
          rx="2"
          stroke={accent}
          strokeWidth="1.5"
        />
        <circle cx="53" cy="14" r="1.8" fill={accent} />
        <path
          d="M3 28 H61"
          stroke={accent}
          strokeOpacity="0.35"
          strokeWidth="0.8"
          strokeLinecap="round"
        />
        <path
          d="M3 32 H40"
          stroke={accent}
          strokeOpacity="0.5"
          strokeWidth="1"
          strokeLinecap="round"
        />
      </svg>
    );
  }

  if (slug === "v8") {
    // Library catalog: a card-catalog drawer of call numbers pulled to one shelf.
    return (
      <svg
        aria-hidden="true"
        viewBox="0 0 64 40"
        className="mt-6 h-10 w-16"
        fill="none"
      >
        <rect
          x="2"
          y="4"
          width="22"
          height="32"
          rx="2"
          stroke={accent}
          strokeOpacity="0.5"
        />
        <path
          d="M2 13 H24 M2 22 H24 M2 31 H24"
          stroke={accent}
          strokeOpacity="0.5"
          strokeWidth="0.9"
        />
        <circle cx="13" cy="8.5" r="1" fill={accent} fillOpacity="0.7" />
        <circle cx="13" cy="17.5" r="1" fill={accent} fillOpacity="0.7" />
        <circle cx="13" cy="26.5" r="1" fill={accent} fillOpacity="0.7" />
        <path
          d="M26 9 C 36 9, 36 20, 44 20 M26 18 H44 M26 27 C 36 27, 36 20, 44 20"
          stroke={accent}
          strokeOpacity="0.6"
          strokeWidth="1.2"
          strokeLinecap="round"
          fill="none"
        />
        <rect
          x="46"
          y="12"
          width="14"
          height="16"
          rx="2"
          stroke={accent}
          strokeWidth="1.5"
        />
        <path
          d="M50 16 H56 M50 20 H56 M50 24 H53"
          stroke={accent}
          strokeWidth="1"
          strokeLinecap="round"
        />
      </svg>
    );
  }

  // The Standup (v9): scattered message bubbles resolved into one shared reply.
  return (
    <svg
      aria-hidden="true"
      viewBox="0 0 64 40"
      className="mt-6 h-10 w-16"
      fill="none"
    >
      <path
        d="M3 5 H21 V13 H9 L5 16 V13 H3 Z"
        stroke={accent}
        strokeOpacity="0.55"
        strokeWidth="1.1"
        strokeLinejoin="round"
        fill="none"
      />
      <path
        d="M3 22 H19 V30 H8 L4 33 V30 H3 Z"
        stroke={accent}
        strokeOpacity="0.55"
        strokeWidth="1.1"
        strokeLinejoin="round"
        fill="none"
      />
      <path
        d="M24 9 C 34 9, 34 20, 42 20 M24 26 C 34 26, 34 20, 42 20"
        stroke={accent}
        strokeOpacity="0.6"
        strokeWidth="1.2"
        strokeLinecap="round"
        fill="none"
      />
      <path
        d="M42 11 H61 V25 H50 L45 29 V25 H42 Z"
        stroke={accent}
        strokeWidth="1.6"
        strokeLinejoin="round"
        fill="none"
      />
      <circle cx="48" cy="18" r="1.3" fill={accent} />
      <circle cx="52" cy="18" r="1.3" fill={accent} />
      <circle cx="56" cy="18" r="1.3" fill={accent} />
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
