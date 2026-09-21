"use client";

import { useRef } from "react";

import { TYPE, svgLabelGap, svgLabelSize } from "../tokens";
import {
  useCycle,
  useNarrowViewport,
  useSceneMotion,
  useSvgLabelScale,
} from "./hooks";
import { dotLines, wrapWords } from "./lines";
import { useSceneRatio } from "./Scene";
import { MC, specTag } from "../palette";

/**
 * Animated console that traces one query from client request through subgraph
 * resolution to a single gateway response.
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

/**
 * Below 1024px the client, gateway and stations stack in a single column
 * (stations below the gateway instead of beside it) so every label keeps
 * its natural glyph width. `MOBILE_W` matches this panel's measured
 * rendered width at 375px.
 */
const MOBILE_W = 333;
const MOBILE_CLIENT = { x: MOBILE_W / 2, y: 30 } as const;
const MOBILE_GATE = { x: 16, y: 66, w: MOBILE_W - 32, h: 250 } as const;
const MOBILE_GATE_IN = {
  x: MOBILE_GATE.x,
  y: MOBILE_GATE.y + MOBILE_GATE.h / 2,
} as const;
const MOBILE_GATE_OUT = {
  x: MOBILE_GATE.x + MOBILE_GATE.w,
  y: MOBILE_GATE.y + MOBILE_GATE.h / 2,
} as const;
const MOBILE_STATION_ROW_H = 78;
const MOBILE_STATION_GAP = 14;
const MOBILE_STATIONS_Y = MOBILE_GATE.y + MOBILE_GATE.h + 30;
const mobileStationY = (i: number) =>
  MOBILE_STATIONS_Y + i * (MOBILE_STATION_ROW_H + MOBILE_STATION_GAP);
