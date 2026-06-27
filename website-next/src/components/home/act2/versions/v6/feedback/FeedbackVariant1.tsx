"use client";

import type { CSSProperties, ReactNode } from "react";
import { motion } from "motion/react";

interface FeedbackVariant1Props {
  readonly className?: string;
}

/**
 * v6 "Agentic coding" hook, variant 1: the approval-gate terminal.
 *
 * Bespoke, one-off illustration (no shared v6 theme): a dark agent terminal
 * cropped to a single transcript whose one lit element is a governance gate row.
 * A destructive `createReview` call is flagged with a coral `destructiveHint`
 * tag, pauses on a violet `PENDING -> GRANTED` gate, and only then does one teal
 * `+ discountedPrice` safe-patch diff line land below it. Everything else stays a
 * dimmed mono transcript, so the eye goes straight to the gate.
 *
 * Sole looping accent: a violet spark crosses the gate from PENDING to GRANTED
 * and the GRANTED pill glows as it arrives. All text is full-opacity at rest, so
 * the first and resting frame is fully legible and there is no layout shift.
 *
 * cc-* dark palette only; status colors encode real status (coral destructive,
 * violet governance, teal safe patch). Inline SVG id prefix "v6-feedback-1-".
 */
const C = {
  termBg: "#0c1322",
  termHead: "#0e1626",
  border: "rgba(245,241,234,0.12)",
  heading: "#f5f0ea",
  dim: "rgba(245,241,234,0.62)",
  faint: "rgba(245,241,234,0.34)",
  ghost: "rgba(245,241,234,0.22)",
  eyebrow: "#62748e",
  accent: "#5eead4",
  coral: "#f0786a",
  coralBg: "rgba(240,120,106,0.13)",
  coralBorder: "rgba(240,120,106,0.36)",
  violet: "#8b8ff0",
  violetSoft: "#7c92c6",
  violetBg: "rgba(139,143,240,0.10)",
  violetBgStrong: "rgba(139,143,240,0.18)",
  violetBorder: "rgba(139,143,240,0.26)",
  violetBorderStrong: "rgba(139,143,240,0.46)",
  mono: 'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace',
} as const;

const TRANSCRIPT: CSSProperties = {
  fontFamily: C.mono,
  fontSize: 11.5,
  lineHeight: "19px",
  fontVariantNumeric: "tabular-nums",
};

export function FeedbackVariant1({ className }: FeedbackVariant1Props) {
  return (
    <div
      className={[
        "mx-auto w-full max-w-[330px] select-none",
        className ?? "",
      ].join(" ")}
    >
      <div
        style={{
          background: C.termBg,
          border: `1px solid ${C.border}`,
          borderRadius: 12,
          overflow: "hidden",
          boxShadow: "0 1px 3px rgba(2,6,16,0.6)",
        }}
      >
        {/* Title row: terminal traffic dots + agent CLI label. */}
        <div
          style={{
            display: "flex",
            alignItems: "center",
            gap: 9,
            padding: "8px 13px",
            background: C.termHead,
            borderBottom: `1px solid ${C.border}`,
          }}
        >
          <span style={{ display: "flex", gap: 6 }}>
            <Dot />
            <Dot />
            <Dot />
          </span>
          <span style={{ fontFamily: C.mono, fontSize: 11, color: C.eyebrow }}>
            agent &middot; /graphql/mcp
          </span>
        </div>

        {/* The transcript. Everything is dimmed except the gate, the coral tag,
            and the teal safe-patch line. */}
        <div style={{ ...TRANSCRIPT, color: C.faint, padding: "12px 14px" }}>
          {/* 1. The destructive agent call. */}
          <div style={{ marginBottom: 4 }}>
            <span style={{ color: C.faint }}>&rarr; </span>
            <span style={{ color: C.dim }}>createReview</span>
            <span style={{ color: C.faint }}>(schema: </span>
            <span style={{ color: C.faint }}>&quot;eshops/api&quot;</span>
            <span style={{ color: C.faint }}>)</span>
          </div>

          {/* 1b. Coral destructiveHint tag attached to the call above the gate. */}
          <div style={{ marginBottom: 9, paddingLeft: 14 }}>
            <span
              style={{
                display: "inline-flex",
                alignItems: "center",
                gap: 5,
                padding: "1px 7px",
                fontSize: 9.5,
                letterSpacing: "0.03em",
                borderRadius: 5,
                color: C.coral,
                background: C.coralBg,
                border: `1px solid ${C.coralBorder}`,
              }}
            >
              <WarnGlyph />
              destructiveHint
            </span>
          </div>

          {/* 2. Staged review id, dimmed. */}
          <div style={{ marginBottom: 9 }}>
            <span style={{ color: C.faint }}>&larr; review </span>
            <span style={{ color: C.ghost }}>rev_8f2c</span>
            <span style={{ color: C.faint }}> staged</span>
          </div>

          {/* 3. The lit governance gate row. */}
          <div
            style={{
              display: "flex",
              alignItems: "center",
              justifyContent: "space-between",
              gap: 8,
              padding: "6px 9px 6px 8px",
              marginBottom: 10,
              borderRadius: 7,
              background: C.violetBg,
              borderLeft: `2px solid ${C.violet}`,
              border: `1px solid ${C.violetBorder}`,
            }}
          >
            <span
              style={{
                display: "inline-flex",
                alignItems: "center",
                gap: 6,
                color: C.violet,
                fontSize: 9,
                letterSpacing: "0.12em",
                textTransform: "uppercase",
                whiteSpace: "nowrap",
              }}
            >
              <LockGlyph />
              approval gate
            </span>

            <span
              style={{ display: "inline-flex", alignItems: "center", gap: 2 }}
            >
              <GatePill tone="pending">PENDING</GatePill>
              <Spark />
              <GrantedPill />
            </span>
          </div>

          {/* 4. The teal safe-patch diff line, landing below the gate. */}
          <div style={{ marginBottom: 5 }}>
            <span style={{ color: C.accent }}>+ </span>
            <span style={{ color: C.accent }}>discountedPrice</span>
            <span style={{ color: C.faint }}>: </span>
            <span style={{ color: C.heading }}>Money</span>
          </div>

          {/* 5. Result, dimmed. */}
          <div>
            <span style={{ color: "rgba(94,234,212,0.55)" }}>&#10003; </span>
            <span style={{ color: C.dim }}>1 field added after approval</span>
          </div>
        </div>
      </div>
    </div>
  );
}

