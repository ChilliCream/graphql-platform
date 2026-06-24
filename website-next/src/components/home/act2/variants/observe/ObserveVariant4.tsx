import type { CSSProperties } from "react";

import { nitro } from "@/src/components/home/act2/variants/nitroTheme";

/**
 * "Production view" scene, variant 4 — service mini-topology.
 *
 * A tiny cropped slice of the Nitro service map: five nodes wired into a
 * left-to-right dependency graph (`api` fans out to `users-svc`, `billing`,
 * and `worker`; `worker` writes to `db`). Edges are dashed Nitro graph beziers
 * with arrow heads. The `api -> billing` gRPC edge is lit SOLID AMBER to flag
 * the one degraded hop, and the `billing` node carries a matching amber dot and
 * surface tint while every other node stays healthy teal.
 *
 * STATIC render of the settled final frame: no motion package, no in-view
 * hooks, no "use client". All chrome is hand-authored markup and the graph is
 * hand-drawn inline SVG laid out on a faint dot grid. The Nitro palette is
 * applied via inline `style={{}}` with `nitro.*` hexes (intentional here; the
 * rest of the site uses cc-* tokens). Every SVG id is prefixed "observe-v4-".
 */

interface ObserveVariant4Props {
  readonly className?: string;
}

type NodeTone = "healthy" | "degraded";

interface TopoNode {
  readonly id: string;
  readonly label: string;
  readonly proto: string;
  readonly x: number;
  readonly y: number;
  readonly tone: NodeTone;
}

const NODE_W = 86;
const NODE_H = 34;

// Five-node layout on a 346x176 canvas. `api` sits at the left rank and fans
// out to a middle rank; `worker` reaches one more rank to `db`.
const NODES: readonly TopoNode[] = [
  { id: "api", label: "api", proto: "gateway", x: 12, y: 71, tone: "healthy" },
  {
    id: "users",
    label: "users-svc",
    proto: "http",
    x: 130,
    y: 12,
    tone: "healthy",
  },
  {
    id: "billing",
    label: "billing",
    proto: "gRPC",
    x: 130,
    y: 71,
    tone: "degraded",
  },
  {
    id: "worker",
    label: "worker",
    proto: "queue",
    x: 130,
    y: 130,
    tone: "healthy",
  },
  { id: "db", label: "db", proto: "postgres", x: 248, y: 130, tone: "healthy" },
];

interface TopoEdge {
  readonly from: string;
  readonly to: string;
  readonly active: boolean;
}

const EDGES: readonly TopoEdge[] = [
  { from: "api", to: "users", active: false },
  { from: "api", to: "billing", active: true },
  { from: "api", to: "worker", active: false },
  { from: "worker", to: "db", active: false },
];

function byId(id: string): TopoNode {
  const node = NODES.find((n) => n.id === id);
  if (!node) {
    throw new Error(`unknown node ${id}`);
  }
  return node;
}

/** Right-edge midpoint of a node (edge source). */
function rightAnchor(n: TopoNode): { x: number; y: number } {
  return { x: n.x + NODE_W, y: n.y + NODE_H / 2 };
}

/** Left-edge midpoint of a node (edge target). */
function leftAnchor(n: TopoNode): { x: number; y: number } {
  return { x: n.x, y: n.y + NODE_H / 2 };
}

function edgePath(from: TopoNode, to: TopoNode): string {
  const a = rightAnchor(from);
  const b = leftAnchor(to);
  const dx = (b.x - a.x) / 2;
  return `M${a.x} ${a.y} C${a.x + dx} ${a.y} ${b.x - dx} ${b.y} ${b.x} ${b.y}`;
}

