"use client";

import { useRef } from "react";

import { useRafLoop } from "@/src/components/mocha/useRafLoop";

import {
  BRASS,
  BRASS_DIM,
  BRASS_FAINT,
  CORD,
  EDGE,
  EDGE_SOFT,
  FIELD,
  LAMP_OFF,
  LAMP_ON,
  LINES,
  MONO,
  PANEL,
  clamp01,
  cord,
  ease,
} from "./palette";

/**
 * One call, several lines, one answer home. The caller's query rides the trunk
 * line into the exchange, the operator patches it into only the lines that
 * hold the data, each answer runs back down its own cord, and the pieces stack
 * into a single response that leaves on the same trunk line.
 *
 * At rest the board holds the finished call: three lines patched, the stack
 * merged into one response, and the response card parked at the caller.
 */

const CYCLE = 8000;
const REST_T = 6000;

const TARGETS: readonly (readonly number[])[] = [
  [0, 1, 3],
  [2, 4],
  [0, 1, 2],
];

const REST_TARGETS = TARGETS[0];

const TRUNK_Y = 150;
const CALLER_X = 72;
const HUB = { x: 210, y: 70, w: 140, h: 160 };
const RIGHT_X = 430;
const LINE_Y = [54, 102, 150, 198, 246];
const CORDS = LINE_Y.map((y) =>
  cord(HUB.x + HUB.w, TRUNK_Y, RIGHT_X - 10, y, 18),
);
const STACK_Y = [120, 138, 156];
const MERGE_Y = 186;

