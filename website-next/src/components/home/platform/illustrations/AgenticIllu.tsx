"use client";

import type { CSSProperties, ReactNode } from "react";
import { motion } from "motion/react";

interface AgenticIlluProps {
  readonly className?: string;
}

/**
 * Platform section illustration: the approval gate.
 *
 * A calm dark agent panel cropped to a single transcript whose one lit element
 * is a human approval gate. A destructive `createReview` call carries a coral
 * `destructiveHint` tag, pauses on a violet `PENDING -> GRANTED` gate, and only
 * then does one teal `+ discountedPrice` safe-patch diff line land below it.
 * Everything else stays a dimmed mono transcript, so the eye goes straight to
 * the gate: risky agent edits stop at a human gate.
 *
 * Section grade re-express of the v6 feedback hook: larger, cleaner, with a soft
 * violet halo behind the gate and a teal gutter on the applied patch so the risk
 * gate (violet) and the approved change (teal) rhyme. The sole looping accent is
 * a violet spark crossing the gate as the GRANTED pill glows. All text is fully
 * legible at rest, so the first frame and any screenshot read cleanly.
 *
 * cc-* dark palette only; status colors encode real status (coral destructive,
 * violet governance, teal safe patch). Inline SVG id prefix "illu-agentic-".
 */
const C = {
  cardBg: "#0c1322",
  headBg: "#0e1626",
  border: "rgba(245,241,234,0.12)",
  heading: "#f5f0ea",
  dim: "rgba(245,241,234,0.62)",
  faint: "rgba(245,241,234,0.42)",
  ghost: "rgba(245,241,234,0.26)",
  eyebrow: "#62748e",
  accent: "#5eead4",
  accentDim: "rgba(94,234,212,0.55)",
  accentBg: "rgba(94,234,212,0.07)",
  accentBorder: "rgba(94,234,212,0.30)",
  coral: "#f0786a",
  coralBg: "rgba(240,120,106,0.13)",
  coralBorder: "rgba(240,120,106,0.36)",
  violet: "#8b8ff0",
  violetSoft: "#7c92c6",
  violetBg: "rgba(139,143,240,0.10)",
  violetBgStrong: "rgba(139,143,240,0.18)",
  violetBorder: "rgba(139,143,240,0.28)",
  violetBorderStrong: "rgba(139,143,240,0.46)",
  mono: 'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace',
} as const;

const TRANSCRIPT: CSSProperties = {
  fontFamily: C.mono,
  fontSize: 13,
  lineHeight: "22px",
  fontVariantNumeric: "tabular-nums",
  color: C.faint,
};

