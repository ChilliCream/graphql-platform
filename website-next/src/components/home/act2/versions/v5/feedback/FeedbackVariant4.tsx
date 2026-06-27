interface FeedbackVariant4Props {
  readonly className?: string;
}

/**
 * Agentic-coding scene, variant 4 (v5 "Schematic Lines"): governed tool
 * lifecycle.
 *
 * One tool walks a governed promotion loop drawn as a reductive monoline
 * hexagon. Six 1px open rings are the ordered governed stages (author, validate,
 * stage, trace, gate, production). The single teal thread is the promotion route
 * the headline names: it begins at the hollow teal source ring (author), traces
 * five edges clockwise through the grey stage rings, and terminates on the teal
 * production node (focal ring + solid teal dot, teal arrowhead). The tool itself,
 * `search-eshops-catalog`, sits in the loop's hollow centre.
 *
 * The one status hue is governance violet on the approval gate: the teal thread
 * passes behind the violet gate ring (the promotion cleared a resolved, GRANTED
 * gate) before it lands in production. A faint grey dashed edge closes the loop
 * back to author, the governed next-revision return. Everything else is
 * cc-ink-faint grey. Exactly one teal accent plus one violet status, nothing
 * else.
 *
 * cc-* palette only; every stroke is 1px non-scaling. React Server Component,
 * settled final frame, no motion, no hooks. Every svg id is prefixed
 * "v5-feedback-4-".
 */

const C = {
  surface: "#0c1322",
  ink: "#a1a3af",
  navLabel: "#62748e",
  inkFaint: "rgba(245,241,234,0.16)",
  accent: "#5eead4",
  violet: "#8b8ff0",
  mono: 'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace',
} as const;

const ID = "v5-feedback-4-";

// Grey stage rings the teal thread threads through (each occludes the line
// behind it). The gate is the governed checkpoint and carries the violet status
// hue. Pointy-top hexagon so the loop's centre stays clear for the tool label.
const STAGES: readonly {
  readonly cx: number;
  readonly cy: number;
  readonly gate: boolean;
}[] = [
  { cx: 190, cy: 54, gate: false }, // validate
  { cx: 190, cy: 98, gate: false }, // stage
  { cx: 140, cy: 120, gate: false }, // trace
  { cx: 90, cy: 98, gate: true }, // gate (governed, granted)
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
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
          governed tool lifecycle
        </p>

        {/* Monoline lifecycle loop floating directly on the card, no inner panel. */}
        <div className="mt-3">
          <svg
            viewBox="0 0 280 150"
            width="100%"
            style={{ display: "block", overflow: "visible" }}
          >
            <defs>
              {/* Teal open chevron: the promotion thread's terminus. */}
              <marker
                id={`${ID}arrow-teal`}
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
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  vectorEffect="non-scaling-stroke"
                />
              </marker>
              {/* Grey open chevron: the governed return edge closing the loop. */}
              <marker
                id={`${ID}arrow-grey`}
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
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  vectorEffect="non-scaling-stroke"
                />
              </marker>
            </defs>

            {/* ---- Teal thread: author -> validate -> stage -> trace -> gate ->
                 production. Drawn first so the stage rings occlude where it
                 passes behind them. ---- */}
            <polyline
              points="147,35 190,54 190,98 140,120 90,98 90,62"
              fill="none"
              stroke={C.accent}
              strokeWidth="1"
              strokeLinecap="round"
              strokeLinejoin="round"
              vectorEffect="non-scaling-stroke"
              markerEnd={`url(#${ID}arrow-teal)`}
            />

            {/* ---- Closing edge: governed return to author (dashed, planned next
                 revision), the loop made closed. ---- */}
            <line
              x1="97"
              y1="51"
              x2="133"
              y2="35"
              stroke={C.inkFaint}
              strokeWidth="1"
              strokeLinecap="round"
              strokeDasharray="2 3"
              vectorEffect="non-scaling-stroke"
              markerEnd={`url(#${ID}arrow-grey)`}
            />

            {/* ---- Stage rings: grey open circles (gate is governance violet),
                 each occludes the thread crossing behind it. ---- */}
            {STAGES.map((stage) => (
              <circle
                key={`${ID}stage-${stage.cx}-${stage.cy}`}
                cx={stage.cx}
                cy={stage.cy}
                r="8"
                fill={C.surface}
                stroke={stage.gate ? C.violet : C.inkFaint}
                strokeWidth="1"
                vectorEffect="non-scaling-stroke"
              />
            ))}

            {/* ---- Source ring: hollow teal, where the thread begins (author). ---- */}
            <circle
              cx="140"
              cy="32"
              r="8"
              fill="none"
              stroke={C.accent}
              strokeWidth="1"
              vectorEffect="non-scaling-stroke"
            />

            {/* ---- Focal node: teal ring + solid teal dot (production). ---- */}
            <circle
              cx="90"
              cy="54"
              r="8"
              fill="none"
              stroke={C.accent}
              strokeWidth="1"
              vectorEffect="non-scaling-stroke"
            />
            <circle cx="90" cy="54" r="2.5" fill={C.accent} />

            {/* ---- The tool walking the loop, in its hollow centre. ---- */}
            <text
              x="140"
              y="73"
              textAnchor="middle"
              fontFamily={C.mono}
              fontSize="7"
              letterSpacing="0.08em"
              fill={C.navLabel}
            >
              TOOL
            </text>
            <text
              x="140"
              y="85"
              textAnchor="middle"
              fontFamily={C.mono}
              fontSize="7.5"
              fill={C.ink}
            >
              search-eshops-catalog
            </text>

            {/* ---- Sparse endpoint labels: the teal production terminus, the
                 violet resolved gate (both to the left of their nodes). ---- */}
            <text
              x="78"
              y="57"
              textAnchor="end"
              fontFamily={C.mono}
              fontSize="7"
              letterSpacing="0.08em"
              fill={C.navLabel}
            >
              PROD
            </text>
            <text
              x="78"
              y="101"
              textAnchor="end"
              fontFamily={C.mono}
              fontSize="7"
              letterSpacing="0.08em"
              fill={C.violet}
            >
              GRANTED
            </text>
          </svg>
        </div>

        {/* Single-element footer: the count of governed stages on the loop. */}
        <div className="border-cc-card-border mt-4 border-t pt-4">
          <p className="font-heading text-cc-heading text-h4 leading-none font-semibold">
            6
          </p>
          <p className="text-cc-ink-dim mt-1.5 text-xs">
            governed stages, one approval gate
          </p>
        </div>
      </div>
    </div>
  );
}
