"use client";

import { TYPE } from "../../brand";
import { useCycle, useSceneMotion } from "./hooks";
import { MC, specTag } from "./palette";

/**
 * "What is Fusion?": the console tracks one query. A single blip arrives from
 * the client, the plan panel resolves which subgraph holds each field, tracked
 * blips fan out to those stations and return, and one response leaves the
 * gateway with the latency the run took.
 */

const W = 640;
const H = 440;
/** The scene box mirrors the viewBox, so the console never letterboxes. */
export const QUERY_TRACK_RATIO = `${W} / ${H}`;
/** 0 arms the run, 5 is the rest frame: plan resolved, one response leaving. */
const PHASES = 6;
const REST = 5;
const BEAT = 1500;

const CLIENT = { x: 34, y: 210 } as const;
const GATE = { x: 176, y: 74, w: 200, h: 292 } as const;
const GATE_IN = { x: GATE.x, y: 210 } as const;
const GATE_OUT = { x: GATE.x + GATE.w, y: 210 } as const;

const PLAN = [
  {
    field: "product.name",
    station: "Catalog",
    language: "JS/TS",
    spec: "GraphQL Federation",
  },
  {
    field: "product.price",
    station: "Billing",
    language: "Java",
    spec: "Apollo Federation",
  },
  {
    field: "product.delivery",
    station: "Shipping",
    language: "Ruby",
    spec: "Apollo Federation",
  },
] as const;

const STATION_X = 452;
const stationY = (i: number) => 108 + i * 104;

const KEYFRAMES = `
@keyframes mc-track-pulse { 0%, 100% { opacity: 0.35; } 50% { opacity: 1; } }
`;

function motionStyle(phase: number, x: number, y: number, shown: boolean) {
  return {
    transform: `translate(${x}px, ${y}px)`,
    opacity: shown ? 1 : 0,
    transition:
      phase === 0 ? "none" : "transform 820ms ease-in-out, opacity 320ms ease",
  };
}

