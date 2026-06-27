interface FeedbackVariant4Props {
  readonly className?: string;
}

/**
 * Agentic-coding scene, variant 4 (v4 "Generated Artifacts"): the governed
 * promotion path one tool walks (locked v4 PATTERN E, a left-to-right rail of
 * stages with one lit node).
 *
 * One artifact tile on cc-surface. A header band names the artifact (the tool
 * `search-eshops-catalog`, borrowed verbatim from the v1/v2 siblings, plus a
 * `promotion` kind tag), then a thin 1px rail carries the tool through its
 * governed stages: four monochrome stage nodes (`author`, `validate`, `stage`,
 * `trace`), the resolved approval gate, and the production terminus.
 *
 * Two color channels sit in two separate regions so they never compete. The
 * status channel is the governance-violet approval gate: a violet diamond on the
 * rail plus a violet RESOLVED pill, the one real-state element. The single teal
 * callout is the version signature on the load-bearing token: the `production`
 * terminus is the one lit node (teal pill border + teal text), with a 2.5px
 * anchor dot, a 1px leader rising into the negative space, a 2px underline tick,
 * and a "PROMOTED" micro-label. Strip the teal and the rail reads as a neutral
 * grey figure with one violet status gate. A Stat duo footer keeps the display
 * numerals. React Server Component, settled final frame, no hooks, no animation.
 * Every svg id is prefixed "v4-feedback-4-".
 */

const C = {
  surface: "#0c1322",
  cardBorder: "rgba(245,241,234,0.12)",
  inkFaint: "rgba(245,241,234,0.16)",
  ink: "#a1a3af",
  inkDim: "rgba(245,241,234,0.62)",
  navLabel: "#62748e",
  heading: "#f5f0ea",
  accent: "#5eead4",
  violet: "#8b8ff0",
  mono: 'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace',
} as const;

const ID = "v4-feedback-4-";

/** The four pre-gate stages on the rail, with their x position along the track. */
const STAGES: readonly { readonly name: string; readonly x: number }[] = [
  { name: "author", x: 36 },
  { name: "validate", x: 78 },
  { name: "stage", x: 118 },
  { name: "trace", x: 156 },
];

const TRACK_Y = 74;
const GATE_X = 196;

export function FeedbackVariant4({ className }: FeedbackVariant4Props) {
  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      aria-hidden="true"
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        {/* Panel eyebrow (ScrollScenes header voice). */}
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
          promotion path
        </p>

        <div className="mt-4">
          <svg
            viewBox="0 0 320 116"
            width="100%"
            role="img"
            aria-label="A governed promotion path for the tool search-eshops-catalog: it walks the rail through author, validate, stage and trace, past a resolved approval gate, to a promoted production terminus."
            style={{ display: "block" }}
          >
            <defs>
              {/* Teal open chevron for the single callout leader. */}
              <marker
                id={`${ID}head`}
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
                />
              </marker>
            </defs>

            {/* ---- Artifact tile holding the promotion rail ---- */}
            <rect
              x={4}
              y={4}
              width={312}
              height={108}
              rx={8}
              fill={C.surface}
              stroke={C.cardBorder}
              strokeWidth={1}
            />

            {/* Header band: the tool name (cream) + promotion kind tag. */}
            <text
              x={14}
              y={18}
              fontFamily={C.mono}
              fontSize={9}
              fontWeight={600}
              fill={C.heading}
            >
              search-eshops-catalog
            </text>
            <text
              x={306}
              y={18}
              textAnchor="end"
              fontFamily={C.mono}
              fontSize={8}
              letterSpacing="0.08em"
              fill={C.navLabel}
            >
              promotion
            </text>
            <line
              x1={4}
              y1={27}
              x2={316}
              y2={27}
              stroke={C.cardBorder}
              strokeWidth={1}
            />

            {/* ---- The rail track (grey, settled / resolved) ---- */}
            <line
              x1={30}
              y1={TRACK_Y}
              x2={GATE_X}
              y2={TRACK_Y}
              stroke={C.inkFaint}
              strokeWidth={1}
            />
            <line
              x1={GATE_X}
              y1={TRACK_Y}
              x2={250}
              y2={TRACK_Y}
              stroke={C.inkFaint}
              strokeWidth={1}
            />

            {/* ---- Four monochrome stage nodes + labels ---- */}
            {STAGES.map((stage) => (
              <g key={stage.name}>
                <text
                  x={stage.x}
                  y={60}
                  textAnchor="middle"
                  fontFamily={C.mono}
                  fontSize={7.5}
                  fill={C.ink}
                >
                  {stage.name}
                </text>
                <circle cx={stage.x} cy={TRACK_Y} r={2.5} fill={C.navLabel} />
              </g>
            ))}

            {/* ---- Approval gate: the one real-state element (governance violet) ---- */}
            <text
              x={GATE_X}
              y={60}
              textAnchor="middle"
              fontFamily={C.mono}
              fontSize={7.5}
              fill={C.inkDim}
            >
              approval
            </text>
            <path
              d={`M${GATE_X} ${TRACK_Y - 6} L${GATE_X + 6} ${TRACK_Y} L${GATE_X} ${TRACK_Y + 6} L${GATE_X - 6} ${TRACK_Y} Z`}
              fill={C.violet}
              fillOpacity={0.12}
              stroke={C.violet}
              strokeWidth={1}
            />
            <rect
              x={GATE_X - 25}
              y={86}
              width={50}
              height={12}
              rx={6}
              fill={C.violet}
              fillOpacity={0.1}
              stroke={C.violet}
              strokeWidth={1}
            />
            <text
              x={GATE_X}
              y={94.5}
              textAnchor="middle"
              fontFamily={C.mono}
              fontSize={6}
              letterSpacing="0.08em"
              fill={C.violet}
            >
              RESOLVED
            </text>

            {/* ---- Production terminus: the single lit node (teal) ---- */}
            <rect
              x={250}
              y={66}
              width={64}
              height={16}
              rx={8}
              fill="none"
              stroke={C.accent}
              strokeWidth={1}
            />
            <text
              x={282}
              y={77}
              textAnchor="middle"
              fontFamily={C.mono}
              fontSize={8.5}
              fill={C.accent}
            >
              production
            </text>

            {/* ---- The single teal callout: label, tick, leader, anchor dot ---- */}
            <text
              x={282}
              y={42}
              textAnchor="middle"
              fontFamily={C.mono}
              fontSize={7}
              letterSpacing="0.14em"
              fill={C.accent}
            >
              PROMOTED
            </text>
            <line
              x1={262}
              y1={46}
              x2={302}
              y2={46}
              stroke={C.accent}
              strokeWidth={2}
            />
            <line
              x1={282}
              y1={49}
              x2={282}
              y2={62}
              stroke={C.accent}
              strokeWidth={1}
              markerEnd={`url(#${ID}head)`}
            />
            <circle cx={282} cy={65} r={2.5} fill={C.accent} />
          </svg>
        </div>

        {/* Stat duo footer: a full governed path, one resolved gate. */}
        <div className="border-cc-card-border mt-4 grid grid-cols-2 gap-4 border-t pt-4">
          <div>
            <p className="font-heading text-cc-heading text-h4 leading-none font-semibold">
              6
            </p>
            <p className="text-cc-ink-dim mt-1.5 text-xs">governed stages</p>
          </div>
          <div>
            <p className="font-heading text-cc-heading text-h4 leading-none font-semibold">
              1
            </p>
            <p className="text-cc-ink-dim mt-1.5 text-xs">approval gate</p>
          </div>
        </div>
      </div>
    </div>
  );
}
