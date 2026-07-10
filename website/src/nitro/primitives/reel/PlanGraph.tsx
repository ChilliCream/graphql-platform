import { useMemo } from "react";
import { motion, useTransform, type MotionValue } from "motion/react";
import { token } from "../../lib/tokens";
import { CodeBlock } from "../CodeBlock";
import type { PlanNode, PlanEdge, PlanStatus } from "../../lib/data/tabs";

const NODE_W = 250;
const H_EXPANDED = 150;
const H_SLIM = 58;
const RANKSEP = 66;
const NODESEP = 26;

const statusBg: Record<PlanStatus, string> = {
  success: token.graphNodeSuccess,
  partial: token.graphNodeWarning,
  failed: token.graphNodeFailure,
};
const statusDot: Record<PlanStatus, string> = {
  success: token.accentHover,
  partial: token.warning,
  failed: token.error,
};

interface Placed extends PlanNode {
  x: number;
  y: number;
  h: number;
}

export interface PlanGraphProps {
  nodes: PlanNode[];
  edges: PlanEdge[];
  progress: MotionValue<number>;
  hoverId?: string;
  revealStart?: number;
  revealEnd?: number;
  hoverStart?: number;
  hoverEnd?: number;
  fitScale?: number;
}

export function PlanGraph({
  nodes,
  edges,
  progress,
  hoverId = "products",
  revealStart = 0.06,
  revealEnd = 0.7,
  hoverStart = 0.74,
  hoverEnd = 0.96,
  fitScale = 1,
}: PlanGraphProps) {
  const { placed, byId, W, H, ranks } = useMemo(() => {
    const ranks = Math.max(...nodes.map((n) => n.rank)) + 1;
    const byRank: PlanNode[][] = Array.from({ length: ranks }, () => []);
    nodes.forEach((n) => byRank[n.rank].push(n));
    byRank.forEach((col) => col.sort((a, b) => a.order - b.order));
    const colH = byRank.map(
      (col) =>
        col.reduce(
          (s, n) =>
            s +
            (n.kind === "resolve" || n.kind === "introspection"
              ? H_SLIM
              : H_EXPANDED),
          0,
        ) +
        (col.length - 1) * NODESEP,
    );
    const H = Math.max(...colH);
    const placed: Placed[] = [];
    byRank.forEach((col, r) => {
      let y = (H - colH[r]) / 2;
      col.forEach((n) => {
        const h =
          n.kind === "resolve" || n.kind === "introspection"
            ? H_SLIM
            : H_EXPANDED;
        placed.push({ ...n, x: r * (NODE_W + RANKSEP), y, h });
        y += h + NODESEP;
      });
    });
    const W = ranks * NODE_W + (ranks - 1) * RANKSEP;
    const byId: Record<string, Placed> = {};
    placed.forEach((p) => (byId[p.id] = p));
    return { placed, byId, W, H, ranks };
  }, [nodes]);

  const lineage = useMemo(() => {
    const set = new Set<string>([hoverId]);
    let added = true;
    while (added) {
      added = false;
      for (const e of edges) {
        if (set.has(e.to) && !set.has(e.from)) {
          set.add(e.from);
          added = true;
        }
      }
    }
    return set;
  }, [edges, hoverId]);

  const slot = (revealEnd - revealStart) / Math.max(1, ranks);
  const appearAt = (n: Placed) => revealStart + slot * n.rank;
  const execAt = (rank: number) => revealStart + slot * rank + slot * 0.45;

  return (
    <div
      style={{
        position: "absolute",
        inset: 0,
        background: token.graphCanvas,
        overflow: "hidden",
      }}
    >
      <div
        style={{
          position: "absolute",
          inset: 0,
          backgroundImage: `radial-gradient(${token.graphDots} 1px, transparent 1px)`,
          backgroundSize: "18px 18px",
          opacity: 0.5,
        }}
      />
      <div
        style={{
          position: "absolute",
          left: "50%",
          top: "50%",
          width: W,
          height: H,
          transform: `translate(-50%,-50%) scale(${fitScale})`,
        }}
      >
        <svg
          width={W}
          height={H}
          style={{ position: "absolute", inset: 0, overflow: "visible" }}
        >
          <defs>
            <marker
              id="pg-arrow"
              markerWidth="7"
              markerHeight="7"
              refX="6"
              refY="3"
              orient="auto"
            >
              <path d="M0 0 L6 3 L0 6 Z" fill={token.graphEdge} />
            </marker>
            <marker
              id="pg-arrow-on"
              markerWidth="7"
              markerHeight="7"
              refX="6"
              refY="3"
              orient="auto"
            >
              <path d="M0 0 L6 3 L0 6 Z" fill={token.graphEdgeActive} />
            </marker>
          </defs>
          {edges.map((e) => (
            <EdgePath
              key={`${e.from}-${e.to}`}
              from={byId[e.from]}
              to={byId[e.to]}
              progress={progress}
              drawAt={appearAt(byId[e.to])}
              execAt={execAt(byId[e.to].rank)}
              hoverStart={hoverStart}
              hoverEnd={hoverEnd}
              lit={lineage.has(e.from) && lineage.has(e.to)}
            />
          ))}
        </svg>
        {placed.map((n) => (
          <NodeCard
            key={n.id}
            node={n}
            progress={progress}
            appearAt={appearAt(n)}
            execAt={execAt(n.rank)}
            hoverStart={hoverStart}
            hoverEnd={hoverEnd}
            inLineage={lineage.has(n.id)}
          />
        ))}
      </div>
    </div>
  );
}

