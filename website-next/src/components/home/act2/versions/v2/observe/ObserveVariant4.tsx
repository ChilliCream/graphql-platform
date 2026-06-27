interface ObserveVariant4Props {
  readonly className?: string;
}

/**
 * Production-view scene (v2 "Flow Diagrams", variant 4: service topology
 * degrading).
 *
 * The checkout request as a hub-and-spoke relationship diagram in the locked
 * v2 flow vocabulary (Chip nodes on cc-surface, 1px cc-ink-faint connectors,
 * thin open arrowheads). `api` is the teal-active hub at the left rank; it fans
 * out over four spokes to `users-svc`, `billing (gRPC)`, and `worker`, and
 * `worker` reaches one rank further to `db`. Three spokes stay healthy grey.
 * The single api -> billing gRPC spoke is the degrading hop: it carries the
 * amber investigating status (real status, not decoration), the billing node
 * wears a matching amber status border, and an amber arrowhead terminates it.
 * The relationship the headline names is which dependency is going bad. A
 * Stat duo footer reports the latency regression and the blast radius.
 *
 * Fully static settled frame: React Server Component, no hooks, no animation.
 * cc-* palette only; every svg id is prefixed "v2-observe-4-".
 */

const C = {
  surface: "#0c1322", // node fill (cc-surface)
  border: "rgba(245, 241, 234, 0.12)", // node / divider border (cc-card-border)
  connector: "rgba(245, 241, 234, 0.16)", // grey connectors (cc-ink-faint)
  ink: "#a1a3af", // node label (cc-ink)
  navLabel: "#62748e", // eyebrow / proto label (cc-nav-label)
  inkDim: "rgba(245, 241, 234, 0.62)", // caption / not-reached (cc-ink-dim)
  heading: "#f5f0ea", // stat numerals (cc-heading)
  accent: "#5eead4", // single traced path + active hub (cc-accent)
  amber: "#fbbf24", // degrading hop status (cc-status-investigating)
} as const;

const MONO =
  'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace';

const ID = "v2-observe-4-";

interface TopoNode {
  readonly label: string;
  readonly proto: string;
  readonly x: number;
  readonly y: number;
  readonly degraded: boolean;
}

const NODE_W = 78;
const NODE_H = 30;

// Hub-and-spoke layout on a 320x176 cell. `api` is the left hub; users-svc,
// billing, and worker are the middle rank; db sits one rank past worker.
const NODES: Readonly<Record<string, TopoNode>> = {
  api: { label: "api", proto: "gateway", x: 14, y: 73, degraded: false },
  users: { label: "users-svc", proto: "http", x: 132, y: 18, degraded: false },
  billing: { label: "billing", proto: "gRPC", x: 132, y: 73, degraded: true },
  worker: { label: "worker", proto: "queue", x: 132, y: 128, degraded: false },
  db: { label: "db", proto: "postgres", x: 244, y: 128, degraded: false },
};

/** Right-edge midpoint of a node (spoke source). */
function rightX(n: TopoNode): number {
  return n.x + NODE_W;
}

/** Vertical midpoint of a node. */
function midY(n: TopoNode): number {
  return n.y + NODE_H / 2;
}

