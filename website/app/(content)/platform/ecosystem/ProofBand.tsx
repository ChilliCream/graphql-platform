import { RevealOnScroll } from "@/src/components/RevealOnScroll";
import { SectionHeading } from "@/src/components/SectionHeading";
import { GITHUB_REPO_URL } from "@/src/components/header/navData";
import { Card } from "@/src/design-system/Card";
import { GitHubIcon } from "@/src/icons/GitHub";

import { CARD_FOCUS_CLASSES } from "./cardFocus";

const HEATMAP_LEVELS = [
  "rgba(245,240,234,0.06)",
  "#0e4429",
  "#006d32",
  "#26a641",
  "#39d353",
] as const;

interface CommitHeatmapProps {
  readonly weeks: ReadonlyArray<ReadonlyArray<number>>;
}

function CommitHeatmap({ weeks }: CommitHeatmapProps) {
  const maxCount = Math.max(...weeks.flat(), 1);
  return (
    <div
      aria-hidden="true"
      className="grid auto-cols-fr grid-flow-col grid-rows-7 gap-[2px]"
    >
      {weeks.flatMap((days, weekIndex) =>
        days.map((count, dayIndex) => (
          <div
            key={`${weekIndex}-${dayIndex}`}
            className="aspect-square rounded-[2px]"
            style={{
              backgroundColor:
                HEATMAP_LEVELS[
                  count === 0
                    ? 0
                    : Math.min(4, Math.ceil((count / maxCount) * 4))
                ],
            }}
          />
        )),
      )}
    </div>
  );
}

interface ProofRowSpec {
  readonly tag: string;
  readonly body: string;
}

const PROOF_ROWS: readonly ProofRowSpec[] = [
  {
    tag: "ONE REPOSITORY",
    body: "The core server, gateway, client, and libraries share one codebase, so the pieces stay in step.",
  },
  {
    tag: "MIT LICENSE",
    body: "Open source under the MIT license. Free to use in commercial products.",
  },
  {
    tag: "PUBLIC DEVELOPMENT",
    body: "Follow issues, pull requests, releases, and changelogs as the work lands.",
  },
];

interface ProofBandProps {
  readonly commitActivity: ReadonlyArray<ReadonlyArray<number>>;
}

export function ProofBand({ commitActivity }: ProofBandProps) {
  return (
    <section className="py-14 sm:py-20">
      <RevealOnScroll>
        <div className="grid grid-cols-1 items-center gap-10 lg:grid-cols-12 lg:gap-16">
          <div className="min-w-0 lg:col-span-5">
            <SectionHeading
              title="Built in the open, in one repository."
              description="The server, gateway, client, and core libraries are all developed in a single public GitHub repository."
            />
          </div>
          <div className="min-w-0 lg:col-span-7">
            <Card
              as="a"
              href={GITHUB_REPO_URL}
              target="_blank"
              rel="noopener noreferrer"
              hoverBorder
              className={`block no-underline backdrop-blur ${CARD_FOCUS_CLASSES}`}
            >
              <div className="border-cc-card-border flex items-center justify-between gap-3 border-b px-6 py-4">
                <span className="flex min-w-0 items-center gap-3">
                  <span className="border-cc-card-border bg-cc-surface text-cc-ink-dim flex h-9 w-9 shrink-0 items-center justify-center rounded-full border">
                    <GitHubIcon className="h-4 w-4 fill-current" />
                  </span>
                  <span className="text-cc-heading truncate font-mono text-sm">
                    ChilliCream/graphql-platform
                  </span>
                </span>
                <span aria-hidden="true" className="text-cc-ink-dim">
                  ↗
                </span>
              </div>
              <div className="divide-cc-card-border divide-y">
                {PROOF_ROWS.map((row) => (
                  <div
                    key={row.tag}
                    className="grid grid-cols-1 items-baseline gap-1 px-6 py-4 sm:grid-cols-[11rem_1fr] sm:gap-6"
                  >
                    <span className="text-cc-ink-dim font-mono text-[0.6rem] tracking-[0.18em] uppercase">
                      {row.tag}
                    </span>
                    <span className="text-cc-ink text-sm">{row.body}</span>
                  </div>
                ))}
              </div>
              <div className="border-cc-card-border border-t">
                <div className="pt-3">
                  <CommitHeatmap weeks={commitActivity} />
                </div>
              </div>
            </Card>
          </div>
        </div>
      </RevealOnScroll>
    </section>
  );
}
