import type { Metadata } from "next";
import NextLink from "next/link";

export const metadata: Metadata = {
  title: "Strawberry Shake Page Variations",
  description:
    "Preview variations for the ChilliCream Strawberry Shake product page.",
  robots: { index: false, follow: false },
};

/* -------------------------------------------------------------------------- */
/*  Manifest                                                                   */
/*  Each variation is verified to exist under                                  */
/*  app/(content)/products/strawberryshake/preview/v<n>/page.tsx. `present`    */
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
    stance: "Code-First Catalogue",
    tagline: "Every piece Strawberry Shake generates, laid out as a surface.",
    summary:
      "A reference shelf of the MSBuild code generator output, client APIs, and store primitives for the .NET developer scanning the box before they open it.",
    angle: "Catalogue",
    accent: "#16b9e4",
    present: true,
  },
  {
    slug: "v2",
    stance: "End-to-End Story",
    tagline: "Schema in, typed client out, store wired into the UI.",
    summary:
      "The page reads like a session at the keyboard, from .graphql operation to generated client, store, and reactive bindings inside a running .NET app.",
    angle: "Narrative",
    accent: "#7c92c6",
    present: true,
  },
  {
    slug: "v3",
    stance: "For-.NET-UI Positioning",
    tagline: "Where a typed GraphQL client changes the .NET UI math.",
    summary:
      "An argument for Strawberry Shake in Blazor, MAUI, WPF, and Avalonia, framed against handwritten HttpClient code and untyped JSON paths.",
    angle: "Positioning",
    accent: "#f0786a",
    present: true,
  },
  {
    slug: "v4",
    stance: "The Reference Manual",
    tagline: "Strawberry Shake laid out as a sidebar-navigable manual.",
    summary:
      "A reference-manual treatment with a persistent sidebar table of contents, deep-linkable sections, and a steady reading column for the .NET developer working through generator output, client APIs, and store primitives.",
    angle: "Sidebar TOC",
    accent: "var(--cc-accent)",
    present: true,
  },
  {
    slug: "v5",
    stance: "The Reference Card",
    tagline: "Strawberry Shake compressed into a dense reference card.",
    summary:
      "A dense catalog rendering of the product page, tight rows and tabular columns packing MSBuild codegen, client surface, and store guarantees into a scan-first reference card.",
    angle: "Dense Catalog",
    accent: "var(--cc-accent)",
    present: true,
  },
  {
    slug: "v6",
    stance: "The House Pour",
    tagline: "Strawberry Shake served from the bar, .graphql in, typed C# out.",
    summary:
      "A barista-bar metaphor for the product page, order tickets as .graphql operations, the MSBuild grinder turning them into typed C# records, store, and subscriptions, all poured for the .NET developer at the counter.",
    angle: "Barista Bar",
    accent: "var(--cc-accent)",
    present: true,
  },
  {
    slug: "v7",
    stance: "Build-Time Forge",
    tagline: "An animated MSBuild forge that turns .graphql into typed C#.",
    summary:
      "A motion-led showcase of the product page, an animated MSBuild forge driving .graphql operations through codegen into typed records, a normalized store, and live subscriptions for the .NET developer watching the build pipeline run.",
    angle: "Motion Showcase",
    accent: "var(--cc-accent)",
    present: true,
  },
  {
    slug: "v8",
    stance: "Field Postcards from the Build",
    tagline:
      "Dispatches from the build pipeline, each stop on a hand-stamped postcard.",
    summary:
      "A postcard-collage treatment of the product page, MSBuild codegen, the typed GraphQL client, the normalized store, and live subscriptions arriving as field postcards from the .NET build, each one stamped, captioned, and pinned for the developer reading the route.",
    angle: "Postcard Collage",
    accent: "#f0786a",
    present: true,
  },
  {
    slug: "v9",
    stance: "Tag Atlas",
    tagline: "The whole surface mapped as a cluster of linked capability tags.",
    summary:
      "A tag-cluster atlas of the product page, MSBuild codegen, typed client APIs, store guarantees, fetch strategies, and subscriptions plotted as a navigable constellation of tags the .NET developer can trace from one capability to the next.",
    angle: "Tag Cluster",
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

export default function StrawberryShakePreviewIndexPage() {
  return (
    <div className="py-6">
      <header className="relative">
        <p className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.22em] uppercase">
          Products / Strawberry Shake / Preview
        </p>
        <h1 className="font-heading text-cc-heading text-hero mt-4 max-w-3xl font-semibold tracking-tight">
          Nine stances on the same{" "}
          <span
            className="bg-clip-text text-transparent"
            style={{ backgroundImage: SPECTRUM }}
          >
            Strawberry Shake
          </span>{" "}
          product page.
        </h1>
        <p className="lead text-cc-ink mt-6 max-w-2xl">
          Internal preview, not indexed. Each variation reframes the same
          surface for a different reader: the cataloguer, the builder walking a
          story, the .NET UI lead weighing the platform call, a neon runtime
          console, a brutalist spec sheet, the house pour at the barista bar, an
          animated build-time forge, field postcards from the build, and a tag
          atlas of the surface.
        </p>

        <dl className="text-cc-ink-dim mt-10 grid gap-4 font-mono text-[0.72rem] tracking-tight sm:grid-cols-3">
          <div className="border-cc-card-border bg-cc-card-bg/40 rounded-lg border p-3">
            <dt className="text-cc-nav-label text-[0.6rem] tracking-[0.22em] uppercase">
              Track
            </dt>
            <dd className="text-cc-heading mt-1">Strawberry Shake</dd>
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
        aria-label="Strawberry Shake page variations"
        className="mt-12 grid gap-6 md:grid-cols-3"
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
        <span>Strawberry Shake / Page Lab</span>
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
      href={`/products/strawberryshake/preview/${variation.slug}`}
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
          style={{
            boxShadow: `inset 0 0 0 1px color-mix(in srgb, ${variation.accent} 20%, transparent)`,
          }}
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
          /products/strawberryshake/preview/{variation.slug}
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
    // Story: schema-to-client flow, arcing line with waypoints.
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

  if (slug === "v4") {
    // Neon cyberpunk: scanline readout with a glowing pulse.
    return (
      <svg
        aria-hidden="true"
        viewBox="0 0 64 40"
        className="mt-6 h-10 w-16"
        fill="none"
      >
        <line
          x1="2"
          y1="8"
          x2="62"
          y2="8"
          stroke={accent}
          strokeOpacity="0.35"
        />
        <line
          x1="2"
          y1="16"
          x2="62"
          y2="16"
          stroke={accent}
          strokeOpacity="0.35"
        />
        <line
          x1="2"
          y1="24"
          x2="62"
          y2="24"
          stroke={accent}
          strokeOpacity="0.35"
        />
        <line
          x1="2"
          y1="32"
          x2="62"
          y2="32"
          stroke={accent}
          strokeOpacity="0.35"
        />
        <path
          d="M2 20 L18 20 L22 12 L28 28 L34 16 L40 24 L46 20 L62 20"
          stroke={accent}
          strokeWidth="1.4"
          strokeLinecap="round"
          strokeLinejoin="round"
        />
        <circle cx="28" cy="28" r="2" fill={accent} />
      </svg>
    );
  }

  if (slug === "v5") {
    // Brutalist: heavy block grid with a single solid accent cell.
    return (
      <svg
        aria-hidden="true"
        viewBox="0 0 64 40"
        className="mt-6 h-10 w-16"
        fill="none"
      >
        <rect
          x="2"
          y="2"
          width="60"
          height="36"
          stroke={accent}
          strokeWidth="2"
        />
        <line x1="22" y1="2" x2="22" y2="38" stroke={accent} strokeWidth="2" />
        <line x1="42" y1="2" x2="42" y2="38" stroke={accent} strokeWidth="2" />
        <line x1="2" y1="20" x2="62" y2="20" stroke={accent} strokeWidth="2" />
        <rect x="22" y="2" width="20" height="18" fill={accent} />
      </svg>
    );
  }

  if (slug === "v6") {
    // Barista: a pour-over cone with drip line and a cup beneath.
    return (
      <svg
        aria-hidden="true"
        viewBox="0 0 64 40"
        className="mt-6 h-10 w-16"
        fill="none"
      >
        <path
          d="M18 4 L46 4 L36 20 L28 20 Z"
          stroke={accent}
          strokeWidth="1.4"
          strokeLinejoin="round"
        />
        <line
          x1="32"
          y1="20"
          x2="32"
          y2="28"
          stroke={accent}
          strokeWidth="1.4"
          strokeLinecap="round"
        />
        <path
          d="M22 28 L42 28 L40 38 L24 38 Z"
          stroke={accent}
          strokeWidth="1.4"
          strokeLinejoin="round"
        />
        <ellipse
          cx="32"
          cy="28"
          rx="10"
          ry="1.6"
          stroke={accent}
          strokeOpacity="0.5"
        />
        <circle cx="32" cy="34" r="1.4" fill={accent} />
      </svg>
    );
  }

  if (slug === "v7") {
    // Motion forge: anvil silhouette with rising spark trails.
    return (
      <svg
        aria-hidden="true"
        viewBox="0 0 64 40"
        className="mt-6 h-10 w-16"
        fill="none"
      >
        <path
          d="M8 26 L56 26 L50 32 L14 32 Z"
          stroke={accent}
          strokeWidth="1.4"
          strokeLinejoin="round"
        />
        <rect
          x="26"
          y="32"
          width="12"
          height="6"
          stroke={accent}
          strokeWidth="1.4"
        />
        <path
          d="M20 26 L24 14"
          stroke={accent}
          strokeWidth="1.4"
          strokeLinecap="round"
        />
        <path
          d="M32 26 L34 8"
          stroke={accent}
          strokeWidth="1.4"
          strokeLinecap="round"
        />
        <path
          d="M44 26 L46 16"
          stroke={accent}
          strokeWidth="1.4"
          strokeLinecap="round"
        />
        <circle cx="24" cy="14" r="1.4" fill={accent} />
        <circle cx="34" cy="8" r="1.6" fill={accent} />
        <circle cx="46" cy="16" r="1.4" fill={accent} />
      </svg>
    );
  }

  if (slug === "v8") {
    // Postcard collage: overlapping stamped cards pinned at angles.
    return (
      <svg
        aria-hidden="true"
        viewBox="0 0 64 40"
        className="mt-6 h-10 w-16"
        fill="none"
      >
        <rect
          x="4"
          y="8"
          width="26"
          height="18"
          rx="2"
          stroke={accent}
          strokeOpacity="0.5"
          transform="rotate(-8 17 17)"
        />
        <rect
          x="22"
          y="6"
          width="26"
          height="18"
          rx="2"
          stroke={accent}
          strokeWidth="1.4"
          transform="rotate(5 35 15)"
        />
        <rect
          x="34"
          y="14"
          width="14"
          height="10"
          rx="1"
          stroke={accent}
          strokeOpacity="0.55"
          transform="rotate(5 41 19)"
        />
        <circle cx="44" cy="9" r="2" fill={accent} />
      </svg>
    );
  }

  if (slug === "v9") {
    // Tag cluster: a hub node with capability tags linked outward.
    return (
      <svg
        aria-hidden="true"
        viewBox="0 0 64 40"
        className="mt-6 h-10 w-16"
        fill="none"
      >
        <line
          x1="32"
          y1="20"
          x2="12"
          y2="8"
          stroke={accent}
          strokeOpacity="0.4"
        />
        <line
          x1="32"
          y1="20"
          x2="54"
          y2="10"
          stroke={accent}
          strokeOpacity="0.4"
        />
        <line
          x1="32"
          y1="20"
          x2="14"
          y2="32"
          stroke={accent}
          strokeOpacity="0.4"
        />
        <line
          x1="32"
          y1="20"
          x2="52"
          y2="30"
          stroke={accent}
          strokeOpacity="0.4"
        />
        <circle cx="32" cy="20" r="3" fill={accent} />
        <rect
          x="4"
          y="4"
          width="16"
          height="7"
          rx="3.5"
          stroke={accent}
          strokeWidth="1.3"
        />
        <rect
          x="46"
          y="6"
          width="14"
          height="7"
          rx="3.5"
          stroke={accent}
          strokeOpacity="0.6"
        />
        <rect
          x="6"
          y="29"
          width="14"
          height="7"
          rx="3.5"
          stroke={accent}
          strokeOpacity="0.6"
        />
        <rect
          x="44"
          y="27"
          width="16"
          height="7"
          rx="3.5"
          stroke={accent}
          strokeOpacity="0.6"
        />
      </svg>
    );
  }

  // Positioning: two stacks side by side with a divider.
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
