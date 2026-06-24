import type { CSSProperties } from "react";

import { nitro } from "@/src/components/home/act2/variants/nitroTheme";

interface WorkflowsVariant2Props {
  readonly className?: string;
}

/**
 * Workflow scene, variant 2 — a small Mocha saga state-machine widget.
 *
 * The ReviewSaga drawn top-to-bottom on a dot-grid canvas as three state nodes,
 * Draft -> Checked -> Published. The transition into Published is in progress, so
 * its connector lights coral, carries a live event chip (ChecksPassed), and ends
 * at the End state; the already-traversed Draft -> Checked edge stays grey.
 *
 * One thin title row, a single state machine, a "validated before traffic"
 * caption. Static, no animation libraries; the settled final frame only. Uses the
 * Nitro palette via inline styles (deliberate for these illustrations); all svg
 * ids are prefixed "workflows-v2-".
 */

interface SagaNode {
  readonly label: string;
  readonly response: string;
  readonly badge?: "Start" | "End";
  /** The transition entering this state is the live one. */
  readonly event?: string;
}

// Draft (Start) -> Checked -> Published (End). The transition into Published is
// the live one, labeled with its event.
const NODES: readonly SagaNode[] = [
  { label: "Draft", response: "ReviewDrafted", badge: "Start" },
  { label: "Checked", response: "ReviewChecked", event: "SubmitForCheck" },
  {
    label: "Published",
    response: "ReviewPublished",
    badge: "End",
    event: "ChecksPassed",
  },
];

const BADGE_BASE: CSSProperties = {
  fontFamily: nitro.font,
  fontSize: 8,
  lineHeight: 1.3,
  letterSpacing: "0.06em",
  textTransform: "uppercase",
  fontWeight: 600,
  padding: "1px 5px",
  borderRadius: 3,
  color: "#ffffff",
};

/** Settled grey checkmark for the title row's validated badge. */
function CheckGlyph() {
  return (
    <svg
      viewBox="0 0 12 12"
      width="11"
      height="11"
      aria-hidden="true"
      style={{ display: "block", flex: "0 0 auto" }}
    >
      <path
        d="M2.5 6.4 5 8.9l4.5-5"
        fill="none"
        stroke={nitro.successText}
        strokeWidth="1.4"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}

/** Connector between two states, with the transition event chip. */
function Transition({
  event,
  last,
}: {
  readonly event: string;
  readonly last: boolean;
}) {
  return (
    <div
      style={{
        position: "relative",
        height: 30,
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
      }}
    >
      <span
        aria-hidden="true"
        style={{
          position: "absolute",
          top: 0,
          bottom: 0,
          left: "50%",
          width: 0,
          borderLeft: last
            ? `2px dashed ${nitro.graphEdgeActive}`
            : `2px solid ${nitro.graphEdge}`,
        }}
      />
      <span
        style={{
          display: "inline-flex",
          alignItems: "center",
          gap: 5,
          background: nitro.bg,
          border: `1px solid ${last ? `${nitro.graphEdgeActive}80` : nitro.border}`,
          borderRadius: 4,
          padding: "2px 7px",
          boxShadow: "0 1px 4px rgba(0, 0, 0, 0.3)",
          whiteSpace: "nowrap",
          zIndex: 1,
        }}
      >
        {last && (
          <span
            aria-hidden="true"
            style={{
              width: 6,
              height: 6,
              borderRadius: "50%",
              background: nitro.graphEdgeActive,
              boxShadow: `0 0 0 3px ${nitro.graphEdgeActive}33`,
              flex: "0 0 auto",
            }}
          />
        )}
        <span style={{ fontSize: 9, fontWeight: 500, color: nitro.text }}>
          {event}
        </span>
      </span>
    </div>
  );
}

/** A single state node card. */
function StateNodeCard({ node }: { readonly node: SagaNode }) {
  const accent =
    node.badge === "Start"
      ? nitro.info
      : node.badge === "End"
        ? nitro.success
        : nitro.graphEdge;
  const background =
    node.badge === "End"
      ? nitro.graphNodeSuccess
      : node.badge === "Start"
        ? "rgba(9, 105, 218, 0.08)"
        : nitro.graphNode;

  return (
    <div
      style={{
        boxSizing: "border-box",
        width: 160,
        margin: "0 auto",
        display: "flex",
        flexDirection: "column",
        gap: 3,
        padding: "8px 11px",
        background,
        border: `1px solid ${nitro.graphEdge}`,
        borderLeft: `3px solid ${accent}`,
        borderRadius: 6,
      }}
    >
      <div style={{ display: "flex", alignItems: "center", gap: 6 }}>
        <span
          style={{ fontWeight: 500, fontSize: 12, color: nitro.textStrong }}
        >
          {node.label}
        </span>
        {node.badge && (
          <span style={{ ...BADGE_BASE, background: accent }}>
            {node.badge}
          </span>
        )}
      </div>
      <span
        style={{
          fontSize: 9,
          fontFamily: nitro.mono,
          color: nitro.textSecondary,
          whiteSpace: "nowrap",
        }}
      >
        {"→"} {node.response}
      </span>
    </div>
  );
}

export function WorkflowsVariant2({ className }: WorkflowsVariant2Props) {
  return (
    <div
      className={className}
      style={{
        margin: "0 auto",
        width: "100%",
        maxWidth: "18rem",
        userSelect: "none",
        fontFamily: nitro.font,
      }}
    >
      <div
        style={{
          borderRadius: nitro.radius,
          border: `1px solid ${nitro.border}`,
          background: nitro.bg,
          boxShadow: "0 1px 3px rgba(2, 6, 16, 0.6)",
          overflow: "hidden",
        }}
      >
        {/* thin title row: saga name + validated badge */}
        <div
          style={{
            display: "flex",
            alignItems: "center",
            gap: 8,
            padding: "8px 12px",
            borderBottom: `1px solid ${nitro.border}`,
            background: nitro.surface,
          }}
        >
          <span
            style={{
              fontFamily: nitro.mono,
              fontSize: 11,
              fontWeight: 600,
              color: nitro.textStrong,
            }}
          >
            ReviewSaga
          </span>
          <span
            style={{
              marginLeft: "auto",
              display: "inline-flex",
              alignItems: "center",
              gap: 4,
              fontFamily: nitro.mono,
              fontSize: 10,
              letterSpacing: "0.04em",
              color: nitro.successText,
            }}
          >
            <CheckGlyph />
            validated
          </span>
        </div>

        {/* state-machine canvas (dot grid) */}
        <div
          style={{
            background: nitro.graphCanvas,
            backgroundImage: `radial-gradient(${nitro.graphDots} 1px, transparent 1px)`,
            backgroundSize: "18px 18px",
            padding: "14px 0",
          }}
        >
          {NODES.map((node, i) => (
            <div key={node.label}>
              {node.event && (
                <Transition event={node.event} last={i === NODES.length - 1} />
              )}
              <StateNodeCard node={node} />
            </div>
          ))}
        </div>

        {/* caption */}
        <div
          style={{
            padding: "8px 12px",
            borderTop: `1px solid ${nitro.border}`,
            background: nitro.surface,
            fontFamily: nitro.mono,
            fontSize: 10.5,
            color: nitro.textDim,
            letterSpacing: "0.04em",
          }}
        >
          validated before traffic
        </div>
      </div>
    </div>
  );
}
