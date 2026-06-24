import type { Metadata } from "next";
import NextLink from "next/link";

export const metadata: Metadata = {
  title: "Training Page Variations",
  description: "Preview variations for the ChilliCream Training page.",
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
    stance: "Curriculum Catalog",
    description:
      "Lays out every course, module, and learning path so buyers can browse the full catalog at a glance.",
    accent: "#16b9e4",
    generated: true,
  },
  {
    slug: "v2",
    index: "02",
    stance: "Team Outcomes",
    description:
      "Leads with the capabilities a team walks away with, then connects each outcome to the training that produces it.",
    accent: "#7c92c6",
    generated: true,
  },
  {
    slug: "v3",
    index: "03",
    stance: "Mixed-Level Workshop",
    description:
      "Frames training as a guided workshop that meets juniors, seniors, and architects on the same engagement.",
    accent: "#f0786a",
    generated: true,
  },
  {
    slug: "v4",
    index: "04",
    stance: "The Long Read",
    description:
      "A centered, narrative treatment that reads training as a single considered essay, letting the curriculum unfold paragraph by paragraph.",
    accent: "var(--color-cc-accent)",
    generated: true,
  },
  {
    slug: "v5",
    index: "05",
    stance: "The Course Reference",
    description:
      "A sidebar table of contents pairs with reference-style sections, framing the training catalog as documentation you can scan and revisit.",
    accent: "var(--color-cc-accent)",
    generated: true,
  },
  {
    slug: "v6",
    index: "06",
    stance: "House Blend",
    description:
      "A barista-style menu of curated training blends, mixing audiences, depths, and formats into signature drinks the team can order off the board.",
    accent: "var(--color-cc-accent)",
    generated: true,
  },
  {
    slug: "v7",
    index: "07",
    stance: "Curriculum Constellation",
    description:
      "A motion-led showcase that maps courses as a constellation of connected nodes, animating the learning paths that link foundations to mastery.",
    accent: "var(--color-cc-accent)",
    generated: true,
  },
  {
    slug: "v8",
    index: "08",
    stance: "The Kilometre Rail",
    description:
      "Plots the curriculum along a measured route, marking each course as a kilometre post so the team can track distance covered from first concepts to the finish line.",
    accent: "var(--color-cc-accent)",
    generated: true,
  },
  {
    slug: "v9",
    index: "09",
    stance: "Reel Six: The Training Filmstrip",
    description:
      "Frames the program as a strip of film, advancing the team frame by frame through scenes of practice, review, and mastery in one continuous reel.",
    accent: "#f0786a",
    generated: true,
  },
];

export default function TrainingPreviewIndexPage() {
  return (
    <div className="py-12 sm:py-16">
      <header className="border-cc-card-border border-b pb-10">
        <div className="text-cc-nav-label font-mono text-xs font-semibold tracking-widest uppercase">
          Internal preview
        </div>
        <h1 className="font-heading text-cc-heading text-h2 sm:text-h1 mt-3">
          Training Page Variations
        </h1>
        <p className="text-cc-ink lead mt-4 max-w-2xl">
          Nine stances for the Training page. Open each variant to review the
          full layout, then pick the direction that fits the audience.
        </p>
        <p className="text-cc-ink-dim mt-2 max-w-2xl text-sm">
          Not indexed. Linked from no production page.
        </p>
      </header>

      <ol className="mt-12 flex flex-col gap-6">
        {VARIANTS.map((variant) =>
          variant.generated ? (
            <li key={variant.slug}>
              <NextLink
                href={`/services/training/preview/${variant.slug}`}
                className="bg-cc-card-bg border-cc-card-border hover:border-cc-card-border-hover group relative block overflow-hidden rounded-2xl border p-8 transition-colors sm:p-10"
              >
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
                    <p className="text-cc-ink mt-3 max-w-2xl">
                      {variant.description}
                    </p>
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
                  </div>
                </div>
              </NextLink>
            </li>
          ) : (
            <li key={variant.slug}>
              <div className="bg-cc-card-bg/60 border-cc-card-border relative block overflow-hidden rounded-2xl border border-dashed p-8 sm:p-10">
                <span
                  aria-hidden="true"
                  className="absolute inset-y-0 left-0 w-1 opacity-60"
                  style={{ background: variant.accent }}
                />
                <div className="flex flex-col gap-6 sm:flex-row sm:items-start sm:gap-10">
                  <div
                    className="font-heading shrink-0 text-5xl leading-none font-semibold opacity-60 sm:text-6xl"
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
                    <p className="text-cc-ink mt-3 max-w-2xl">
                      {variant.description}
                    </p>
                    <p className="text-cc-nav-label mt-6 font-mono text-xs font-semibold tracking-widest uppercase">
                      not generated
                    </p>
                  </div>
                </div>
              </div>
            </li>
          ),
        )}
      </ol>
    </div>
  );
}
