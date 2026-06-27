interface WorkflowsVariant4Props {
  readonly className?: string;
}

/**
 * Workflow scene, v5 "Schematic Lines", concept #4: mediator vs bus, one wiring.
 *
 * One shared generated wiring (the hollow teal source ring at the top) drives two
 * dispatch styles, split by a dashed in-process / cross-service boundary. On the
 * left a mediator star: a grey hub (`mediator.Send`) fed by the wiring and fanning
 * to two in-process handler rings. On the right a grey bus spine: a horizontal
 * message bus with comb registration ticks, the publish tapping on from above and
 * the consumer hanging below. Same model either way.
 *
 * The single teal thread is the in-flight publish the headline names. It leaves
 * the wiring source ring, joins the bus at `bus.PublishAsync`, lights the active
 * bus segment, and terminates on the focal consumer ring (stroked teal) with a
 * teal chevron and a solid teal landing dot. Strip the teal and the schematic
 * reads as a quiet grey wiring map; nothing else is teal and there is no status
 * hue (workflows cells carry only the thread). The mediator branch and both bus
 * ends stay cc-ink-faint grey.
 *
 * React Server Component: no hooks, no client APIs, settled final frame only. All
 * literal names are borrowed verbatim from the v2 sibling. Every svg id is
 * prefixed "v5-workflows-4-".
 */

const C = {
  surface: "#0c1322",
  ink: "#a1a3af",
  navLabel: "#62748e",
  inkFaint: "rgba(245, 241, 234, 0.16)",
  accent: "#5eead4",
  mono: 'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace',
} as const;

const ID = "v5-workflows-4-";

export function WorkflowsVariant4({ className }: WorkflowsVariant4Props) {
  // The grey comb of registration ticks along the message bus baseline.
  const busTicks: readonly number[] = [156, 174, 192, 210, 228, 246];

  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      aria-hidden="true"
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
          one wiring, two dispatches
        </p>

        <svg
          viewBox="0 0 280 150"
          width="100%"
          role="img"
          aria-label="One shared generated wiring drives two dispatch styles split by an in-process versus cross-service boundary: a mediator hub fanning to in-process handlers on the left, and a message bus on the right where the publish is traced in flight to its consumer."
          className="mt-4"
          style={{ display: "block", fontFamily: C.mono }}
        >
          <defs>
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
              />
            </marker>
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
              />
            </marker>
          </defs>

          {/* Dashed in-process / cross-service boundary the wiring straddles. */}
          <line
            x1="140"
            y1="44"
            x2="140"
            y2="140"
            stroke={C.inkFaint}
            strokeWidth="1"
            strokeDasharray="2 3"
            strokeLinecap="round"
            vectorEffect="non-scaling-stroke"
          />

          {/* ---- grey skeleton: the mediator star (in-process) ---- */}

          {/* command spoke: shared wiring into the mediator hub */}
          <line
            x1="131.5"
            y1="33"
            x2="68.2"
            y2="84.9"
            stroke={C.inkFaint}
            strokeWidth="1"
            strokeLinecap="round"
            vectorEffect="non-scaling-stroke"
            markerEnd={`url(#${ID}arrow-grey)`}
          />

          {/* mediator hub + two in-process handler spokes */}
          <line
            x1="57.4"
            y1="96.6"
            x2="46.9"
            y2="111.9"
            stroke={C.inkFaint}
            strokeWidth="1"
            strokeLinecap="round"
            vectorEffect="non-scaling-stroke"
          />
          <line
            x1="66.9"
            y1="96.3"
            x2="79"
            y2="112"
            stroke={C.inkFaint}
            strokeWidth="1"
            strokeLinecap="round"
            vectorEffect="non-scaling-stroke"
          />
          <circle
            cx="62"
            cy="90"
            r="8"
            fill="none"
            stroke={C.inkFaint}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
          />
          <circle
            cx="44"
            cy="116"
            r="5"
            fill="none"
            stroke={C.inkFaint}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
          />
          <circle
            cx="82"
            cy="116"
            r="5"
            fill="none"
            stroke={C.inkFaint}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
          />

          {/* ---- grey skeleton: the bus spine (cross-service) ---- */}

          {/* comb registration ticks: message slots on the bus */}
          <g
            stroke={C.inkFaint}
            strokeWidth="1"
            strokeLinecap="round"
            vectorEffect="non-scaling-stroke"
          >
            {busTicks.map((x) => (
              <line key={x} x1={x} y1="88" x2={x} y2="95" />
            ))}
          </g>

          {/* bus baseline, grey before the publish tap and after the consumer */}
          <line
            x1="150"
            y1="88"
            x2="170"
            y2="88"
            stroke={C.inkFaint}
            strokeWidth="1"
            strokeLinecap="round"
            vectorEffect="non-scaling-stroke"
          />
          <line
            x1="224"
            y1="88"
            x2="256"
            y2="88"
            stroke={C.inkFaint}
            strokeWidth="1"
            strokeLinecap="round"
            vectorEffect="non-scaling-stroke"
          />

          {/* ---- teal thread: the in-flight publish ---- */}
          <path
            d="M144.8 35.9 L170 88 H224 V108"
            fill="none"
            stroke={C.accent}
            strokeWidth="1"
            strokeLinecap="round"
            strokeLinejoin="round"
            vectorEffect="non-scaling-stroke"
            markerEnd={`url(#${ID}arrow-teal)`}
          />

          {/* shared generated-wiring source ring (hollow teal) */}
          <circle
            cx="140"
            cy="26"
            r="11"
            fill="none"
            stroke={C.accent}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
          />

          {/* focal consumer ring + solid teal landing dot */}
          <circle
            cx="224"
            cy="116"
            r="8"
            fill={C.surface}
            stroke={C.accent}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
          />
          <circle cx="224" cy="116" r="2.5" fill={C.accent} />

          {/* ---- sparse micro-labels ---- */}
          <text
            x="62"
            y="54"
            textAnchor="middle"
            fontSize="7"
            letterSpacing="0.08em"
            fill={C.navLabel}
          >
            IN-PROCESS
          </text>
          <text x="62" y="138" textAnchor="middle" fontSize="8" fill={C.ink}>
            mediator.Send
          </text>
          <text
            x="206"
            y="54"
            textAnchor="middle"
            fontSize="7"
            letterSpacing="0.08em"
            fill={C.navLabel}
          >
            CROSS-SERVICE
          </text>
          <text x="174" y="78" fontSize="8" fill={C.ink}>
            bus.PublishAsync
          </text>
          <text x="224" y="138" textAnchor="middle" fontSize="8" fill={C.ink}>
            consumer
          </text>
        </svg>

        {/* lone footer numeral: both dispatch styles, one generated wiring */}
        <div className="border-cc-card-border mt-4 border-t pt-4">
          <p className="font-heading text-cc-heading text-h4 leading-none font-semibold">
            1
          </p>
          <p className="text-cc-ink-dim mt-1.5 text-xs">
            wiring drives both dispatches
          </p>
        </div>
      </div>
    </div>
  );
}