export function ObserveVariant4({ className }: ObserveVariant4Props) {
  const cardStyle: CSSProperties = {
    background: nitro.bg,
    border: `1px solid ${nitro.border}`,
    borderRadius: nitro.radius,
    padding: 14,
    fontFamily: nitro.font,
    color: nitro.text,
  };

  return (
    <div
      className={className}
      style={{
        position: "relative",
        margin: "0 auto",
        width: "100%",
        maxWidth: 320,
        userSelect: "none",
      }}
    >
      <div style={cardStyle}>
        {/* Single thin title row. */}
        <div
          style={{
            display: "flex",
            alignItems: "center",
            justifyContent: "space-between",
            gap: 12,
          }}
        >
          <span
            style={{
              fontFamily: nitro.mono,
              fontSize: 11,
              letterSpacing: "0.02em",
              color: nitro.textDim,
            }}
          >
            service map
          </span>
          <span
            style={{
              display: "inline-flex",
              alignItems: "center",
              gap: 6,
              borderRadius: 999,
              border: `1px solid ${nitro.warning}66`,
              background: `${nitro.warning}1a`,
              padding: "3px 9px",
              fontFamily: nitro.mono,
              fontSize: 10,
              letterSpacing: "0.04em",
              color: nitro.warning,
              whiteSpace: "nowrap",
            }}
          >
            <span
              aria-hidden="true"
              style={{
                display: "inline-block",
                width: 6,
                height: 6,
                borderRadius: 999,
                background: nitro.warning,
              }}
            />
            1 degraded
          </span>
        </div>

        {/* Topology graph on a faint dot grid. */}
        <div
          style={{
            position: "relative",
            marginTop: 12,
            background: nitro.graphCanvas,
            border: `1px solid ${nitro.border}`,
            borderRadius: 5,
            overflow: "hidden",
          }}
        >
          <div
            aria-hidden="true"
            style={{
              position: "absolute",
              inset: 0,
              backgroundImage: `radial-gradient(${nitro.graphDots} 1px, transparent 1px)`,
              backgroundSize: "16px 16px",
              opacity: 0.45,
            }}
          />

          <svg
            role="img"
            aria-label="Service topology: api fans out to users-svc, billing over gRPC, and worker; worker writes to db. The api to billing gRPC edge is degraded."
            viewBox="0 0 346 176"
            style={{
              position: "relative",
              display: "block",
              width: "100%",
              height: "auto",
            }}
          >
            <defs>
              <marker
                id="observe-v4-arrow"
                markerWidth="7"
                markerHeight="7"
                refX="6"
                refY="3"
                orient="auto"
              >
                <path d="M0 0 L6 3 L0 6 Z" fill={nitro.graphEdge} />
              </marker>
              <marker
                id="observe-v4-arrow-on"
                markerWidth="7"
                markerHeight="7"
                refX="6"
                refY="3"
                orient="auto"
              >
                <path d="M0 0 L6 3 L0 6 Z" fill={nitro.graphEdgeActive} />
              </marker>
            </defs>

            {/* Edges (drawn under the nodes). */}
            {EDGES.map((e) => {
              const from = byId(e.from);
              const to = byId(e.to);
              return (
                <path
                  key={`${e.from}-${e.to}`}
                  d={edgePath(from, to)}
                  fill="none"
                  stroke={e.active ? nitro.graphEdgeActive : nitro.graphEdge}
                  strokeWidth={2}
                  strokeLinecap="round"
                  strokeDasharray={e.active ? "none" : "6 5"}
                  markerEnd={
                    e.active
                      ? "url(#observe-v4-arrow-on)"
                      : "url(#observe-v4-arrow)"
                  }
                />
              );
            })}

            {/* gRPC protocol label on the lit edge. */}
            <text
              x={byId("billing").x - 4}
              y={byId("billing").y + NODE_H / 2 - 7}
              textAnchor="end"
              style={{
                fill: nitro.warning,
                fontFamily: nitro.mono,
                fontSize: "9px",
                fontWeight: 600,
              }}
            >
              gRPC
            </text>

            {/* Nodes. */}
            {NODES.map((n) => {
              const degraded = n.tone === "degraded";
              const dot = degraded ? nitro.warning : nitro.successText;
              return (
                <g key={n.id}>
                  <rect
                    x={n.x}
                    y={n.y}
                    width={NODE_W}
                    height={NODE_H}
                    rx={6}
                    fill={degraded ? nitro.graphNodeWarning : nitro.graphNode}
                    stroke={degraded ? `${nitro.warning}88` : nitro.graphEdge}
                    strokeWidth={1}
                  />
                  <circle
                    cx={n.x + 13}
                    cy={n.y + NODE_H / 2}
                    r={3.5}
                    fill={dot}
                  />
                  <text
                    x={n.x + 24}
                    y={n.y + 14}
                    style={{
                      fill: nitro.textStrong,
                      fontFamily: nitro.mono,
                      fontSize: "11px",
                      fontWeight: 600,
                    }}
                  >
                    {n.label}
                  </text>
                  <text
                    x={n.x + 24}
                    y={n.y + 26}
                    style={{
                      fill: nitro.textDim,
                      fontFamily: nitro.mono,
                      fontSize: "9px",
                    }}
                  >
                    {n.proto}
                  </text>
                </g>
              );
            })}
          </svg>
        </div>

        {/* Footer hairline: the one degraded hop. */}
        <div
          style={{
            marginTop: 12,
            paddingTop: 10,
            borderTop: `1px solid ${nitro.border}`,
            display: "flex",
            alignItems: "center",
            justifyContent: "space-between",
          }}
        >
          <span
            style={{
              fontFamily: nitro.mono,
              fontSize: 10,
              color: nitro.textDim,
            }}
          >
            <span style={{ color: nitro.textSecondary }}>api</span>
            <span style={{ color: nitro.warning }}> &rarr; </span>
            <span style={{ color: nitro.textSecondary }}>billing</span>
          </span>
          <span
            style={{
              borderRadius: 999,
              border: `1px solid ${nitro.border}`,
              padding: "2px 8px",
              fontFamily: nitro.mono,
              fontSize: 10,
              color: nitro.warning,
            }}
          >
            p99 +210ms
          </span>
        </div>
      </div>
    </div>
  );
}
