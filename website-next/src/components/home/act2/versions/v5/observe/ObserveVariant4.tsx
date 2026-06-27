interface ObserveVariant4Props {
  readonly className?: string;
}

/**
 * Production-view scene, variant 4 (v5 "Schematic Lines"): service topology
 * degrading.
 *
 * The checkout dependency graph reduced to a monoline node-edge schematic. `api`
 * is a hollow teal source ring on the left; it fans out at a single junction to
 * `users-svc`, `billing`, and `worker` (grey 1px edges with open arrowheads),
 * and `worker` reaches one rank further to `db`. Every node is a thin open
 * circle, no boxes or chips.
 *
 * The single teal thread leaves the api source ring and ends in a solid teal dot
 * at the fan-out junction, the point where the route the headline names narrows
 * to one edge. From that junction the api -> billing gRPC hop continues in amber,
 * the one real status (degrading), and the billing node carries a matching amber
 * ring. Strip the teal and amber and the graph reads as a quiet grey topology;
 * the eye lands on the one dependency going bad.
 *
 * Borrowed content (exact, from the v2 / Nitro siblings): api (gateway) fans out
 * to users-svc (http), billing (gRPC, degrading), worker (queue), and db
 * (postgres); the api -> billing gRPC hop regresses p99 by +210ms. React Server
 * Component: no hooks, no client APIs, settled final frame only. Every svg id is
 * prefixed "v5-observe-4-".
 */

const C = {
  inkFaint: "rgba(245, 241, 234, 0.16)",
  ink: "#a1a3af",
  accent: "#5eead4",
  amber: "#fbbf24",
  mono: 'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace',
} as const;

const ID = "v5-observe-4-";

type NodeTone = "hub" | "degraded" | "plain";

interface TopoNode {
  readonly label: string;
  readonly cx: number;
  readonly cy: number;
  readonly r: number;
  readonly tone: NodeTone;
}

// Node-edge layout on the 280x150 cell. `api` is the left hub; users-svc,
// billing, and worker share a middle column; db sits one rank past worker.
const NODES: readonly TopoNode[] = [
  { label: "api", cx: 32, cy: 75, r: 11, tone: "hub" },
  { label: "users-svc", cx: 150, cy: 30, r: 8, tone: "plain" },
  { label: "billing", cx: 150, cy: 75, r: 8, tone: "degraded" },
  { label: "worker", cx: 150, cy: 120, r: 8, tone: "plain" },
  { label: "db", cx: 242, cy: 120, r: 8, tone: "plain" },
] as const;

// Shared fan-out junction column and the rank entry edge.
const JUNCTION_X = 92;

function ringStroke(tone: NodeTone): string {
  if (tone === "hub") {
    return C.accent;
  }
  if (tone === "degraded") {
    return C.amber;
  }
  return C.inkFaint;
}

export function ObserveVariant4({ className }: ObserveVariant4Props) {
  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      aria-hidden="true"
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
          service topology
        </p>

        {/* Monoline dependency graph floating directly on the card. */}
        <svg
          viewBox="0 0 280 150"
          width="100%"
          role="img"
          aria-label="Service topology for the checkout request: api fans out to users-svc, billing over gRPC, and worker, and worker writes to db. The api to billing gRPC hop is degrading, drawn amber as the dependency going bad."
          className="mt-4"
          style={{ display: "block", fontFamily: C.mono }}
        >
          <defs>
            <marker
              id={`${ID}arrow`}
              markerWidth="6"
              markerHeight="6"
              refX="5"
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
            <marker
              id={`${ID}arrowAmber`}
              markerWidth="6"
              markerHeight="6"
              refX="5"
              refY="3"
              orient="auto"
              markerUnits="userSpaceOnUse"
            >
              <path
                d="M0 0.5 L5 3 L0 5.5"
                fill="none"
                stroke={C.amber}
                strokeWidth="1"
                strokeLinecap="round"
                strokeLinejoin="round"
                vectorEffect="non-scaling-stroke"
              />
            </marker>
          </defs>

          {/* Grey skeleton: the fan-out bus and the healthy hops. */}
          <g
            fill="none"
            stroke={C.inkFaint}
            strokeWidth="1"
            strokeLinecap="round"
            strokeLinejoin="round"
            vectorEffect="non-scaling-stroke"
          >
            {/* vertical fan-out bus from the users rank to the worker rank */}
            <path d={`M${JUNCTION_X} 30 V120`} />
            {/* api -> users-svc (healthy) */}
            <path d={`M${JUNCTION_X} 30 H140`} markerEnd={`url(#${ID}arrow)`} />
            {/* api -> worker (healthy) */}
            <path
              d={`M${JUNCTION_X} 120 H140`}
              markerEnd={`url(#${ID}arrow)`}
            />
            {/* worker -> db (healthy) */}
            <path d="M158 120 H232" markerEnd={`url(#${ID}arrow)`} />
          </g>

          {/* The degrading hop: api -> billing over gRPC, the one real status. */}
          <path
            d={`M${JUNCTION_X} 75 H140`}
            fill="none"
            stroke={C.amber}
            strokeWidth="1"
            strokeLinecap="round"
            strokeLinejoin="round"
            vectorEffect="non-scaling-stroke"
            markerEnd={`url(#${ID}arrowAmber)`}
          />

          {/* The teal thread: from the api source ring to the fan-out junction,
              ending in a solid teal dot where the route narrows to the bad hop. */}
          <path
            d={`M43 75 H${JUNCTION_X}`}
            fill="none"
            stroke={C.accent}
            strokeWidth="1"
            strokeLinecap="round"
            strokeLinejoin="round"
            vectorEffect="non-scaling-stroke"
          />
          <circle cx={JUNCTION_X} cy="75" r="2.5" fill={C.accent} />

          {/* Nodes: thin open circles, drawn over the edges. */}
          {NODES.map((n) => (
            <circle
              key={n.label}
              cx={n.cx}
              cy={n.cy}
              r={n.r}
              fill="none"
              stroke={ringStroke(n.tone)}
              strokeWidth="1"
              vectorEffect="non-scaling-stroke"
            />
          ))}

          {/* Sparse node labels, centered below each node. */}
          {NODES.map((n) => (
            <text
              key={`${n.label}-label`}
              x={n.cx}
              y={n.cy + n.r + 12}
              textAnchor="middle"
              fontSize="8"
              fill={n.tone === "degraded" ? C.amber : C.ink}
            >
              {n.label}
            </text>
          ))}

          {/* Protocol of the degrading hop. */}
          <text
            x={(JUNCTION_X + 140) / 2}
            y="69"
            textAnchor="middle"
            fontSize="7"
            letterSpacing="0.08em"
            fill={C.amber}
          >
            gRPC
          </text>
        </svg>

        {/* Single footer numeral: the regression on the degrading hop. */}
        <div className="border-cc-card-border mt-4 border-t pt-4">
          <p className="font-heading text-cc-heading text-h4 leading-none font-semibold">
            +210ms
          </p>
          <p className="text-cc-ink-dim mt-1.5 text-xs">
            p99 latency, billing hop
          </p>
        </div>
      </div>
    </div>
  );
}
