"use client";

import { FONTS } from "../../brand";
import { useCityCycle, useCityMotion } from "./hooks";
import { CITY, FONT, LABEL, SCENE_H, SCENE_W, box, iso, tile } from "./palette";

/**
 * "Composition protects the graph, Nitro protects your clients": the traffic
 * camera. The Ordering team closes a road on its own block, the zoning
 * inspection still stamps the plans - nothing in the city plan objects - but
 * the camera over the junction has been logging which vehicle really drives
 * that road, and the scooter of the mobile client is on it.
 *
 * Rest state: the road closed, the permit stamp still green and the camera log
 * showing the mobile client's trip marked breaking, so the still frame already
 * carries the whole point.
 *
 * Every line is set in `FONT` units, so the smallest one still reads at the
 * site's 11px label on a 375px screen; at that size the log runs the full
 * width under the junction, one line per trip.
 */

const PHASES = 5;
const REST = 4;
const BEAT = 1700;

const ORIGIN = "translate(180, 112)";
const SIZE = 2.4;
/** The camera log: a full-width panel under the junction. */
const LOG = { x: 10, y: 280, w: SCENE_W - 20, h: 190 } as const;

const BLOCK = box(0, 0, SIZE, SIZE, 52);
const PLAZA = box(5.6, 2.6, 2.6, 2.6, 38);

/** The road the Ordering team closes: from its door to the plaza. */
const ROAD_FROM = iso(1.2, 2.6);
const ROAD_TO = iso(5.6, 3.9);

interface Trip {
  readonly client: string;
  readonly road: string;
  readonly breaking: boolean;
  /** The phase at which the camera log shows this trip. */
  readonly phase: number;
}

/** What the camera really saw, not what the schema allows. */
const TRIPS: readonly Trip[] = [
  { client: "Web", road: "Order.total", breaking: false, phase: 1 },
  { client: "Partner API", road: "Order.buyer", breaking: false, phase: 2 },
  { client: "Mobile", road: "Order.trackingCode", breaking: true, phase: 3 },
];

interface LogRowProps {
  readonly trip: Trip;
  readonly phase: number;
  readonly y: number;
}

function LogRow({ trip, phase, y }: LogRowProps) {
  const shown = phase >= trip.phase;
  const colour = trip.breaking ? CITY.stop : CITY.ok;

  return (
    <g opacity={shown ? 1 : 0.25}>
      <rect
        x={24}
        y={y - 15}
        width={16}
        height={16}
        rx={3}
        fill={shown ? colour : CITY.ink}
      />
      <text
        x={52}
        y={y}
        fill={CITY.heading}
        fontFamily={FONTS.heading}
        fontSize={FONT.caption}
      >
        {trip.client}
      </text>
      <text
        x={211}
        y={y}
        fill={CITY.ink}
        fontFamily={FONTS.mono}
        fontSize={FONT.label}
      >
        {trip.road}
      </text>
      <text
        x={LOG.w - 24}
        y={y}
        textAnchor="end"
        fill={shown ? colour : CITY.ink}
        fontSize={FONT.label}
        style={LABEL}
      >
        {trip.breaking ? "BREAKING" : "SAFE"}
      </text>
    </g>
  );
}

