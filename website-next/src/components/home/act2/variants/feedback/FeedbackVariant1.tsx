import type { CSSProperties, ReactNode } from "react";

import { nitro } from "@/src/components/home/act2/variants/nitroTheme";

interface FeedbackVariant1Props {
  readonly className?: string;
}

const MONO: CSSProperties = {
  fontFamily: nitro.mono,
  fontVariantNumeric: "tabular-nums",
};

/**
 * Agentic-coding scene (variant 1, Agent console + approval gate): a compact
 * GitHub-dark terminal cropped to a single coding-agent transcript.
 *
 * Five lines settled to their final frame: the agent invokes the `createReview`
 * MCP tool, the gateway returns a schema id, an approval line resolves from
 * PENDING to GRANTED, and one safe-patch `+`diff line is printed. No chrome
 * beyond the terminal traffic dots and a single thin title row. Fully static,
 * no animation, no hooks.
 */
export function FeedbackVariant1({ className }: FeedbackVariant1Props) {
  return (
    <div
      className={["mx-auto w-full max-w-sm select-none", className ?? ""].join(
        " ",
      )}
      style={{ fontFamily: nitro.font }}
    >
      <div
        style={{
          background: nitro.bg,
          border: `1px solid ${nitro.border}`,
          borderRadius: nitro.radius,
          overflow: "hidden",
          boxShadow: "0 1px 3px rgba(2, 6, 16, 0.6)",
        }}
      >
        {/* Single thin title row: terminal traffic dots + agent CLI label. */}
        <div
          style={{
            display: "flex",
            alignItems: "center",
            gap: 9,
            padding: "8px 12px",
            background: nitro.surface,
            borderBottom: `1px solid ${nitro.border}`,
          }}
        >
          <span style={{ display: "flex", gap: 6 }}>
            <Dot />
            <Dot />
            <Dot />
          </span>
          <span
            style={{
              ...MONO,
              fontSize: 11,
              color: nitro.textSecondary,
            }}
          >
            agent · /graphql/mcp
          </span>
        </div>

        {/* The five-line agent transcript. */}
        <div
          style={{
            ...MONO,
            fontSize: 11.5,
            lineHeight: "20px",
            color: nitro.text,
            padding: "11px 13px",
            whiteSpace: "pre-wrap",
          }}
        >
          {/* 1. Agent invokes the createReview MCP tool. */}
          <Line>
            <span style={{ color: nitro.synComment }}>→ tool</span>{" "}
            <span style={{ color: nitro.synName }}>createReview</span>
            <span style={{ color: nitro.synPunct }}>(</span>
            <span style={{ color: nitro.synField }}>schema</span>
            <span style={{ color: nitro.synPunct }}>:</span>{" "}
            <span style={{ color: nitro.synString }}>
              &quot;eshops/api&quot;
            </span>
            <span style={{ color: nitro.synPunct }}>)</span>
          </Line>

          {/* 2. Gateway returns the staged review id. */}
          <Line>
            <span style={{ color: nitro.textDim }}>← review </span>
            <span style={{ color: nitro.synString }}>rev_8f2c</span>{" "}
            <span style={{ color: nitro.textDim }}>staged</span>
          </Line>

          {/* 3. Approval gate, settled from PENDING to GRANTED. */}
          <Line>
            <span style={{ color: nitro.textDim }}>approval </span>
            <Pill bg="rgba(110,118,129,0.18)" fg={nitro.textDim}>
              PENDING
            </Pill>
            <span style={{ color: nitro.textDim }}> → </span>
            <Pill bg="rgba(63,208,127,0.16)" fg={nitro.successText}>
              GRANTED
            </Pill>
          </Line>

          {/* 4. One safe-patch diff line. */}
          <Line>
            <span style={{ color: nitro.successText }}>+ </span>
            <span style={{ color: nitro.synField }}>discountedPrice</span>
            <span style={{ color: nitro.synPunct }}>:</span>{" "}
            <span style={{ color: nitro.synType }}>Money</span>
          </Line>

          {/* 5. Result summary. */}
          <Line last>
            <span style={{ color: nitro.successText }}>✓</span>{" "}
            <span style={{ color: nitro.textSecondary }}>
              1 field added, validated before traffic
            </span>
          </Line>
        </div>
      </div>
    </div>
  );
}

/** One transcript row with consistent spacing. */
function Line({
  children,
  last,
}: {
  readonly children: ReactNode;
  readonly last?: boolean;
}) {
  return <div style={{ marginBottom: last ? 0 : 5 }}>{children}</div>;
}

/** Small status pill used for the approval states. */
function Pill({
  children,
  bg,
  fg,
}: {
  readonly children: ReactNode;
  readonly bg: string;
  readonly fg: string;
}) {
  return (
    <span
      style={{
        display: "inline-block",
        padding: "0 6px",
        fontSize: 10,
        letterSpacing: "0.06em",
        borderRadius: 4,
        background: bg,
        color: fg,
      }}
    >
      {children}
    </span>
  );
}

/** Terminal traffic dot. */
function Dot() {
  return (
    <span
      style={{
        width: 9,
        height: 9,
        borderRadius: "50%",
        background: nitro.border,
        display: "inline-block",
      }}
    />
  );
}
