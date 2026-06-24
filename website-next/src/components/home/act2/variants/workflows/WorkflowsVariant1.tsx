import { nitro } from "@/src/components/home/act2/variants/nitroTheme";

interface WorkflowsVariant1Props {
  readonly className?: string;
}

/**
 * Workflow scene, variant 1 — a tiny Nitro Mocha message topology, cropped to
 * the single essential flow.
 *
 * Three node pills stack top to bottom on the visualizer's dot-grid canvas:
 * a published message (OrderPlaced), the consumer that handles it (ReserveStock),
 * and the reaction event it emits (StockReserved). The one in-flight edge into
 * the handler lights coral with a single travelling dot; the settled edge stays
 * grey.
 *
 * Static, no animation libraries; the settled final frame only. Uses the Nitro
 * palette via inline styles (deliberate for these illustrations); all svg ids
 * are prefixed "workflows-v1-".
 */

type NodeKind = "message" | "consumer";

interface TopoNode {
  readonly label: string;
  readonly kind: NodeKind;
  /** The edge entering this node is the live one. */
  readonly inFlight?: boolean;
}

// publish (OrderPlaced) -> handler (ReserveStock) -> react (StockReserved)
const NODES: readonly TopoNode[] = [
  { label: "OrderPlaced", kind: "message" },
  { label: "ReserveStock", kind: "consumer", inFlight: true },
  { label: "StockReserved", kind: "message" },
];

const ICON_COLOR: Record<NodeKind, string> = {
  message: nitro.warning,
  consumer: nitro.successText,
};

/** Codicon-style glyph: mail for a message, gear for a consumer. */
function NodeGlyph({ kind }: { readonly kind: NodeKind }) {
  const color = ICON_COLOR[kind];
  if (kind === "message") {
    return (
      <svg
        viewBox="0 0 14 14"
        width="13"
        height="13"
        aria-hidden="true"
        style={{ display: "block", flex: "0 0 auto" }}
      >
        <rect
          x="1.5"
          y="3"
          width="11"
          height="8"
          rx="1.4"
          fill="none"
          stroke={color}
          strokeWidth="1.2"
        />
        <path
          d="M2 3.8 7 7.6 12 3.8"
          fill="none"
          stroke={color}
          strokeWidth="1.2"
          strokeLinecap="round"
          strokeLinejoin="round"
        />
      </svg>
    );
  }
  return (
    <svg
      viewBox="0 0 14 14"
      width="13"
      height="13"
      aria-hidden="true"
      style={{ display: "block", flex: "0 0 auto" }}
    >
      <circle
        cx="7"
        cy="7"
        r="2.1"
        fill="none"
        stroke={color}
        strokeWidth="1.2"
      />
      <path
        d="M7 1.5v2.1M7 10.4v2.1M1.5 7h2.1M10.4 7h2.1M3.1 3.1l1.5 1.5M9.4 9.4l1.5 1.5M10.9 3.1 9.4 4.6M4.6 9.4 3.1 10.9"
        fill="none"
        stroke={color}
        strokeWidth="1.2"
        strokeLinecap="round"
      />
    </svg>
  );
}

/** Vertical connector between two pills; coral + dotted when in flight. */
function Edge({ active }: { readonly active: boolean }) {
  const color = active ? nitro.graphEdgeActive : nitro.graphEdge;
  return (
    <div
      style={{
        position: "relative",
        height: 22,
        display: "flex",
        justifyContent: "center",
      }}
    >
      <span
        aria-hidden="true"
        style={{
          width: 0,
          borderLeft: active ? `2px dashed ${color}` : `1.6px solid ${color}`,
        }}
      />
      {active && (
        <span
          aria-hidden="true"
          style={{
            position: "absolute",
            top: "50%",
            left: "50%",
            transform: "translate(-50%, -50%)",
            width: 7,
            height: 7,
            borderRadius: "50%",
            background: nitro.graphEdgeActive,
            border: `1.5px solid ${nitro.bg}`,
          }}
        />
      )}
    </div>
  );
}

/** One node pill. */
function NodeCard({ node }: { readonly node: TopoNode }) {
  return (
    <div
      style={{
        display: "flex",
        alignItems: "center",
        gap: 8,
        padding: "0 12px",
        height: 38,
        width: 150,
        margin: "0 auto",
        background: nitro.graphNode,
        border: `1px solid ${nitro.graphEdge}`,
        borderRadius: 6,
        boxShadow: "0 2px 8px rgba(0, 0, 0, 0.3)",
      }}
    >
      <NodeGlyph kind={node.kind} />
      <span
        style={{
          fontFamily: nitro.mono,
          fontWeight: 500,
          fontSize: 11.5,
          color: nitro.text,
          whiteSpace: "nowrap",
        }}
      >
        {node.label}
      </span>
    </div>
  );
}

export function WorkflowsVariant1({ className }: WorkflowsVariant1Props) {
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
        {/* thin title row: topology name + live state */}
        <div
          style={{
            display: "flex",
            alignItems: "center",
            gap: 8,
            padding: "9px 12px",
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
            Topology
          </span>
          <span
            style={{
              marginLeft: "auto",
              display: "inline-flex",
              alignItems: "center",
              gap: 5,
              fontFamily: nitro.mono,
              fontSize: 10,
              letterSpacing: "0.04em",
              color: nitro.graphEdgeActive,
            }}
          >
            <span
              aria-hidden="true"
              style={{
                width: 6,
                height: 6,
                borderRadius: "50%",
                background: nitro.graphEdgeActive,
                boxShadow: `0 0 0 3px ${nitro.graphEdgeActive}33`,
              }}
            />
            in flight
          </span>
        </div>

        {/* topology canvas (dot grid) */}
        <div
          style={{
            background: nitro.graphCanvas,
            backgroundImage: `radial-gradient(${nitro.graphDots} 1px, transparent 1px)`,
            backgroundSize: "20px 20px",
            padding: "20px 0",
          }}
        >
          {NODES.map((node, i) => (
            <div key={node.label}>
              {i > 0 && <Edge active={node.inFlight === true} />}
              <NodeCard node={node} />
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
