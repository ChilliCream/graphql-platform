"use client";

import { motion } from "motion/react";

interface WorkflowsVariant2Props {
  readonly className?: string;
}

/**
 * v6 "Workflow" hook, variant 2: a linear saga progress track that cannot stall.
 *
 * Bespoke, one-off illustration (no shared v6 theme). The ReviewSaga reads left to
 * right as three pill nodes joined by directional arrows: `Draft` is done (solid
 * teal, checked), `Checked` is live (amber, soft glow), and `Published` is pending
 * (dashed outline, dim). A small `all paths terminal` badge sits in the header, and
 * the final arrow resolves into a teal terminal end-dot, so every run provably
 * lands on an end state. Below each node sits the real message it emits
 * (ReviewDrafted / ReviewChecked / ReviewPublished), and the footer states that the
 * run survives a restart and resumes at the live state.
 *
 * Sole looping accent: the amber glow behind the live `Checked` node breathes
 * gently. Every label, arrow, badge, and the terminal dot are fully legible at rest
 * with no layout shift.
 *
 * cc-* dark palette only; status colors encode real status (amber = the live
 * transition in flight). Inline SVG id prefix "v6-workflows-2-".
 */
const ID = "v6-workflows-2-";

const C = {
  page: "#0b0f1a",
  surface: "#0c1322",
  cardBorder: "rgba(245, 241, 234, 0.12)",
  heading: "#f5f0ea",
  inkDim: "rgba(245, 241, 234, 0.62)",
  navLabel: "#62748e",
  accent: "#5eead4",
  amber: "#fbbf24",
  MONO: 'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace',
} as const;

// Track geometry inside the 320 x 94 viewBox.
const PILL_W = 78;
const PILL_H = 40;
const PILL_Y = 24;
const ROW_Y = PILL_Y + PILL_H / 2; // vertical center of every node and arrow
const RESPONSE_Y = PILL_Y + PILL_H + 14;

type NodeStatus = "done" | "live" | "pending";

// Draft -> Checked -> Published. Draft is complete, the Checked -> Published
// transition is the one in flight, Published is the terminal state.
const NODES: readonly {
  readonly x: number;
  readonly label: string;
  readonly response: string;
  readonly status: NodeStatus;
}[] = [
  { x: 6, label: "Draft", response: "ReviewDrafted", status: "done" },
  { x: 100, label: "Checked", response: "ReviewChecked", status: "live" },
  {
    x: 194,
    label: "Published",
    response: "ReviewPublished",
    status: "pending",
  },
];

const LIVE_X = NODES[1].x;
const TERMINAL_X = 301;

/** Directional connectors between the pills, colored by progress. */
const ARROWS: readonly {
  readonly x1: number;
  readonly x2: number;
  readonly marker: string;
  readonly stroke: string;
  readonly dashed: boolean;
}[] = [
  {
    x1: 84,
    x2: 98,
    marker: `${ID}arrow-done`,
    stroke: C.accent,
    dashed: false,
  },
  {
    x1: 178,
    x2: 192,
    marker: `${ID}arrow-live`,
    stroke: C.amber,
    dashed: false,
  },
  {
    x1: 272,
    x2: 289,
    marker: `${ID}arrow-pending`,
    stroke: C.navLabel,
    dashed: true,
  },
];

function nodeStroke(status: NodeStatus) {
  if (status === "done") {
    return C.accent;
  }
  if (status === "live") {
    return C.amber;
  }
  return C.cardBorder;
}

function nodeFill(status: NodeStatus) {
  if (status === "done") {
    return "rgba(94, 234, 212, 0.10)";
  }
  if (status === "live") {
    return "rgba(251, 191, 36, 0.10)";
  }
  return "rgba(245, 241, 234, 0.02)";
}

