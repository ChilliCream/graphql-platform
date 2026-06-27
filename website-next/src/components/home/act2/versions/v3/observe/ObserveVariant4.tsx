/**
 * "Production view" scene, concept 4 ("Service topology degrading"), v3
 * "Signal & Metrics".
 *
 * Leads with the measured cost of the bad hop: a single cream hero numeral
 * "+210 ms" (the p99 the degrading `api -> billing` gRPC edge adds) over a
 * lowercase mono caption that names the hop. The supporting signal is the
 * checkout mini-topology, the take that makes this cell the topology read among
 * the five: `api` fans out to `users-svc`, `billing`, and `worker`, and `worker`
 * writes to `db`. `api` is the single teal active source (where the trace is
 * measured from); the one `api -> billing` gRPC hop and the `billing` node carry
 * the lone amber status, the genuine degrading dependency, so the graph answers
 * which dependency is going bad.
 *
 * Content is faithful to the v1/v2 baseline: nodes api / users-svc / billing /
 * worker / db; the four directed edges; the degrading `api -> billing` gRPC hop;
 * "1 degraded"; "p99 +210ms".
 *
 * Strict cc-* dark palette. Exactly one teal accent (the active `api` source)
 * and the hero numeral stays cream; amber is the single status hue and owns the
 * degrading hop, its node, and the "1 degraded" chip; teal never encodes status.
 * Static settled frame: no animation, no hooks, no "use client". Server
 * component. Every SVG id is prefixed "v3-observe-4-".
 */

interface ObserveVariant4Props {
  readonly className?: string;
}

/* Strict cc-* dark tokens mirrored locally (matching the v3 system). Teal is the
 * single decorative accent (the active source node); status hues encode real
 * state only. */
const cc = {
  surface: "#0c1322",
  cardBorder: "rgba(245,241,234,0.12)",
  inkFaint: "rgba(245,241,234,0.16)",
  heading: "#f5f0ea",
  ink: "#a1a3af",
  inkDim: "rgba(245,241,234,0.62)",
  navLabel: "#62748e",
  teal: "#5eead4",
  amber: "#fbbf24",
} as const;

const MONO =
  'ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, "Liberation Mono", monospace';
const HEADING = '"Josefin Sans", Futura, sans-serif';

type NodeTone = "source" | "degraded" | "healthy";

interface TopoNode {
  readonly id: string;
  readonly label: string;
  /** top-left position in viewBox units. */
  readonly x: number;
  readonly y: number;
  readonly tone: NodeTone;
}

const NODE_W = 60;
const NODE_H = 15;

// Compact, wide-aspect left-to-right fan-out: the supporting structure under the
// number. Kept flatter than a square graph so the cell height matches the
// sibling cells in the set.
const NODES: readonly TopoNode[] = [
  { id: "api", label: "api", x: 4, y: 22.5, tone: "source" },
  { id: "users", label: "users-svc", x: 100, y: 1.5, tone: "healthy" },
  { id: "billing", label: "billing", x: 100, y: 22.5, tone: "degraded" },
  { id: "worker", label: "worker", x: 100, y: 43.5, tone: "healthy" },
  { id: "db", label: "db", x: 196, y: 43.5, tone: "healthy" },
];

interface TopoEdge {
  readonly from: string;
  readonly to: string;
  /** the one degrading hop. */
  readonly degrading: boolean;
}

const EDGES: readonly TopoEdge[] = [
  { from: "api", to: "users", degrading: false },
  { from: "api", to: "billing", degrading: true },
  { from: "api", to: "worker", degrading: false },
  { from: "worker", to: "db", degrading: false },
];

function byId(id: string): TopoNode {
  const node = NODES.find((n) => n.id === id);
  if (!node) {
    throw new Error(`unknown node ${id}`);
  }
  return node;
}