export function ObserveVariant4({ className }: ObserveVariant4Props) {
  const api = NODES.api;
  const users = NODES.users;
  const billing = NODES.billing;
  const worker = NODES.worker;
  const db = NODES.db;

  // Spokes leave api at a shared bus x, then turn vertically into the rank
  // (single-elbow orthogonal, no curves).
  const busX = rightX(api) + 16;

  function spoke(to: TopoNode): string {
    const sx = rightX(api);
    const sy = midY(api);
    const ty = midY(to);
    return `M${sx} ${sy} H${busX} V${ty} H${to.x}`;
  }

  // worker -> db is a straight horizontal hop on the bottom rank.
  const wdY = midY(worker);
  const wdPath = `M${rightX(worker)} ${wdY} H${db.x}`;

  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      aria-hidden="true"
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        <div className="flex items-center justify-between">
          <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
            service map / checkout
          </p>
          <span
            className="inline-flex items-center gap-1.5 rounded-full border px-2 py-0.5 font-mono text-[0.55rem] tracking-[0.08em] uppercase"
            style={{ borderColor: `${C.amber}66`, color: C.amber }}
          >
            <span
              aria-hidden="true"
              className="inline-block size-1.5 rounded-full"
              style={{ background: C.amber }}
            />
            1 degraded
          </span>
        </div>

        <svg
          viewBox="0 0 320 176"
          width="100%"
          role="img"
          aria-label="Hub-and-spoke service topology for the checkout request: api fans out to users-svc, billing over gRPC, and worker, and worker writes to db. The api to billing gRPC spoke is degrading, flagged amber as the dependency going bad."
          className="mt-3 block"
        >
          <defs>
            {/* Thin open arrowheads (single triangle, 1px stroke). */}
            <marker
              id={`${ID}arrow`}
              markerWidth="8"
              markerHeight="8"
              refX="6"
              refY="3"
              orient="auto"
            >
              <path
                d="M0 0 L6 3 L0 6"
                fill="none"
                stroke={C.connector}
                strokeWidth={1}
              />
            </marker>
            <marker
              id={`${ID}arrowAccent`}
              markerWidth="8"
              markerHeight="8"
              refX="6"
              refY="3"
              orient="auto"
            >
              <path
                d="M0 0 L6 3 L0 6"
                fill="none"
                stroke={C.accent}
                strokeWidth={1}
              />
            </marker>
            <marker
              id={`${ID}arrowAmber`}
              markerWidth="8"
              markerHeight="8"
              refX="6"
              refY="3"
              orient="auto"
            >
              <path
                d="M0 0 L6 3 L0 6"
                fill="none"
                stroke={C.amber}
                strokeWidth={1}
              />
            </marker>
          </defs>

          {/* ===== Spokes (drawn under the nodes) ===== */}
          {/* Healthy fan-out hops: grey 1px connectors, thin open arrowheads. */}
          <g fill="none" strokeWidth={1} stroke={C.connector}>
            <path d={spoke(users)} markerEnd={`url(#${ID}arrow)`} />
            <path d={spoke(worker)} markerEnd={`url(#${ID}arrow)`} />
            <path d={wdPath} markerEnd={`url(#${ID}arrow)`} />
          </g>

          {/* The traced path the headline names: the checkout hop into billing.
              api leaves teal (the active source), and the spoke turns amber at
              the degrading gRPC link, so the diagram reads source -> the one
              dependency going bad. */}
          <path
            d={`M${rightX(api)} ${midY(api)} H${busX}`}
            fill="none"
            stroke={C.accent}
            strokeWidth={1}
          />
          <path
            d={`M${busX} ${midY(api)} V${midY(billing)} H${billing.x}`}
            fill="none"
            stroke={C.amber}
            strokeWidth={1}
            markerEnd={`url(#${ID}arrowAmber)`}
          />
          {/* gRPC protocol annotation on the degrading edge. */}
          <text
            x={busX + 5}
            y={midY(billing) - 5}
            fill={C.amber}
            fontFamily={MONO}
            fontSize={8.5}
            letterSpacing="0.04em"
          >
            gRPC
          </text>

          {/* ===== Nodes ===== */}
          <Chip node={api} active />
          <Chip node={users} />
          <Chip node={billing} />
          <Chip node={worker} />
          <Chip node={db} />
        </svg>

        {/* Footer: the one degrading hop named, plus the two key numbers. */}
        <div className="border-cc-card-border mt-4 flex items-center justify-between border-t pt-4">
          <span className="font-mono text-[0.65rem]">
            <span style={{ color: C.ink }}>api</span>
            <span style={{ color: C.amber }} className="px-1">
              &rarr;
            </span>
            <span style={{ color: C.ink }}>billing</span>
          </span>
          <span
            className="rounded-full border px-2 py-0.5 font-mono text-[0.55rem] tracking-[0.04em]"
            style={{ borderColor: C.border, color: C.amber }}
          >
            gRPC degrading
          </span>
        </div>

        <div className="border-cc-card-border mt-4 grid grid-cols-2 gap-4 border-t pt-4">
          <div>
            <p
              className="font-heading text-h4 leading-none font-semibold"
              style={{ color: C.heading }}
            >
              +210ms
            </p>
            <p className="mt-1.5 text-xs" style={{ color: C.inkDim }}>
              p99 latency, billing
            </p>
          </div>
          <div>
            <p
              className="font-heading text-h4 leading-none font-semibold"
              style={{ color: C.heading }}
            >
              #1
            </p>
            <p className="mt-1.5 text-xs" style={{ color: C.inkDim }}>
              impact: checkout
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}

/**
 * A topology node as a v2 Chip: rounded bordered pill on cc-surface with a mono
 * label and protocol sub-label. The active hub takes the teal Chip style; the
 * degrading node takes an amber status border. Every other node stays grey.
 */
function Chip({
  node,
  active = false,
}: {
  readonly node: TopoNode;
  readonly active?: boolean;
}) {
  const stroke = node.degraded ? C.amber : active ? C.accent : C.border;
  const dot = node.degraded ? C.amber : active ? C.accent : C.navLabel;
  const labelColor = node.degraded ? C.amber : active ? C.accent : C.ink;
  return (
    <g>
      <rect
        x={node.x}
        y={node.y}
        width={NODE_W}
        height={NODE_H}
        rx={8}
        fill={C.surface}
        stroke={stroke}
        strokeWidth={1}
      />
      {node.degraded || active ? (
        <circle cx={node.x + 11} cy={node.y + 11} r="2.5" fill={dot} />
      ) : (
        <circle
          cx={node.x + 11}
          cy={node.y + 11}
          r="2.5"
          fill="none"
          stroke={dot}
          strokeWidth={1}
        />
      )}
      <text
        x={node.x + 19}
        y={node.y + 13.5}
        fill={labelColor}
        fontFamily={MONO}
        fontSize={9}
        letterSpacing="0.02em"
        fontWeight={500}
      >
        {node.label}
      </text>
      <text
        x={node.x + 11}
        y={node.y + 24}
        fill={C.navLabel}
        fontFamily={MONO}
        fontSize={8}
        letterSpacing="0.06em"
      >
        {node.proto}
      </text>
    </g>
  );
}