const MOBILE_H = mobileStationY(PLAN.length - 1) + MOBILE_STATION_ROW_H + 46;
const MOBILE_RATIO = `${MOBILE_W} / ${MOBILE_H}`;

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
  const svgRef = useRef<SVGSVGElement>(null);
  const desktopScale = useSvgLabelScale(svgRef, W);
  const mobile = useNarrowViewport();
  const mobileScale = useSvgLabelScale(svgRef, MOBILE_W);
  const scale = mobile ? mobileScale : desktopScale;
  useSceneRatio(mobile ? MOBILE_RATIO : null);
  const label = svgLabelSize(TYPE.label, scale);
  const caption = svgLabelSize(TYPE.caption, scale);
  /**
   * A sidebar-narrow desktop slot boosts `label` here the same way a narrow
   * viewport does. Any boost at all — not just past some width band —
   * pushes the GATE subtitle and a station card's spec/status lines past
   * their own panel: they wrap onto two lines instead once `label` grows
   * past its floor.
   */
  const crowded = !mobile && scale < 1;
  /**
   * The extra room the GATE subtitle's own wrap onto two lines needs; the
   * three plan rows below it (and nothing past them — `LATENCY` keeps its
   * fixed slot at the card's own bottom) shift down by the same amount.
   */
  const gateSubtitleWrapGap = crowded ? svgLabelGap(18, label, TYPE.label) : 0;

  const client = mobile ? MOBILE_CLIENT : CLIENT;
  const gate = mobile ? MOBILE_GATE : GATE;
  const gateIn = mobile ? MOBILE_GATE_IN : GATE_IN;
  const gateOut = mobile ? MOBILE_GATE_OUT : GATE_OUT;
  const inbound = phase === 0 ? client : gateIn;
  const outbound = phase >= 5 ? client : gateOut;
  const latency = phase >= 4 ? "42" : phase >= 2 ? "18" : "--";

  if (mobile) {
    const rowGap18 = svgLabelGap(18, label, TYPE.label);
    const planRowStride = 56;
    const planStartY = gate.y + 78 + (rowGap18 - 18);

    return (
      <svg
        ref={svgRef}
        viewBox={`0 0 ${MOBILE_W} ${MOBILE_H}`}
        className="h-full w-full"
      >
        <style>{KEYFRAMES}</style>
        <rect width={MOBILE_W} height={MOBILE_H} fill={MC.bg} />

        <path
          d={`M${MOBILE_W / 2} 56V${gate.y}`}
          fill="none"
          stroke={MC.line}
          strokeDasharray="3 7"
        />
        <text
          x={16}
          y={client.y}
          fill={MC.dim}
          fontFamily={MC.mono}
          fontSize={label}
          letterSpacing="0.16em"
        >
          MOBILE
        </text>
        <text
          x={16}
          y={client.y + 18}
          fill={MC.dim}
          fontFamily={MC.mono}
          fontSize={label}
          letterSpacing="0.14em"
        >
          ONE QUERY
        </text>
        <rect
          x={MOBILE_W - 36}
          y={client.y - 14}
          width="20"
          height="20"
          rx="5"
          fill="none"
          stroke={MC.signal}
          strokeOpacity="0.7"
          style={{
            animation: running
              ? "mc-track-pulse 2400ms ease-in-out infinite"
              : "none",
          }}
        />

        <rect
          x={gate.x}
          y={gate.y}
          width={gate.w}
          height={gate.h}
          rx="10"
          fill={MC.panel}
          stroke={MC.phosphor}
          strokeOpacity="0.4"
        />
        <text
          x={gate.x + 16}
          y={gate.y + 26}
          fill={MC.ink}
          fontFamily={MC.mono}
          fontSize={caption}
          letterSpacing="0.2em"
        >
          GATEWAY
        </text>
        <text
          x={gate.x + 16}
          y={gate.y + 26 + rowGap18}
          fill={MC.dim}
          fontFamily={MC.mono}
          fontSize={label}
          letterSpacing="0.04em"
        >
          ONE ENDPOINT · QUERY PLAN
        </text>

        {PLAN.map((row, i) => {
          const resolved = phase >= 2;
          const y = planStartY + i * planRowStride;
          const arrowText = resolved
            ? `→ ${row.station.toUpperCase()}`
            : "→ RESOLVING";
          return (
            <g key={row.field}>
              <text
                x={gate.x + 16}
                y={y}
                fill={MC.ink}
                fontFamily={MC.mono}
                fontSize={label}
              >
                {row.field}
              </text>
              <text
                x={gate.x + 16}
                y={y + rowGap18}
                fill={resolved ? MC.phosphor : MC.dim}
                fontFamily={MC.mono}
                fontSize={label}
                letterSpacing="0.14em"
                style={{ transition: "fill 400ms ease" }}
              >
                {arrowText}
              </text>
            </g>
          );
        })}

        <text
          x={gate.x + 16}
          y={gate.y + gate.h - 16}
          fill={MC.phosphor}
          fontFamily={MC.mono}
          fontSize={label}
          letterSpacing="0.14em"
        >
          {`LATENCY ${latency} ms`}
        </text>

        {PLAN.map((row, i) => {
          const y = mobileStationY(i);
          const called = phase >= 3;
          const resultText =
            phase >= 4 ? "PARTIAL RESULT RETURNED" : "SUBGRAPH STANDING BY";
          const stationCx = MOBILE_W / 2;
          return (
            <g key={row.station}>
              <path
                d={`M${gateOut.x} ${gateOut.y}V${y + 6}`}
                fill="none"
                stroke={called ? MC.signal : MC.line}
                strokeOpacity={called ? 0.55 : 0.3}
                style={{ transition: "stroke 400ms ease" }}
              />
              <rect
                x={16}
                y={y}
                width={MOBILE_W - 32}
                height={MOBILE_STATION_ROW_H}
                rx="8"
                fill={MC.panel}
                stroke={called ? MC.phosphor : MC.panelEdge}
                strokeOpacity={called ? 0.6 : 1}
                style={{ transition: "stroke 400ms ease" }}
              />
              <text
                x={28}
                y={y + 24}
                fill={MC.ink}
                fontFamily={MC.mono}
                fontSize={caption}
                letterSpacing="0.14em"
              >
                {row.station.toUpperCase()}
              </text>
              <text
                x={28}
                y={y + 24 + rowGap18}
                fill={MC.dim}
                fontFamily={MC.mono}
                fontSize={label}
                letterSpacing="0.06em"
              >
                {`${row.language} · ${specTag(row.spec)}`}
              </text>
              <text
                x={28}
                y={y + 24 + rowGap18 + svgLabelGap(14, label, TYPE.label)}
                fill={phase >= 4 ? MC.phosphor : MC.dim}
                fontFamily={MC.mono}
                fontSize={label}
                letterSpacing="0.02em"
                style={{ transition: "fill 400ms ease" }}
              >
                {resultText}
              </text>

              <circle
                r="4"
                fill={MC.signal}
                style={motionStyle(
                  phase,
                  phase >= 4 ? gateOut.x : stationCx,
                  phase >= 4 ? gateOut.y : y,
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
        <text
          x={MOBILE_W - 16}
          y={MOBILE_H - 16}
          fill={MC.dim}
          fontFamily={MC.mono}
          fontSize={label}
          letterSpacing="0.1em"
          textAnchor="end"
        >
          {phase >= 5 ? "ONE RESPONSE RETURNED" : "TRACKING ONE QUERY"}
        </text>
      </svg>
    );
  }

  return (
    <svg ref={svgRef} viewBox={`0 0 ${W} ${H}`} className="h-full w-full">
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
        fontSize={label}
        letterSpacing="0.16em"
      >
        MOBILE
      </text>
      <text
        x={CLIENT.x}
        y={CLIENT.y + 32}
        fill={MC.dim}
        fontFamily={MC.mono}
        fontSize={label}
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
        fontSize={caption}
        letterSpacing="0.2em"
      >
        GATEWAY
      </text>
      <text
        x={GATE.x + 16}
        y={GATE.y + 26 + svgLabelGap(18, label, TYPE.label)}
        fill={MC.dim}
        fontFamily={MC.mono}
        fontSize={label}
        letterSpacing="0.04em"
      >
        {crowded
          ? dotLines("ONE ENDPOINT · QUERY PLAN").map((line, i) => (
              <tspan
                key={i}
                x={GATE.x + 16}
                dy={i === 0 ? 0 : svgLabelGap(18, label, TYPE.label)}
              >
                {line}
              </tspan>
            ))
          : "ONE ENDPOINT · QUERY PLAN"}
      </text>

      {PLAN.map((row, i) => {
        const resolved = phase >= 2;
        const y =
          GATE.y +
          78 +
          (svgLabelGap(18, label, TYPE.label) - 18) +
          i * 62 +
          gateSubtitleWrapGap;
        const arrowText = resolved
          ? `→ ${row.station.toUpperCase()}`
          : "→ RESOLVING";
        return (
          <g key={row.field}>
            <text
              x={GATE.x + 16}
              y={y}
              fill={MC.ink}
              fontFamily={MC.mono}
              fontSize={label}
            >
              {row.field}
            </text>
            <text
              x={GATE.x + 16}
              y={y + svgLabelGap(18, label, TYPE.label)}
              fill={resolved ? MC.phosphor : MC.dim}
              fontFamily={MC.mono}
              fontSize={label}
              letterSpacing="0.14em"
              style={{ transition: "fill 400ms ease" }}
            >
              {arrowText}
            </text>
          </g>
        );
      })}

      <text
        x={GATE.x + 16}
        y={GATE.y + GATE.h - 18}
        fill={MC.phosphor}
        fontFamily={MC.mono}
        fontSize={label}
        letterSpacing="0.14em"
      >
        {`LATENCY ${latency} ms`}
      </text>

      {PLAN.map((row, i) => {
        const y = stationY(i);
        const called = phase >= 3;
        const resultText =
          phase >= 4 ? "PARTIAL RESULT RETURNED" : "SUBGRAPH STANDING BY";
        const resultGap = svgLabelGap(14, label, TYPE.label);
        const resultLines = crowded ? wrapWords(resultText, 14) : [resultText];
        const specText = `${row.language} · ${specTag(row.spec)}`;
        const specLines = crowded ? dotLines(specText) : [specText];
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
              height={
                60 +
                svgLabelGap(18, label, TYPE.label) -
                18 +
                resultGap -
                14 +
                (resultLines.length - 1) * resultGap +
                (specLines.length - 1) * resultGap
              }
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
              fontSize={caption}
              letterSpacing="0.14em"
            >
              {row.station.toUpperCase()}
            </text>
            <text
              x={STATION_X + 12}
              y={y - 8 + svgLabelGap(18, label, TYPE.label)}
              fill={MC.dim}
              fontFamily={MC.mono}
              fontSize={label}
              letterSpacing="0.06em"
            >
              {specLines.map((line, li) => (
                <tspan
                  key={li}
                  x={STATION_X + 12}
                  dy={li === 0 ? 0 : resultGap}
                >
                  {line}
                </tspan>
              ))}
            </text>
            <text
              x={STATION_X + 12}
              y={
                y -
                8 +
                svgLabelGap(18, label, TYPE.label) +
                resultGap +
                (specLines.length - 1) * resultGap
              }
              fill={phase >= 4 ? MC.phosphor : MC.dim}
              fontFamily={MC.mono}
              fontSize={label}
              letterSpacing="0.02em"
              style={{ transition: "fill 400ms ease" }}
            >
              {resultLines.map((line, li) => (
                <tspan
                  key={li}
                  x={STATION_X + 12}
                  dy={li === 0 ? 0 : resultGap}
                >
                  {line}
                </tspan>
              ))}
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
        fontSize={label}
        letterSpacing="0.16em"
        textAnchor="end"
      >
        {phase >= 5 ? "ONE RESPONSE RETURNED" : "TRACKING ONE QUERY"}
      </text>
    </svg>
  );
}
