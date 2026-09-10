"use client";

import { useRef } from "react";

import { useRafLoop } from "@/src/components/mocha/useRafLoop";

import {
  BRASS,
  BRASS_DIM,
  BRASS_FAINT,
  CALLERS,
  CORD,
  EDGE,
  EDGE_SOFT,
  FIELD,
  LAMP_OFF,
  LAMP_ON,
  LINES,
  MONO,
  PANEL,
  PANEL_TOP,
  SOURCES,
  SPEC_TAG,
  clamp01,
  cord,
  ease,
} from "./palette";

/**
 * The hero board: three caller lines on the left, one exchange in the middle,
 * the five subgraph lines and the two adapter jacks on the right. The operator
 * patches one incoming call into several service lines at once, the answers
 * run back down the cords into one line home, and the rotary counter of active
 * calls clicks over.
 *
 * At rest (reduced motion, and the server render) the board sits mid-call:
 * one caller patched through to Catalog, Billing and Shipping, lamps lit,
 * counter reading 088.
 */

const CYCLE = 9000;
const REST_T = 6000;

interface Call {
  /** Index into CALLERS. */
  readonly caller: number;
  /** Indices into RIGHT_Y: the jacks the operator patches this call into. */
  readonly targets: readonly number[];
}

/** Which caller is on the board this cycle, and which right-hand jacks answer. */
const CALLS: readonly Call[] = [
  { caller: 0, targets: [0, 1, 3] },
  { caller: 1, targets: [2, 4] },
  { caller: 2, targets: [0, 2, 5] },
  { caller: 1, targets: [1, 6] },
];

/** The call the board rests on, server-rendered and under reduced motion. */
const REST_CALL = CALLS[0];

const CALLER_Y = [150, 232, 314];
const CALLER_X = 60;
const PORT_IN = { x: 212, y: 236 };
const PORT_OUT = { x: 348, y: 236 };
const RIGHT_X = 430;
const LINE_Y = [100, 152, 204, 256, 308];
const SOURCE_Y = [374, 418];

/** Right-hand jacks: the five subgraphs, then the two adapters. */
const RIGHT_Y = [...LINE_Y, ...SOURCE_Y];
const RIGHT_JACKS = [
  ...LINES.map((line) => ({
    name: line.name,
    tag: `${line.lang} · ${SPEC_TAG[line.spec]}`,
    adapter: false,
  })),
  ...SOURCES.map((source) => ({
    name: source.name,
    tag: source.kind as string,
    adapter: true,
  })),
];

const CALLER_CORDS = CALLER_Y.map((y) =>
  cord(CALLER_X + 10, y, PORT_IN.x, PORT_IN.y, 34),
);
const RIGHT_CORDS = RIGHT_Y.map((y) =>
  cord(PORT_OUT.x, PORT_OUT.y, RIGHT_X - 10, y, 24),
);

const DRUM_H = 26;
const DRUM_DIGITS = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0];

/** Odometer position of decade `k` for a continuous counter value. */
function digitPos(value: number, k: number): number {
  const scale = 10 ** k;
  const whole = Math.floor(value / scale) % 10;
  const rest = value % scale;
  return whole + (rest > scale - 1 ? rest - (scale - 1) : 0);
}

interface JackProps {
  readonly cx: number;
  readonly cy: number;
  /** Adapter jacks are ringed with a dashed collar. */
  readonly dashed?: boolean;
}

/** One jack in the field: brass ring, dark core. */
function Jack({ cx, cy, dashed }: JackProps) {
  return (
    <>
      <circle
        cx={cx}
        cy={cy}
        r="10"
        fill={FIELD}
        stroke={dashed ? EDGE_SOFT : EDGE}
        strokeDasharray={dashed ? "3 3" : undefined}
      />
      <circle cx={cx} cy={cy} r="4" fill={PANEL} stroke={EDGE_SOFT} />
    </>
  );
}

