import type { CSSProperties } from "react";

import { nitro } from "@/src/components/home/act2/variants/nitroTheme";

/**
 * "Release safety" scene, variant 3 — the Nitro client-impact mini-list.
 *
 * A small, focused slice of the schema registry: before a change is published,
 * Nitro reconciles the proposed schema against the operations each registered
 * client actually sends. This widget shows that verdict as a compact list of
 * three named clients (Web, iOS, Partner), each with the count of its operations
 * that still validate and a tiny status bar reading the same fraction.
 *
 * STATIC render of the settled final frame: no motion package, no in-view hooks,
 * no "use client". Chrome is hand-authored markup; the Nitro palette is applied
 * via inline `style={{}}` with `nitro.*` values (intentional here; the rest of
 * the site uses cc-* tokens). Color is rationed as status: green when every
 * operation still validates, amber when some are at risk, neutral when queued.
 * Every SVG id is prefixed "guardrails-v3-".
 */

interface GuardrailsVariant3Props {
  readonly className?: string;
}

type ClientStatus = "ok" | "at-risk" | "queued";

interface ClientRow {
  readonly name: string;
  readonly status: ClientStatus;
  /** Operations that still validate against the proposed schema. */
  readonly ok: number;
  /** Total registered operations for this client. */
  readonly total: number;
}

const STATUS_COLOR: Record<ClientStatus, string> = {
  ok: nitro.successText,
  "at-risk": nitro.warning,
  queued: nitro.textDim,
};

const STATUS_LABEL: Record<ClientStatus, string> = {
  ok: "validated",
  "at-risk": "at risk",
  queued: "queued",
};

const MONO: CSSProperties = {
  fontFamily: nitro.mono,
  fontVariantNumeric: "tabular-nums",
};

const CLIENTS: readonly ClientRow[] = [
  { name: "Web", status: "ok", ok: 5, total: 5 },
  { name: "iOS", status: "at-risk", ok: 3, total: 5 },
  { name: "Partner", status: "queued", ok: 0, total: 4 },
];

/** Status glyph cloned by eye from Nitro's success / warning / pending icons. */
function StatusIcon({ status }: { readonly status: ClientStatus }) {
  const color = STATUS_COLOR[status];
  if (status === "ok") {
    return (
      <svg
        viewBox="0 0 16 16"
        width={13}
        height={13}
        aria-hidden="true"
        style={{ flex: "0 0 auto" }}
      >
        <circle cx="8" cy="8" r="6.5" fill={color} />
        <path
          d="M5.1 8.2 7.1 10.2 11 6"
          fill="none"
          stroke={nitro.bg}
          strokeWidth="1.6"
          strokeLinecap="round"
          strokeLinejoin="round"
        />
      </svg>
    );
  }
  if (status === "at-risk") {
    return (
      <svg
        viewBox="0 0 16 16"
        width={13}
        height={13}
        aria-hidden="true"
        style={{ flex: "0 0 auto" }}
      >
        <path d="M8 1.6 15 13.4H1L8 1.6Z" fill={color} />
        <rect x="7.2" y="5.4" width="1.6" height="4" rx="0.8" fill={nitro.bg} />
        <circle cx="8" cy="11" r="0.95" fill={nitro.bg} />
      </svg>
    );
  }
  return (
    <svg
      viewBox="0 0 16 16"
      width={13}
      height={13}
      aria-hidden="true"
      style={{ flex: "0 0 auto" }}
    >
      <circle
        cx="8"
        cy="8"
        r="6"
        fill="none"
        stroke={color}
        strokeWidth="1.4"
        strokeDasharray="2.2 2.2"
      />
    </svg>
  );
}

/** A thin track that fills to the share of operations still validating. */
function StatusBar({ row }: { readonly row: ClientRow }) {
  const fraction = row.total === 0 ? 0 : row.ok / row.total;
  return (
    <div
      style={{
        position: "relative",
        height: 5,
        width: "100%",
        background: nitro.surface,
        border: `1px solid ${nitro.border}`,
        borderRadius: 999,
        overflow: "hidden",
      }}
    >
      <div
        style={{
          position: "absolute",
          insetBlock: 0,
          insetInlineStart: 0,
          width: `${Math.round(fraction * 100)}%`,
          background: STATUS_COLOR[row.status],
          borderRadius: 999,
        }}
      />
    </div>
  );
}

/** One client row: status glyph + name, the n/total tally, and the status bar. */
function ClientItem({
  row,
  last,
}: {
  readonly row: ClientRow;
  readonly last: boolean;
}) {
  return (
    <li
      style={{
        padding: "10px 14px",
        borderBottom: last ? "none" : `1px solid ${nitro.border}`,
      }}
    >
      <div
        style={{
          display: "flex",
          alignItems: "center",
          gap: 8,
          marginBottom: 7,
        }}
      >
        <StatusIcon status={row.status} />
        <span
          style={{ fontSize: 12.5, color: nitro.textStrong, fontWeight: 500 }}
        >
          {row.name}
        </span>
        <span style={{ flex: 1 }} />
        <span style={{ ...MONO, fontSize: 11.5, color: nitro.textSecondary }}>
          {row.ok}/{row.total}
        </span>
        <span
          style={{
            fontSize: 10,
            color: STATUS_COLOR[row.status],
            whiteSpace: "nowrap",
          }}
        >
          {STATUS_LABEL[row.status]}
        </span>
      </div>
      <StatusBar row={row} />
    </li>
  );
}

export function GuardrailsVariant3({ className }: GuardrailsVariant3Props) {
  return (
    <div
      className={className}
      style={{
        margin: "0 auto",
        width: "100%",
        maxWidth: "20rem",
        userSelect: "none",
        fontFamily: nitro.font,
        background: nitro.bg,
        border: `1px solid ${nitro.border}`,
        borderRadius: nitro.radius,
        overflow: "hidden",
        color: nitro.text,
        boxShadow: "0 1px 3px rgba(2,6,16,0.6)",
      }}
    >
      {/* Title row: what this check is reconciling. */}
      <div
        style={{
          display: "flex",
          alignItems: "center",
          gap: 8,
          height: 36,
          padding: "0 14px",
          background: nitro.surface,
          borderBottom: `1px solid ${nitro.border}`,
        }}
      >
        <svg
          viewBox="0 0 16 16"
          width={13}
          height={13}
          aria-hidden="true"
          style={{ flex: "0 0 auto" }}
        >
          <path
            fill={nitro.icGraphql}
            d="M8 1.2 13.9 4.6v6.8L8 14.8 2.1 11.4V4.6L8 1.2Zm0 1.5L3.4 5.3v5.4L8 13.3l4.6-2.6V5.3L8 2.7Z"
          />
          <circle cx="8" cy="8" r="1.7" fill={nitro.icGraphql} />
        </svg>
        <span
          style={{
            fontSize: 12.5,
            fontWeight: 600,
            color: nitro.textStrong,
            whiteSpace: "nowrap",
          }}
        >
          Client impact
        </span>
        <span style={{ flex: 1 }} />
        <span style={{ ...MONO, fontSize: 11, color: nitro.textSecondary }}>
          checkout-v3
        </span>
      </div>

      {/* The three registered clients and how each fares against the change. */}
      <ul style={{ listStyle: "none", margin: 0, padding: 0 }}>
        {CLIENTS.map((row, i) => (
          <ClientItem
            key={row.name}
            row={row}
            last={i === CLIENTS.length - 1}
          />
        ))}
      </ul>
    </div>
  );
}