export function AgenticIllu({ className }: AgenticIlluProps) {
  return (
    <div
      aria-hidden="true"
      className={["mx-auto w-full", className ?? ""].join(" ")}
    >
      <div
        style={{
          maxWidth: 440,
          margin: "0 auto",
          background: C.cardBg,
          border: `1px solid ${C.border}`,
          borderRadius: 14,
          overflow: "hidden",
          boxShadow: "0 1px 3px rgba(2,6,16,0.6)",
        }}
      >
        {/* Title row: terminal traffic dots + agent CLI surface. */}
        <div
          style={{
            display: "flex",
            alignItems: "center",
            gap: 10,
            padding: "10px 16px",
            background: C.headBg,
            borderBottom: `1px solid ${C.border}`,
          }}
        >
          <span style={{ display: "flex", gap: 6 }}>
            <Dot />
            <Dot />
            <Dot />
          </span>
          <span
            style={{
              fontFamily: C.mono,
              fontSize: 12,
              color: C.eyebrow,
              letterSpacing: "0.01em",
            }}
          >
            agent &middot; /graphql/mcp
          </span>
        </div>

        {/* The transcript. Everything is dimmed except the gate, the coral tag,
            and the teal safe-patch line. */}
        <div style={{ ...TRANSCRIPT, padding: "16px 18px 17px" }}>
          {/* 1. The destructive agent call. */}
          <div style={{ marginBottom: 5 }}>
            <span style={{ color: C.faint }}>&rarr; </span>
            <span style={{ color: C.dim }}>createReview</span>
            <span style={{ color: C.faint }}>(schema: </span>
            <span style={{ color: C.faint }}>&quot;eshops/api&quot;</span>
            <span style={{ color: C.faint }}>)</span>
          </div>

          {/* 1b. Coral destructiveHint tag attached to the call above the gate. */}
          <div style={{ marginBottom: 11, paddingLeft: 16 }}>
            <span
              style={{
                display: "inline-flex",
                alignItems: "center",
                gap: 6,
                padding: "2px 8px",
                fontSize: 11,
                letterSpacing: "0.02em",
                borderRadius: 6,
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
          <div style={{ marginBottom: 11 }}>
            <span style={{ color: C.faint }}>&larr; review </span>
            <span style={{ color: C.ghost }}>rev_8f2c</span>
            <span style={{ color: C.faint }}> staged</span>
          </div>

          {/* 3. The lit governance gate row, with a soft violet halo behind it. */}
          <div style={{ position: "relative", marginBottom: 12 }}>
            <GateHalo />
            <div
              style={{
                position: "relative",
                display: "flex",
                alignItems: "center",
                justifyContent: "space-between",
                gap: 10,
                padding: "9px 12px 9px 11px",
                borderRadius: 9,
                background: C.violetBg,
                border: `1px solid ${C.violetBorder}`,
                borderLeft: `2px solid ${C.violet}`,
                boxShadow: "0 0 0 1px rgba(139,143,240,0.05)",
              }}
            >
              <span
                style={{
                  display: "inline-flex",
                  alignItems: "center",
                  gap: 7,
                  color: C.violet,
                  fontSize: 10.5,
                  letterSpacing: "0.13em",
                  textTransform: "uppercase",
                  whiteSpace: "nowrap",
                }}
              >
                <LockGlyph />
                approval gate
              </span>

              <span
                style={{ display: "inline-flex", alignItems: "center", gap: 3 }}
              >
                <PendingPill>PENDING</PendingPill>
                <Spark />
                <GrantedPill />
              </span>
            </div>
          </div>

          {/* 4. The teal safe-patch diff line, landing below the gate. */}
          <div
            style={{
              display: "flex",
              alignItems: "center",
              justifyContent: "space-between",
              gap: 10,
              paddingLeft: 9,
              marginBottom: 7,
              borderLeft: `2px solid ${C.accentBorder}`,
            }}
          >
            <span>
              <span style={{ color: C.accent }}>+ </span>
              <span style={{ color: C.accent }}>discountedPrice</span>
              <span style={{ color: C.faint }}>: </span>
              <span style={{ color: C.heading }}>Money</span>
            </span>
            <span
              style={{
                flexShrink: 0,
                padding: "1px 8px",
                fontSize: 10,
                letterSpacing: "0.05em",
                borderRadius: 5,
                color: C.accent,
                background: C.accentBg,
                border: `1px solid ${C.accentBorder}`,
                whiteSpace: "nowrap",
              }}
            >
              safe patch
            </span>
          </div>

          {/* 5. Result, dimmed. */}
          <div>
            <span style={{ color: C.accentDim }}>&#10003; </span>
            <span style={{ color: C.dim }}>1 field added after approval</span>
          </div>
        </div>
      </div>
    </div>
  );
}

/** Soft violet radial halo behind the gate, marking it as the lit focal row. */
function GateHalo() {
  return (
    <svg
      aria-hidden="true"
      width="100%"
      height="100%"
      preserveAspectRatio="none"
      style={{
        position: "absolute",
        inset: 0,
        width: "100%",
        height: "100%",
        pointerEvents: "none",
      }}
    >
      <defs>
        <radialGradient id="illu-agentic-gate-halo" cx="50%" cy="50%" r="62%">
          <stop offset="0%" stopColor="rgba(139,143,240,0.20)" />
          <stop offset="100%" stopColor="rgba(139,143,240,0)" />
        </radialGradient>
      </defs>
      <rect
        x="-8%"
        y="-60%"
        width="116%"
        height="220%"
        fill="url(#illu-agentic-gate-halo)"
      />
    </svg>
  );
}

/** Static PENDING pill on the upstream side of the gate. */
function PendingPill({ children }: { readonly children: ReactNode }) {
  return (
    <span
      style={{
        display: "inline-block",
        padding: "2px 8px",
        fontSize: 10,
        letterSpacing: "0.07em",
        borderRadius: 5,
        color: C.violetSoft,
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
        gap: 5,
        padding: "2px 8px",
        fontSize: 10,
        letterSpacing: "0.07em",
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
          "0 0 11px rgba(139,143,240,0.5)",
          "0 0 0px rgba(139,143,240,0)",
        ],
      }}
      transition={{
        duration: 2.6,
        times: [0, 0.55, 0.78, 1],
        repeat: Infinity,
        repeatDelay: 0.6,
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
        width: 24,
        height: 14,
        margin: "0 1px",
      }}
    >
      <span
        style={{
          fontFamily: C.mono,
          fontSize: 12,
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
        animate={{ x: [0, 18], opacity: [0, 1, 1, 0] }}
        transition={{
          duration: 2.6,
          times: [0, 0.2, 0.72, 1],
          repeat: Infinity,
          repeatDelay: 0.6,
          ease: "easeInOut",
        }}
      />
    </span>
  );
}

/** Small violet check inside the GRANTED pill. */
function CheckGlyph() {
  return (
    <svg width="10" height="10" viewBox="0 0 12 12" aria-hidden="true">
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
    <svg width="13" height="13" viewBox="0 0 24 24" aria-hidden="true">
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
    <svg width="11" height="11" viewBox="0 0 12 12" aria-hidden="true">
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
        width: 10,
        height: 10,
        borderRadius: "50%",
        background: C.border,
        display: "inline-block",
      }}
    />
  );
}
