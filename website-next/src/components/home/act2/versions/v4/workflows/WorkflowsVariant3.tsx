interface WorkflowsVariant3Props {
  readonly className?: string;
}

/**
 * Workflow scene, v4 "Generated Artifacts", concept #3: pluggable transport swap.
 *
 * Locked v4 PATTERN E (a left-to-right rail of stages with one lit node). The
 * top grey tile is the real call site, a `ReviewService.cs` snippet whose
 * `await bus.PublishAsync(new ReviewPublished(id));` never changes (`PublishAsync`
 * is the one cream strong token). A single teal route drops out of that call into
 * the transport rail below, a row of five interchangeable transports plugged onto
 * one bus. RabbitMQ is the selected node carrying the in-flight message; the other
 * four (Postgres, In-process, Kafka, Azure SB) stay monochrome to read as
 * available-but-not-selected. Swap the node, the same publish runs over it.
 *
 * The single teal callout is the only teal in the cell besides the selected node
 * and its drop: a 2px underline tick beneath the RabbitMQ label, a 1px leader
 * sweeping into the open lower-right, and an "IN FLIGHT" micro-label. Strip the
 * teal cluster and the rail reads as five neutral grey slots. No status hue is
 * used since selection, not health, is the real state here.
 *
 * Literal content (PublishAsync, ReviewPublished, the five transport names,
 * RabbitMQ selected) is borrowed verbatim from the v1 / v2 siblings so the
 * artifact is accurate. React Server Component: no "use client", no hooks, no
 * animation, settled final frame only. Every svg id is prefixed "v4-workflows-3-".
 */

const MONO =
  'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace';

/** Locked v4 cc-* palette: surfaces, ink ramp, the one cream token, single teal. */
const C = {
  surface: "#0c1322",
  cardBorder: "rgba(245, 241, 234, 0.12)",
  inkFaint: "rgba(245, 241, 234, 0.16)",
  ink: "#a1a3af",
  inkDim: "rgba(245, 241, 234, 0.62)",
  heading: "#f5f0ea",
  navLabel: "#62748e",
  accent: "#5eead4",
} as const;

// Five interchangeable transports plugged onto one publish bus. RabbitMQ is the
// selected node the in-flight message routes through (v1 / v2 sibling literals).
const TRANSPORTS: readonly {
  readonly name: string;
  readonly cx: number;
  readonly selected: boolean;
}[] = [
  { name: "RabbitMQ", cx: 80, selected: true },
  { name: "Postgres", cx: 130, selected: false },
  { name: "In-process", cx: 180, selected: false },
  { name: "Kafka", cx: 230, selected: false },
  { name: "Azure SB", cx: 280, selected: false },
];

const ID = "v4-workflows-3-";

export function WorkflowsVariant3({ className }: WorkflowsVariant3Props) {
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

        {/* source call tile -> transport rail, one teal drop + one teal callout */}
        <svg
          viewBox="0 0 320 160"
          width="100%"
          className="mt-4"
          style={{ display: "block", fontFamily: MONO }}
        >
          <defs>
            <marker
              id={`${ID}arrow`}
              markerWidth="6"
              markerHeight="6"
              refX="4.5"
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

          {/* source tile (grey): the call site that never changes */}
          <rect
            x={10}
            y={4}
            width={300}
            height={58}
            rx={8}
            fill={C.surface}
            stroke={C.cardBorder}
            strokeWidth="1"
          />
          <text x={22} y={18} fill={C.inkDim} fontSize="8.5">
            ReviewService.cs
          </text>
          <text
            x={298}
            y={18}
            textAnchor="end"
            fill={C.navLabel}
            fontSize="7"
            letterSpacing="0.05em"
          >
            .cs
          </text>
          <line
            x1={10}
            y1={27}
            x2={310}
            y2={27}
            stroke={C.cardBorder}
            strokeWidth="1"
          />
          <text x={22} y={44} fontSize="9.5">
            <tspan fill={C.inkDim}>await </tspan>
            <tspan fill={C.navLabel}>bus.</tspan>
            <tspan fill={C.heading}>PublishAsync</tspan>
            <tspan fill={C.inkDim}>(</tspan>
          </text>
          <text x={34} y={57} fontSize="9.5">
            <tspan fill={C.navLabel}>new </tspan>
            <tspan fill={C.ink}>ReviewPublished</tspan>
            <tspan fill={C.inkDim}>(id));</tspan>
          </text>

          {/* the one teal route: the publish drops into the selected transport */}
          <path
            d="M80 64 V104"
            fill="none"
            stroke={C.accent}
            strokeWidth="1"
            markerEnd={`url(#${ID}arrow)`}
          />

          {/* the bus: five interchangeable transports plug onto one rail */}
          <line
            x1={80}
            y1={112}
            x2={280}
            y2={112}
            stroke={C.cardBorder}
            strokeWidth="1"
          />
          {TRANSPORTS.map((t) =>
            t.selected ? (
              <circle key={t.name} cx={t.cx} cy={112} r={3.5} fill={C.accent} />
            ) : (
              <circle
                key={t.name}
                cx={t.cx}
                cy={112}
                r={2.5}
                fill={C.surface}
                stroke={C.navLabel}
                strokeWidth="1"
              />
            ),
          )}

          {/* transport labels: the selected node is teal, the rest stay grey */}
          {TRANSPORTS.map((t) => (
            <text
              key={t.name}
              x={t.cx}
              y={127}
              textAnchor="middle"
              fontSize="8"
              fill={t.selected ? C.accent : C.inkDim}
            >
              {t.name}
            </text>
          ))}

          {/* signature callout on the selected transport: a 2px underline tick, a
              1px leader into the open lower-right, and the IN FLIGHT micro-label */}
          <line
            x1={61}
            y1={132}
            x2={99}
            y2={132}
            stroke={C.accent}
            strokeWidth="2"
          />
          <path
            d="M99 132 C 140 142, 174 147, 196 148"
            fill="none"
            stroke={C.accent}
            strokeWidth="1"
            markerEnd={`url(#${ID}arrow)`}
          />
          <text
            x={205}
            y={151}
            fill={C.accent}
            fontSize="7"
            letterSpacing="0.12em"
          >
            IN FLIGHT
          </text>
        </svg>

        {/* Stat duo: one publish call, five interchangeable transports */}
        <div className="border-cc-card-border mt-4 grid grid-cols-2 gap-4 border-t pt-4">
          <Stat figure="1" label="publish call" />
          <Stat figure="5" label="transports" />
        </div>
      </div>
    </div>
  );
}

interface StatProps {
  readonly figure: string;
  readonly label: string;
}

/** The ScrollScenes Stat: a display numeral over a small dim caption. */
function Stat({ figure, label }: StatProps) {
  return (
    <div>
      <p className="font-heading text-cc-heading text-h4 leading-none font-semibold">
        {figure}
      </p>
      <p className="text-cc-ink-dim mt-1.5 text-xs">{label}</p>
    </div>
  );
}
