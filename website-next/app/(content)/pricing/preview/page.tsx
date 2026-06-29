import type { Metadata } from "next";
import NextLink from "next/link";

export const metadata: Metadata = {
  title: "Pricing Variations",
  robots: { index: false, follow: false },
};

interface Variant {
  readonly slug: "v1" | "v2" | "v3" | "v4" | "v5" | "v6" | "v7" | "v8" | "v9";
  readonly eyebrow: string;
  readonly name: string;
  readonly summary: string;
  readonly accent: string;
}

const VARIANTS: readonly Variant[] = [
  {
    slug: "v1",
    eyebrow: "V1",
    name: "Classic Three-Tier",
    summary:
      "Three side-by-side plans with a highlighted Most Popular column, optimized for fast comparison.",
    accent: "#16b9e4",
  },
  {
    slug: "v2",
    eyebrow: "V2",
    name: "Calculator-First",
    summary:
      "Lead with an interactive cost estimate so visitors size their plan before they see one.",
    accent: "#7c92c6",
  },
  {
    slug: "v3",
    eyebrow: "V3",
    name: "Story-Led Outcomes",
    summary:
      "Frame the product in motion first, then anchor each plan to the outcome it unlocks.",
    accent: "#f0786a",
  },
  {
    slug: "v4",
    eyebrow: "V4",
    name: "The Pricing CLI",
    summary:
      "A code-walkthrough stance that presents the plans as a terminal session, with prompts, flags, and annotated output.",
    accent: "var(--color-cc-accent)",
  },
  {
    slug: "v5",
    eyebrow: "V5",
    name: "The Pricing Dispatch",
    summary:
      "An editorial-longform stance that frames pricing as a written dispatch, with a lead, pull quotes, and tier sections as chapters.",
    accent: "var(--color-cc-accent)",
  },
  {
    slug: "v6",
    eyebrow: "V6",
    name: "The Nitro Menu",
    summary:
      "A barista-inspired stance that presents the tiers as menu items, with notes, sizes, and pairings instead of feature checklists.",
    accent: "var(--color-cc-accent)",
  },
  {
    slug: "v7",
    eyebrow: "V7",
    name: "Tier Reveal",
    summary:
      "A motion-led showcase where each tier unveils itself in sequence, animating the jump from one plan to the next.",
    accent: "var(--color-cc-accent)",
  },
  {
    slug: "v8",
    eyebrow: "V8",
    name: "Counters at the Switch",
    summary:
      "A countup-ticker stance where the headline numbers (ops, uptime, environments) animate up to their values as each plan comes into view.",
    accent: "var(--color-cc-accent)",
  },
  {
    slug: "v9",
    eyebrow: "V9",
    name: "Tier Cascade",
    summary:
      "A staggered-reveal stance where the tiers cascade into place one after another, stepping the reader up from free to dedicated to self-hosted.",
    accent: "var(--color-cc-accent)",
  },
];

export default function PricingPreviewIndexPage() {
  return (
    <>
      <header className="text-center">
        <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
          Pricing / Preview
        </p>
        <h1 className="font-heading text-cc-heading text-hero mt-3">
          Nine stances on the same plans.
        </h1>
        <p className="lead text-cc-ink mx-auto mt-5 max-w-2xl">
          Internal preview, not indexed. Pick a variation to walk through how it
          frames the same Nitro tiers from a different angle.
        </p>
      </header>

      <section className="mt-16 grid gap-6 md:grid-cols-2 lg:grid-cols-3">
        {VARIANTS.map((variant) => (
          <VariantCard key={variant.slug} variant={variant} />
        ))}
      </section>
    </>
  );
}

interface VariantCardProps {
  readonly variant: Variant;
}

function VariantCard({ variant }: VariantCardProps) {
  return (
    <NextLink
      href={`/pricing/preview/${variant.slug}`}
      className="bg-cc-card-bg/60 border-cc-card-border hover:border-cc-card-border-hover group relative flex flex-col overflow-hidden rounded-3xl border p-7 no-underline transition-colors"
    >
      <span
        aria-hidden="true"
        className="absolute inset-x-0 top-0 h-px opacity-60"
        style={{
          background: `linear-gradient(90deg, transparent, ${variant.accent}, transparent)`,
        }}
      />
      <div className="flex items-center justify-between">
        <span className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
          {variant.eyebrow}
        </span>
        <VariantGlyph slug={variant.slug} accent={variant.accent} />
      </div>
      <h2 className="font-heading text-cc-heading text-h3 mt-8 font-semibold">
        {variant.name}
      </h2>
      <p className="text-cc-ink mt-3 text-sm leading-relaxed">
        {variant.summary}
      </p>
      <span className="text-cc-accent group-hover:text-cc-heading mt-8 inline-flex items-center gap-2 font-mono text-xs tracking-[0.12em] uppercase transition-colors">
        Open variation
        <ArrowGlyph />
      </span>
    </NextLink>
  );
}

