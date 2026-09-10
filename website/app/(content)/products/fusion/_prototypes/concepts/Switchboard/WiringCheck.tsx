"use client";

import { useRef } from "react";

import { useRafLoop } from "@/src/components/mocha/useRafLoop";

import {
  BRASS,
  BRASS_DIM,
  BRASS_FAINT,
  EDGE,
  EDGE_SOFT,
  FIELD,
  LAMP_FAULT,
  LAMP_LIVE,
  LAMP_OFF,
  LINES,
  MONO,
  PANEL,
  clamp01,
  ease,
} from "./palette";

/**
 * Composition drawn as the wiring check the board must pass before it goes
 * live. A scanner walks the wiring diagram row by row; a bad wire stops it
 * there, lights the fault lamp and leaves the LIVE lamp dark, and the fixed
 * diagram passes clean.
 *
 * At rest the diagram is stopped on a conflict: two wires passed, one faulted,
 * the rest never checked, fault lamp lit.
 */

const CYCLE = 7000;
const REST_T = 5200;
/** Rest sits inside the faulting half of the loop. */
const REST_LIFE = 0;

const FAULT_ROW = 2;
const FAULTS = ["TYPE CONFLICT", "MISSING FIELD", "INCOMPATIBLE ENUM"];

const SHEET = { x: 26, y: 40, w: 330, h: 230 };
const ROW_Y = [72, 110, 148, 186, 224];
const ROW_H = 30;
const BOARD = { x: 384, y: 40, w: 150, h: 230 };
const SCAN_AT = (i: number) => 500 + i * 620;

interface Frame {
  /** -1 while the row is unchecked, 0 passed, 1 faulted. */
  readonly status: readonly number[];
  readonly scanY: number;
  readonly scanning: boolean;
  readonly fault: string;
  readonly faulted: boolean;
  readonly live: boolean;
}

function frameAt(t: number, life: number): Frame {
  const cycle = Math.floor(life / CYCLE);
  const faulting = cycle % 2 === 0;
  const fault = FAULTS[Math.floor(cycle / 2) % FAULTS.length];
  const stopAt = faulting ? FAULT_ROW : ROW_Y.length - 1;
  const status = ROW_Y.map((_, i) => {
    if (i > stopAt || t < SCAN_AT(i) + 300) return -1;
    return faulting && i === FAULT_ROW ? 1 : 0;
  });
  const reached = clamp01((t - 500) / (SCAN_AT(stopAt) - 200));
  const scanY = ROW_Y[0] + ease(reached) * (ROW_Y[stopAt] - ROW_Y[0]);
  const settled = t > SCAN_AT(stopAt) + 300;
  return {
    status,
    scanY,
    scanning: t < SCAN_AT(stopAt) + 900,
    fault,
    faulted: faulting && settled,
    live: !faulting && settled,
  };
}

const REST = frameAt(REST_T, REST_LIFE);

