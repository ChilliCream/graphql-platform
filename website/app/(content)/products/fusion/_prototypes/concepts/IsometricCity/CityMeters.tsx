"use client";

import type { CSSProperties } from "react";

import { FONTS } from "../../brand";
import { useCityMotion } from "./hooks";
import {
  CITY,
  LABEL,
  METER_FONT,
  METER_H,
  METER_W,
  box,
  iso,
  tile,
} from "./palette";

/**
 * Nitro band: the city's traffic desk. Every road out of the plaza carries a
 * pulse of the traffic really running on it, and the desk beside the map reads
 * latency, throughput and error rate for the gateway and for each subgraph
 * behind it.
 *
 * Rest state: the map with its roads lit and every meter standing at its last
 * reading, so the still frame is a full dashboard rather than an empty one.
 *
 * Every line is set in `METER_FONT` units, so the smallest one still reads at
 * the site's 11px label on a 375px screen. At that size the desk runs the full
 * width and names every road itself, gateway first, so the plan above it
 * carries no signage of its own - only the beacon over the plaza.
 */

const CSS = `
.ic-meters[data-run="false"] [class*="ic-m-"] { animation: none; }
.ic-meters .ic-m-pulse { animation: ic-m-pulse 3.2s linear infinite; }
.ic-meters .ic-m-bar { animation: ic-m-bar 6s ease-in-out infinite; transform-box: fill-box; transform-origin: left center; }
.ic-meters .ic-m-beacon { animation: ic-m-beacon 4s ease-in-out infinite; }
@keyframes ic-m-pulse { to { stroke-dashoffset: -44; } }
@keyframes ic-m-bar {
  0%, 100% { transform: scaleX(1); }
  50% { transform: scaleX(var(--swing)); }
}
@keyframes ic-m-beacon {
  0%, 100% { opacity: 0.5; }
  50% { opacity: 1; }
}
`;

/** The city plan sits above the desk, drawn small enough to leave room for it. */
const MAP_SCALE = 0.28;
const ORIGIN = `translate(${METER_W / 2}, 12) scale(${MAP_SCALE})`;
const SIZE = 1.8;
/** The desk: a full-width panel under the plan. */
const DESK = { x: 10, y: 110, w: METER_W - 20, h: METER_H - 120 } as const;
/** Left edge of the three meter columns, and their pitch. */
const COLUMN = { x: 165, pitch: 178, w: 150 } as const;
const PLAZA = box(2.2, 2.2, 2, 2, 32);
const PLAZA_DOOR = iso(3.2, 4.2);

interface Road {
  readonly name: string;
  readonly cell: readonly [number, number];
  readonly height: number;
  /** Latency, throughput and error-rate readings, in bar fractions. */
  readonly meters: readonly [number, number, number];
  readonly swing: number;
}

const ROADS: readonly Road[] = [
  {
    name: "Catalog",
    cell: [0, 0],
    meters: [0.42, 0.78, 0.08],
    height: 40,
    swing: 1.12,
  },
  {
    name: "Billing",
    cell: [6, 0],
    meters: [0.61, 0.44, 0.05],
    height: 34,
    swing: 0.9,
  },
  {
    name: "Ordering",
    cell: [6, 6],
    meters: [0.35, 0.86, 0.12],
    height: 38,
    swing: 1.08,
  },
  {
    name: "Shipping",
    cell: [0, 6],
    meters: [0.54, 0.52, 0.04],
    height: 30,
    swing: 0.94,
  },
  {
    name: "Accounts",
    cell: [3, 8.4],
    meters: [0.28, 0.66, 0.02],
    height: 36,
    swing: 1.06,
  },
];

const METER_LABELS = ["latency", "throughput", "error rate"] as const;

interface MeterRowProps {
  readonly label: string;
  readonly meters: readonly [number, number, number];
  readonly swing: number;
  readonly y: number;
  readonly gateway: boolean;
}

