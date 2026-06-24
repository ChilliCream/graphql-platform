import type { CSSProperties } from "react";

import { nitro } from "@/src/components/home/act2/variants/nitroTheme";

interface WorkflowsVariant5Props {
  readonly className?: string;
}

/**
 * Workflow scene, variant 5 — a compact outbox / inbox mini-pair as an
 * authentic cropped Nitro telemetry widget.
 *
 * Two tiny stacks sit on a single Mocha.Messaging card: a 2-row OUTBOX on the
 * left (events committed in the same write) and a 2-row INBOX on the right
 * (events processed at most once). One message is mid-transit: its row spans the
 * gap between the stacks, carrying a single MessageId across the connector. That
 * MessageId is the inbox dedup key, so the footer reads "exactly-once processing".
 *
 * Static, no animation libraries; the settled final frame only. Uses the Nitro
 * palette via inline styles (deliberate for these illustrations). All svg ids
 * are prefixed "workflows-v5-".
 */

interface StackRow {
  /** Short MessageId (Guid prefix), the shared dedup key. */
  readonly id: string;
  /** Domain event type carried by the message. */
  readonly type: string;
  /** Settled status text shown at the row tail. */
  readonly status: string;
}

/** Transactional outbox: events committed alongside the write. */
const OUTBOX: readonly StackRow[] = [
  { id: "91e0bd", type: "PaymentCaptured", status: "dispatched" },
  { id: "7c33a8", type: "ReviewPublished", status: "dispatched" },
];

/** Idempotent inbox: each MessageId processed at most once. */
const INBOX: readonly StackRow[] = [
  { id: "62b7f4", type: "BasketCheckedOut", status: "processed" },
  { id: "5d0a19", type: "StockReserved", status: "processed" },
];

/** The single message in flight, carrying its MessageId across the gap. */
const IN_FLIGHT = { id: "a4f1c2", type: "OrderPlaced" } as const;

const STACK_TITLE: CSSProperties = {
  display: "flex",
  alignItems: "center",
  gap: 5,
  marginBottom: 6,
  fontFamily: nitro.mono,
  fontSize: 9,
  fontWeight: 600,
  letterSpacing: "0.07em",
  textTransform: "uppercase",
  color: nitro.textSecondary,
};

const ROW_ID: CSSProperties = {
  fontFamily: nitro.mono,
  fontSize: 10,
  letterSpacing: "0.02em",
  color: nitro.textSecondary,
};

const ROW_TYPE: CSSProperties = {
  fontFamily: nitro.mono,
  fontSize: 9,
  color: nitro.textDim,
  whiteSpace: "nowrap",
  overflow: "hidden",
  textOverflow: "ellipsis",
};

/** One settled ledger row in a stack. */
function StackRowCard({ row }: { readonly row: StackRow }) {
  return (
    <div
      style={{
        display: "flex",
        flexDirection: "column",
        gap: 1,
        padding: "5px 8px",
        borderRadius: 4,
        border: `1px solid ${nitro.border}`,
        background: nitro.card,
      }}
    >
      <span style={ROW_ID}>{row.id}</span>
      <span style={ROW_TYPE}>
        {row.type} · {row.status}
      </span>
    </div>
  );
}

/** Outbox or inbox stack: a small title plus its two settled rows. */
function Stack({
  title,
  dot,
  rows,
}: {
  readonly title: string;
  readonly dot: string;
  readonly rows: readonly StackRow[];
}) {
  return (
    <div style={{ display: "flex", flexDirection: "column", gap: 6 }}>
      <span style={STACK_TITLE}>
        <span
          aria-hidden="true"
          style={{ width: 6, height: 6, borderRadius: 2, background: dot }}
        />
        {title}
      </span>
      {rows.map((row) => (
        <StackRowCard key={row.id} row={row} />
      ))}
    </div>
  );
}