function EdgePath({
  from,
  to,
  progress,
  drawAt,
  execAt,
  hoverStart,
  hoverEnd,
  lit,
}: {
  from: Placed;
  to: Placed;
  progress: MotionValue<number>;
  drawAt: number;
  execAt: number;
  hoverStart: number;
  hoverEnd: number;
  lit: boolean;
}) {
  const x1 = from.x + NODE_W;
  const y1 = from.y + from.h / 2;
  const x2 = to.x;
  const y2 = to.y + to.h / 2;
  const dx = (x2 - x1) / 2;
  const d = `M${x1} ${y1} C${x1 + dx} ${y1} ${x2 - dx} ${y2} ${x2} ${y2}`;

  const len = Math.hypot(x2 - x1, y2 - y1) + Math.abs(y2 - y1) + 40;
  const dash = useTransform(progress, [drawAt, drawAt + 0.06], [len, 0], {
    clamp: true,
  });
  const dimmed = (p: number) => p >= hoverStart && p <= hoverEnd && !lit;
  const stroke = useTransform(progress, (p) =>
    p >= execAt && !dimmed(p) ? token.graphEdgeActive : token.graphEdge,
  );
  const opacity = useTransform(progress, (p) =>
    p < drawAt ? 0 : dimmed(p) ? 0.15 : 1,
  );
  const dasharray = useTransform(progress, (p) =>
    p >= execAt && !dimmed(p) ? "none" : "6 5",
  );
  const marker = useTransform(progress, (p) =>
    p >= execAt && !dimmed(p) ? "url(#pg-arrow-on)" : "url(#pg-arrow)",
  );

  return (
    <motion.path
      d={d}
      fill="none"
      style={{
        stroke,
        opacity,
        strokeDasharray: dasharray,
        strokeDashoffset: dash,
        markerEnd: marker as unknown as string,
      }}
      strokeWidth={2}
      strokeLinecap="round"
    />
  );
}

function NodeCard({
  node,
  progress,
  appearAt,
  execAt,
  hoverStart,
  hoverEnd,
  inLineage,
}: {
  node: Placed;
  progress: MotionValue<number>;
  appearAt: number;
  execAt: number;
  hoverStart: number;
  hoverEnd: number;
  inLineage: boolean;
}) {
  const opacity = useTransform(progress, (p) => {
    const base =
      p < appearAt ? 0 : p < appearAt + 0.05 ? (p - appearAt) / 0.05 : 1;
    if (p >= hoverStart && p <= hoverEnd && !inLineage) return base * 0.25;
    return base;
  });
  const headerBg = useTransform(progress, (p) =>
    p >= execAt ? statusBg[node.status] : "transparent",
  );
  const dotOpacity = useTransform(progress, [execAt, execAt + 0.05], [0, 1], {
    clamp: true,
  });
  const expanded =
    node.kind === "root" ||
    node.kind === "fetch" ||
    node.kind === "introspection";

  return (
    <motion.div
      data-testid="plan-node"
      data-node-id={node.id}
      style={{
        position: "absolute",
        left: node.x,
        top: node.y,
        width: NODE_W,
        height: node.h,
        opacity,
        background: token.graphNode,
        border: `1px solid ${token.graphEdge}`,
        borderRadius: 8,
        overflow: "hidden",
        display: "flex",
        flexDirection: "column",
        boxShadow: "0 2px 8px rgba(1,4,9,0.4)",
      }}
    >
      <motion.div
        style={{
          flex: "0 0 auto",
          padding: "7px 10px",
          borderBottom: `1px solid ${token.graphEdge}`,
          background: headerBg,
        }}
      >
        <div style={{ display: "flex", alignItems: "center", gap: 6 }}>
          <motion.span
            style={{
              width: 7,
              height: 7,
              borderRadius: "50%",
              background: statusDot[node.status],
              opacity: dotOpacity,
              flex: "0 0 auto",
            }}
          />
          <span
            style={{
              fontSize: 12.5,
              fontWeight: 600,
              color: token.textStrong,
              whiteSpace: "nowrap",
              overflow: "hidden",
              textOverflow: "ellipsis",
            }}
          >
            {node.title}
          </span>
          {node.batch != null && (
            <span
              style={{
                marginLeft: "auto",
                fontSize: 9.5,
                padding: "1px 5px",
                borderRadius: 4,
                background: "rgba(88,166,255,0.18)",
                color: token.blue,
                whiteSpace: "nowrap",
              }}
            >
              Batch: {node.batch}
            </span>
          )}
        </div>
        <div
          style={{
            display: "flex",
            alignItems: "center",
            gap: 6,
            marginTop: 2,
          }}
        >
          {node.subtitle && (
            <span
              style={{
                fontSize: 10.5,
                fontFamily: token.mono,
                color: token.textSecondary,
              }}
            >
              {node.subtitle}
            </span>
          )}
          <motion.span
            style={{
              marginLeft: "auto",
              fontSize: 10.5,
              color: token.textSecondary,
              opacity: dotOpacity,
            }}
          >
            duration:{" "}
            <strong style={{ color: token.text }}>{node.durationMs}ms</strong>
          </motion.span>
        </div>
      </motion.div>

      {expanded && node.subOp && (
        <div
          style={{
            flex: 1,
            minHeight: 0,
            overflow: "hidden",
            padding: "2px 4px",
          }}
        >
          <CodeBlock
            code={node.subOp}
            lang="graphql"
            gutter={false}
            caret={false}
            fontSize={8.5}
            lineHeight={11}
            padding={2}
          />
        </div>
      )}
      {expanded && (
        <div
          style={{
            flex: "0 0 auto",
            display: "flex",
            gap: 12,
            padding: "5px 10px",
            borderTop: `1px solid ${token.graphEdge}`,
            fontSize: 9.5,
            color: token.blue,
          }}
        >
          <span>View Raw Data</span>
          <span>View Analytics</span>
        </div>
      )}
    </motion.div>
  );
}