interface VariantGlyphProps {
  readonly slug: Variant["slug"];
  readonly accent: string;
}

function VariantGlyph({ slug, accent }: VariantGlyphProps) {
  if (slug === "v1") {
    return (
      <svg
        aria-hidden="true"
        viewBox="0 0 48 32"
        className="h-8 w-12"
        fill="none"
      >
        <rect
          x="1"
          y="5"
          width="12"
          height="22"
          rx="2"
          stroke={accent}
          strokeOpacity="0.55"
        />
        <rect
          x="18"
          y="1"
          width="12"
          height="30"
          rx="2"
          stroke={accent}
          strokeWidth="1.5"
        />
        <rect
          x="35"
          y="5"
          width="12"
          height="22"
          rx="2"
          stroke={accent}
          strokeOpacity="0.55"
        />
      </svg>
    );
  }

  if (slug === "v2") {
    return (
      <svg
        aria-hidden="true"
        viewBox="0 0 48 32"
        className="h-8 w-12"
        fill="none"
      >
        <rect
          x="1"
          y="1"
          width="46"
          height="30"
          rx="3"
          stroke={accent}
          strokeOpacity="0.55"
        />
        <line
          x1="6"
          y1="10"
          x2="42"
          y2="10"
          stroke={accent}
          strokeOpacity="0.35"
        />
        <circle cx="14" cy="10" r="2" fill={accent} />
        <line
          x1="6"
          y1="20"
          x2="42"
          y2="20"
          stroke={accent}
          strokeOpacity="0.35"
        />
        <circle cx="30" cy="20" r="2" fill={accent} />
      </svg>
    );
  }

  if (slug === "v3") {
    return (
      <svg
        aria-hidden="true"
        viewBox="0 0 48 32"
        className="h-8 w-12"
        fill="none"
      >
        <path
          d="M2 24 L14 14 L24 20 L34 8 L46 12"
          stroke={accent}
          strokeWidth="1.5"
          strokeLinecap="round"
          strokeLinejoin="round"
        />
        <circle cx="14" cy="14" r="2" fill={accent} />
        <circle cx="34" cy="8" r="2" fill={accent} />
      </svg>
    );
  }

  if (slug === "v4") {
    return (
      <svg
        aria-hidden="true"
        viewBox="0 0 48 32"
        className="h-8 w-12"
        fill="none"
      >
        <rect
          x="3"
          y="3"
          width="42"
          height="26"
          rx="1"
          stroke={accent}
          strokeOpacity="0.55"
        />
        <line
          x1="24"
          y1="3"
          x2="24"
          y2="29"
          stroke={accent}
          strokeOpacity="0.45"
        />
        <line
          x1="7"
          y1="9"
          x2="21"
          y2="9"
          stroke={accent}
          strokeOpacity="0.55"
        />
        <line
          x1="7"
          y1="14"
          x2="21"
          y2="14"
          stroke={accent}
          strokeOpacity="0.35"
        />
        <line
          x1="7"
          y1="19"
          x2="21"
          y2="19"
          stroke={accent}
          strokeOpacity="0.35"
        />
        <line
          x1="7"
          y1="24"
          x2="21"
          y2="24"
          stroke={accent}
          strokeOpacity="0.35"
        />
        <line
          x1="27"
          y1="9"
          x2="41"
          y2="9"
          stroke={accent}
          strokeOpacity="0.55"
        />
        <line
          x1="27"
          y1="14"
          x2="41"
          y2="14"
          stroke={accent}
          strokeOpacity="0.35"
        />
        <line
          x1="27"
          y1="19"
          x2="41"
          y2="19"
          stroke={accent}
          strokeOpacity="0.35"
        />
        <line
          x1="27"
          y1="24"
          x2="41"
          y2="24"
          stroke={accent}
          strokeOpacity="0.35"
        />
      </svg>
    );
  }

  if (slug === "v5") {
    return (
      <svg
        aria-hidden="true"
        viewBox="0 0 48 32"
        className="h-8 w-12"
        fill="none"
      >
        <rect
          x="3"
          y="3"
          width="42"
          height="26"
          rx="1"
          stroke={accent}
          strokeOpacity="0.55"
        />
        <line x1="7" y1="8" x2="29" y2="8" stroke={accent} strokeWidth="1.5" />
        <line
          x1="7"
          y1="13"
          x2="22"
          y2="13"
          stroke={accent}
          strokeOpacity="0.45"
        />
        <line
          x1="7"
          y1="17"
          x2="22"
          y2="17"
          stroke={accent}
          strokeOpacity="0.35"
        />
        <line
          x1="7"
          y1="21"
          x2="22"
          y2="21"
          stroke={accent}
          strokeOpacity="0.35"
        />
        <line
          x1="7"
          y1="25"
          x2="22"
          y2="25"
          stroke={accent}
          strokeOpacity="0.35"
        />
        <rect
          x="27"
          y="13"
          width="14"
          height="12"
          stroke={accent}
          strokeOpacity="0.45"
        />
      </svg>
    );
  }

  if (slug === "v6") {
    return (
      <svg
        aria-hidden="true"
        viewBox="0 0 48 32"
        className="h-8 w-12"
        fill="none"
      >
        <path
          d="M14 6 H32 V20 a8 8 0 0 1 -8 8 h-2 a8 8 0 0 1 -8 -8 Z"
          stroke={accent}
          strokeOpacity="0.55"
        />
        <path
          d="M32 10 h4 a4 4 0 0 1 0 8 h-4"
          stroke={accent}
          strokeOpacity="0.45"
        />
        <path
          d="M18 2 v3 M23 1 v4 M28 2 v3"
          stroke={accent}
          strokeOpacity="0.55"
          strokeLinecap="round"
        />
      </svg>
    );
  }

  if (slug === "v7") {
    return (
      <svg
        aria-hidden="true"
        viewBox="0 0 48 32"
        className="h-8 w-12"
        fill="none"
      >
        <rect
          x="2"
          y="18"
          width="10"
          height="12"
          rx="1"
          stroke={accent}
          strokeOpacity="0.35"
        />
        <rect
          x="14"
          y="11"
          width="10"
          height="19"
          rx="1"
          stroke={accent}
          strokeOpacity="0.55"
        />
        <rect
          x="26"
          y="4"
          width="10"
          height="26"
          rx="1"
          stroke={accent}
          strokeWidth="1.5"
        />
        <path
          d="M40 14 L44 10 M40 10 L44 14 M40 22 L44 18 M40 18 L44 22"
          stroke={accent}
          strokeOpacity="0.6"
          strokeLinecap="round"
        />
      </svg>
    );
  }

  if (slug === "v8") {
    return (
      <svg
        aria-hidden="true"
        viewBox="0 0 48 32"
        className="h-8 w-12"
        fill="none"
      >
        <rect
          x="2"
          y="6"
          width="13"
          height="20"
          rx="2"
          stroke={accent}
          strokeOpacity="0.55"
        />
        <rect
          x="17.5"
          y="6"
          width="13"
          height="20"
          rx="2"
          stroke={accent}
          strokeWidth="1.5"
        />
        <rect
          x="33"
          y="6"
          width="13"
          height="20"
          rx="2"
          stroke={accent}
          strokeOpacity="0.55"
        />
        <path
          d="M6 10 L11 10 M22 9 L26 9 M37 10 L42 10"
          stroke={accent}
          strokeOpacity="0.45"
          strokeLinecap="round"
        />
        <path
          d="M8.5 14 V22 M24 13 V23 M39.5 14 V22"
          stroke={accent}
          strokeOpacity="0.7"
          strokeLinecap="round"
        />
        <path
          d="M8.5 14 L6.5 16 M8.5 14 L10.5 16 M24 13 L21.5 15.5 M24 13 L26.5 15.5 M39.5 14 L37.5 16 M39.5 14 L41.5 16"
          stroke={accent}
          strokeOpacity="0.55"
          strokeLinecap="round"
        />
      </svg>
    );
  }

  if (slug === "v9") {
    return (
      <svg
        aria-hidden="true"
        viewBox="0 0 48 32"
        className="h-8 w-12"
        fill="none"
      >
        <rect
          x="2"
          y="3"
          width="28"
          height="7"
          rx="1.5"
          stroke={accent}
          strokeWidth="1.5"
        />
        <rect
          x="10"
          y="12.5"
          width="28"
          height="7"
          rx="1.5"
          stroke={accent}
          strokeOpacity="0.55"
        />
        <rect
          x="18"
          y="22"
          width="28"
          height="7"
          rx="1.5"
          stroke={accent}
          strokeOpacity="0.35"
        />
      </svg>
    );
  }

  return (
    <svg
      aria-hidden="true"
      viewBox="0 0 48 32"
      className="h-8 w-12"
      fill="none"
    >
      <rect
        x="2"
        y="2"
        width="44"
        height="28"
        rx="2"
        stroke={accent}
        strokeOpacity="0.45"
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
