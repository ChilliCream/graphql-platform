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
  MONO,
  PANEL,
  PANEL_TOP,
  ease,
} from "./palette";

/**
 * Two plug standards, one jack strip. Every plug - two-ring for the GraphQL
 * Federation specification, three-ring for Apollo Federation - seats in the
 * same jack, and the OpenAPI and gRPC sources reach it through a dashed
 * adapter sleeve. Mid-cycle one line is unplugged and re-plugged to the other
 * standard while its lamp never goes out.
 *
 * At rest the strip is fully seated: both standards side by side and both
 * adapters in place.
 */

const CYCLE = 9000;
const REST_T = 3000;

interface Jack {
  readonly name: string;
  readonly tag: string;
  /** Rings on the plug tip: 2 for GraphQL Federation, 3 for Apollo Federation. */
  readonly rings: 2 | 3;
  /** A source that is not a GraphQL server, patched through an adapter sleeve. */
  readonly adapter?: boolean;
}

const JACKS: readonly Jack[] = [
  { name: "CATALOG", tag: "GQL FED", rings: 2 },
  { name: "BILLING", tag: "APOLLO FED", rings: 3 },
  { name: "ORDERING", tag: "GQL FED", rings: 2 },
  { name: "SHIPPING", tag: "APOLLO FED", rings: 3 },
  { name: "PAYMENTS", tag: "OPENAPI", rings: 2, adapter: true },
  { name: "INVENTORY", tag: "GRPC", rings: 3, adapter: true },
];

/** The four GraphQL lines can move between standards; the adapters cannot. */
const SWAPPABLE = 4;
const OTHER_TAG: Record<string, string> = {
  "GQL FED": "APOLLO FED",
  "APOLLO FED": "GQL FED",
};

const COL_X = [70, 154, 238, 322, 406, 490];
const JACK_Y = 214;
const PLUG_TOP = 132;
const LIFT = 74;

