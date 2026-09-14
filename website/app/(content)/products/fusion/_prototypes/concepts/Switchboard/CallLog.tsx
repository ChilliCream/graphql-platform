"use client";

import { useRef } from "react";

import { useRafLoop } from "@/src/components/mocha/useRafLoop";

import { TYPE } from "../../brand";
import {
  BRASS,
  BRASS_DIM,
  BRASS_FAINT,
  BRASS_WASH,
  EDGE,
  EDGE_SOFT,
  FIELD,
  LAMP_FAULT,
  LAMP_LIVE,
  LAMP_OFF,
  LAMP_ON,
  MONO,
  PANEL,
  PANEL_TOP,
  ease,
} from "./palette";

/**
 * The exchange keeps a call log: every operation its registered clients
 * actually place. A rewiring order arrives, composition passes it green, and
 * the log is run against it row by row - safe, risky or breaking - before the
 * rewiring is merged.
 *
 * At rest the whole log is stamped, with the breaking row and the fault lamp
 * lit next to a green composition chip.
 */

const CYCLE = 8000;
const REST_T = 5600;

type Verdict = "SAFE" | "RISKY" | "BREAKING";

const VERDICT_COLOR: Record<Verdict, string> = {
  SAFE: LAMP_LIVE,
  RISKY: LAMP_ON,
  BREAKING: LAMP_FAULT,
};

interface Entry {
  readonly client: string;
  readonly operation: string;
  readonly verdict: Verdict;
}

const LOG: readonly Entry[] = [
  { client: "WEB", operation: "cart.total", verdict: "SAFE" },
  {
    client: "MOBILE",
    operation: "order.deliveryEstimate",
    verdict: "BREAKING",
  },
  { client: "PARTNER", operation: "invoice.total", verdict: "SAFE" },
  { client: "MOBILE", operation: "order.status", verdict: "RISKY" },
];

const ROW_Y = [118, 152, 186, 220];
const STAMP_AT = (i: number) => 1000 + i * 560;
const LOG_PANEL = { x: 26, y: 100, w: 340, h: 152 };
const SIDE = { x: 390, y: 100, w: 144, h: 152 };

function stamped(i: number, t: number): boolean {
  return t > STAMP_AT(i) + 200;
}

