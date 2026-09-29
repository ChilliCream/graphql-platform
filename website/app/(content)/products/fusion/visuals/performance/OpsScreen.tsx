"use client";

import { token } from "@/src/nitro";

import { CACHED_DOCUMENTS, CACHED_PLANS, formatCount } from "./data";
import { ELECTRIC_DIM, SCREEN_BG } from "./hud";
import type { TelemetryMotion } from "./useTelemetryClock";
import { TrendChart, TrendLine } from "./TrendLine";

interface OpsScreenProps {
  readonly telemetry: TelemetryMotion;
}

interface KeyProps {
  readonly color: string;
  readonly label: string;
}

interface OdometerRowProps {
  readonly label: string;
  readonly value: number;
}

const TEXT = {
  fontSize: 11,
  lineHeight: 1,
  fontFamily: token.mono,
} as const;

function Key({ color, label }: KeyProps) {
  return (
    <span className="flex items-center whitespace-nowrap" style={{ gap: 4 }}>
      <span
        style={{
          width: 6,
          height: 6,
          borderRadius: 999,
          background: color,
          display: "inline-block",
        }}
      />
      <span style={{ ...TEXT, color: token.textSecondary }}>{label}</span>
    </span>
  );
}

function OdometerRow({ label, value }: OdometerRowProps) {
  return (
    <div
      className="flex items-baseline justify-between whitespace-nowrap"
      style={{ gap: 6 }}
    >
      <span
        className="uppercase"
        style={{ ...TEXT, color: token.textSecondary }}
      >
        {label}
      </span>
      <span
        style={{
          ...TEXT,
          fontWeight: 700,
          color: token.textStrong,
          fontVariantNumeric: "tabular-nums",
        }}
      >
        {formatCount(value)}
      </span>
    </div>
  );
}

export function OpsScreen({ telemetry }: OpsScreenProps) {
  return (
    <div
      className="flex flex-col"
      style={{
        gap: 3,
        padding: "4px 6px",
        borderRadius: 4,
        background: SCREEN_BG,
        boxShadow: `inset 0 0 0 1px ${ELECTRIC_DIM}`,
      }}
    >
      <div style={{ height: "clamp(22px, 10cqw, 40px)", margin: "0 8px" }}>
        <TrendChart label="Network traffic, incoming in red and outgoing in blue, in percent">
          <TrendLine
            values={telemetry.netOutHistory}
            domain={[0, 100]}
            color={token.chThroughput}
          />
          <TrendLine
            values={telemetry.netInHistory}
            domain={[0, 100]}
            color={token.error}
          />
        </TrendChart>
      </div>
      <div
        aria-hidden="true"
        className="flex items-center justify-center"
        style={{ gap: 10 }}
      >
        <Key color={token.error} label="in" />
        <Key color={token.chThroughput} label="out" />
      </div>
      <OdometerRow label="cached docs" value={CACHED_DOCUMENTS} />
      <OdometerRow label="cached plans" value={CACHED_PLANS} />
    </div>
  );
}