export function WorkflowsVariant5({ className }: WorkflowsVariant5Props) {
  return (
    <div
      className={className}
      style={{
        margin: "0 auto",
        width: "100%",
        maxWidth: "20rem",
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
        {/* Thin title row. */}
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
            Mocha.Messaging
          </span>
        </div>

        {/* Body: the two stacks with the in-flight row spanning the gap. */}
        <div style={{ padding: "12px 12px 14px" }}>
          <div
            style={{
              display: "grid",
              gridTemplateColumns: "1fr 1fr",
              columnGap: 14,
            }}
          >
            <Stack title="Outbox" dot={nitro.warning} rows={OUTBOX} />
            <Stack title="Inbox" dot={nitro.successText} rows={INBOX} />
          </div>

          {/* The single in-flight message: one row spanning both stacks, the
              shared MessageId crossing the gap. */}
          <div
            style={{
              marginTop: 12,
              display: "grid",
              gridTemplateColumns: "1fr auto 1fr",
              alignItems: "stretch",
            }}
          >
            {/* Outbox side: committed (amber). */}
            <div
              style={{
                display: "flex",
                flexDirection: "column",
                gap: 1,
                padding: "5px 8px",
                borderRadius: "4px 0 0 4px",
                border: `1px solid ${nitro.warning}66`,
                borderRight: "none",
                background: `${nitro.warning}14`,
              }}
            >
              <span
                style={{
                  fontFamily: nitro.mono,
                  fontSize: 10,
                  fontWeight: 600,
                  color: nitro.warning,
                }}
              >
                {IN_FLIGHT.id}
              </span>
              <span
                style={{
                  fontFamily: nitro.mono,
                  fontSize: 9,
                  color: nitro.text,
                }}
              >
                {IN_FLIGHT.type} · committed
              </span>
            </div>

            {/* Connector: the shared MessageId crossing the gap. */}
            <div
              style={{
                display: "flex",
                alignItems: "center",
                gap: 4,
                padding: "0 8px",
                borderTop: `1px dashed ${nitro.borderStrong}`,
                borderBottom: `1px dashed ${nitro.borderStrong}`,
                background: nitro.surface,
              }}
            >
              <span
                style={{
                  fontFamily: nitro.mono,
                  fontSize: 7.5,
                  letterSpacing: "0.05em",
                  textTransform: "uppercase",
                  color: nitro.textDim,
                  whiteSpace: "nowrap",
                }}
              >
                MessageId
              </span>
              <span style={{ color: nitro.successText, fontSize: 11 }}>
                {"→"}
              </span>
            </div>

            {/* Inbox side: same MessageId, deduplicated (teal). */}
            <div
              style={{
                display: "flex",
                flexDirection: "column",
                gap: 1,
                padding: "5px 8px",
                borderRadius: "0 4px 4px 0",
                border: `1px solid ${nitro.successText}66`,
                borderLeft: "none",
                background: `${nitro.successText}12`,
              }}
            >
              <span
                style={{
                  fontFamily: nitro.mono,
                  fontSize: 10,
                  fontWeight: 600,
                  color: nitro.successText,
                }}
              >
                {IN_FLIGHT.id}
              </span>
              <span
                style={{
                  fontFamily: nitro.mono,
                  fontSize: 9,
                  color: nitro.text,
                }}
              >
                {IN_FLIGHT.type} · dedup
              </span>
            </div>
          </div>
        </div>

        {/* Footer: the honest guarantee. */}
        <div
          style={{
            display: "flex",
            alignItems: "center",
            gap: 8,
            padding: "7px 12px",
            borderTop: `1px solid ${nitro.border}`,
            background: nitro.surface,
            fontFamily: nitro.mono,
            fontSize: 10,
          }}
        >
          <span
            aria-hidden="true"
            style={{
              width: 5,
              height: 5,
              borderRadius: "50%",
              background: nitro.successText,
            }}
          />
          <span style={{ color: nitro.textSecondary, letterSpacing: "0.03em" }}>
            exactly-once processing
          </span>
        </div>
      </div>
    </div>
  );
}
