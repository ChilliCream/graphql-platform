interface FeedbackVariant1Props {
  readonly className?: string;
}

/**
 * Agentic-coding scene, variant 1 (v2 "Flow Diagrams"): approval-gated agent
 * action.
 *
 * A horizontal pipeline that traces one risky agent action through a human gate
 * before it lands. The teal path runs continuously from the active "coding
 * agent" node, through the createReview tool call, into the approval gate, and
 * out to the single applied patch. The gate is the only interruption: a chip
 * whose border encodes real status, drawn settling from amber PENDING to teal
 * GRANTED, so the eye reads "paused here, then released".
 *
 * Nodes are the locked Chip primitive (bg-cc-surface, 1px cc-card-border).
 * The applied patch is a derived/terminal node (rounded-md, tighter padding).
 * Connectors are 1px: grey by default, teal only along the single traced path.
 * A two-stat footer carries the two numbers the concept turns on. No status
 * color competes with the teal accent; amber appears only on the settled-from
 * PENDING phase of the gate. Fully static, no hooks, no animation.
 */

const C = {
  surface: "#0c1322",
  cardBorder: "rgba(245,241,234,0.12)",
  ink: "#a1a3af",
  inkFaint: "rgba(245,241,234,0.16)",
  inkDim: "rgba(245,241,234,0.62)",
  navLabel: "#62748e",
  heading: "#f5f0ea",
  accent: "#5eead4",
  amber: "#fbbf24",
  mono: 'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace',
} as const;

const ID = "v2-feedback-1-";