export function QueryTrack() {
  const running = useSceneMotion();
  const phase = useCycle(running, PHASES, BEAT, REST);

  const inbound = phase === 0 ? CLIENT : GATE_IN;
  const outbound = phase >= 5 ? CLIENT : GATE_OUT;
  const latency = phase >= 4 ? "42" : phase >= 2 ? "18" : "--";

  return (
    <svg viewBox={`0 0 ${W} ${H}`} className="h-full w-full">
      <style>{KEYFRAMES}</style>
      <rect width={W} height={H} fill={MC.bg} />

      <path
        d={`M${CLIENT.x + 10} ${CLIENT.y}H${GATE_IN.x}`}
        stroke={MC.line}
        strokeDasharray="3 7"
      />
      <text
        x={CLIENT.x}
        y={CLIENT.y - 22}
        fill={MC.dim}
        fontFamily={MC.mono}
        fontSize={TYPE.label}
        letterSpacing="0.16em"
      >
        MOBILE
      </text>
      <text
        x={CLIENT.x}
        y={CLIENT.y + 32}
        fill={MC.dim}
        fontFamily={MC.mono}
        fontSize={TYPE.label}
        letterSpacing="0.14em"
      >
        ONE QUERY
      </text>

      <rect
        x={GATE.x}
        y={GATE.y}
        width={GATE.w}
        height={GATE.h}
        rx="10"
        fill={MC.panel}
        stroke={MC.phosphor}
        strokeOpacity="0.4"
      />
      <text
        x={GATE.x + 16}
        y={GATE.y + 26}
        fill={MC.ink}
        fontFamily={MC.mono}
        fontSize={TYPE.caption}
        letterSpacing="0.2em"
      >
        GATEWAY
      </text>
      <text
        x={GATE.x + 16}
        y={GATE.y + 44}
        fill={MC.dim}
        fontFamily={MC.mono}
        fontSize={TYPE.label}
        letterSpacing="0.04em"
      >
        ONE ENDPOINT · QUERY PLAN
      </text>

      {PLAN.map((row, i) => {
        const resolved = phase >= 2;
        const y = GATE.y + 78 + i * 62;
        return (
          <g key={row.field}>
            <text
              x={GATE.x + 16}
              y={y}
              fill={MC.ink}
              fontFamily={MC.mono}
              fontSize={TYPE.label}
            >
              {row.field}
            </text>
            <text
              x={GATE.x + 16}
              y={y + 18}
              fill={resolved ? MC.phosphor : MC.dim}
              fontFamily={MC.mono}
              fontSize={TYPE.label}
              letterSpacing="0.14em"
              style={{ transition: "fill 400ms ease" }}
            >
              {resolved ? `→ ${row.station.toUpperCase()}` : "→ RESOLVING"}
            </text>
          </g>
        );
      })}

      <text
        x={GATE.x + 16}
        y={GATE.y + GATE.h - 18}
        fill={MC.phosphor}
        fontFamily={MC.mono}
        fontSize={TYPE.label}
        letterSpacing="0.14em"
      >
        {`LATENCY ${latency} ms`}
      </text>

      {PLAN.map((row, i) => {
        const y = stationY(i);
        const called = phase >= 3;
        return (
          <g key={row.station}>
            <path
              d={`M${GATE_OUT.x} 210C${GATE_OUT.x + 40} 210 ${STATION_X - 40} ${y} ${STATION_X} ${y}`}
              fill="none"
              stroke={called ? MC.signal : MC.line}
              strokeOpacity={called ? 0.55 : 0.3}
              style={{ transition: "stroke 400ms ease" }}
            />
            <rect
              x={STATION_X}
              y={y - 30}
              width="158"
              height="60"
              rx="8"
              fill={MC.panel}
              stroke={called ? MC.phosphor : MC.panelEdge}
              strokeOpacity={called ? 0.6 : 1}
              style={{ transition: "stroke 400ms ease" }}
            />
            <text
              x={STATION_X + 12}
              y={y - 8}
              fill={MC.ink}
              fontFamily={MC.mono}
              fontSize={TYPE.caption}
              letterSpacing="0.14em"
            >
              {row.station.toUpperCase()}
            </text>
            <text
              x={STATION_X + 12}
              y={y + 10}
              fill={MC.dim}
              fontFamily={MC.mono}
              fontSize={TYPE.label}
              letterSpacing="0.06em"
            >
              {`${row.language} · ${specTag(row.spec)}`}
            </text>
            <text
              x={STATION_X + 12}
              y={y + 24}
              fill={phase >= 4 ? MC.phosphor : MC.dim}
              fontFamily={MC.mono}
              fontSize={TYPE.label}
              letterSpacing="0.02em"
              style={{ transition: "fill 400ms ease" }}
            >
              {phase >= 4 ? "PARTIAL RESULT RETURNED" : "SUBGRAPH STANDING BY"}
            </text>

            <circle
              r="4"
              fill={MC.signal}
              style={motionStyle(
                phase,
                phase >= 4 ? GATE_OUT.x : phase >= 3 ? STATION_X : GATE_OUT.x,
                phase >= 4 ? 210 : phase >= 3 ? y : 210,
                phase >= 3,
              )}
            />
          </g>
        );
      })}

      <circle
        r="5"
        fill={MC.signal}
        style={motionStyle(phase, inbound.x, inbound.y, phase <= 1)}
      />
      <circle
        r="6"
        fill={MC.phosphor}
        style={motionStyle(phase, outbound.x, outbound.y, phase >= 5)}
      />
      <rect
        x={CLIENT.x - 12}
        y={CLIENT.y - 12}
        width="24"
        height="24"
        rx="6"
        fill="none"
        stroke={MC.signal}
        strokeOpacity="0.7"
        style={{
          animation: running
            ? "mc-track-pulse 2400ms ease-in-out infinite"
            : "none",
        }}
      />
      <text
        x={W - 16}
        y={H - 16}
        fill={MC.dim}
        fontFamily={MC.mono}
        fontSize={TYPE.label}
        letterSpacing="0.16em"
        textAnchor="end"
      >
        {phase >= 5 ? "ONE RESPONSE RETURNED" : "TRACKING ONE QUERY"}
      </text>
    </svg>
  );
}