export function TrafficCameraLog() {
  const run = useCityMotion();
  const phase = useCityCycle(run, PHASES, BEAT, REST);
  const closed = phase >= 1;
  const flagged = phase >= 3;

  return (
    <div className="ic-cam absolute inset-0" data-run={run ? "true" : "false"}>
      <svg
        viewBox={`0 0 ${SCENE_W} ${SCENE_H}`}
        className="h-full w-full"
        aria-hidden="true"
      >
        <rect width={SCENE_W} height={SCENE_H} fill={CITY.sky} />

        <g transform={ORIGIN}>
          <polygon
            points={tile(-1.4, -1.4, 11, 9)}
            fill={CITY.ground}
            stroke={CITY.edge}
          />

          {/* The road the subgraph team withdraws */}
          <line
            x1={ROAD_FROM[0]}
            y1={ROAD_FROM[1]}
            x2={ROAD_TO[0]}
            y2={ROAD_TO[1]}
            stroke={closed ? CITY.stop : CITY.accent}
            strokeWidth={5}
            strokeLinecap="round"
            strokeDasharray={closed ? "6 10" : undefined}
            opacity={closed ? 0.7 : 0.9}
          />
          <text
            x={SCENE_W / 2 - 180}
            y={(ROAD_FROM[1] + ROAD_TO[1]) / 2 - 14}
            textAnchor="middle"
            fill={closed ? CITY.stop : CITY.ink}
            fontSize={FONT.label}
            style={LABEL}
          >
            {closed ? "Order.trackingCode REMOVED" : "Order.trackingCode"}
          </text>

          {/* The subgraph that closed it */}
          <polygon
            points={BLOCK.left}
            fill={CITY.blockLeft}
            stroke={CITY.edge}
          />
          <polygon
            points={BLOCK.right}
            fill={CITY.blockRight}
            stroke={CITY.edge}
          />
          <polygon points={BLOCK.top} fill={CITY.blockTop} stroke={CITY.edge} />
          <text
            x={BLOCK.roof[0]}
            y={BLOCK.roof[1] + 6}
            textAnchor="middle"
            fill={CITY.heading}
            fontFamily={FONTS.heading}
            fontSize={FONT.caption}
          >
            Ordering
          </text>
          <text
            x={BLOCK.roof[0]}
            y={BLOCK.roof[1] + 28}
            textAnchor="middle"
            fill={CITY.ink}
            fontSize={FONT.label}
            style={LABEL}
          >
            Go
          </text>

          {/* Zoning still says yes: the source schemas still compose */}
          <g
            transform={`translate(${BLOCK.roof[0] - 120}, ${BLOCK.roof[1] - 96})`}
          >
            <rect
              width="240"
              height="40"
              rx="8"
              fill={CITY.plazaLeft}
              stroke={CITY.ok}
            />
            <circle cx={24} cy={20} r={6} fill={CITY.ok} />
            <text
              x={44}
              y={27}
              fill={CITY.ok}
              fontSize={FONT.label}
              style={LABEL}
            >
              ZONING: PASS
            </text>
          </g>

          {/* The plaza and the camera watching the junction */}
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
          <text
            x={PLAZA.roof[0]}
            y={PLAZA.roof[1] + 6}
            textAnchor="middle"
            fill={CITY.heading}
            fontFamily={FONTS.heading}
            fontSize={FONT.caption}
          >
            Gateway
          </text>

          <g transform={`translate(${ROAD_TO[0] - 34}, ${ROAD_TO[1] - 96})`}>
            <line
              x1={12}
              y1={4}
              x2={12}
              y2={72}
              stroke={CITY.edge}
              strokeWidth={2}
            />
            <rect
              x={0}
              y={0}
              width={30}
              height={14}
              rx={3}
              fill={CITY.facadeTop}
              stroke={CITY.edge}
            />
            <circle
              cx={34}
              cy={7}
              r={5}
              fill={flagged ? CITY.stop : CITY.accent}
            />
            <text
              x={48}
              y={14}
              fill={CITY.ink}
              fontSize={FONT.label}
              style={LABEL}
            >
              CAMERA
            </text>
          </g>

          {/* The scooter still on the closed road */}
          <g
            transform={`translate(${(ROAD_FROM[0] + ROAD_TO[0]) / 2 + 12}, ${
              (ROAD_FROM[1] + ROAD_TO[1]) / 2 + 6
            })`}
            opacity={flagged ? 1 : 0.4}
          >
            <polygon
              points="0,0 20,7 20,17 0,10"
              fill={flagged ? CITY.stop : CITY.window}
            />
            <text
              x={-10}
              y={16}
              textAnchor="end"
              fill={CITY.ink}
              fontSize={FONT.label}
              style={LABEL}
            >
              Mobile
            </text>
          </g>
        </g>

        {/* The camera log: the operations real clients publish */}
        <g transform={`translate(${LOG.x}, ${LOG.y})`}>
          <rect
            width={LOG.w}
            height={LOG.h}
            rx="10"
            fill={CITY.plazaLeft}
            stroke={CITY.edge}
          />
          <text
            x={24}
            y={34}
            fill={CITY.ink}
            fontSize={FONT.label}
            style={LABEL}
          >
            TRAFFIC CAMERA LOG
          </text>
          <text
            x={24}
            y={72}
            fill={CITY.heading}
            fontFamily={FONTS.heading}
            fontSize={FONT.caption}
          >
            registered clients
          </text>
          {TRIPS.map((trip, i) => (
            <LogRow
              key={trip.client}
              trip={trip}
              phase={phase}
              y={112 + i * 34}
            />
          ))}
        </g>
      </svg>
    </div>
  );
}