export function PatchThrough() {
  const root = useRef<HTMLDivElement>(null);
  const ticket = useRef<SVGGElement>(null);
  const answer = useRef<SVGGElement>(null);
  const cords = useRef<(SVGPathElement | null)[]>([]);
  const sparks = useRef<(SVGPathElement | null)[]>([]);
  const lamps = useRef<(SVGCircleElement | null)[]>([]);
  const names = useRef<(SVGTextElement | null)[]>([]);
  const bars = useRef<(SVGGElement | null)[]>([]);
  const merged = useRef<SVGGElement>(null);

  useRafLoop(
    root,
    () => {
      const apply = (t: number, life: number) => {
        const targets = TARGETS[Math.floor(life / CYCLE) % TARGETS.length];
        const fade = 1 - ease((t - 6800) / 800);

        const inbound = ease(t / 900);
        if (ticket.current) {
          const x = CALLER_X + 24 + inbound * (HUB.x - CALLER_X - 24);
          ticket.current.setAttribute("transform", `translate(${x} 0)`);
          ticket.current.style.opacity = String(
            (inbound < 1 ? 1 : 0) * (t > 100 ? 1 : 0),
          );
        }

        LINE_Y.forEach((_, i) => {
          const slot = targets.indexOf(i);
          const start = 1000 + slot * 260;
          const draw = slot < 0 ? 0 : ease((t - start) / 520);
          const back = slot < 0 ? 0 : ease((t - start - 700) / 700);
          const path = cords.current[i];
          const spark = sparks.current[i];
          const lamp = lamps.current[i];
          const name = names.current[i];
          if (path) {
            path.style.opacity = String(draw * fade);
            path.style.strokeDashoffset = String(1 - draw);
          }
          if (spark) {
            spark.style.opacity = back > 0 && back < 1 ? String(fade) : "0";
            spark.style.strokeDashoffset = String(-(1 - back));
          }
          if (lamp) {
            const on = slot >= 0 && draw > 0;
            lamp.style.fill = on ? LAMP_ON : LAMP_OFF;
            lamp.style.opacity = on ? String(fade) : "1";
          }
          if (name) {
            name.style.opacity = slot >= 0 ? "1" : "0.4";
          }
        });

        const collapse = ease((t - 3600) / 600);
        bars.current.forEach((bar, slot) => {
          if (!bar) return;
          const start = 1000 + slot * 260;
          const landed =
            slot < targets.length ? ease((t - start - 1400) / 300) : 0;
          const y = STACK_Y[slot] + collapse * (MERGE_Y - STACK_Y[slot]);
          bar.setAttribute("transform", `translate(0 ${y - STACK_Y[slot]})`);
          bar.style.opacity = String(landed * (1 - collapse) * fade);
        });
        if (merged.current) {
          merged.current.style.opacity = String(collapse * fade);
        }

        const outbound = ease((t - 4200) / 900);
        if (answer.current) {
          const x = HUB.x - 76 - outbound * (HUB.x - 76 - CALLER_X - 12);
          answer.current.setAttribute("transform", `translate(${x} 0)`);
          answer.current.style.opacity = String(
            clamp01((t - 4200) / 200) * fade,
          );
        }
      };

      return {
        frame: (t, _dt, life) => apply(t, life),
        rest: () => apply(REST_T, 0),
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
        aria-label="One caller's query travels into the gateway, which patches it into only the lines that hold the data and sends one merged response back on the same trunk line."
      >
        <path
          d={`M ${CALLER_X + 10} ${TRUNK_Y} H ${HUB.x}`}
          stroke={CORD[0]}
          strokeWidth="2.5"
          strokeLinecap="round"
          fill="none"
          opacity="0.6"
        />
        <circle cx={CALLER_X} cy={TRUNK_Y} r="10" fill={FIELD} stroke={EDGE} />
        <circle cx={CALLER_X} cy={TRUNK_Y} r="4" fill={PANEL} />
        <text
          x={CALLER_X}
          y={TRUNK_Y - 22}
          fill={BRASS_DIM}
          fontFamily={MONO}
          fontSize="10"
          letterSpacing="1.5"
          textAnchor="middle"
        >
          CLIENT
        </text>

        <rect
          x={HUB.x}
          y={HUB.y}
          width={HUB.w}
          height={HUB.h}
          rx="10"
          fill={FIELD}
          stroke={EDGE}
        />
        <text
          x={HUB.x + HUB.w / 2}
          y={HUB.y + 26}
          fill={BRASS}
          fontFamily={MONO}
          fontSize="12"
          letterSpacing="2"
          textAnchor="middle"
        >
          GATEWAY
        </text>
        <text
          x={HUB.x + HUB.w / 2}
          y={HUB.y + 42}
          fill={BRASS_FAINT}
          fontFamily={MONO}
          fontSize="7.5"
          letterSpacing="1"
          textAnchor="middle"
        >
          ONE ENDPOINT
        </text>

        {STACK_Y.map((y, slot) => (
          <g
            key={y}
            ref={(el) => {
              bars.current[slot] = el;
            }}
            style={{ opacity: 0 }}
          >
            <rect
              x={HUB.x + 22}
              y={y}
              width="96"
              height="12"
              rx="3"
              fill={CORD[slot]}
              opacity="0.45"
            />
          </g>
        ))}
        <g ref={merged} style={{ opacity: 1 }}>
          <rect
            x={HUB.x + 22}
            y={MERGE_Y}
            width="96"
            height="18"
            rx="4"
            fill={PANEL}
            stroke={EDGE}
          />
          <text
            x={HUB.x + 70}
            y={MERGE_Y + 12}
            fill={BRASS}
            fontFamily={MONO}
            fontSize="7.5"
            letterSpacing="1"
            textAnchor="middle"
          >
            ONE RESPONSE
          </text>
        </g>

        {CORDS.map((d, i) => (
          <g key={i}>
            <path
              ref={(el) => {
                cords.current[i] = el;
              }}
              d={d}
              pathLength={1}
              fill="none"
              stroke={CORD[i]}
              strokeWidth="2.5"
              strokeLinecap="round"
              strokeDasharray="1 1"
              style={{
                opacity: REST_TARGETS.includes(i) ? 1 : 0,
                strokeDashoffset: 0,
              }}
            />
            <path
              ref={(el) => {
                sparks.current[i] = el;
              }}
              d={d}
              pathLength={1}
              fill="none"
              stroke={LAMP_ON}
              strokeWidth="3.5"
              strokeLinecap="round"
              strokeDasharray="0.12 1"
              style={{ opacity: 0 }}
            />
          </g>
        ))}

        {LINES.map((line, i) => (
          <g key={line.name}>
            <circle
              cx={RIGHT_X}
              cy={LINE_Y[i]}
              r="9"
              fill={FIELD}
              stroke={EDGE}
            />
            <circle cx={RIGHT_X} cy={LINE_Y[i]} r="3.5" fill={PANEL} />
            <circle
              ref={(el) => {
                lamps.current[i] = el;
              }}
              cx={RIGHT_X}
              cy={LINE_Y[i] - 18}
              r="3.5"
              fill={REST_TARGETS.includes(i) ? LAMP_ON : LAMP_OFF}
            />
            <text
              ref={(el) => {
                names.current[i] = el;
              }}
              x={RIGHT_X + 16}
              y={LINE_Y[i] + 3}
              fill={BRASS}
              fontFamily={MONO}
              fontSize="11"
              letterSpacing="1"
              style={{ opacity: REST_TARGETS.includes(i) ? 1 : 0.4 }}
            >
              {line.name}
            </text>
          </g>
        ))}

        <g ref={ticket} style={{ opacity: 0 }}>
          <rect
            x="0"
            y={TRUNK_Y - 11}
            width="62"
            height="22"
            rx="4"
            fill={PANEL}
            stroke={EDGE}
          />
          <text
            x="31"
            y={TRUNK_Y + 3}
            fill={BRASS}
            fontFamily={MONO}
            fontSize="8"
            letterSpacing="1"
            textAnchor="middle"
          >
            ONE QUERY
          </text>
        </g>

        <g ref={answer} style={{ opacity: 1 }} transform="translate(84 0)">
          <rect
            x="0"
            y={TRUNK_Y - 11}
            width="76"
            height="22"
            rx="4"
            fill={PANEL}
            stroke={EDGE_SOFT}
          />
          <text
            x="38"
            y={TRUNK_Y + 3}
            fill={BRASS_DIM}
            fontFamily={MONO}
            fontSize="8"
            letterSpacing="1"
            textAnchor="middle"
          >
            ONE RESPONSE
          </text>
        </g>
      </svg>
    </div>
  );
}