export function PlugStandards() {
  const root = useRef<HTMLDivElement>(null);
  const plugs = useRef<(SVGGElement | null)[]>([]);
  const tips2 = useRef<(SVGGElement | null)[]>([]);
  const tips3 = useRef<(SVGGElement | null)[]>([]);
  const lamps = useRef<(SVGCircleElement | null)[]>([]);
  const tags = useRef<(SVGTextElement | null)[]>([]);

  useRafLoop(
    root,
    () => {
      const apply = (t: number, life: number) => {
        const swap = Math.floor(life / CYCLE) % SWAPPABLE;

        JACKS.forEach((jack, i) => {
          const seat = ease((t - i * 200) / 700);
          const swapped = i === swap;
          const out = swapped ? ease((t - 3400) / 700) : 0;
          const back = swapped ? ease((t - 4400) / 700) : 0;
          const lifted = out - back;
          const y = (1 - seat) * -LIFT - lifted * LIFT;

          const plug = plugs.current[i];
          if (plug) {
            plug.setAttribute("transform", `translate(0 ${y})`);
          }

          const lamp = lamps.current[i];
          if (lamp) {
            // The line keeps serving across the move: the lamp only waits for
            // the very first seating of the cycle.
            lamp.style.fill = seat > 0.6 ? LAMP_ON : LAMP_OFF;
          }

          if (swapped) {
            const flipped = t > 4000;
            const rings = flipped ? (jack.rings === 2 ? 3 : 2) : jack.rings;
            const tip2 = tips2.current[i];
            const tip3 = tips3.current[i];
            if (tip2) tip2.style.opacity = rings === 2 ? "1" : "0";
            if (tip3) tip3.style.opacity = rings === 3 ? "1" : "0";
            const tag = tags.current[i];
            if (tag) {
              tag.textContent = flipped ? OTHER_TAG[jack.tag] : jack.tag;
            }
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
        aria-label="One jack strip taking both plug standards: two-ring GraphQL Federation plugs and three-ring Apollo Federation plugs seat in the same jacks, with OpenAPI and gRPC sources plugged in through adapter sleeves."
      >
        <clipPath id="sw-plugs-clip">
          <rect x="0" y="24" width="560" height="254" />
        </clipPath>

        <text
          x="30"
          y="40"
          fill={BRASS_DIM}
          fontFamily={MONO}
          fontSize="9"
          letterSpacing="2"
        >
          ONE GATEWAY
        </text>
        <text
          x="530"
          y="40"
          fill={BRASS_FAINT}
          fontFamily={MONO}
          fontSize="9"
          letterSpacing="2"
          textAnchor="end"
        >
          ONE COMPOSITE SCHEMA
        </text>

        <g clipPath="url(#sw-plugs-clip)">
          {JACKS.map((jack, i) => (
            <g
              key={jack.name}
              ref={(el) => {
                plugs.current[i] = el;
              }}
            >
              <path
                d={`M ${COL_X[i]} ${PLUG_TOP} V -120`}
                stroke={CORD[i]}
                strokeWidth="3"
                strokeLinecap="round"
                fill="none"
                strokeDasharray={jack.adapter ? "6 5" : undefined}
              />
              <rect
                x={COL_X[i] - 14}
                y={PLUG_TOP}
                width="28"
                height="44"
                rx="5"
                fill={PANEL_TOP}
                stroke={EDGE}
              />
              <g
                ref={(el) => {
                  tips2.current[i] = el;
                }}
                style={{ opacity: jack.rings === 2 ? 1 : 0 }}
              >
                <rect
                  x={COL_X[i] - 4}
                  y={PLUG_TOP + 44}
                  width="8"
                  height="28"
                  rx="2"
                  fill={BRASS_DIM}
                />
                <rect
                  x={COL_X[i] - 5}
                  y={PLUG_TOP + 50}
                  width="10"
                  height="3"
                  fill={PANEL}
                />
                <rect
                  x={COL_X[i] - 5}
                  y={PLUG_TOP + 58}
                  width="10"
                  height="3"
                  fill={PANEL}
                />
              </g>
              <g
                ref={(el) => {
                  tips3.current[i] = el;
                }}
                style={{ opacity: jack.rings === 3 ? 1 : 0 }}
              >
                <rect
                  x={COL_X[i] - 4}
                  y={PLUG_TOP + 44}
                  width="8"
                  height="28"
                  rx="2"
                  fill={BRASS_DIM}
                />
                <rect
                  x={COL_X[i] - 5}
                  y={PLUG_TOP + 48}
                  width="10"
                  height="3"
                  fill={PANEL}
                />
                <rect
                  x={COL_X[i] - 5}
                  y={PLUG_TOP + 55}
                  width="10"
                  height="3"
                  fill={PANEL}
                />
                <rect
                  x={COL_X[i] - 5}
                  y={PLUG_TOP + 62}
                  width="10"
                  height="3"
                  fill={PANEL}
                />
              </g>
            </g>
          ))}
        </g>

        <rect
          x="30"
          y="196"
          width="500"
          height="80"
          rx="10"
          fill={FIELD}
          stroke={EDGE}
        />
        {JACKS.map((jack, i) => (
          <g key={jack.name}>
            <circle
              cx={COL_X[i]}
              cy={JACK_Y}
              r="11"
              fill={PANEL}
              stroke={EDGE}
            />
            <circle
              cx={COL_X[i]}
              cy={JACK_Y}
              r="5"
              fill={FIELD}
              stroke={EDGE_SOFT}
            />
            <text
              x={COL_X[i]}
              y={JACK_Y + 32}
              fill={BRASS}
              fontFamily={MONO}
              fontSize="9.5"
              letterSpacing="0.5"
              textAnchor="middle"
            >
              {jack.name}
            </text>
            <text
              ref={(el) => {
                tags.current[i] = el;
              }}
              x={COL_X[i]}
              y={JACK_Y + 44}
              fill={BRASS_FAINT}
              fontFamily={MONO}
              fontSize="7.5"
              textAnchor="middle"
            >
              {jack.tag}
            </text>
            <circle
              ref={(el) => {
                lamps.current[i] = el;
              }}
              cx={COL_X[i]}
              cy={JACK_Y + 56}
              r="4"
              fill={LAMP_ON}
            />
          </g>
        ))}
      </svg>
    </div>
  );
}
