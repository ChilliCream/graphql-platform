interface ObserveVariant4Props {
  readonly className?: string;
}

/**
 * "Production view" scene, v4 "Generated Artifacts", variant 4: the checkout
 * service topology, emitted from live traffic with one hop degrading.
 *
 * Locked v4 PATTERN D (a directed dependency graph on one cc-surface tile with a
 * single annotated token). `api` fans out to `users-svc`, `billing` over gRPC,
 * and `worker`; `worker` writes to `db`. The healthy hops are solid 1px faint
 * grey routes with open chevrons; the `api -> billing` hop is the one alarmed
 * data path, drawn solid amber, and `billing` carries the matching amber status
 * dot. The concept IS a real status (a dependency degrading), so amber is the
 * single accent cluster and teal steps aside entirely.
 *
 * The signature callout adopts the status hue: an amber anchor dot on the
 * `billing` node (the dependency going bad), a 1px amber leader rising into the
 * top-right negative space, the load-bearing `+210 ms` p99 figure, a 2px amber
 * tick, and a "SLOW HOP" micro-label. The amber "1 DEGRADED" pill summarises it
 * in the title bar. Strip the amber and a neutral grey graph remains; the lone
 * cream strong token is the `checkout` request name.
 *
 * Literal content (node names api / users-svc / billing / worker / db, the gRPC
 * protocol, the api -> billing degraded hop, p99 +210 ms) is borrowed verbatim
 * from the v1 sibling. React Server Component, settled final frame, no motion.
 * Every svg id is prefixed "v4-observe-4-".
 */

const C = {
  surface: "#0c1322",
  cardBorder: "rgba(245,241,234,0.12)",
  inkFaint: "rgba(245,241,234,0.16)",
  ink: "#a1a3af",
  inkDim: "rgba(245,241,234,0.62)",
  navLabel: "#62748e",
  heading: "#f5f0ea",
  amber: "#fbbf24",
  mono: 'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace',
} as const;

const ID = "v4-observe-4-";

const NODE_H = 22;

interface TopoNode {
  readonly id: string;
  readonly label: string;
  readonly x: number;
  readonly y: number;
  readonly w: number;
  readonly degraded?: boolean;
}

// Five nodes on a left-to-right dependency graph inside the tile.
const NODES: readonly TopoNode[] = [
  { id: "api", label: "api", x: 14, y: 78, w: 52 },
  { id: "users", label: "users-svc", x: 120, y: 40, w: 84 },
  { id: "billing", label: "billing", x: 120, y: 78, w: 84, degraded: true },
  { id: "worker", label: "worker", x: 120, y: 116, w: 84 },
  { id: "db", label: "db", x: 232, y: 116, w: 58 },
];

interface TopoEdge {
  readonly from: string;
  readonly to: string;
  readonly degraded?: boolean;
}

const EDGES: readonly TopoEdge[] = [
  { from: "api", to: "users" },
  { from: "api", to: "billing", degraded: true },
  { from: "api", to: "worker" },
  { from: "worker", to: "db" },
];

function nodeById(id: string): TopoNode {
  const found = NODES.find((n) => n.id === id);
  if (!found) {
    throw new Error(`unknown node ${id}`);
  }
  return found;
}

function rightAnchor(n: TopoNode): readonly [number, number] {
  return [n.x + n.w, n.y + NODE_H / 2];
}

function leftAnchor(n: TopoNode): readonly [number, number] {
  return [n.x, n.y + NODE_H / 2];
}