/** Static PENDING pill (and the neutral base style shared by the gate pills). */
function GatePill({
  children,
  tone,
}: {
  readonly children: ReactNode;
  readonly tone: "pending";
}) {
  return (
    <span
      style={{
        display: "inline-block",
        padding: "1px 7px",
        fontSize: 9,
        letterSpacing: "0.06em",
        borderRadius: 5,
        color: tone === "pending" ? C.violetSoft : C.violet,
        background: C.violetBg,
        border: `1px solid ${C.violetBorder}`,
        whiteSpace: "nowrap",
      }}
    >
      {children}
    </span>
  );
}

/** GRANTED pill with a sole looping violet glow synced to the gate spark. */
function GrantedPill() {
  return (
    <motion.span
      style={{
        display: "inline-flex",
        alignItems: "center",
        gap: 4,
        padding: "1px 7px",
        fontSize: 9,
        letterSpacing: "0.06em",
        borderRadius: 5,
        color: C.violet,
        background: C.violetBgStrong,
        border: `1px solid ${C.violetBorderStrong}`,
        whiteSpace: "nowrap",
      }}
      initial={{ boxShadow: "0 0 0px rgba(139,143,240,0)" }}
      animate={{
        boxShadow: [
          "0 0 0px rgba(139,143,240,0)",
          "0 0 0px rgba(139,143,240,0)",
          "0 0 9px rgba(139,143,240,0.5)",
          "0 0 0px rgba(139,143,240,0)",
        ],
      }}
      transition={{
        duration: 2.4,
        times: [0, 0.55, 0.78, 1],
        repeat: Infinity,
        repeatDelay: 0.5,
        ease: "easeInOut",
      }}
    >
      <CheckGlyph />
      GRANTED
    </motion.span>
  );
}

/** The gate connector: a dim violet arrow with a single spark crossing it. */
function Spark() {
  return (
    <span
      aria-hidden="true"
      style={{
        position: "relative",
        display: "inline-flex",
        alignItems: "center",
        justifyContent: "center",
        width: 22,
        height: 12,
        margin: "0 1px",
      }}
    >
      <span
        style={{
          fontFamily: C.mono,
          fontSize: 11,
          color: "rgba(139,143,240,0.45)",
        }}
      >
        &rarr;
      </span>
      <motion.span
        style={{
          position: "absolute",
          left: 1,
          top: "50%",
          width: 4,
          height: 4,
          marginTop: -2,
          borderRadius: "50%",
          background: C.violet,
        }}
        initial={{ x: 0, opacity: 0 }}
        animate={{ x: [0, 16], opacity: [0, 1, 1, 0] }}
        transition={{
          duration: 2.4,
          times: [0, 0.2, 0.72, 1],
          repeat: Infinity,
          repeatDelay: 0.5,
          ease: "easeInOut",
        }}
      />
    </span>
  );
}

/** Small violet check inside the GRANTED pill. */
function CheckGlyph() {
  return (
    <svg width="9" height="9" viewBox="0 0 12 12" aria-hidden="true">
      <path
        d="M2.5 6.4 L5 8.6 L9.6 3.4"
        fill="none"
        stroke={C.violet}
        strokeWidth="1.6"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}

/** Violet padlock marking the governance gate. */
function LockGlyph() {
  return (
    <svg width="11" height="11" viewBox="0 0 24 24" aria-hidden="true">
      <rect
        x="4.5"
        y="10.5"
        width="15"
        height="10"
        rx="2.2"
        fill="none"
        stroke={C.violet}
        strokeWidth="1.6"
      />
      <path
        d="M8 10.5 V7.6 a4 4 0 0 1 8 0 V10.5"
        fill="none"
        stroke={C.violet}
        strokeWidth="1.6"
        strokeLinecap="round"
      />
    </svg>
  );
}

/** Coral warning triangle for the destructiveHint tag. */
function WarnGlyph() {
  return (
    <svg width="9" height="9" viewBox="0 0 12 12" aria-hidden="true">
      <path
        d="M6 1.4 L11 10.4 H1 Z"
        fill="none"
        stroke={C.coral}
        strokeWidth="1.2"
        strokeLinejoin="round"
      />
      <path
        d="M6 4.8 V7.2"
        stroke={C.coral}
        strokeWidth="1.2"
        strokeLinecap="round"
      />
      <circle cx="6" cy="8.9" r="0.7" fill={C.coral} />
    </svg>
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
        background: C.border,
        display: "inline-block",
      }}
    />
  );
}
