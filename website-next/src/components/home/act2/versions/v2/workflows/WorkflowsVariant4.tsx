interface WorkflowsVariant4Props {
  readonly className?: string;
}

/**
 * Workflow scene, v2 "Flow Diagrams", variant 4: mediator vs bus, one wiring.
 *
 * Merge/converge topology in the locked cc-* flow-diagram system. One shared
 * "generated wiring" hub box feeds two sibling flows that descend from it: an
 * in-process mediator command/handler pair on the left, and a cross-service bus
 * publish/consume pair on the right. The same generated dispatch surface drives
 * both, so the diagram reads as one model running in-process or across services.
 *
 * Exactly one teal path is traced: the in-flight publish leaving the wiring hub
 * down through bus.PublishAsync into its consumer (active teal chips plus teal
 * connectors and arrowheads). The mediator side and the wiring hub stay cream
 * label / grey ink on the cc-surface fill, every other connector stays grey. A
 * Stat duo footer carries the two key numbers (one model, lines of wiring code).
 *
 * React Server Component: no hooks, no client APIs, settled final frame only.
 * All svg ids are prefixed "v2-workflows-4-".
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

const ID = "v2-workflows-4-";

interface FlowNode {
  /** Mono identifier rendered inside the chip. */
  readonly label: string;
  /** Eyebrow kind line above the label. */
  readonly kind: string;
  /** Chip left edge in the 320x156 canvas. */
  readonly x: number;
  /** Chip vertical center. */
  readonly cy: number;
  /** The chips on the traced in-flight publish path light teal. */
  readonly active?: boolean;
}

const CHIP_W = 124;
const CHIP_H = 30;

// Shared generated-wiring hub, centered above the two sibling flows.
const HUB = { cx: 160, cy: 26, w: 124, h: 30 } as const;

// Left flow = in-process mediator command/handler; right flow = cross-service
// bus publish/consume. The bus pair carries the single teal in-flight publish.
const LEFT_X = 14;
const RIGHT_X = 182;

const NODES: readonly FlowNode[] = [
  { label: "mediator.Send", kind: "command", x: LEFT_X, cy: 86 },
  { label: "ReviewHandler", kind: "in-process", x: LEFT_X, cy: 130 },
  {
    label: "bus.PublishAsync",
    kind: "publish",
    x: RIGHT_X,
    cy: 86,
    active: true,
  },
  {
    label: "consumer",
    kind: "cross-service",
    x: RIGHT_X,
    cy: 130,
    active: true,
  },
];

export function WorkflowsVariant4({ className }: WorkflowsVariant4Props) {
  const hubBottom = HUB.cy + HUB.h / 2;
  const leftCx = LEFT_X + CHIP_W / 2;
  const rightCx = RIGHT_X + CHIP_W / 2;
  // Single horizontal bus the hub drops onto before each flow turns down, so the
  // converge reads as one tree with single-elbow orthogonal paths, no curves.
  const busY = 58;

  // Hub to each flow's command/publish node (single elbow through the bus line).
  function fromHub(targetCx: number, targetTop: number): string {
    return `M${HUB.cx} ${hubBottom} V${busY} H${targetCx} V${targetTop}`;
  }

  // Vertical hop from a command/publish node down to its handler/consumer.
  function downHop(cx: number, fromBottom: number, toTop: number): string {
    return `M${cx} ${fromBottom} V${toTop}`;
  }

  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
          one wiring, two dispatches
        </p>

        <div className="mt-3">
          <svg
            viewBox="0 0 320 156"
            width="100%"
            role="img"
            aria-label="One generated wiring node feeds two sibling flows: an in-process mediator command and handler on the left, and a cross-service bus publish and consumer on the right. The same model dispatches in-process or across services, with the bus publish traced in flight."
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

            {/* Grey connectors: hub into the mediator flow + the mediator hop. */}
            <g fill="none" strokeWidth={1}>
              <path
                d={fromHub(leftCx, NODES[0].cy - CHIP_H / 2 - 5)}
                stroke={CC.inkFaint}
                markerEnd={`url(#${ID}arrow)`}
              />
              <path
                d={downHop(
                  leftCx,
                  NODES[0].cy + CHIP_H / 2,
                  NODES[1].cy - CHIP_H / 2 - 5,
                )}
                stroke={CC.inkFaint}
                markerEnd={`url(#${ID}arrow)`}
              />
            </g>

            {/* The single traced teal path: the in-flight publish leaving the
                shared wiring hub down through bus.PublishAsync into its consumer.
                One continuous, unbranching route from source to outcome. */}
            <g fill="none" strokeWidth={1}>
              <path
                d={fromHub(rightCx, NODES[2].cy - CHIP_H / 2 - 5)}
                stroke={CC.accent}
                markerEnd={`url(#${ID}arrowTeal)`}
              />
              <path
                d={downHop(
                  rightCx,
                  NODES[2].cy + CHIP_H / 2,
                  NODES[3].cy - CHIP_H / 2 - 5,
                )}
                stroke={CC.accent}
                markerEnd={`url(#${ID}arrowTeal)`}
              />
            </g>

            {/* Shared generated-wiring hub box. */}
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
                y={HUB.cy - 3}
                fill={CC.navLabel}
                fontFamily={MONO}
                fontSize={6.5}
                letterSpacing="0.08em"
                textAnchor="middle"
              >
                GENERATED
              </text>
              <text
                x={HUB.cx}
                y={HUB.cy + 9}
                fill={CC.ink}
                fontFamily={MONO}
                fontSize={9}
                textAnchor="middle"
              >
                Mocha wiring
              </text>
            </g>

            {/* Two sibling flows. The bus publish/consume pair reads teal. */}
            {NODES.map((n) => {
              const y = n.cy - CHIP_H / 2;
              const stroke = n.active ? CC.accent : CC.cardBorder;
              const labelColor = n.active ? CC.accent : CC.ink;
              return (
                <g key={n.label}>
                  <rect
                    x={n.x}
                    y={y}
                    width={CHIP_W}
                    height={CHIP_H}
                    rx="6"
                    fill={CC.surface}
                    stroke={stroke}
                    strokeWidth={1}
                  />
                  <text
                    x={n.x + 11}
                    y={y + 12}
                    fill={CC.navLabel}
                    fontFamily={MONO}
                    fontSize={6.5}
                    letterSpacing="0.08em"
                  >
                    {n.kind.toUpperCase()}
                  </text>
                  <text
                    x={n.x + 11}
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
              1
            </p>
            <p className="text-cc-ink-dim mt-1.5 text-xs">model, two paths</p>
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
