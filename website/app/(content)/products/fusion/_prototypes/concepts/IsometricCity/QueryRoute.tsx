"use client";

import type { CSSProperties } from "react";

import { useCityMotion } from "./hooks";
import { CITY, LABEL, box, iso, poly, rightQuad, tile } from "./palette";

/**
 * "What is Fusion?": one route across the map. A vehicle stops at the plaza's
 * single door, and the route the gateway plans is drawn block by block - it
 * visits the four buildings that hold the data and comes back to the plaza,
 * where the one response leaves through the same door.
 *
 * Rest state: the whole route is drawn, every stop is lit and the vehicle
 * waits at the door, so the still frame already says "one query, four
 * buildings, one answer".
 */

const CSS = `
.ic-route[data-run="false"] [class*="ic-r-"] { animation: none; }
.ic-route .ic-r-draw { animation: ic-r-draw 11s ease-in-out infinite; }
.ic-route .ic-r-stop { animation: ic-r-stop 11s ease-in-out infinite; }
.ic-route .ic-r-car { animation: ic-r-car 11s ease-in-out infinite; }
.ic-route .ic-r-out { animation: ic-r-out 11s ease-in-out infinite; }
@keyframes ic-r-draw {
  0%, 4% { stroke-dashoffset: var(--len); }
  62%, 100% { stroke-dashoffset: 0; }
}
@keyframes ic-r-stop {
  0%, 8% { opacity: 0.2; }
  20%, 100% { opacity: 1; }
}
@keyframes ic-r-out {
  0%, 66% { opacity: 0; transform: translate(0, 0); }
  78% { opacity: 1; }
  100% { opacity: 0; transform: translate(74px, 40px); }
}
`;

const ORIGIN = "translate(320, 112)";
const SIZE = 2.6;
const PLAZA = box(3.2, 3.2, SIZE, SIZE, 34);

interface Stop {
  readonly name: string;
  readonly language: string;
  readonly field: string;
  readonly cell: readonly [number, number];
  readonly height: number;
}

/** The four buildings this one query has to visit, clockwise from the top. */
const STOPS: readonly Stop[] = [
  {
    name: "Catalog",
    language: "JS/TS",
    field: "product",
    cell: [0, 0],
    height: 52,
  },
  {
    name: "Billing",
    language: "Java",
    field: "price",
    cell: [6.6, 0],
    height: 44,
  },
  {
    name: "Ordering",
    language: "Go",
    field: "order",
    cell: [6.6, 6.6],
    height: 48,
  },
  {
    name: "Shipping",
    language: "Ruby",
    field: "delivery",
    cell: [0, 6.6],
    height: 40,
  },
];

/** Ground centre of a block, where the route touches it. */
function centre(cell: readonly [number, number]): readonly [number, number] {
  return iso(cell[0] + SIZE / 2, cell[1] + SIZE / 2);
}

const DOOR = iso(4.5, 6.4);
const WAYPOINTS: readonly (readonly [number, number])[] = [
  DOOR,
  centre(STOPS[0].cell),
  centre(STOPS[1].cell),
  centre(STOPS[2].cell),
  centre(STOPS[3].cell),
  DOOR,
];

const ROUTE = WAYPOINTS.map(
  ([x, y], i) => `${i === 0 ? "M" : "L"}${x} ${y}`,
).join(" ");

/** Rough path length, enough for a dash-offset draw. */
const ROUTE_LEN = WAYPOINTS.reduce((sum, point, i) => {
  if (i === 0) return sum;
  const previous = WAYPOINTS[i - 1];
  return sum + Math.hypot(point[0] - previous[0], point[1] - previous[1]);
}, 0);

/** The van follows the same waypoints, one keyframe per stop. */
const CAR_FRAMES = WAYPOINTS.map((point, i) => {
  const percent = 6 + (i / (WAYPOINTS.length - 1)) * 56;
  const dx = point[0] - DOOR[0];
  const dy = point[1] - DOOR[1];
  return `${percent.toFixed(1)}% { transform: translate(${dx.toFixed(1)}px, ${dy.toFixed(1)}px); }`;
}).join("\n  ");