export function ObserveVariant4({ className }: ObserveVariant4Props) {
  const idp = "v3-observe-4-";

  const topoW = 260;
  const topoH = 60;
  const right = (n: TopoNode) => ({ x: n.x + NODE_W, y: n.y + NODE_H / 2 });
  const left = (n: TopoNode) => ({ x: n.x, y: n.y + NODE_H / 2 });
  const elbow = (from: TopoNode, to: TopoNode) => {
    const a = right(from);
    const b = left(to);
    const midX = a.x + (b.x - a.x) / 2;
    return `M${a.x} ${a.y} H${midX} V${b.y} H${b.x}`;
  };

  const billing = byId("billing");

  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      aria-hidden="true"
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        {/* eyebrow + the single status hue chip (1 degraded) */}
        <div className="flex items-center justify-between">
          <span
            className="font-mono text-[0.58rem] tracking-[0.15em] uppercase"
            style={{ color: cc.navLabel }}
          >
            service map
          </span>
          <span
            className="inline-flex items-center gap-1.5 rounded-full border px-2 py-0.5 font-mono text-[0.5rem] tracking-[0.08em] uppercase"
            style={{ borderColor: `${cc.amber}59`, color: cc.amber }}
          >
            <span
              aria-hidden="true"
              className="inline-block h-1.5 w-1.5 rounded-full"
              style={{ backgroundColor: cc.amber }}
            />
            1 degraded
          </span>
        </div>

        {/* HERO numeral: the p99 the degrading hop adds */}
        <div className="mt-3 flex items-baseline gap-1.5">
          <span
            className="leading-none font-semibold"
            style={{
              fontFamily: HEADING,
              fontSize: "2.25rem",
              color: cc.heading,
            }}
          >
            +210
          </span>
          <span
            className="font-mono text-[0.85rem]"
            style={{ color: cc.navLabel }}
          >
            ms
          </span>
        </div>
        <p
          className="mt-1 font-mono text-[0.68rem] lowercase"
          style={{ color: cc.inkDim }}
        >
          p99 added on api &rarr; billing
        </p>

        {/* signal: the checkout mini-topology; only the degrading gRPC hop is amber */}
        <div className="border-cc-card-border mt-3 border-t pt-3">
          <svg
            viewBox={`0 0 ${topoW} ${topoH}`}
            width="100%"
            role="img"
            aria-label="api fans out to users-svc, billing over gRPC, and worker; worker writes to db. The api to billing gRPC hop is degrading."
            style={{ display: "block" }}
          >
            <defs>
              <marker
                id={`${idp}arrow`}
                markerWidth="7"
                markerHeight="7"
                refX="5.5"
                refY="3"
                orient="auto"
              >
                <polyline
                  points="1.5,1 5.5,3 1.5,5"
                  fill="none"
                  stroke={cc.navLabel}
                  strokeWidth="1"
                  strokeLinecap="round"
                  strokeLinejoin="round"
                />
              </marker>
              <marker
                id={`${idp}arrow-on`}
                markerWidth="7"
                markerHeight="7"
                refX="5.5"
                refY="3"
                orient="auto"
              >
                <polyline
                  points="1.5,1 5.5,3 1.5,5"
                  fill="none"
                  stroke={cc.amber}
                  strokeWidth="1"
                  strokeLinecap="round"
                  strokeLinejoin="round"
                />
              </marker>
            </defs>

            {/* edges, under the nodes; only the degrading hop is amber */}
            {EDGES.map((e) => {
              const from = byId(e.from);
              const to = byId(e.to);
              return (
                <path
                  key={`${idp}edge-${e.from}-${e.to}`}
                  d={elbow(from, to)}
                  fill="none"
                  stroke={e.degrading ? cc.amber : cc.cardBorder}
                  strokeWidth={1}
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  markerEnd={
                    e.degrading ? `url(#${idp}arrow-on)` : `url(#${idp}arrow)`
                  }
                />
              );
            })}

            {/* gRPC tag on the degrading hop */}
            <text
              x={billing.x - 10}
              y={billing.y + NODE_H / 2 - 3}
              textAnchor="middle"
              fill={cc.amber}
              style={{
                fontFamily: MONO,
                fontSize: "6.5px",
                fontWeight: 600,
                letterSpacing: "0.05em",
              }}
            >
              gRPC
            </text>

            {/* nodes: surface chips with a 1px border; source teal, bad amber */}
            {NODES.map((n) => {
              const isSource = n.tone === "source";
              const isDegraded = n.tone === "degraded";
              const accent = isDegraded
                ? cc.amber
                : isSource
                  ? cc.teal
                  : cc.navLabel;
              const labelColor = isDegraded
                ? cc.heading
                : isSource
                  ? cc.teal
                  : cc.ink;
              const strokeColor = isDegraded
                ? cc.amber
                : isSource
                  ? `${cc.teal}66`
                  : cc.cardBorder;
              return (
                <g key={`${idp}node-${n.id}`}>
                  <rect
                    x={n.x}
                    y={n.y}
                    width={NODE_W}
                    height={NODE_H}
                    rx="5"
                    fill={cc.surface}
                    stroke={strokeColor}
                    strokeWidth="1"
                  />
                  <circle
                    cx={n.x + 9}
                    cy={n.y + NODE_H / 2}
                    r="2.2"
                    fill={accent}
                  />
                  <text
                    x={n.x + 16}
                    y={n.y + NODE_H / 2 + 2.3}
                    fill={labelColor}
                    style={{
                      fontFamily: MONO,
                      fontSize: "7.5px",
                      fontWeight: isDegraded || isSource ? 600 : 500,
                      letterSpacing: "0.01em",
                    }}
                  >
                    {n.label}
                  </text>
                </g>
              );
            })}
          </svg>
        </div>
      </div>
    </div>
  );
}