export function WiringCheck() {
  const root = useRef<HTMLDivElement>(null);
  const scanner = useRef<SVGGElement>(null);
  const statuses = useRef<(SVGTextElement | null)[]>([]);
  const wires = useRef<(SVGPathElement | null)[]>([]);
  const liveLamp = useRef<SVGCircleElement>(null);
  const faultLamp = useRef<SVGCircleElement>(null);
  const verdict = useRef<SVGTextElement>(null);

  useRafLoop(
    root,
    () => {
      const apply = (t: number, life: number) => {
        const f = frameAt(t, life);

        if (scanner.current) {
          scanner.current.setAttribute("transform", `translate(0 ${f.scanY})`);
          scanner.current.style.opacity = f.scanning ? "1" : "0";
        }

        f.status.forEach((state, i) => {
          const status = statuses.current[i];
          if (status) {
            status.textContent =
              state === 1 ? f.fault : state === 0 ? "PASS" : "";
            status.style.fill = state === 1 ? LAMP_FAULT : BRASS_DIM;
          }
          const wire = wires.current[i];
          if (wire) {
            wire.style.stroke =
              state === 1 ? LAMP_FAULT : state === 0 ? LAMP_LIVE : EDGE_SOFT;
            wire.style.opacity = state < 0 ? "0.5" : "1";
          }
        });

        if (liveLamp.current) {
          liveLamp.current.style.fill = f.live ? LAMP_LIVE : LAMP_OFF;
        }
        if (faultLamp.current) {
          faultLamp.current.style.fill = f.faulted ? LAMP_FAULT : LAMP_OFF;
        }
        if (verdict.current) {
          verdict.current.textContent = f.faulted
            ? "BUILD STOPPED"
            : f.live
              ? "READY TO GO LIVE"
              : "CHECKING";
          verdict.current.style.fill = f.faulted ? LAMP_FAULT : BRASS_DIM;
        }
      };

      return {
        frame: (t, _dt, life) => apply(t, life),
        rest: () => apply(REST_T, REST_LIFE),
      };
    },
    { period: CYCLE, threshold: 0.15 },
  );

  return (
    <div ref={root} className="w-full">
      <svg
        viewBox="0 0 560 300"
        className="h-auto w-full"
        role="img"
        aria-label="A wiring diagram checked row by row before the board goes live: passed wires turn green, a bad wire stops the check and lights the fault lamp while the live lamp stays dark."
      >
        <text
          x={SHEET.x}
          y="30"
          fill={BRASS_DIM}
          fontFamily={MONO}
          fontSize="9"
          letterSpacing="2"
        >
          COMPOSITION · WIRING CHECK
        </text>
        <rect
          x={SHEET.x}
          y={SHEET.y}
          width={SHEET.w}
          height={SHEET.h}
          rx="8"
          fill={FIELD}
          stroke={EDGE}
        />

        <g
          ref={scanner}
          transform={`translate(0 ${REST.scanY})`}
          style={{ opacity: REST.scanning ? 1 : 0 }}
        >
          <rect
            x={SHEET.x + 6}
            y="0"
            width={SHEET.w - 12}
            height={ROW_H}
            rx="4"
            fill="rgba(226, 200, 148, 0.08)"
          />
          <path
            d={`M ${SHEET.x + 6} 0 H ${SHEET.x + SHEET.w - 6}`}
            stroke={BRASS_FAINT}
            strokeWidth="1.5"
            fill="none"
          />
        </g>

        {LINES.map((line, i) => (
          <g key={line.name}>
            <text
              x={SHEET.x + 20}
              y={ROW_Y[i] + 20}
              fill={BRASS}
              fontFamily={MONO}
              fontSize="10.5"
              letterSpacing="1"
            >
              {line.name}
            </text>
            <text
              ref={(el) => {
                statuses.current[i] = el;
              }}
              x={SHEET.x + SHEET.w - 20}
              y={ROW_Y[i] + 20}
              fill={REST.status[i] === 1 ? LAMP_FAULT : BRASS_DIM}
              fontFamily={MONO}
              fontSize="8.5"
              letterSpacing="1"
              textAnchor="end"
            >
              {REST.status[i] === 1
                ? REST.fault
                : REST.status[i] === 0
                  ? "PASS"
                  : ""}
            </text>
            <path
              ref={(el) => {
                wires.current[i] = el;
              }}
              d={`M ${SHEET.x + SHEET.w} ${ROW_Y[i] + 15} H ${BOARD.x}`}
              strokeWidth="2"
              fill="none"
              style={{
                stroke:
                  REST.status[i] === 1
                    ? LAMP_FAULT
                    : REST.status[i] === 0
                      ? LAMP_LIVE
                      : EDGE_SOFT,
                opacity: REST.status[i] < 0 ? 0.5 : 1,
              }}
            />
          </g>
        ))}

        <rect
          x={BOARD.x}
          y={BOARD.y}
          width={BOARD.w}
          height={BOARD.h}
          rx="8"
          fill={PANEL}
          stroke={EDGE}
        />
        <text
          x={BOARD.x + BOARD.w / 2}
          y={BOARD.y + 26}
          fill={BRASS}
          fontFamily={MONO}
          fontSize="11"
          letterSpacing="2"
          textAnchor="middle"
        >
          THE BOARD
        </text>
        <circle
          ref={liveLamp}
          cx={BOARD.x + 30}
          cy={BOARD.y + 68}
          r="9"
          fill={REST.live ? LAMP_LIVE : LAMP_OFF}
        />
        <text
          x={BOARD.x + 48}
          y={BOARD.y + 72}
          fill={BRASS_DIM}
          fontFamily={MONO}
          fontSize="9.5"
          letterSpacing="1"
        >
          LIVE
        </text>
        <circle
          ref={faultLamp}
          cx={BOARD.x + 30}
          cy={BOARD.y + 108}
          r="9"
          fill={REST.faulted ? LAMP_FAULT : LAMP_OFF}
        />
        <text
          x={BOARD.x + 48}
          y={BOARD.y + 112}
          fill={BRASS_DIM}
          fontFamily={MONO}
          fontSize="9.5"
          letterSpacing="1"
        >
          FAULT
        </text>
        <text
          ref={verdict}
          x={BOARD.x + BOARD.w / 2}
          y={BOARD.y + 158}
          fontFamily={MONO}
          fontSize="9"
          letterSpacing="1"
          textAnchor="middle"
          style={{ fill: REST.faulted ? LAMP_FAULT : BRASS_DIM }}
        >
          {REST.faulted
            ? "BUILD STOPPED"
            : REST.live
              ? "READY TO GO LIVE"
              : "CHECKING"}
        </text>
        <text
          x={BOARD.x + BOARD.w / 2}
          y={BOARD.y + 190}
          fill={BRASS_FAINT}
          fontFamily={MONO}
          fontSize="7.5"
          letterSpacing="1"
          textAnchor="middle"
        >
          CHECKED IN THE BUILD,
        </text>
        <text
          x={BOARD.x + BOARD.w / 2}
          y={BOARD.y + 202}
          fill={BRASS_FAINT}
          fontFamily={MONO}
          fontSize="7.5"
          letterSpacing="1"
          textAnchor="middle"
        >
          NOT AT RUNTIME
        </text>
      </svg>
    </div>
  );
}