const CAR_CSS = `
@keyframes ic-r-car {
  0% { transform: translate(0, 0); }
  ${CAR_FRAMES}
  100% { transform: translate(0, 0); }
}
`;

interface StopBlockProps {
  readonly stop: Stop;
  readonly index: number;
}

function StopBlock({ stop, index }: StopBlockProps) {
  const faces = box(stop.cell[0], stop.cell[1], SIZE, SIZE, stop.height);
  const [rx, ry] = faces.roof;

  return (
    <g>
      <polygon points={faces.left} fill={CITY.blockLeft} stroke={CITY.edge} />
      <polygon points={faces.right} fill={CITY.blockRight} stroke={CITY.edge} />
      <polygon points={faces.top} fill={CITY.blockTop} stroke={CITY.edge} />
      <text
        x={rx}
        y={ry - 12}
        textAnchor="middle"
        fill={CITY.heading}
        fontSize={14}
      >
        {stop.name}
      </text>
      <text
        x={rx}
        y={ry + 4}
        textAnchor="middle"
        fill={CITY.ink}
        fontSize={10}
        style={LABEL}
      >
        {`${stop.language} · ${stop.field}`}
      </text>
      <circle
        className="ic-r-stop"
        cx={centre(stop.cell)[0]}
        cy={centre(stop.cell)[1]}
        r={6}
        fill={CITY.accent}
        style={{ animationDelay: `${index * 0.9}s` }}
      />
    </g>
  );
}

export function QueryRoute() {
  const run = useCityMotion();

  return (
    <div
      className="ic-route absolute inset-0"
      data-run={run ? "true" : "false"}
    >
      <style>{CSS + CAR_CSS}</style>
      <svg viewBox="0 0 640 480" className="h-full w-full" aria-hidden="true">
        <rect width="640" height="480" fill={CITY.sky} />

        <g transform={ORIGIN}>
          <polygon
            points={tile(-0.6, -0.6, 10.4, 10.4)}
            fill={CITY.ground}
            stroke={CITY.edge}
          />

          {/* The planned route, drawn stop by stop */}
          <path
            className="ic-r-draw"
            d={ROUTE}
            fill="none"
            stroke={CITY.accent}
            strokeWidth={3}
            strokeLinejoin="round"
            strokeDasharray={ROUTE_LEN}
            strokeDashoffset={0}
            opacity="0.75"
            style={{ "--len": `${ROUTE_LEN}` } as CSSProperties}
          />

          {STOPS.map((stop, i) => (
            <StopBlock key={stop.name} stop={stop} index={i} />
          ))}

          {/* The plaza: one gateway, one door */}
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
          <polygon
            points={rightQuad(5.8, 4.1, 0, 0.8, 16)}
            fill={CITY.window}
            opacity="0.9"
          />
          <text
            x={PLAZA.roof[0]}
            y={PLAZA.roof[1] - 10}
            textAnchor="middle"
            fill={CITY.heading}
            fontSize={14}
          >
            Gateway
          </text>

          {/* One query in, one response out, through the same door */}
          <g className="ic-r-car">
            <polygon
              points={poly([
                [DOOR[0] + 6, DOOR[1] - 12],
                [DOOR[0] + 34, DOOR[1] - 2],
                [DOOR[0] + 34, DOOR[1] + 10],
                [DOOR[0] + 6, DOOR[1]],
              ])}
              fill={CITY.ok}
              opacity="0.9"
            />
          </g>
          <g className="ic-r-out">
            <polygon
              points={poly([
                [DOOR[0] + 6, DOOR[1] - 12],
                [DOOR[0] + 34, DOOR[1] - 2],
                [DOOR[0] + 34, DOOR[1] + 10],
                [DOOR[0] + 6, DOOR[1]],
              ])}
              fill={CITY.window}
              opacity="0.9"
            />
          </g>

          <text
            x={DOOR[0] + 46}
            y={DOOR[1] + 26}
            fill={CITY.ink}
            fontSize={10}
            style={LABEL}
          >
            ONE QUERY IN · ONE RESPONSE OUT
          </text>
        </g>
      </svg>
    </div>
  );
}