function edgePath(
  a: readonly [number, number],
  b: readonly [number, number],
): string {
  const dx = (b[0] - a[0]) / 2;
  return `M${a[0]} ${a[1]} C${a[0] + dx} ${a[1]} ${b[0] - dx} ${b[1]} ${b[0]} ${b[1]}`;
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

        <div className="mt-3">
          <svg
            viewBox="0 0 320 172"
            width="100%"
            style={{ display: "block", fontFamily: C.mono }}
          >
            <defs>
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
                  stroke={C.navLabel}
                  strokeWidth="1"
                />
              </marker>
              <marker
                id={`${ID}headAmber`}
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
                  stroke={C.amber}
                  strokeWidth="1"
                />
              </marker>
            </defs>

            {/* ---- Artifact tile: the checkout service graph ---- */}
            <rect
              x={6}
              y={4}
              width={308}
              height={152}
              rx={8}
              fill={C.surface}
              stroke={C.cardBorder}
              strokeWidth={1}
            />

            {/* Title bar: request name (the one cream token) + amber status pill. */}
            <text x={15} y={18} fontSize={11} fontWeight={600} fill={C.heading}>
              checkout
            </text>
            <rect
              x={240}
              y={7}
              width={68}
              height={15}
              rx={7.5}
              fill={C.amber}
              fillOpacity={0.08}
              stroke={C.amber}
              strokeWidth={1}
            />
            <circle cx={249} cy={14.5} r={2.5} fill={C.amber} />
            <text
              x={256}
              y={18}
              fontSize={7}
              letterSpacing="0.06em"
              fill={C.amber}
            >
              1 DEGRADED
            </text>
            <line
              x1={6}
              y1={26}
              x2={314}
              y2={26}
              stroke={C.cardBorder}
              strokeWidth={1}
            />

            {/* Edges (drawn under the nodes); healthy = solid faint, degraded = amber. */}
            {EDGES.map((e) => {
              const a = rightAnchor(nodeById(e.from));
              const b = leftAnchor(nodeById(e.to));
              return (
                <path
                  key={`${e.from}-${e.to}`}
                  d={edgePath(a, b)}
                  fill="none"
                  stroke={e.degraded ? C.amber : C.inkFaint}
                  strokeWidth={e.degraded ? 1.5 : 1}
                  strokeLinecap="round"
                  markerEnd={
                    e.degraded ? `url(#${ID}headAmber)` : `url(#${ID}head)`
                  }
                />
              );
            })}

            {/* gRPC protocol label on the degraded hop (neutral metadata). */}
            <text x={74} y={85} fontSize={8} fill={C.navLabel}>
              gRPC
            </text>

            {/* Nodes. */}
            {NODES.map((n) => (
              <g key={n.id}>
                <rect
                  x={n.x}
                  y={n.y}
                  width={n.w}
                  height={NODE_H}
                  rx={6}
                  fill={C.surface}
                  stroke={n.degraded ? C.amber : C.cardBorder}
                  strokeWidth={1}
                  strokeOpacity={n.degraded ? 0.5 : 1}
                />
                <circle
                  cx={n.x + 11}
                  cy={n.y + NODE_H / 2}
                  r={2.5}
                  fill={n.degraded ? C.amber : C.navLabel}
                />
                <text
                  x={n.x + 20}
                  y={n.y + NODE_H / 2 + 3}
                  fontSize={9}
                  fill={C.ink}
                >
                  {n.label}
                </text>
              </g>
            ))}

            {/* ---- The single amber callout: the degrading dependency ---- */}
            <circle cx={203} cy={82} r={4} fill={C.amber} fillOpacity={0.18} />
            <circle cx={203} cy={82} r={2.2} fill={C.amber} />
            <path
              d="M204 80 L235 62"
              fill="none"
              stroke={C.amber}
              strokeWidth={1}
            />
            <text x={239} y={66} fontSize={12} fontWeight={600} fill={C.amber}>
              +210 ms
            </text>
            <line
              x1={239}
              y1={70}
              x2={283}
              y2={70}
              stroke={C.amber}
              strokeWidth={2}
            />
            <text
              x={239}
              y={81}
              fontSize={7}
              letterSpacing="0.1em"
              fill={C.amber}
            >
              SLOW HOP
            </text>
          </svg>
        </div>

        <div className="border-cc-ink-faint mt-4 border-t border-dashed pt-3">
          <p className="text-cc-ink-dim text-center text-xs">
            billing is the slow hop in the checkout request
          </p>
        </div>
      </div>
    </div>
  );
}
