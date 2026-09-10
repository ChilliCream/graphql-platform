"use client";

import { useRef } from "react";
import type { CSSProperties } from "react";

import { anim, useCycle, useElementMotion } from "./hooks";
import { CLIENTS, MC, SOURCES, STATIONS, specTag } from "./palette";

/**
 * Hero visual: the ops-room wall map. A radar sweep turns around the gateway
 * console while the station plates come online one after another and the
 * readout counts the subgraphs that have joined the composite schema.
 */

const W = 1200;
const H = 700;
const CX = 600;
const CY = 352;
const RINGS = [130, 235, 340] as const;
const SWEEP_MS = 9000;
/** Steps 0-4 bring one station online each; 5-7 hold the full board. */
const STEPS = 8;
const REST_STEP = STEPS - 1;

const px = (station: { x: number; y: number }) => ({
  x: (station.x / 100) * W,
  y: (station.y / 100) * H,
});

const SIGNALS = CLIENTS.map((label, i) => {
  const y = 150 + i * 200;
  return { label, x: 78, y, dx: CX - 96 - 78, dy: CY - y };
});

const KEYFRAMES = `
@keyframes mc-wall-sweep { to { transform: rotate(360deg); } }
@keyframes mc-wall-ping { 0% { transform: scale(0.28); opacity: 0.55; } 100% { transform: scale(1); opacity: 0; } }
@keyframes mc-wall-signal {
  0% { transform: translate(0, 0); opacity: 0; }
  12% { opacity: 1; }
  88% { opacity: 1; }
  100% { transform: translate(var(--mc-dx), var(--mc-dy)); opacity: 0; }
}
`;

