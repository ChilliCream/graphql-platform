interface WorkflowsVariant2Props {
  readonly className?: string;
}

/**
 * "Workflow" scene, v5 "Schematic Lines", concept #2: saga state machine.
 *
 * The ReviewSaga drawn as a circular state machine. Three states sit on an
 * implied ring: Draft (entered via the short Start stub) and Checked are done,
 * Published is pending. The grey skeleton is the saga entry stub plus the
 * already-traversed Draft -> Checked arc. The single teal thread is the
 * in-flight transition the saga is processing right now: it leaves the hollow
 * teal source ring at Checked, curves down on the live `ChecksPassed` event, and
 * terminates at the teal Published focal ring with a solid teal dot. Nothing
 * else is teal and there is no status hue (workflows cells carry only the
 * thread); the done states stay cc-ink-faint grey.
 *
 * Content matches the v2 sibling: saga `ReviewSaga`, states Draft -> Checked ->
 * Published, the live transition fired by `ChecksPassed`, validated before
 * traffic. The schematic floats on the shared sibling card with 1px non-scaling
 * stroke vocab only and no inner panel chrome.
 *
 * React Server Component: no hooks, no client APIs, settled final frame only.
 * Every svg id is prefixed "v5-workflows-2-".
 */

const C = {
  ink: "#a1a3af",
  navLabel: "#62748e",
  inkFaint: "rgba(245, 241, 234, 0.16)",
  accent: "#5eead4",
  mono: 'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace',
} as const;

const ID = "v5-workflows-2-";

export function WorkflowsVariant2({ className }: WorkflowsVariant2Props) {
  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      aria-hidden="true"
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
          review saga
        </p>

        <div className="mt-4">
          <svg
            viewBox="0 0 280 150"
            width="100%"
            style={{
              display: "block",
              overflow: "visible",
              fontFamily: C.mono,
            }}
          >
            <defs>
              {/* Grey open chevron for the already-traversed transition. */}
              <marker
                id={`${ID}grey`}
                markerWidth="6"
                markerHeight="6"
                refX="4.6"
                refY="3"
                orient="auto"
                markerUnits="userSpaceOnUse"
              >
                <path
                  d="M0 0.5 L5 3 L0 5.5"
                  fill="none"
                  stroke={C.inkFaint}
                  strokeWidth="1"
                  vectorEffect="non-scaling-stroke"
                  strokeLinecap="round"
                  strokeLinejoin="round"
                />
              </marker>

              {/* Teal open chevron for the single in-flight thread terminus. */}
              <marker
                id={`${ID}teal`}
                markerWidth="6"
                markerHeight="6"
                refX="4.6"
                refY="3"
                orient="auto"
                markerUnits="userSpaceOnUse"
              >
                <path
                  d="M0 0.5 L5 3 L0 5.5"
                  fill="none"
                  stroke={C.accent}
                  strokeWidth="1"
                  vectorEffect="non-scaling-stroke"
                  strokeLinecap="round"
                  strokeLinejoin="round"
                />
              </marker>
            </defs>

            {/* ---- grey skeleton ---- */}

            {/* saga entry stub: the Start transition into Draft */}
            <line
              x1="66"
              y1="46"
              x2="86"
              y2="46"
              stroke={C.inkFaint}
              strokeWidth="1"
              vectorEffect="non-scaling-stroke"
              strokeLinecap="round"
              markerEnd={`url(#${ID}grey)`}
            />

            {/* already-traversed Draft -> Checked arc (done) */}
            <path
              d="M103 41 Q140 14 176 43"
              fill="none"
              stroke={C.inkFaint}
              strokeWidth="1"
              vectorEffect="non-scaling-stroke"
              strokeLinecap="round"
              markerEnd={`url(#${ID}grey)`}
            />

            {/* Draft node: done, the saga's start state */}
            <circle
              cx="97"
              cy="46"
              r="8"
              fill="none"
              stroke={C.inkFaint}
              strokeWidth="1"
              vectorEffect="non-scaling-stroke"
            />
            <text
              x="97"
              y="30"
              textAnchor="middle"
              fontSize="7"
              letterSpacing="0.08em"
              fill={C.navLabel}
            >
              DRAFT
            </text>

            {/* Checked node: done, and the hollow teal source ring */}
            <circle
              cx="183"
              cy="46"
              r="8"
              fill="none"
              stroke={C.accent}
              strokeWidth="1"
              vectorEffect="non-scaling-stroke"
            />
            <text
              x="183"
              y="30"
              textAnchor="middle"
              fontSize="7"
              letterSpacing="0.08em"
              fill={C.navLabel}
            >
              CHECKED
            </text>

            {/* ---- teal thread: in-flight transition into Published ---- */}
            <path
              d="M188 52 Q214 80 147 99"
              fill="none"
              stroke={C.accent}
              strokeWidth="1"
              vectorEffect="non-scaling-stroke"
              strokeLinecap="round"
              strokeLinejoin="round"
              markerEnd={`url(#${ID}teal)`}
            />

            {/* live transition event firing the advance */}
            <text x="210" y="96" textAnchor="middle" fontSize="8" fill={C.ink}>
              ChecksPassed
            </text>

            {/* Published focal node: pending destination of the thread */}
            <circle
              cx="140"
              cy="108"
              r="11"
              fill="none"
              stroke={C.accent}
              strokeWidth="1"
              vectorEffect="non-scaling-stroke"
            />
            <circle cx="140" cy="108" r="2.5" fill={C.accent} />
            <text
              x="140"
              y="131"
              textAnchor="middle"
              fontSize="7"
              letterSpacing="0.08em"
              fill={C.navLabel}
            >
              PUBLISHED
            </text>
          </svg>
        </div>

        {/* single dim caption: why the saga gates the transition */}
        <div className="border-cc-card-border mt-4 border-t pt-4">
          <p className="text-cc-ink-dim text-xs">validated before traffic</p>
        </div>
      </div>
    </div>
  );
}
