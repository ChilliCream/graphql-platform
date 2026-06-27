interface WorkflowsVariant1Props {
  readonly className?: string;
}

/**
 * Workflow scene, v2 "Flow Diagrams", variant 1: compile-time wiring manifest.
 *
 * Hub-and-spoke topology in the locked cc-* flow-diagram system. A central
 * source-generator hub box ("Mocha codegen") radiates 1px grey connectors out
 * to the small pieces it discovered and wired at build time: a command, its
 * handler, an event, and a saga. Exactly one teal path is traced: the
 * CreateReview command in flight from the hub into its handler (active teal
 * chips plus teal connectors and arrowheads). Every other node stays cream
 * label / grey ink on the cc-surface fill, every other connector stays grey. A
 * Stat duo footer carries the two key numbers (handlers discovered, wired at
 * build time).
 *
 * React Server Component: no hooks, no client APIs, settled final frame only.
 * All svg ids are prefixed "v2-workflows-1-".
 */

const CC = {
  surface: "#0c1322",
  ink: "#a1a3af",
  inkFaint: "rgba(245,241,234,0.16)",
  cardBorder: "rgba(245,241,234,0.12)",
  navLabel: "#62748e",
  accent: "#5eead4",
} as const;

const MONO =
  'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace';

const ID = "v2-workflows-1-";

interface SpokeNode {
  /** Mono identifier rendered inside the chip. */
  readonly label: string;
  /** Eyebrow kind line above the label. */
  readonly kind: string;
  /** Vertical center of the chip in the 320x150 canvas. */
  readonly cy: number;
  /** The chips on the traced in-flight path light teal. */
  readonly active?: boolean;
}

// Geometry of the right rank of discovered/wired pieces. All chips share a left
// edge and width so the fan-out reads as one tree.
const CHIP_X = 176;
const CHIP_W = 132;
const CHIP_H = 30;

// Central source-generator hub plus four discovered pieces fanning to the right.
// CreateReview (command) and ReviewHandler form the single teal path.
const HUB = { cx: 64, cy: 75, w: 84, h: 42 } as const;

const SPOKES: readonly SpokeNode[] = [
  { label: "CreateReview", kind: "command", cy: 24, active: true },
  { label: "ReviewHandler", kind: "handler", cy: 60, active: true },
  { label: "ReviewCreated", kind: "event", cy: 96 },
  { label: "PublishSaga", kind: "saga", cy: 132 },
];

export function WorkflowsVariant1({ className }: WorkflowsVariant1Props) {
  // Shared vertical bus the hub connectors leave from before turning into each
  // rank, so the fan-out is a single-elbow orthogonal tree with no curves.
  const hubRightX = HUB.cx + HUB.w / 2;
  const busX = hubRightX + 24;

  function spoke(cy: number): string {
    return `M${hubRightX} ${HUB.cy} H${busX} V${cy} H${CHIP_X - 5}`;
  }

  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
          wiring manifest
        </p>

        <div className="mt-3">
          <svg
            viewBox="0 0 320 156"
            width="100%"
            role="img"
            aria-label="A compile-time wiring manifest: Mocha's source generator at the center fans out to the command, handler, event, and saga it discovered and wired at build time, with the CreateReview command traced in flight to its handler."
            style={{ display: "block" }}
          >
            <defs>
              <marker
                id={`${ID}arrow`}
                markerWidth="7"
                markerHeight="7"
                refX="5"
                refY="3"
                orient="auto"
              >
                <path
                  d="M0 0 L5 3 L0 6"
                  fill="none"
                  stroke={CC.inkFaint}
                  strokeWidth={1}
                />
              </marker>
              <marker
                id={`${ID}arrowTeal`}
                markerWidth="7"
                markerHeight="7"
                refX="5"
                refY="3"
                orient="auto"
              >
                <path
                  d="M0 0 L5 3 L0 6"
                  fill="none"
                  stroke={CC.accent}
                  strokeWidth={1}
                />
              </marker>
            </defs>

            {/* Grey spoke connectors (drawn under the nodes). */}
            <g fill="none" strokeWidth={1}>
              {SPOKES.filter((n) => !n.active).map((n) => (
                <path
                  key={n.label}
                  d={spoke(n.cy)}
                  stroke={CC.inkFaint}
                  markerEnd={`url(#${ID}arrow)`}
                />
              ))}
            </g>

            {/* The single traced teal path: the CreateReview command leaving the
                hub and continuing into ReviewHandler. One continuous,
                unbranching route from source to outcome. */}
            <g fill="none" strokeWidth={1}>
              <path
                d={spoke(SPOKES[0].cy)}
                stroke={CC.accent}
                markerEnd={`url(#${ID}arrowTeal)`}
              />
              <path
                d={spoke(SPOKES[1].cy)}
                stroke={CC.accent}
                markerEnd={`url(#${ID}arrowTeal)`}
              />
            </g>

            {/* Central source-generator hub box. */}
            <g>
              <rect
                x={HUB.cx - HUB.w / 2}
                y={HUB.cy - HUB.h / 2}
                width={HUB.w}
                height={HUB.h}
                rx="8"
                fill={CC.surface}
                stroke={CC.cardBorder}
                strokeWidth={1}
              />
              <text
                x={HUB.cx}
                y={HUB.cy - 4}
                fill={CC.navLabel}
                fontFamily={MONO}
                fontSize={7}
                letterSpacing="0.08em"
                textAnchor="middle"
              >
                SOURCE GEN
              </text>
              <text
                x={HUB.cx}
                y={HUB.cy + 9}
                fill={CC.ink}
                fontFamily={MONO}
                fontSize={9}
                textAnchor="middle"
              >
                Mocha codegen
              </text>
            </g>

            {/* Discovered/wired chips. Command + handler on the path read teal. */}
            {SPOKES.map((n) => {
              const y = n.cy - CHIP_H / 2;
              const stroke = n.active ? CC.accent : CC.cardBorder;
              const labelColor = n.active ? CC.accent : CC.ink;
              return (
                <g key={n.label}>
                  <rect
                    x={CHIP_X}
                    y={y}
                    width={CHIP_W}
                    height={CHIP_H}
                    rx="6"
                    fill={CC.surface}
                    stroke={stroke}
                    strokeWidth={1}
                  />
                  <text
                    x={CHIP_X + 11}
                    y={y + 12}
                    fill={CC.navLabel}
                    fontFamily={MONO}
                    fontSize={6.5}
                    letterSpacing="0.08em"
                  >
                    {n.kind.toUpperCase()}
                  </text>
                  <text
                    x={CHIP_X + 11}
                    y={y + 23}
                    fill={labelColor}
                    fontFamily={MONO}
                    fontSize={9.5}
                  >
                    {n.label}
                  </text>
                </g>
              );
            })}
          </svg>
        </div>

        <div className="border-cc-card-border mt-4 grid grid-cols-2 gap-4 border-t pt-4">
          <div>
            <p className="font-heading text-cc-heading text-h4 leading-none font-semibold">
              12
            </p>
            <p className="text-cc-ink-dim mt-1.5 text-xs">
              handlers discovered
            </p>
          </div>
          <div>
            <p className="font-heading text-cc-heading text-h4 leading-none font-semibold">
              0
            </p>
            <p className="text-cc-ink-dim mt-1.5 text-xs">
              lines of wiring code
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}
