/**
 * "Agentic coding" scene, concept #5 ("SKILL.md as source of truth"), v3
 * "Signal & Metrics" (dark cc-* panel).
 *
 * Leads with the measured result: ONE reviewed, checked-in SKILL.md is the single
 * source of truth a coding agent loads before it calls the MCP tools. The hero is
 * a cream display "1" with a font-heading unit ("reviewed SKILL.md"), the inverted
 * ScrollScenes Stat idiom. Beneath it the SKILL.md artifact is demoted to grey
 * supporting structure (not a literal editor window): a cc-surface micro-panel
 * naming exactly what the file declares (the YAML frontmatter, the /graphql/mcp
 * example, and the createReview @destructive hint).
 *
 * The one teal accent is the "measure mark" on the artifact header: a 1px teal
 * tick ending in the single filled teal node, marking that one file as the live
 * source of truth (the value the headline names). The createReview destructive
 * hint is the cell's only genuine status, so coral owns it alone; teal still owns
 * the metric and nothing else is tinted. A dashed-divider line closes the read.
 *
 * Content is faithful to the v2 FeedbackVariant5: a reviewed, checked-in SKILL.md
 * with frontmatter, a /graphql/mcp example, and the createReview @destructive hint.
 *
 * Static settled frame: a React Server Component, no "use client", no hooks, no
 * motion. Teal is the lone decorative hue; coral is rationed to the one status
 * line. Any svg id would be prefixed "v3-feedback-5-" (this take needs none).
 */
const cc = {
  ink: "#a1a3af",
  accent: "#5eead4",
  coral: "#f0786a",
} as const;

interface FeedbackVariant5Props {
  readonly className?: string;
}

/** One declared line from the checked-in artifact, in file order. */
interface Declared {
  readonly key: string;
  readonly value: string;
  /** The destructive tool hint: the single status (coral) element. */
  readonly status?: boolean;
}

const DECLARED: readonly Declared[] = [
  { key: "frontmatter", value: "name, tools" },
  { key: "example", value: "/graphql/mcp" },
  { key: "createReview", value: "@destructive", status: true },
];

export function FeedbackVariant5({ className }: FeedbackVariant5Props) {
  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      aria-hidden="true"
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        {/* eyebrow: names the view, identical placement across the set */}
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
          source of truth
        </p>

        {/* HERO: one reviewed SKILL.md, the honest headline metric */}
        <div className="mt-3 flex items-baseline gap-2.5">
          <span
            className="text-cc-heading font-heading leading-none font-semibold"
            style={{ fontSize: "2.75rem" }}
          >
            1
          </span>
          <span className="text-cc-ink-dim font-heading text-[0.95rem] font-semibold">
            reviewed SKILL.md
          </span>
        </div>
        <p className="text-cc-ink-dim mt-1.5 font-mono text-[0.7rem] lowercase">
          loaded before the agent calls the tools
        </p>

        {/* the SKILL.md artifact (demoted support): a cc-surface micro-panel. The
            single teal measure mark on its header names the one checked-in source
            of truth; the createReview hint carries the lone coral status. */}
        <div className="border-cc-card-border bg-cc-surface mt-4 overflow-hidden rounded-lg border">
          {/* header: the filename, the teal measure mark, the checked-in tag */}
          <div className="border-cc-card-border flex items-center justify-between border-b px-3 py-2">
            <span className="flex items-center gap-2">
              {/* measure mark: 1px teal tick + the one filled teal node */}
              <svg
                viewBox="0 0 24 14"
                width="24"
                height="14"
                aria-hidden="true"
                style={{ display: "block", overflow: "visible" }}
              >
                <line
                  x1="1"
                  y1="7"
                  x2="14"
                  y2="7"
                  stroke={cc.accent}
                  strokeWidth="1"
                />
                <circle cx="18" cy="7" r="2.75" fill={cc.accent} />
              </svg>
              <span className="text-cc-heading font-mono text-[0.62rem]">
                SKILL.md
              </span>
            </span>
            <span className="text-cc-nav-label border-cc-card-border rounded border px-1.5 py-0.5 font-mono text-[0.5rem] tracking-[0.08em] uppercase">
              checked in
            </span>
          </div>

          {/* declared lines: frontmatter, the /graphql/mcp example, the hint */}
          <div className="grid gap-1.5 px-3 py-2.5">
            {DECLARED.map((d) => (
              <div key={d.key} className="flex items-baseline gap-2.5">
                <span className="text-cc-nav-label w-24 shrink-0 font-mono text-[0.55rem] whitespace-nowrap">
                  {d.key}
                </span>
                <span
                  className="font-mono text-[0.62rem] whitespace-nowrap"
                  style={{ color: d.status ? cc.coral : cc.ink }}
                >
                  {d.value}
                </span>
              </div>
            ))}
          </div>
        </div>

        {/* dashed-divider footnote: the honest read, no second accent */}
        <div className="border-cc-ink-faint mt-4 border-t border-dashed pt-3">
          <p className="text-cc-ink-dim text-center font-mono text-[0.62rem]">
            a checked-in artifact grounds the agent
          </p>
        </div>
      </div>
    </div>
  );
}
