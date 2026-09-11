"use client";

import { useRef } from "react";

import { useRafLoop } from "@/src/components/mocha/useRafLoop";

import { TYPE } from "../../brand";
import {
  BRASS,
  BRASS_DIM,
  BRASS_FAINT,
  CORD,
  EDGE,
  EDGE_SOFT,
  FIELD,
  LAMP_ON,
  LINES,
  MONO,
  PANEL,
  PANEL_TOP,
  ease,
} from "./palette";

/**
 * The rack of line cards behind the jack strip. Every line is an ordinary
 * card: the language on it changes, the jack it plugs into does not, and the
 * plug-in bay stamped for a vendor module stays empty on every card.
 *
 * One card at a time slides out and comes back in another language while its
 * lamp stays lit. At rest the rack is full, with the Catalog card already
 * swapped.
 */

const CYCLE = 6000;
const REST_T = 4200;
const TRAVEL = 460;

const LANGS = ["JS/TS", "GO", "C#", "JAVA", "PYTHON", "RUBY"];

/** The top card runs a pool without C#, so the rack never leads with it. */
const TOP_LANGS = LANGS.filter((lang) => lang !== "C#");

/**
 * The language on line `i` at cycle `c`, time `t`. Lines swap one per cycle in
 * rack order and every swap steps one place down that line's pool, so the rack
 * is stateless: the same inputs always draw the same frame.
 */
function langOf(i: number, c: number, t: number): string {
  const pool = i === 0 ? TOP_LANGS : LANGS;
  const rot = Math.floor(c / LINES.length);
  const turn = c % LINES.length;
  const swaps = i < turn ? 1 : i === turn && t > 900 ? 1 : 0;
  const steps = ((i - rot - swaps) % pool.length) + pool.length;
  return pool[steps % pool.length];
}

const ROW_Y = [50, 96, 142, 188, 234];
const CARD_X = 48;
const CARD_W = 372;
const CARD_H = 38;
const JACK_X = 486;

export function LineCards() {
  const root = useRef<HTMLDivElement>(null);
  const cards = useRef<(SVGGElement | null)[]>([]);
  const langs = useRef<(SVGTextElement | null)[]>([]);
  const lamps = useRef<(SVGCircleElement | null)[]>([]);

  useRafLoop(
    root,
    () => {
      const apply = (t: number, life: number) => {
        const c = Math.floor(life / CYCLE);
        const turn = c % LINES.length;

        LINES.forEach((_, i) => {
          const moving = i === turn;
          const out = moving ? ease((t - 200) / 700) : 0;
          const back = moving ? ease((t - 1000) / 700) : 0;
          const card = cards.current[i];
          if (card) {
            card.setAttribute(
              "transform",
              `translate(${(out - back) * TRAVEL} 0)`,
            );
          }
          const lang = langs.current[i];
          if (lang) {
            lang.textContent = langOf(i, c, t);
          }
          const lamp = lamps.current[i];
          if (lamp) {
            const seat = moving ? ease((t - 1700) / 400) : 0;
            lamp.setAttribute("r", String(4 + seat * (1 - seat) * 6));
            lamp.style.fill = LAMP_ON;
          }
        });
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
        aria-label="A rack of line cards, one per subgraph, each showing its language and an empty bay stamped no extra module; one card slides out and returns in another language while its lamp stays lit."
      >
        <rect
          x="24"
          y="30"
          width="512"
          height="252"
          rx="12"
          fill={PANEL}
          stroke={EDGE}
        />
        <text
          x="40"
          y="22"
          fill={BRASS_DIM}
          fontFamily={MONO}
          fontSize={TYPE.label}
          letterSpacing="2"
        >
          LINE CARDS
        </text>
        <text
          x="520"
          y="22"
          fill={BRASS_FAINT}
          fontFamily={MONO}
          fontSize={TYPE.label}
          letterSpacing="2"
          textAnchor="end"
        >
          SAME JACK, ANY LANGUAGE
        </text>

        <clipPath id="sw-rack-clip">
          <rect x="34" y="36" width="428" height="240" />
        </clipPath>

        {LINES.map((line, i) => (
          <g key={line.name}>
            <rect
              x={CARD_X - 8}
              y={ROW_Y[i] - 4}
              width={CARD_W + 16}
              height={CARD_H + 8}
              rx="6"
              fill={FIELD}
              stroke={EDGE_SOFT}
            />
            <g clipPath="url(#sw-rack-clip)">
              <g
                ref={(el) => {
                  cards.current[i] = el;
                }}
              >
                <rect
                  x={CARD_X}
                  y={ROW_Y[i]}
                  width={CARD_W}
                  height={CARD_H}
                  rx="4"
                  fill={PANEL_TOP}
                  stroke={EDGE}
                />
                <rect
                  x={CARD_X}
                  y={ROW_Y[i]}
                  width="4"
                  height={CARD_H}
                  fill={CORD[i]}
                />
                <text
                  x={CARD_X + 16}
                  y={ROW_Y[i] + 24}
                  fill={BRASS}
                  fontFamily={MONO}
                  fontSize={TYPE.label}
                  letterSpacing="1"
                >
                  {line.name}
                </text>
                <rect
                  x={CARD_X + 112}
                  y={ROW_Y[i] + 10}
                  width="62"
                  height="19"
                  rx="4"
                  fill={FIELD}
                  stroke={EDGE_SOFT}
                />
                <text
                  ref={(el) => {
                    langs.current[i] = el;
                  }}
                  x={CARD_X + 143}
                  y={ROW_Y[i] + 23}
                  fill={BRASS}
                  fontFamily={MONO}
                  fontSize={TYPE.label}
                  textAnchor="middle"
                >
                  {langOf(i, 0, REST_T)}
                </text>
                <rect
                  x={CARD_X + 190}
                  y={ROW_Y[i] + 10}
                  width="166"
                  height="19"
                  rx="4"
                  fill="none"
                  stroke={EDGE_SOFT}
                  strokeDasharray="4 4"
                />
                <text
                  x={CARD_X + 273}
                  y={ROW_Y[i] + 23}
                  fill={BRASS_FAINT}
                  fontFamily={MONO}
                  fontSize={TYPE.label}
                  letterSpacing="1"
                  textAnchor="middle"
                >
                  BAY EMPTY
                </text>
              </g>
            </g>

            <path
              d={`M ${CARD_X + CARD_W + 12} ${ROW_Y[i] + CARD_H / 2} H ${JACK_X - 12}`}
              stroke={CORD[i]}
              strokeWidth="2.5"
              strokeLinecap="round"
              fill="none"
              opacity="0.7"
            />
            <circle
              cx={JACK_X}
              cy={ROW_Y[i] + CARD_H / 2}
              r="10"
              fill={FIELD}
              stroke={EDGE}
            />
            <circle
              ref={(el) => {
                lamps.current[i] = el;
              }}
              cx={JACK_X}
              cy={ROW_Y[i] + CARD_H / 2}
              r="4"
              fill={LAMP_ON}
            />
          </g>
        ))}
      </svg>
    </div>
  );
}
