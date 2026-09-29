"use client";

import { token } from "@/src/nitro";

import { CACHED_DOCUMENTS, CACHED_PLANS, formatCount } from "./data";
import type { TelemetryMotion } from "./useTelemetryClock";
import { LABEL_COLOR } from "./hud";
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
      <span style={{ ...TEXT, color: LABEL_COLOR }}>{label}</span>
    </span>
  );
}

function OdometerRow({ label, value }: OdometerRowProps) {
  return (
    <div
      className="flex items-baseline justify-between whitespace-nowrap"
      style={{ gap: 4 }}
    >
      <span className="uppercase" style={{ ...TEXT, color: LABEL_COLOR }}>
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
      style={{ gap: "clamp(2px, 1.2cqw - 2px, 4px)", width: "max-content" }}
    >
      <span
        className="text-center uppercase"
        style={{ ...TEXT, color: LABEL_COLOR }}
      >
        network
      </span>
      <div
        style={{
          width: 0,
          minWidth: "calc(100% - 16px)",
          height: "clamp(14px, 10cqw - 9px, 32px)",
          margin: "0 8px",
        }}
      >
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