function drumTransform(value: number, k: number): string {
  return `translate(0 ${-digitPos(value, k) * DRUM_H})`;
}

export function HeroBoard() {
  const root = useRef<HTMLDivElement>(null);
  const callerCords = useRef<(SVGPathElement | null)[]>([]);
  const callerSparks = useRef<(SVGPathElement | null)[]>([]);
  const callerLamps = useRef<(SVGCircleElement | null)[]>([]);
  const rightCords = useRef<(SVGPathElement | null)[]>([]);
  const rightSparks = useRef<(SVGPathElement | null)[]>([]);
  const rightLamps = useRef<(SVGCircleElement | null)[]>([]);
  const busLamps = useRef<(SVGCircleElement | null)[]>([]);
  const drums = useRef<(SVGGElement | null)[]>([]);

  useRafLoop(
    root,
    () => {
      const apply = (t: number, life: number) => {
        const index = Math.floor(life / CYCLE) % CALLS.length;
        const call = CALLS[index];
        const fade = 1 - ease((t - 7200) / 800);

        const inDraw = ease((t - 500) / 800);
        const inSpark = ease((t - 3700) / 800);

        callerCords.current.forEach((path, i) => {
          if (!path) return;
          const live = i === call.caller;
          path.style.opacity = live ? String(inDraw * fade) : "0";
          path.style.strokeDashoffset = String(1 - inDraw);
        });
        callerSparks.current.forEach((path, i) => {
          if (!path) return;
          const live = i === call.caller && inSpark > 0 && inSpark < 1;
          path.style.opacity = live ? String(fade) : "0";
          path.style.strokeDashoffset = String(-(1 - inSpark));
        });
        callerLamps.current.forEach((lamp, i) => {
          if (!lamp) return;
          const ringing = t < 600 && Math.floor(t / 150) % 2 === 0;
          const on = i === call.caller && (ringing || t > 500);
          lamp.style.fill = on ? LAMP_ON : LAMP_OFF;
          lamp.style.opacity = on ? String(fade) : "1";
        });

        RIGHT_Y.forEach((_, i) => {
          const slot = call.targets.indexOf(i);
          const path = rightCords.current[i];
          const spark = rightSparks.current[i];
          const lamp = rightLamps.current[i];
          const start = 1400 + slot * 220;
          const draw = slot < 0 ? 0 : ease((t - start) / 700);
          const back = slot < 0 ? 0 : ease((t - start - 1200) / 900);
          if (path) {
            path.style.opacity = String(draw * fade);
            path.style.strokeDashoffset = String(1 - draw);
          }
          if (spark) {
            const live = back > 0 && back < 1;
            spark.style.opacity = live ? String(fade) : "0";
            spark.style.strokeDashoffset = String(-(1 - back));
          }
          if (lamp) {
            const on = draw >= 1;
            lamp.style.fill = on ? LAMP_ON : LAMP_OFF;
            lamp.style.opacity = on ? String(fade) : "1";
          }
        });

        busLamps.current.forEach((lamp, i) => {
          if (!lamp) return;
          const lit = i < call.targets.length && inDraw >= 1;
          lamp.style.fill = lit ? LAMP_ON : LAMP_OFF;
          lamp.style.opacity = lit ? String(fade) : "1";
        });

        const value = 87 + Math.floor(life / CYCLE) + clamp01((t - 3700) / 500);
        drums.current.forEach((drum, k) => {
          if (!drum) return;
          drum.setAttribute("transform", drumTransform(value, 2 - k));
        });
      };

      return {
        frame: (t, _dt, life) => apply(t, life),
        rest: () => apply(REST_T, 0),
      };
    },
    { period: CYCLE, threshold: 0.12 },
  );

  return (
    <div ref={root} className="w-full">
      <svg
        viewBox="0 0 560 470"
        className="h-auto w-full"
        role="img"
        aria-label="A telephone exchange board: caller lines on the left patched through one gateway into the Catalog, Billing, Ordering, Shipping and Accounts lines, with OpenAPI and gRPC adapter jacks below them."
      >
        <rect
          x="6"
          y="6"
          width="548"
          height="458"
          rx="16"
          fill={PANEL}
          stroke={EDGE}
        />
        <path d="M 6 60 H 554" stroke={EDGE_SOFT} fill="none" strokeWidth="1" />
        <path
          d="M 22 60 V 452 H 538"
          stroke={EDGE_SOFT}
          fill="none"
          strokeWidth="1"
        />
        <path
          d="M 6 22 A 16 16 0 0 1 22 6 H 538 A 16 16 0 0 1 554 22 V 60 H 6 Z"
          fill={PANEL_TOP}
        />

        <text
          x="26"
          y="32"
          fill={BRASS}
          fontFamily={MONO}
          fontSize="15"
          letterSpacing="4"
        >
          FUSION EXCHANGE
        </text>
        <text
          x="26"
          y="48"
          fill={BRASS_FAINT}
          fontFamily={MONO}
          fontSize="9"
          letterSpacing="2"
        >
          ONE COMPOSITE SCHEMA
        </text>

        <rect
          x="394"
          y="14"
          width="140"
          height="38"
          rx="6"
          fill={FIELD}
          stroke={EDGE_SOFT}
        />
        <text
          x="402"
          y="30"
          fill={BRASS_FAINT}
          fontFamily={MONO}
          fontSize="7.5"
        >
          ACTIVE
        </text>
        <text
          x="402"
          y="42"
          fill={BRASS_FAINT}
          fontFamily={MONO}
          fontSize="7.5"
        >
          CALLS
        </text>
        <clipPath id="sw-hero-drum">
          <rect x="440" y="17" width="88" height="32" />
        </clipPath>
        <g clipPath="url(#sw-hero-drum)">
          {[0, 1, 2].map((k) => (
            <g
              key={k}
              ref={(el) => {
                drums.current[k] = el;
              }}
              transform={drumTransform(88, 2 - k)}
            >
              {DRUM_DIGITS.map((d, i) => (
                <text
                  key={i}
                  x={470 + k * 26}
                  y={41 + i * DRUM_H}
                  fill={BRASS}
                  fontFamily={MONO}
                  fontSize="19"
                  textAnchor="middle"
                >
                  {d}
                </text>
              ))}
            </g>
          ))}
        </g>

        <rect
          x={PORT_IN.x}
          y="146"
          width={PORT_OUT.x - PORT_IN.x}
          height="180"
          rx="10"
          fill={FIELD}
          stroke={EDGE}
        />
        <text
          x="280"
          y="184"
          fill={BRASS}
          fontFamily={MONO}
          fontSize="14"
          letterSpacing="2"
          textAnchor="middle"
        >
          GATEWAY
        </text>
        <text
          x="280"
          y="202"
          fill={BRASS_FAINT}
          fontFamily={MONO}
          fontSize="8.5"
          textAnchor="middle"
        >
          DISTRIBUTED
        </text>
        <text
          x="280"
          y="213"
          fill={BRASS_FAINT}
          fontFamily={MONO}
          fontSize="8.5"
          textAnchor="middle"
        >
          EXECUTOR
        </text>
        {[0, 1, 2, 3, 4].map((i) => (
          <circle
            key={i}
            ref={(el) => {
              busLamps.current[i] = el;
            }}
            cx={240 + i * 20}
            cy="268"
            r="4"
            fill={i < REST_CALL.targets.length ? LAMP_ON : LAMP_OFF}
          />
        ))}
        <text
          x="280"
          y="304"
          fill={BRASS_FAINT}
          fontFamily={MONO}
          fontSize="8"
          letterSpacing="1"
          textAnchor="middle"
        >
          ONE ENDPOINT
        </text>
        <circle
          cx={PORT_IN.x}
          cy={PORT_IN.y}
          r="6"
          fill={PANEL}
          stroke={EDGE}
        />
        <circle
          cx={PORT_OUT.x}
          cy={PORT_OUT.y}
          r="6"
          fill={PANEL}
          stroke={EDGE}
        />

        {CALLER_CORDS.map((d, i) => (
          <g key={i}>
            <path
              ref={(el) => {
                callerCords.current[i] = el;
              }}
              d={d}
              pathLength={1}
              fill="none"
              stroke={CORD[0]}
              strokeWidth="2.5"
              strokeLinecap="round"
              strokeDasharray="1 1"
              style={{
                opacity: i === REST_CALL.caller ? 1 : 0,
                strokeDashoffset: 0,
              }}
            />
            <path
              ref={(el) => {
                callerSparks.current[i] = el;
              }}
              d={d}
              pathLength={1}
              fill="none"
              stroke={LAMP_ON}
              strokeWidth="3.5"
              strokeLinecap="round"
              strokeDasharray="0.1 1"
              style={{ opacity: 0 }}
            />
          </g>
        ))}

        {RIGHT_CORDS.map((d, i) => (
          <g key={i}>
            <path
              ref={(el) => {
                rightCords.current[i] = el;
              }}
              d={d}
              pathLength={1}
              fill="none"
              stroke={CORD[i]}
              strokeWidth="2.5"
              strokeLinecap="round"
              strokeDasharray="1 1"
              style={{
                opacity: REST_CALL.targets.includes(i) ? 1 : 0,
                strokeDashoffset: 0,
              }}
            />
            <path
              ref={(el) => {
                rightSparks.current[i] = el;
              }}
              d={d}
              pathLength={1}
              fill="none"
              stroke={LAMP_ON}
              strokeWidth="3.5"
              strokeLinecap="round"
              strokeDasharray="0.1 1"
              style={{ opacity: 0 }}
            />
          </g>
        ))}

        {CALLERS.map((name, i) => (
          <g key={name}>
            <text
              x={CALLER_X}
              y={CALLER_Y[i] - 20}
              fill={BRASS_DIM}
              fontFamily={MONO}
              fontSize="10"
              letterSpacing="1.5"
              textAnchor="middle"
            >
              {name}
            </text>
            <Jack cx={CALLER_X} cy={CALLER_Y[i]} />
            <circle
              ref={(el) => {
                callerLamps.current[i] = el;
              }}
              cx={CALLER_X}
              cy={CALLER_Y[i] + 22}
              r="4"
              fill={i === REST_CALL.caller ? LAMP_ON : LAMP_OFF}
            />
          </g>
        ))}

        <path
          d="M 380 346 H 538"
          stroke={EDGE_SOFT}
          fill="none"
          strokeDasharray="3 4"
        />
        <text
          x="380"
          y="362"
          fill={BRASS_FAINT}
          fontFamily={MONO}
          fontSize="8"
          letterSpacing="1"
        >
          VIA ADAPTER
        </text>

        {RIGHT_JACKS.map((jack, i) => (
          <g key={jack.name}>
            <Jack cx={RIGHT_X} cy={RIGHT_Y[i]} dashed={jack.adapter} />
            <circle
              ref={(el) => {
                rightLamps.current[i] = el;
              }}
              cx={RIGHT_X}
              cy={RIGHT_Y[i] - 20}
              r="4"
              fill={REST_CALL.targets.includes(i) ? LAMP_ON : LAMP_OFF}
            />
            <text
              x={RIGHT_X + 18}
              y={RIGHT_Y[i] - 1}
              fill={BRASS}
              fontFamily={MONO}
              fontSize="11"
              letterSpacing="1"
            >
              {jack.name}
            </text>
            <text
              x={RIGHT_X + 18}
              y={RIGHT_Y[i] + 11}
              fill={BRASS_FAINT}
              fontFamily={MONO}
              fontSize="8"
            >
              {jack.tag}
            </text>
          </g>
        ))}
      </svg>
    </div>
  );
}