export function WorkflowsVariant2({ className }: WorkflowsVariant2Props) {
  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        {/* header: saga name + the "can't get stuck" guarantee badge */}
        <div className="flex items-center justify-between gap-3">
          <span className="text-cc-heading font-mono text-sm font-semibold">
            ReviewSaga
          </span>
          <span className="border-cc-accent/50 text-cc-accent inline-flex shrink-0 items-center rounded-full border px-2 py-0.5 font-mono text-[0.55rem] tracking-[0.08em] whitespace-nowrap uppercase">
            all paths terminal
          </span>
        </div>

        {/* linear saga track: Draft (done) -> Checked (live) -> Published -> end */}
        <svg
          viewBox="0 0 320 94"
          width="100%"
          role="img"
          aria-label="ReviewSaga running left to right: Draft is done, the Checked state is live, Published is pending, and the final arrow resolves into a terminal end state"
          className="mt-4"
          style={{ display: "block", overflow: "visible", fontFamily: C.MONO }}
        >
          <defs>
            <filter
              id={`${ID}glow`}
              x="-60%"
              y="-60%"
              width="220%"
              height="220%"
            >
              <feGaussianBlur stdDeviation="7" />
            </filter>

            {ARROWS.map((arrow) => (
              <marker
                key={arrow.marker}
                id={arrow.marker}
                markerWidth="7"
                markerHeight="7"
                refX="5"
                refY="3.5"
                orient="auto"
                markerUnits="userSpaceOnUse"
              >
                <path
                  d="M0.5 0.5 L5 3.5 L0.5 6.5"
                  fill="none"
                  stroke={arrow.stroke}
                  strokeWidth="1"
                  strokeLinecap="round"
                  strokeLinejoin="round"
                />
              </marker>
            ))}
          </defs>

          {/* sole looping accent: the live node breathes a soft amber glow */}
          <motion.rect
            x={LIVE_X - 5}
            y={PILL_Y - 5}
            width={PILL_W + 10}
            height={PILL_H + 10}
            rx="13"
            fill={C.amber}
            filter={`url(#${ID}glow)`}
            initial={{ opacity: 0.42 }}
            animate={{ opacity: [0.42, 0.7, 0.42] }}
            transition={{ duration: 2.8, repeat: Infinity, ease: "easeInOut" }}
          />

          {/* directional connectors */}
          {ARROWS.map((arrow) => (
            <line
              key={arrow.marker}
              x1={arrow.x1}
              y1={ROW_Y}
              x2={arrow.x2}
              y2={ROW_Y}
              stroke={arrow.stroke}
              strokeWidth="1.25"
              strokeOpacity={arrow.dashed ? 0.7 : 1}
              strokeDasharray={arrow.dashed ? "4 3" : undefined}
              strokeLinecap="round"
              vectorEffect="non-scaling-stroke"
              markerEnd={`url(#${arrow.marker})`}
            />
          ))}

          {/* state pills */}
          {NODES.map((node) => {
            const stroke = nodeStroke(node.status);
            const isPending = node.status === "pending";
            return (
              <g key={node.label}>
                <rect
                  x={node.x}
                  y={PILL_Y}
                  width={PILL_W}
                  height={PILL_H}
                  rx="11"
                  fill={nodeFill(node.status)}
                  stroke={stroke}
                  strokeWidth="1.25"
                  strokeDasharray={isPending ? "4 3" : undefined}
                  vectorEffect="non-scaling-stroke"
                />

                {/* top-left status glyph: check / live dot / pending ring */}
                {node.status === "done" && (
                  <path
                    d={`M${node.x + 8} ${PILL_Y + 11} l2 2 l4.5 -5`}
                    fill="none"
                    stroke={C.accent}
                    strokeWidth="1.5"
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    vectorEffect="non-scaling-stroke"
                  />
                )}
                {node.status === "live" && (
                  <circle
                    cx={node.x + 12}
                    cy={PILL_Y + 11}
                    r="3"
                    fill={C.amber}
                  />
                )}
                {isPending && (
                  <circle
                    cx={node.x + 12}
                    cy={PILL_Y + 11}
                    r="3"
                    fill="none"
                    stroke={C.navLabel}
                    strokeWidth="1.2"
                    vectorEffect="non-scaling-stroke"
                  />
                )}

                <text
                  x={node.x + PILL_W / 2}
                  y={ROW_Y + 4}
                  textAnchor="middle"
                  fontSize="11.5"
                  fontWeight="500"
                  fill={isPending ? C.inkDim : C.heading}
                >
                  {node.label}
                </text>

                {/* the real message this state emits */}
                <text
                  x={node.x + PILL_W / 2}
                  y={RESPONSE_Y}
                  textAnchor="middle"
                  fontSize="8"
                  fill={C.navLabel}
                >
                  {node.response}
                </text>
              </g>
            );
          })}

          {/* terminal end state: every path resolves here */}
          <circle
            cx={TERMINAL_X}
            cy={ROW_Y}
            r="6"
            fill="none"
            stroke={C.accent}
            strokeWidth="1.25"
            vectorEffect="non-scaling-stroke"
          />
          <circle cx={TERMINAL_X} cy={ROW_Y} r="2.6" fill={C.accent} />
          <text
            x={TERMINAL_X}
            y={RESPONSE_Y}
            textAnchor="middle"
            fontSize="8"
            fill={C.navLabel}
          >
            End
          </text>
        </svg>

        {/* footer: the persistence promise, resumes after a restart */}
        <div className="border-cc-card-border mt-4 flex items-center gap-2 border-t pt-3">
          <svg
            viewBox="0 0 24 24"
            width="13"
            height="13"
            aria-hidden="true"
            fill="none"
            stroke={C.inkDim}
            strokeWidth="2"
            strokeLinecap="round"
            strokeLinejoin="round"
            style={{ flex: "0 0 auto" }}
          >
            <polyline points="23 4 23 10 17 10" />
            <path d="M20.49 15a9 9 0 1 1-2.12-9.36L23 10" />
          </svg>
          <span className="text-cc-ink-dim font-mono text-[0.62rem]">
            survives a restart, resumes at the live state
          </span>
        </div>
      </div>
    </div>
  );
}
