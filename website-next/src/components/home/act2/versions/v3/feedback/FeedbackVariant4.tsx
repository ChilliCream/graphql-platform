/**
 * "Agentic coding" scene, concept 4 ("Governed tool lifecycle"), v3 "Signal &
 * Metrics" (dark cc-* panel).
 *
 * Re-expresses the v2 SKILL.md tool (`search-eshops-catalog`, the agent skill
 * that queries the MCP endpoint) as the measured result the v3 strategy leads
 * with: the tool walks one governed promotion path and has cleared 3 of its 4
 * lifecycle states, settling at the human approval gate. So the hero is the cream
 * "3 / 4" numeral over the lowercase mono caption "lifecycle: approved", and the
 * single teal signal is a 4-cell lifecycle segment row (layout B): authored,
 * validated, and approved are the three traversed teal cells, while "published"
 * stays the dashed pending cell still ahead. The approved cell is also the
 * governance gate, so it alone carries a violet border, the cell's only genuine
 * status; teal owns the three progress cells, violet owns only the gate and the
 * footer reading, and the numeral stays cream.
 *
 * Content is faithful to the v2 governed-lifecycle take (the search-eshops-catalog
 * tool walking a governed promotion path with a resolved human approval gate).
 * Only the visual language changes to the v3 dark metrics panel.
 *
 * Static settled frame: no animation, no motion, no hooks, no "use client".
 * Server component, aria-hidden root. Local cc palette mirrors the cc-* tokens
 * exactly. Any svg id would be prefixed "v3-feedback-4-" (this take needs none).
 */

interface FeedbackVariant4Props {
  readonly className?: string;
}

/* Strict cc-* dark palette, mirrored locally per the v3 system. Teal is the only
 * decorative hue and is bound to the lifecycle progress signal (the three cleared
 * cells); violet encodes the single genuine status, the human approval gate. */
const cc = {
  surface: "#0c1322",
  heading: "#f5f0ea",
  ink: "#a1a3af",
  inkDim: "rgba(245,241,234,0.62)",
  cardBorder: "rgba(245,241,234,0.12)",
  inkFaint: "rgba(245,241,234,0.16)",
  navLabel: "#62748e",
  accent: "#5eead4",
  violet: "#7c92c6",
  mono: 'ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, "Liberation Mono", monospace',
  display: '"Josefin Sans", Futura, sans-serif',
} as const;

/** A lifecycle state the tool is promoted through. */
interface LifecycleCell {
  readonly label: string;
  /** "cleared" cells are traversed (teal); "gate" is a cleared cell that is also
   * the governance approval (teal fill, violet border); "pending" is the state
   * still ahead (dashed). */
  readonly kind: "cleared" | "gate" | "pending";
}

/* search-eshops-catalog has cleared author and validate, just passed the human
 * approval gate, and is promoting into production. */
const LIFECYCLE: readonly LifecycleCell[] = [
  { label: "authored", kind: "cleared" },
  { label: "validated", kind: "cleared" },
  { label: "approved", kind: "gate" },
  { label: "published", kind: "pending" },
];

export function FeedbackVariant4({ className }: FeedbackVariant4Props) {
  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      aria-hidden="true"
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        {/* eyebrow + the tool under governance */}
        <div className="flex items-baseline justify-between gap-3">
          <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
            governed lifecycle
          </p>
          <span
            className="shrink-0 whitespace-nowrap"
            style={{
              fontFamily: cc.mono,
              fontSize: "0.55rem",
              color: cc.navLabel,
            }}
          >
            search-eshops-catalog
          </span>
        </div>

        {/* hero numeral: how far the tool has advanced down the governed path */}
        <div className="mt-3">
          <p
            className="text-cc-heading leading-none font-semibold"
            style={{
              fontFamily: cc.display,
              fontSize: "2.25rem",
              fontVariantNumeric: "tabular-nums",
            }}
          >
            3 / 4
          </p>
          <p
            className="text-cc-ink-dim mt-2 lowercase"
            style={{ fontFamily: cc.mono, fontSize: "0.7rem" }}
          >
            lifecycle: approved
          </p>
        </div>

        {/* the teal signal: 4 lifecycle states as segment cells. authored,
            validated, and approved are the three traversed teal cells (the "3 / 4"
            the headline names); "approved" is also the governance gate, so it
            alone carries the violet border; "published" is the dashed pending cell
            still ahead. */}
        <div className="mt-4">
          <div className="flex items-center gap-1.5">
            {LIFECYCLE.map((cell) => {
              if (cell.kind === "pending") {
                return (
                  <span
                    key={cell.label}
                    className="flex-1 rounded-[3px] border border-dashed"
                    style={{
                      height: 18,
                      background: cc.surface,
                      borderColor: cc.inkFaint,
                    }}
                  />
                );
              }
              return (
                <span
                  key={cell.label}
                  className="flex-1 rounded-[3px]"
                  style={{
                    height: 18,
                    background: cc.accent,
                    opacity: 0.7,
                    border:
                      cell.kind === "gate"
                        ? `1px solid ${cc.violet}`
                        : undefined,
                  }}
                />
              );
            })}
          </div>

          {/* state labels under their cells */}
          <div className="mt-2 flex items-center gap-1.5">
            {LIFECYCLE.map((cell) => (
              <span
                key={cell.label}
                className="flex-1 text-center"
                style={{
                  fontFamily: cc.mono,
                  fontSize: "0.5rem",
                  letterSpacing: "0.04em",
                  color: cell.kind === "gate" ? cc.violet : cc.navLabel,
                  fontWeight: cell.kind === "gate" ? 600 : 400,
                }}
              >
                {cell.label}
              </span>
            ))}
          </div>
        </div>

        {/* interpretation caption under a dashed divider: the gate resolves and
            the tool reaches its destination */}
        <div className="border-cc-ink-faint mt-4 border-t border-dashed pt-3">
          <p
            className="flex items-center gap-1.5"
            style={{
              fontFamily: cc.mono,
              fontSize: "0.62rem",
              color: cc.inkDim,
            }}
          >
            <span
              aria-hidden="true"
              className="rounded-full"
              style={{
                width: 5,
                height: 5,
                flex: "0 0 auto",
                background: cc.violet,
              }}
            />
            <span style={{ color: cc.violet }}>approval granted</span>
            <span>&middot; promotes to production</span>
          </p>
        </div>
      </div>
    </div>
  );
}
