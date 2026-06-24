import type { CSSProperties } from "react";

import { nitro } from "@/src/components/home/act2/variants/nitroTheme";

interface WorkflowsVariant4Props {
  readonly className?: string;
}

/**
 * Workflow scene, variant 4 — "Transport chips + publish line" as a small,
 * focused slice of the Nitro / Banana Cake Pop Mocha visualizer.
 *
 * A single compact card: one mono `bus.PublishAsync(...)` send rendered in
 * GitHub-dark editor tokens, above a row of five interchangeable transport
 * chips (RabbitMQ / Postgres / In-process / Kafka / Azure SB). RabbitMQ is the
 * selected transport this frame, lit orange with a filled dot. The same publish
 * runs over whichever chip is selected, so the caption reads "swap transport,
 * same publish".
 *
 * Static, no animation libraries; the settled final frame only. Uses the Nitro
 * palette via inline styles (deliberate for these illustrations); all svg ids
 * are prefixed "workflows-v4-".
 */

type SynTok = readonly [text: string, color: string];

const SYN = {
  keyword: nitro.synKeyword,
  type: nitro.synType,
  member: nitro.synField,
  punct: nitro.synPunct,
} as const;

// The darker canvas surface Mocha uses for its code/header tint.
const HEADER_BG = "#0d1117";

const PUBLISH_LINE: readonly SynTok[] = [
  ["await ", SYN.keyword],
  ["bus", SYN.punct],
  [".", SYN.punct],
  ["PublishAsync", SYN.member],
  ["(", SYN.punct],
  ["new ", SYN.keyword],
  ["ReviewPublished", SYN.type],
  ["(id)", SYN.punct],
  [");", SYN.punct],
];

interface TransportChip {
  readonly id: string;
  /** Display name as it appears in the transport picker. */
  readonly name: string;
  /** The selected transport carrying this publish. */
  readonly selected?: boolean;
}

const CHIPS: readonly TransportChip[] = [
  { id: "rabbitmq", name: "RabbitMQ", selected: true },
  { id: "postgres", name: "Postgres" },
  { id: "in-process", name: "In-process" },
  { id: "kafka", name: "Kafka" },
  { id: "azure-sb", name: "Azure SB" },
];

const CHIP_BASE: CSSProperties = {
  display: "inline-flex",
  alignItems: "center",
  gap: 5,
  padding: "3px 8px",
  borderRadius: 999,
  fontFamily: nitro.mono,
  fontSize: 11,
  whiteSpace: "nowrap",
};

/** One transport chip. The selected one is tinted + lit orange. */
function Chip({ chip }: { readonly chip: TransportChip }) {
  const active = chip.selected === true;
  return (
    <span
      style={{
        ...CHIP_BASE,
        background: active ? `${nitro.warning}1f` : nitro.surface,
        border: `1px solid ${active ? nitro.graphEdgeActive : nitro.border}`,
        color: active ? nitro.textStrong : nitro.textSecondary,
        fontWeight: active ? 600 : 400,
      }}
    >
      <span
        aria-hidden="true"
        style={{
          width: 6,
          height: 6,
          borderRadius: "50%",
          flex: "0 0 auto",
          background: active ? nitro.graphEdgeActive : "transparent",
          border: active ? "none" : `1.2px solid ${nitro.borderStrong}`,
        }}
      />
      {chip.name}
    </span>
  );
}

export function WorkflowsVariant4({ className }: WorkflowsVariant4Props) {
  return (
    <div
      className={className}
      style={{
        margin: "0 auto",
        width: "100%",
        maxWidth: "21rem",
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
        {/* Thin title row: section label + selected transport. */}
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
            Mocha.Publish
          </span>
          <span
            style={{
              marginLeft: "auto",
              fontFamily: nitro.mono,
              fontSize: 10,
              color: nitro.graphEdgeActive,
            }}
          >
            RabbitMQ
          </span>
        </div>

        <div style={{ padding: 12 }}>
          {/* The canonical publish line, in editor tokens. */}
          <div
            style={{
              border: `1px solid ${nitro.border}`,
              borderRadius: 5,
              background: HEADER_BG,
              padding: "8px 10px",
              fontFamily: nitro.mono,
              fontSize: 11,
              lineHeight: "18px",
              whiteSpace: "pre",
              overflowX: "auto",
            }}
          >
            {PUBLISH_LINE.map((t, i) => (
              <span key={i} style={{ color: t[1] }}>
                {t[0]}
              </span>
            ))}
          </div>

          {/* Five interchangeable transport chips, RabbitMQ selected. */}
          <div
            style={{
              display: "flex",
              flexWrap: "wrap",
              gap: 6,
              marginTop: 12,
            }}
          >
            {CHIPS.map((chip) => (
              <Chip key={chip.id} chip={chip} />
            ))}
          </div>

          {/* Caption: the publish is the same across transports. */}
          <div
            style={{
              marginTop: 12,
              fontFamily: nitro.mono,
              fontSize: 10,
              letterSpacing: "0.04em",
              color: nitro.textDim,
            }}
          >
            swap transport, same publish
          </div>
        </div>
      </div>
    </div>
  );
}