export function FeedbackVariant1({ className }: FeedbackVariant1Props) {
  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        {/* Panel eyebrow (ScrollScenes header). */}
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
          approval-gated action
        </p>

        {/* The single traced flow: agent -> tool -> gate -> applied patch. */}
        <div className="mt-4">
          <svg
            viewBox="0 0 288 116"
            width="100%"
            role="img"
            aria-label="Flow diagram: a coding agent calls the createReview tool, pauses at a human approval gate that settles from pending to granted, then a single safe patch is applied"
            style={{ display: "block" }}
          >
            <defs>
              {/* Teal open arrowhead for the traced approved path. */}
              <marker
                id={`${ID}headTeal`}
                markerWidth="6"
                markerHeight="6"
                refX="4.4"
                refY="3"
                orient="auto"
                markerUnits="userSpaceOnUse"
              >
                <path
                  d="M1 1 L4.5 3 L1 5"
                  fill="none"
                  stroke={C.accent}
                  strokeWidth="1"
                />
              </marker>
              {/* Grey open arrowhead for the deferred apply hop. */}
              <marker
                id={`${ID}headGrey`}
                markerWidth="6"
                markerHeight="6"
                refX="4.4"
                refY="3"
                orient="auto"
                markerUnits="userSpaceOnUse"
              >
                <path
                  d="M1 1 L4.5 3 L1 5"
                  fill="none"
                  stroke={C.inkFaint}
                  strokeWidth="1"
                />
              </marker>
            </defs>

            {/* ---- Connectors (drawn under the nodes) ---- */}
            {/* Teal traced segment 1: active agent -> tool call. */}
            <line
              x1="64"
              y1="30"
              x2="98"
              y2="30"
              stroke={C.accent}
              strokeWidth="1"
              markerEnd={`url(#${ID}headTeal)`}
            />
            {/* Teal traced segment 2: tool call -> approval gate. */}
            <line
              x1="186"
              y1="30"
              x2="206"
              y2="30"
              stroke={C.accent}
              strokeWidth="1"
              markerEnd={`url(#${ID}headTeal)`}
            />
            {/* Teal traced segment 3: gate (granted) elbows down to the patch. */}
            <path
              d="M250 44 L250 78 L208 78"
              fill="none"
              stroke={C.accent}
              strokeWidth="1"
              markerEnd={`url(#${ID}headTeal)`}
            />

            {/* ---- Node: coding agent (active source, teal) ---- */}
            <g>
              <rect
                x="6"
                y="20"
                width="58"
                height="20"
                rx="6"
                fill={C.surface}
                stroke={C.accent}
                strokeWidth="1"
                strokeOpacity="0.6"
              />
              <text
                x="35"
                y="33"
                textAnchor="middle"
                fill={C.accent}
                fontFamily={C.mono}
                fontSize="9"
              >
                coding agent
              </text>
            </g>

            {/* ---- Node: createReview tool call (chip on the path) ---- */}
            <g>
              <rect
                x="98"
                y="20"
                width="88"
                height="20"
                rx="6"
                fill={C.surface}
                stroke={C.cardBorder}
                strokeWidth="1"
              />
              <text
                x="142"
                y="33"
                textAnchor="middle"
                fill={C.ink}
                fontFamily={C.mono}
                fontSize="9"
              >
                createReview()
              </text>
            </g>

            {/* ---- Node: human approval gate, settling PENDING -> GRANTED ----
                 The gate interrupts the teal path; its border encodes status. */}
            <g>
              {/* Gate frame: granted (teal) border, the settled state. */}
              <rect
                x="206"
                y="18"
                width="76"
                height="26"
                rx="8"
                fill={C.surface}
                stroke={C.accent}
                strokeWidth="1"
                strokeOpacity="0.6"
              />
              {/* gate label (eyebrow line). */}
              <text
                x="244"
                y="28"
                textAnchor="middle"
                fill={C.navLabel}
                fontFamily={C.mono}
                fontSize="6.5"
                letterSpacing="0.1em"
              >
                HUMAN GATE
              </text>
              {/* state transition: amber PENDING settled to teal GRANTED. */}
              <text
                x="214"
                y="39"
                fill={C.amber}
                fontFamily={C.mono}
                fontSize="7"
                letterSpacing="0.04em"
              >
                PENDING
              </text>
              <text
                x="245"
                y="39"
                fill={C.inkFaint}
                fontFamily={C.mono}
                fontSize="7"
              >
                &#8594;
              </text>
              <text
                x="252"
                y="39"
                fill={C.accent}
                fontFamily={C.mono}
                fontSize="7"
                letterSpacing="0.04em"
              >
                GRANTED
              </text>
            </g>

            {/* ---- Node: applied safe patch (derived/terminal, rounded-md) ---- */}
            <g>
              <rect
                x="120"
                y="68"
                width="88"
                height="20"
                rx="5"
                fill={C.surface}
                stroke={C.accent}
                strokeWidth="1"
                strokeOpacity="0.6"
              />
              <text
                x="164"
                y="81"
                textAnchor="middle"
                fill={C.accent}
                fontFamily={C.mono}
                fontSize="9"
              >
                + 1 safe patch
              </text>
            </g>

            {/* ---- Not-yet-reached sibling: a blocked/declined branch off the
                 gate, drawn dashed/grey to show the gate can stop the path ---- */}
            <path
              d="M244 44 L244 58 L120 58 L120 68"
              fill="none"
              stroke={C.inkFaint}
              strokeWidth="1"
              strokeDasharray="2 3"
              markerEnd={`url(#${ID}headGrey)`}
            />
            <rect
              x="62"
              y="68"
              width="50"
              height="20"
              rx="5"
              fill={C.surface}
              stroke={C.inkFaint}
              strokeWidth="1"
              strokeDasharray="3 2"
            />
            <text
              x="87"
              y="81"
              textAnchor="middle"
              fill={C.inkDim}
              fontFamily={C.mono}
              fontSize="8.5"
            >
              declined
            </text>
          </svg>
        </div>

        {/* Two-stat footer (ServiceTopology pattern): the two numbers the
            approval gate turns on. */}
        <div className="border-cc-card-border mt-4 grid grid-cols-2 gap-4 border-t pt-4">
          <div>
            <p className="font-heading text-cc-heading text-h4 leading-none font-semibold">
              1
            </p>
            <p className="text-cc-ink-dim mt-1.5 text-xs">
              approval, then land
            </p>
          </div>
          <div>
            <p className="font-heading text-cc-heading text-h4 leading-none font-semibold">
              0
            </p>
            <p className="text-cc-ink-dim mt-1.5 text-xs">
              writes before grant
            </p>
          </div>
        </div>

        {/* Caption under a dashed divider (ScrollScenes voice). */}
        <div className="border-cc-ink-faint mt-4 border-t border-dashed pt-3">
          <p className="text-cc-ink-dim text-center text-xs">
            risky tool calls wait for a human yes
          </p>
        </div>
      </div>
    </div>
  );
}