function MeterRow({ label, meters, swing, y, gateway }: MeterRowProps) {
  return (
    <g>
      <text
        x={24}
        y={y + 4}
        fill={gateway ? CITY.heading : CITY.ink}
        fontFamily={FONTS.heading}
        fontSize={METER_FONT.caption}
      >
        {label}
      </text>
      {meters.map((value, i) => (
        <g
          key={METER_LABELS[i]}
          transform={`translate(${COLUMN.x + i * COLUMN.pitch}, ${y - 8})`}
        >
          <rect width={COLUMN.w} height={12} rx={6} fill={CITY.road} />
          <rect
            className="ic-m-bar"
            width={Math.max(12, Math.round(value * COLUMN.w))}
            height={12}
            rx={6}
            fill={i === 2 ? CITY.warn : gateway ? CITY.accent : CITY.ok}
            style={
              {
                "--swing": swing,
                animationDelay: `${i * 0.4}s`,
              } as CSSProperties
            }
          />
        </g>
      ))}
    </g>
  );
}

export function CityMeters() {
  const run = useCityMotion();

  return (
    <div
      className="ic-meters absolute inset-0"
      data-run={run ? "true" : "false"}
    >
      <style>{CSS}</style>
      <svg
        viewBox={`0 0 ${METER_W} ${METER_H}`}
        className="h-full w-full"
        aria-hidden="true"
      >
        <rect width={METER_W} height={METER_H} fill={CITY.sky} />

        <g transform={ORIGIN}>
          <polygon
            points={tile(-0.8, -0.8, 8, 10)}
            fill={CITY.ground}
            stroke={CITY.edge}
          />

          {ROADS.map((road) => {
            const [x, y] = iso(
              road.cell[0] + SIZE / 2,
              road.cell[1] + SIZE / 2,
            );
            return (
              <line
                key={road.name}
                className="ic-m-pulse"
                x1={x}
                y1={y}
                x2={PLAZA_DOOR[0]}
                y2={PLAZA_DOOR[1]}
                stroke={CITY.accent}
                strokeWidth={2}
                strokeDasharray="6 16"
                opacity="0.55"
              />
            );
          })}

          {ROADS.map((road) => {
            const faces = box(
              road.cell[0],
              road.cell[1],
              SIZE,
              SIZE,
              road.height,
            );
            return (
              <g key={road.name}>
                <polygon
                  points={faces.left}
                  fill={CITY.blockLeft}
                  stroke={CITY.edge}
                />
                <polygon
                  points={faces.right}
                  fill={CITY.blockRight}
                  stroke={CITY.edge}
                />
                <polygon
                  points={faces.top}
                  fill={CITY.blockTop}
                  stroke={CITY.edge}
                />
              </g>
            );
          })}

          <polygon
            points={PLAZA.left}
            fill={CITY.plazaLeft}
            stroke={CITY.edge}
          />
          <polygon
            points={PLAZA.right}
            fill={CITY.plazaRight}
            stroke={CITY.edge}
          />
          <polygon points={PLAZA.top} fill={CITY.plazaTop} stroke={CITY.edge} />
          <circle
            className="ic-m-beacon"
            cx={PLAZA.roof[0]}
            cy={PLAZA.roof[1] - 12}
            r={5}
            fill={CITY.accent}
          />
        </g>

        {/* The traffic desk */}
        <g transform={`translate(${DESK.x}, ${DESK.y})`}>
          <rect
            width={DESK.w}
            height={DESK.h}
            rx="12"
            fill={CITY.plazaLeft}
            stroke={CITY.edge}
          />
          <text
            x={34}
            y={30}
            fill={CITY.ink}
            fontSize={METER_FONT.label}
            style={LABEL}
          >
            TRAFFIC DESK
          </text>
          {METER_LABELS.map((label, i) => (
            <text
              key={label}
              x={COLUMN.x + i * COLUMN.pitch}
              y={66}
              fill={CITY.ink}
              fontSize={METER_FONT.label}
              style={LABEL}
            >
              {label}
            </text>
          ))}
          <MeterRow
            label="Gateway"
            meters={[0.46, 0.92, 0.03]}
            swing={1.1}
            y={104}
            gateway
          />
          {ROADS.map((road, i) => (
            <MeterRow
              key={road.name}
              label={road.name}
              meters={road.meters}
              swing={road.swing}
              y={138 + i * 34}
              gateway={false}
            />
          ))}
        </g>
      </svg>
    </div>
  );
}