export function WallMap() {
  const ref = useRef<HTMLDivElement>(null);
  const running = useElementMotion(ref);
  const step = useCycle(running, STEPS, 1200, REST_STEP);
  const online = Math.min(step + 1, STATIONS.length);

  return (
    <div ref={ref} className="absolute inset-0" aria-hidden="true">
      <style>{KEYFRAMES}</style>
      <svg
        viewBox={`0 0 ${W} ${H}`}
        preserveAspectRatio="xMidYMid meet"
        className="h-full w-full"
      >
        <defs>
          <radialGradient id="mc-wall-glow" cx="50%" cy="50%" r="50%">
            <stop offset="0%" stopColor={MC.phosphor} stopOpacity="0.18" />
            <stop offset="100%" stopColor={MC.phosphor} stopOpacity="0" />
          </radialGradient>
          <linearGradient id="mc-wall-sweep-fill" x1="0" y1="0" x2="1" y2="0">
            <stop offset="0%" stopColor={MC.phosphor} stopOpacity="0.34" />
            <stop offset="100%" stopColor={MC.phosphor} stopOpacity="0" />
          </linearGradient>
          <pattern
            id="mc-wall-grid"
            width="48"
            height="48"
            patternUnits="userSpaceOnUse"
          >
            <path d="M48 0H0V48" fill="none" stroke={MC.grid} strokeWidth="1" />
          </pattern>
        </defs>

        <rect width={W} height={H} fill={MC.bg} />
        <rect width={W} height={H} fill="url(#mc-wall-grid)" />
        <circle cx={CX} cy={CY} r={380} fill="url(#mc-wall-glow)" />

        {RINGS.map((r) => (
          <circle
            key={r}
            cx={CX}
            cy={CY}
            r={r}
            fill="none"
            stroke={MC.line}
            strokeOpacity="0.35"
            strokeDasharray="2 10"
          />
        ))}
        <path
          d={`M${CX - 380} ${CY}H${CX + 380}M${CX} ${CY - 300}V${CY + 300}`}
          stroke={MC.line}
          strokeOpacity="0.22"
        />

        <g
          style={{
            transformBox: "view-box",
            transformOrigin: `${CX}px ${CY}px`,
            animation: anim(
              running,
              `mc-wall-sweep ${SWEEP_MS}ms linear infinite`,
            ),
            transform: running ? undefined : "rotate(-34deg)",
          }}
        >
          <path
            d={`M${CX} ${CY}L${CX + 360} ${CY - 128}A384 384 0 0 1 ${CX + 360} ${CY + 128}Z`}
            fill="url(#mc-wall-sweep-fill)"
          />
          <path
            d={`M${CX} ${CY}H${CX + 380}`}
            stroke={MC.phosphor}
            strokeOpacity="0.7"
          />
        </g>

        {STATIONS.map((station, i) => {
          const { x, y } = px(station);
          const lit = i < online;
          return (
            <g
              key={station.name}
              style={{ opacity: lit ? 1 : 0.3, transition: "opacity 500ms" }}
            >
              <path
                d={`M${CX} ${CY}L${x} ${y}`}
                stroke={lit ? MC.phosphor : MC.line}
                strokeOpacity={lit ? 0.4 : 0.25}
                strokeDasharray="4 8"
              />
              <rect
                x={x - 7}
                y={y - 7}
                width="14"
                height="14"
                transform={`rotate(45 ${x} ${y})`}
                fill={lit ? MC.phosphor : "none"}
                fillOpacity="0.25"
                stroke={lit ? MC.phosphor : MC.line}
              />
              {lit && (
                <circle
                  cx={x}
                  cy={y}
                  r="26"
                  fill="none"
                  stroke={MC.phosphor}
                  strokeOpacity="0.5"
                  style={{
                    transformBox: "fill-box",
                    transformOrigin: "center",
                    animation: anim(
                      running,
                      `mc-wall-ping 2600ms ease-out ${i * 240}ms infinite`,
                    ),
                    opacity: running ? undefined : 0,
                  }}
                />
              )}
              <text
                x={x + 18}
                y={y - 2}
                fill={MC.ink}
                fontFamily={MC.mono}
                fontSize="15"
                letterSpacing="0.08em"
              >
                {station.name.toUpperCase()}
              </text>
              <text
                x={x + 18}
                y={y + 16}
                fill={MC.dim}
                fontFamily={MC.mono}
                fontSize="11"
                letterSpacing="0.16em"
              >
                {`${station.language} · ${specTag(station.spec)}`}
              </text>
            </g>
          );
        })}

        {SOURCES.map((source, i) => (
          <text
            key={source.name}
            x={CX - 250 + i * 300}
            y={CY + 300}
            fill={MC.dim}
            fontFamily={MC.mono}
            fontSize="11"
            letterSpacing="0.16em"
            textAnchor="middle"
          >
            {`${source.name.toUpperCase()} · ${source.kind.toUpperCase()} FEED`}
          </text>
        ))}

        {SIGNALS.map((signal, i) => (
          <g key={signal.label}>
            <path
              d={`M${signal.x} ${signal.y}L${signal.x + signal.dx} ${signal.y + signal.dy}`}
              stroke={MC.signal}
              strokeOpacity="0.24"
            />
            <text
              x={signal.x}
              y={signal.y - 12}
              fill={MC.dim}
              fontFamily={MC.mono}
              fontSize="11"
              letterSpacing="0.16em"
            >
              {signal.label.toUpperCase()}
            </text>
            <circle
              cx={signal.x}
              cy={signal.y}
              r="4"
              fill={MC.signal}
              style={
                {
                  "--mc-dx": `${signal.dx}px`,
                  "--mc-dy": `${signal.dy}px`,
                  animation: anim(
                    running,
                    `mc-wall-signal 3400ms linear ${i * 900}ms infinite`,
                  ),
                  transform: running
                    ? undefined
                    : `translate(${signal.dx * 0.5}px, ${signal.dy * 0.5}px)`,
                } as CSSProperties
              }
            />
          </g>
        ))}

        <g>
          <rect
            x={CX - 96}
            y={CY - 44}
            width="192"
            height="88"
            rx="10"
            fill={MC.panel}
            stroke={MC.phosphor}
            strokeOpacity="0.45"
          />
          <text
            x={CX}
            y={CY - 14}
            fill={MC.ink}
            fontFamily={MC.mono}
            fontSize="16"
            letterSpacing="0.22em"
            textAnchor="middle"
          >
            GATEWAY
          </text>
          <text
            x={CX}
            y={CY + 10}
            fill={MC.dim}
            fontFamily={MC.mono}
            fontSize="11"
            letterSpacing="0.16em"
            textAnchor="middle"
          >
            COMPOSITE SCHEMA
          </text>
          <text
            x={CX}
            y={CY + 30}
            fill={MC.phosphor}
            fontFamily={MC.mono}
            fontSize="11"
            letterSpacing="0.16em"
            textAnchor="middle"
          >
            DISTRIBUTED EXECUTOR
          </text>
        </g>
      </svg>

      <div
        className="absolute right-4 bottom-4 rounded-md border px-3 py-2 text-right sm:right-8 sm:bottom-8"
        style={{
          background: MC.panel,
          borderColor: MC.panelEdge,
          fontFamily: MC.mono,
        }}
      >
        <p className="text-[10px] tracking-[0.22em]" style={{ color: MC.dim }}>
          SUBGRAPHS ONLINE
        </p>
        <p
          className="text-2xl tabular-nums"
          style={{ color: MC.phosphor }}
        >{`${String(online).padStart(2, "0")} / ${String(STATIONS.length).padStart(2, "0")}`}</p>
      </div>
    </div>
  );
}