export function CallLog() {
  const root = useRef<HTMLDivElement>(null);
  const head = useRef<SVGGElement>(null);
  const marks = useRef<(SVGTextElement | null)[]>([]);
  const lamps = useRef<(SVGCircleElement | null)[]>([]);
  const faultLamp = useRef<SVGCircleElement>(null);
  const verdictText = useRef<SVGTextElement>(null);

  useRafLoop(
    root,
    () => {
      const apply = (t: number) => {
        const last = STAMP_AT(LOG.length - 1) + 400;
        if (head.current) {
          const travel = ease((t - 800) / (last - 800));
          head.current.setAttribute(
            "transform",
            `translate(0 ${ROW_Y[0] + travel * (ROW_Y[LOG.length - 1] - ROW_Y[0])})`,
          );
          head.current.style.opacity = t > 700 && t < last + 500 ? "1" : "0";
        }

        LOG.forEach((entry, i) => {
          const on = stamped(i, t);
          const mark = marks.current[i];
          if (mark) {
            mark.textContent = on ? entry.verdict : "";
            mark.style.fill = VERDICT_COLOR[entry.verdict];
          }
          const lamp = lamps.current[i];
          if (lamp) {
            lamp.style.fill = on ? VERDICT_COLOR[entry.verdict] : LAMP_OFF;
          }
        });

        const breaking = LOG.findIndex((e) => e.verdict === "BREAKING");
        const caught = stamped(breaking, t);
        if (faultLamp.current) {
          faultLamp.current.style.fill = caught ? LAMP_FAULT : LAMP_OFF;
        }
        if (verdictText.current) {
          verdictText.current.textContent = caught
            ? "1 BREAKING"
            : "MATCHING...";
          verdictText.current.style.fill = caught ? LAMP_FAULT : BRASS_FAINT;
        }
      };

      return {
        frame: (t) => apply(t),
        rest: () => apply(REST_T),
      };
    },
    { period: CYCLE, threshold: 0.15 },
  );

  return (
    <div ref={root} className="w-full">
      <svg
        viewBox="0 0 560 280"
        className="h-auto w-full"
        role="img"
        aria-label="A rewiring order that composes green, run against the exchange's call log of operations from registered clients: rows stamp safe, risky or breaking, and the breaking row lights the fault lamp."
      >
        <rect
          x="26"
          y="26"
          width="508"
          height="52"
          rx="8"
          fill={PANEL_TOP}
          stroke={EDGE}
        />
        <text
          x="44"
          y="46"
          fill={BRASS_DIM}
          fontFamily={MONO}
          fontSize={TYPE.label}
          letterSpacing="2"
        >
          REWIRING ORDER
        </text>
        <text
          x="44"
          y="63"
          fill={BRASS}
          fontFamily={MONO}
          fontSize={TYPE.label}
          letterSpacing="0.5"
        >
          REMOVE Order.deliveryEstimate
        </text>
        <rect
          x="360"
          y="38"
          width="156"
          height="28"
          rx="6"
          fill={FIELD}
          stroke={EDGE_SOFT}
        />
        <circle cx="378" cy="52" r="5" fill={LAMP_LIVE} />
        <text
          x="392"
          y="56"
          fill={BRASS_DIM}
          fontFamily={MONO}
          fontSize={TYPE.label}
          letterSpacing="1"
        >
          COMPOSES CLEAN
        </text>

        <text
          x={LOG_PANEL.x}
          y="94"
          fill={BRASS_FAINT}
          fontFamily={MONO}
          fontSize={TYPE.label}
          letterSpacing="2"
        >
          CALL LOG · REGISTERED CLIENTS
        </text>
        <rect
          x={LOG_PANEL.x}
          y={LOG_PANEL.y}
          width={LOG_PANEL.w}
          height={LOG_PANEL.h}
          rx="8"
          fill={FIELD}
          stroke={EDGE}
        />

        <g
          ref={head}
          transform={`translate(0 ${ROW_Y[LOG.length - 1]})`}
          style={{ opacity: 0 }}
        >
          <rect
            x={LOG_PANEL.x + 6}
            y="-14"
            width={LOG_PANEL.w - 12}
            height="28"
            rx="4"
            fill={BRASS_WASH}
          />
        </g>

        {LOG.map((entry, i) => (
          <g key={`${entry.client}-${entry.operation}`}>
            <circle
              ref={(el) => {
                lamps.current[i] = el;
              }}
              cx={LOG_PANEL.x + 20}
              cy={ROW_Y[i]}
              r="4.5"
              fill={VERDICT_COLOR[entry.verdict]}
            />
            <text
              x={LOG_PANEL.x + 34}
              y={ROW_Y[i] + 4}
              fill={BRASS_FAINT}
              fontFamily={MONO}
              fontSize={TYPE.label}
              letterSpacing="0.5"
            >
              {entry.client}
            </text>
            <text
              x={LOG_PANEL.x + 90}
              y={ROW_Y[i] + 4}
              fill={BRASS}
              fontFamily={MONO}
              fontSize={TYPE.label}
            >
              {entry.operation}
            </text>
            <text
              ref={(el) => {
                marks.current[i] = el;
              }}
              x={LOG_PANEL.x + LOG_PANEL.w - 16}
              y={ROW_Y[i] + 4}
              fontFamily={MONO}
              fontSize={TYPE.label}
              letterSpacing="1"
              textAnchor="end"
              style={{ fill: VERDICT_COLOR[entry.verdict] }}
            >
              {entry.verdict}
            </text>
          </g>
        ))}

        <rect
          x={SIDE.x}
          y={SIDE.y}
          width={SIDE.w}
          height={SIDE.h}
          rx="8"
          fill={PANEL}
          stroke={EDGE}
        />
        <text
          x={SIDE.x + SIDE.w / 2}
          y={SIDE.y + 28}
          fill={BRASS}
          fontFamily={MONO}
          fontSize={TYPE.caption}
          letterSpacing="3"
          textAnchor="middle"
        >
          NITRO
        </text>
        <text
          x={SIDE.x + SIDE.w / 2}
          y={SIDE.y + 44}
          fill={BRASS_FAINT}
          fontFamily={MONO}
          fontSize={TYPE.label}
          letterSpacing="1"
          textAnchor="middle"
        >
          SCHEMA GOVERNANCE
        </text>
        <circle
          ref={faultLamp}
          cx={SIDE.x + 26}
          cy={SIDE.y + 82}
          r="9"
          fill={LAMP_FAULT}
        />
        <text
          ref={verdictText}
          x={SIDE.x + 44}
          y={SIDE.y + 86}
          fontFamily={MONO}
          fontSize={TYPE.label}
          letterSpacing="1"
          style={{ fill: LAMP_FAULT }}
        >
          1 BREAKING
        </text>
        <text
          x={SIDE.x + SIDE.w / 2}
          y={SIDE.y + 122}
          fill={BRASS_FAINT}
          fontFamily={MONO}
          fontSize={TYPE.label}
          letterSpacing="1"
          textAnchor="middle"
        >
          BEFORE THE MERGE
        </text>
      </svg>
    </div>
  );
}
