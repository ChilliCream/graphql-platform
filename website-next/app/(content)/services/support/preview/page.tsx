import type { Metadata } from "next";
import NextLink from "next/link";

export const metadata: Metadata = {
  title: "Support Page Variations",
  description: "Preview variations for the ChilliCream Support page.",
  robots: { index: false, follow: false },
};

interface Variant {
  readonly slug: "v1" | "v2" | "v3" | "v4" | "v5" | "v6" | "v7" | "v8" | "v9";
  readonly index: string;
  readonly stance: string;
  readonly description: string;
  readonly accent: string;
  readonly generated: boolean;
}

const VARIANTS: readonly Variant[] = [
  {
    slug: "v1",
    index: "01",
    stance: "Clarity-First Tiers",
    description:
      "Leads with a side-by-side tier comparison so buyers can self-select in one glance.",
    accent: "#16b9e4",
    generated: true,
  },
  {
    slug: "v2",
    index: "02",
    stance: "SLA-Promise Forward",
    description:
      "Puts response times, coverage windows, and the on-call promise at the top of the page.",
    accent: "#7c92c6",
    generated: true,
  },
  {
    slug: "v3",
    index: "03",
    stance: "Engagement Funnel",
    description:
      "Walks the reader from triage through long-term partnership as a guided narrative.",
    accent: "#f0786a",
    generated: true,
  },
  {
    slug: "v4",
    index: "04",
    stance: "The Handbook",
    description:
      "Frames support as a reference handbook with a persistent sidebar table of contents that lets buyers scan tiers, SLAs, and scope.",
    accent: "#16b9e4",
    generated: true,
  },
  {
    slug: "v5",
    index: "05",
    stance: "SUPPORT.REGISTRY",
    description:
      "Lays out every tier, response time, and coverage line as a dense catalog so operators can compare entries at a glance.",
    accent: "#16b9e4",
    generated: true,
  },
  {
    slug: "v6",
    index: "06",
    stance: "Service Bar",
    description:
      "Presents tiers as a barista-style menu where each support plan reads like a hand-prepared order with crafted ingredients.",
    accent: "var(--color-cc-accent)",
    generated: true,
  },
  {
    slug: "v7",
    index: "07",
    stance: "Response Clock",
    description:
      "Centers the page on an animated response-time showcase so SLAs, escalation paths, and on-call rhythm read at a glance.",
    accent: "var(--color-cc-accent)",
    generated: true,
  },
  {
    slug: "v8",
    index: "08",
    stance: "Numbered Ascent",
    description:
      "Stacks the tiers as oversized numerals that build a clear climb from first response to full partnership.",
    accent: "#f0786a",
    generated: true,
  },
  {
    slug: "v9",
    index: "09",
    stance: "Ruled Ledger",
    description:
      "Sets each tier behind an eyebrow rule so SLAs, scope, and coverage line up like entries in a precise ledger.",
    accent: "#16b9e4",
    generated: true,
  },
];

export default function SupportPreviewIndexPage() {
  return (
    <div className="py-12 sm:py-16">
      <header className="border-cc-card-border border-b pb-10">
        <div className="text-cc-nav-label font-mono text-xs font-semibold tracking-widest uppercase">
          Internal preview
        </div>
        <h1 className="font-heading text-cc-heading text-h2 sm:text-h1 mt-3">
          Support Page Variations
        </h1>
        <p className="text-cc-ink lead mt-4 max-w-2xl">
          Nine stances for the Support page. Open each variant to review the
          full layout, then pick the direction that fits the audience.
        </p>
        <p className="text-cc-ink-dim mt-2 max-w-2xl text-sm">
          Not indexed. Linked from no production page.
        </p>
      </header>

      <ol className="mt-12 flex flex-col gap-6">
        {VARIANTS.map((variant) => (
          <li key={variant.slug}>
            {variant.generated ? (
              <NextLink
                href={`/services/support/preview/${variant.slug}`}
                className="bg-cc-card-bg border-cc-card-border hover:border-cc-card-border-hover group relative block overflow-hidden rounded-2xl border p-8 transition-colors sm:p-10"
              >
                <VariantCardBody variant={variant} />
              </NextLink>
            ) : (
              <div className="bg-cc-card-bg border-cc-card-border relative block overflow-hidden rounded-2xl border border-dashed p-8 opacity-70 sm:p-10">
                <VariantCardBody variant={variant} />
              </div>
            )}
          </li>
        ))}
      </ol>
    </div>
  );
}

interface VariantCardBodyProps {
  readonly variant: Variant;
}

function VariantCardBody({ variant }: VariantCardBodyProps) {
  return (
    <>
      <span
        aria-hidden="true"
        className="absolute inset-y-0 left-0 w-1"
        style={{ background: variant.accent }}
      />
      <div className="flex flex-col gap-6 sm:flex-row sm:items-start sm:gap-10">
        <div
          className="font-heading shrink-0 text-5xl leading-none font-semibold sm:text-6xl"
          style={{ color: variant.accent }}
        >
          {variant.index}
        </div>
        <div className="min-w-0 flex-1">
          <div className="text-cc-nav-label font-mono text-xs font-semibold tracking-widest uppercase">
            Variant {variant.slug}
          </div>
          <h2 className="font-heading text-cc-heading text-h3 mt-2">
            {variant.stance}
          </h2>
          <p className="text-cc-ink mt-3 max-w-2xl">{variant.description}</p>
          {variant.generated ? (
            <div className="text-cc-accent mt-6 inline-flex items-center gap-2 text-sm font-medium">
              Open variant
              <svg
                aria-hidden="true"
                width="16"
                height="16"
                viewBox="0 0 16 16"
                fill="none"
                className="transition-transform group-hover:translate-x-1"
              >
                <path
                  d="M3 8h10M9 4l4 4-4 4"
                  stroke="currentColor"
                  strokeWidth="1.5"
                  strokeLinecap="round"
                  strokeLinejoin="round"
                />
              </svg>
            </div>
          ) : (
            <div className="text-cc-ink-dim mt-6 inline-flex items-center gap-2 font-mono text-xs font-semibold tracking-widest uppercase">
              Not generated
            </div>
          )}
        </div>
      </div>
    </>
  );
}
