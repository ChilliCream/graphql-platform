interface WorkflowsVariant3Props {
  readonly className?: string;
}

/**
 * Workflow scene, variant 3 (v5 "Schematic Lines"): pluggable transport swap.
 *
 * Skeleton: a vertical socket rail holding five transport rings, the one slot a
 * PublishAsync call binds to. The four alternatives (Postgres, in-process, Kafka,
 * Azure SB) are drawn as grey dashed rings to read as available-but-not-bound,
 * the transports you can swap in. RabbitMQ is the current selection: the focal
 * ring stroked teal with a solid teal terminal dot, the in-flight message it now
 * carries. A grey delivery line continues to the consumer. One call, swap the
 * ring under it.
 *
 * The teal thread is the single accent: it leaves the hollow teal source ring
 * (the PublishAsync call) and traces the one route the headline names, the call
 * landing on the selected RabbitMQ transport. Strip the teal and the schematic
 * reads as a quiet grey selector; every other ring, the rail, and the delivery
 * line stay neutral.
 *
 * React Server Component: no hooks, no client APIs, settled final frame only.
 * Transport names are borrowed verbatim from the v2 sibling. Every svg id is
 * prefixed "v5-workflows-3-".
 */

const C = {
  surface: "#0c1322",
  ink: "#a1a3af",
  inkFaint: "rgba(245, 241, 234, 0.16)",
  navLabel: "#62748e",
  accent: "#5eead4",
  mono: 'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace',
} as const;

const ID = "v5-workflows-3-";

export function WorkflowsVariant3({ className }: WorkflowsVariant3Props) {
  // The swappable slot: four not-currently-bound transports drawn dashed.
  const alternatives: readonly number[] = [27, 51, 99, 123];

  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      aria-hidden="true"
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
          pluggable transport
        </p>

        {/* selector rail: one call binds to one of five swappable transports */}
        <svg
          width="100%"
          viewBox="0 0 280 150"
          fill="none"
          className="mt-4 block"
          style={{ fontFamily: C.mono }}
        >
          <defs>
            <marker
              id={`${ID}arrow-grey`}
              markerUnits="userSpaceOnUse"
              markerWidth="6"
              markerHeight="6"
              refX="5"
              refY="3"
              orient="auto"
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
            <marker
              id={`${ID}arrow-teal`}
              markerUnits="userSpaceOnUse"
              markerWidth="6"
              markerHeight="6"
              refX="5"
              refY="3"
              orient="auto"
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
          </defs>

          {/* the socket spine the transport rings are strung on */}
          <line
            x1="146"
            y1="19"
            x2="146"
            y2="131"
            stroke={C.inkFaint}
            strokeWidth="1"
            strokeLinecap="round"
            vectorEffect="non-scaling-stroke"
          />

          {/* grey delivery line: the selected transport forwards to the consumer */}
          <line
            x1="156"
            y1="75"
            x2="232"
            y2="75"
            stroke={C.inkFaint}
            strokeWidth="1"
            strokeLinecap="round"
            vectorEffect="non-scaling-stroke"
            markerEnd={`url(#${ID}arrow-grey)`}
          />

          {/* consumer ring */}
          <circle
            cx="240"
            cy="75"
            r="8"
            fill="none"
            stroke={C.inkFaint}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
          />

          {/* the four swappable alternatives: grey dashed, not currently bound */}
          <g
            stroke={C.inkFaint}
            strokeWidth="1"
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeDasharray="2 3"
            vectorEffect="non-scaling-stroke"
          >
            {alternatives.map((cy) => (
              <circle key={cy} cx="146" cy={cy} r="8" fill="none" />
            ))}
          </g>

          {/* teal thread: the PublishAsync call lands on the selected transport */}
          <line
            x1="56"
            y1="75"
            x2="136"
            y2="75"
            stroke={C.accent}
            strokeWidth="1"
            strokeLinecap="round"
            vectorEffect="non-scaling-stroke"
            markerEnd={`url(#${ID}arrow-teal)`}
          />

          {/* focal RabbitMQ ring (teal) + the in-flight message it carries */}
          <circle
            cx="146"
            cy="75"
            r="8"
            fill={C.surface}
            stroke={C.accent}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
          />
          <circle cx="146" cy="75" r="2.5" fill={C.accent} />

          {/* hollow teal source ring: the one PublishAsync call */}
          <circle
            cx="44"
            cy="75"
            r="11"
            fill="none"
            stroke={C.accent}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
          />

          {/* sparse micro-labels */}
          <text
            x="146"
            y="12"
            textAnchor="middle"
            fill={C.navLabel}
            fontSize="7"
            letterSpacing="0.08em"
          >
            TRANSPORT
          </text>
          <text x="96" y="69" textAnchor="middle" fill={C.ink} fontSize="8">
            RabbitMQ
          </text>
          <text x="44" y="100" textAnchor="middle" fill={C.ink} fontSize="8">
            PublishAsync()
          </text>
          <text x="240" y="100" textAnchor="middle" fill={C.ink} fontSize="8">
            consumer
          </text>
        </svg>

        {/* lone footer numeral: the size of the swappable slot */}
        <div className="border-cc-card-border mt-4 border-t pt-4">
          <p className="font-heading text-cc-heading text-h4 leading-none font-semibold">
            5
          </p>
          <p className="text-cc-ink-dim mt-1.5 text-xs">
            transports behind one PublishAsync
          </p>
        </div>
      </div>
    </div>
  );
}
