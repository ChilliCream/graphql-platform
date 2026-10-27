"use client";

import { token } from "@/src/nitro";

import type { TelemetryMotion } from "./useTelemetryClock";
import { LABEL_COLOR, NETWORK_CHART_WIDTH } from "./hud";
import { TrendChart, TrendLine } from "./TrendLine";

interface OpsScreenProps {
  readonly telemetry: TelemetryMotion;
}

interface KeyProps {
  readonly color: string;
  readonly label: string;
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

export function OpsScreen({ telemetry }: OpsScreenProps) {
  return (
    <div
      className="flex flex-col items-center"
      style={{ gap: "clamp(3px, 1.2cqw - 2px, 4px)" }}
    >
      <span className="uppercase" style={{ ...TEXT, color: LABEL_COLOR }}>
        network
      </span>
      <div
        style={{
          width: NETWORK_CHART_WIDTH,
          height: "clamp(16px, 12cqw - 12px, 40px)",
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
    </div>
  );
}
