"use client";

import { FONTS } from "../../brand";
import { useCityCycle, useCityMotion } from "./hooks";
import {
  CITY,
  FONT,
  LABEL,
  SCENE_H,
  SCENE_W,
  box,
  iso,
  poly,
  tile,
} from "./palette";

/**
 * "Any GraphQL server, no plugin": the zoning inspection. Five buildings in
 * any language already stand on the block; the sixth is still a lot with
 * scaffolding, and its plans go through the inspection that composition runs
 * in the build. Two checks pass, the third finds an incompatible enum, the
 * permit is refused and the boom stays down, so the lot never opens onto the
 * road.
 *
 * Rest state: the refused permit - the conflict row flagged, the stamp reading
 * BUILD STOPPED and the boom down - so the still frame says a conflict fails
 * the pipeline instead of the gateway.
 *
 * Every line is set in `FONT` units, so the smallest one still reads at the
 * site's 11px label on a 375px screen. At that size a building carries one
 * sign - its name and its language - the blocks stand four and a half cells
 * apart, and the inspector's board runs the full width under the city.
 */

const PHASES = 6;
const REST = 5;
const BEAT = 1500;

const ORIGIN = "translate(239, 90)";
const SIZE = 2.2;
/** Cells between two blocks in the same row: the signage pitch. */
const PITCH = 4.6;
/** The inspector's board: a full-width panel under the city. */
const BOARD = { x: 20, y: 320, w: SCENE_W - 40, h: 150 } as const;

interface Standing {
  readonly name: string;
  readonly language: string;
  readonly cell: readonly [number, number];
  readonly height: number;
}

/** The buildings already open: an ordinary server each, no plugin bolted on. */
const STANDING: readonly Standing[] = [
  { name: "Catalog", language: "JS/TS", cell: [0, 0], height: 46 },
  { name: "Billing", language: "Java", cell: [PITCH, 0], height: 44 },
  { name: "Ordering", language: "Go", cell: [2 * PITCH, 0], height: 50 },
  { name: "Shipping", language: "Ruby", cell: [0, 3.6], height: 42 },
  { name: "Accounts", language: "Python", cell: [PITCH, 3.6], height: 48 },
];

/** The lot under inspection, still fenced off. */
const LOT: readonly [number, number] = [2 * PITCH, 3.6];
const LOT_BOX = box(LOT[0] + 0.3, LOT[1] + 0.3, 1.6, 1.6, 34);

interface Check {
  readonly label: string;
  readonly detail: string;
  /** The phase at which the inspector reaches this row. */
  readonly phase: number;
  readonly fails: boolean;
}

const CHECKS: readonly Check[] = [
  { label: "types agree", detail: "Order.total", phase: 1, fails: false },
  { label: "fields present", detail: "Order.buyer", phase: 2, fails: false },
  { label: "enums compatible", detail: "ShipMode", phase: 3, fails: true },
];

interface CheckRowProps {
  readonly check: Check;
  readonly phase: number;
  readonly y: number;
}

function CheckRow({ check, phase, y }: CheckRowProps) {
  const reached = phase >= check.phase;
  const colour = !reached ? CITY.ink : check.fails ? CITY.stop : CITY.ok;

  return (
    <g opacity={reached ? 1 : 0.4}>
      <rect
        x={44}
        y={y - 18}
        width={22}
        height={22}
        rx={4}
        fill="none"
        stroke={colour}
        strokeWidth={2}
      />
      {reached ? (
        <path
          d={
            check.fails
              ? `M49 ${y - 13} L61 ${y - 1} M61 ${y - 13} L49 ${y - 1}`
              : `M49 ${y - 8} L54 ${y - 3} L62 ${y - 15}`
          }
          fill="none"
          stroke={colour}
          strokeWidth={2.5}
        />
      ) : null}
      <text x={80} y={y} fill={CITY.heading} fontSize={FONT.label}>
        {check.label}
      </text>
      <text
        x={BOARD.w - 24}
        y={y}
        textAnchor="end"
        fill={colour}
        fontSize={FONT.label}
        style={LABEL}
      >
        {check.detail}
      </text>
    </g>
  );
}

interface OpenBuildingProps {
  readonly building: Standing;
}

function OpenBuilding({ building }: OpenBuildingProps) {
  const faces = box(
    building.cell[0],
    building.cell[1],
    SIZE,
    SIZE,
    building.height,
  );
  const [rx, ry] = faces.roof;

  return (
    <g>
      <polygon points={faces.left} fill={CITY.blockLeft} stroke={CITY.edge} />
      <polygon points={faces.right} fill={CITY.blockRight} stroke={CITY.edge} />
      <polygon points={faces.top} fill={CITY.blockTop} stroke={CITY.edge} />
      <text
        x={rx}
        y={ry + 6}
        textAnchor="middle"
        fill={CITY.heading}
        fontSize={FONT.label}
        style={LABEL}
      >
        {`${building.name} · ${building.language}`}
      </text>
    </g>
  );
}

export function ZoningPermit() {
  const run = useCityMotion();
  const phase = useCityCycle(run, PHASES, BEAT, REST);
  const refused = phase >= 4;
  const [boomX, boomY] = iso(LOT[0] + 1.1, LOT[1] - 0.2);

  return (
    <div className="ic-zone absolute inset-0" data-run={run ? "true" : "false"}>
      <svg
        viewBox={`0 0 ${SCENE_W} ${SCENE_H}`}
        className="h-full w-full"
        aria-hidden="true"
      >
        <rect width={SCENE_W} height={SCENE_H} fill={CITY.sky} />

        {/* What the inspection is really saying, over the city */}
        <text
          x={SCENE_W / 2}
          y={34}
          textAnchor="middle"
          fill={CITY.ink}
          fontSize={FONT.label}
          style={LABEL}
        >
          {refused
            ? "THE PIPELINE FAILS, NOT THE GATEWAY"
            : "CHECKING SOURCE SCHEMAS"}
        </text>

        <g transform={ORIGIN}>
          <polygon
            points={tile(-1, -1, 12.4, 8)}
            fill={CITY.ground}
            stroke={CITY.edge}
          />
          <polygon points={tile(-1, 2.8, 12.4, 0.6)} fill={CITY.road} />

          {STANDING.map((building) => (
            <OpenBuilding key={building.name} building={building} />
          ))}

          {/* The lot: fenced, scaffolded, waiting for its permit */}
          <polygon
            points={tile(LOT[0], LOT[1], SIZE, SIZE)}
            fill={CITY.road}
            stroke={refused ? CITY.stop : CITY.warn}
            strokeDasharray="6 6"
          />
          <polygon
            points={LOT_BOX.left}
            fill={CITY.facadeLeft}
            stroke={CITY.edge}
            opacity="0.8"
          />
          <polygon
            points={LOT_BOX.right}
            fill={CITY.facadeRight}
            stroke={CITY.edge}
            opacity="0.8"
          />
          <polygon
            points={LOT_BOX.top}
            fill="none"
            stroke={refused ? CITY.stop : CITY.warn}
            strokeDasharray="4 5"
          />
          <text
            x={LOT_BOX.roof[0]}
            y={LOT_BOX.roof[1] + 6}
            textAnchor="middle"
            fill={refused ? CITY.stop : CITY.warn}
            fontSize={FONT.label}
            style={LABEL}
          >
            {refused ? "NOT OPENED" : "IN REVIEW"}
          </text>

          {/* The boom across the lot entrance: down while the permit is refused */}
          <line
            x1={boomX}
            y1={boomY}
            x2={boomX + 54}
            y2={boomY + (refused ? 14 : -30)}
            stroke={refused ? CITY.stop : CITY.ok}
            strokeWidth={4}
            strokeLinecap="round"
          />
          <circle cx={boomX} cy={boomY} r={4} fill={CITY.ink} />
        </g>

        {/* The stamp that lands once the inspection is done */}
        <g transform="translate(30, 236)" opacity={refused ? 1 : 0.25}>
          <polygon
            points={poly([
              [0, 0],
              [250, 0],
              [250, 54],
              [0, 54],
            ])}
            fill="none"
            stroke={refused ? CITY.stop : CITY.ink}
            strokeWidth={3}
            transform="rotate(-6)"
          />
          <text
            x={18}
            y={36}
            fill={refused ? CITY.stop : CITY.ink}
            fontSize={FONT.label}
            style={LABEL}
            transform="rotate(-6)"
          >
            BUILD STOPPED
          </text>
        </g>

        {/* The inspector's board: the composition step in the build */}
        <g transform={`translate(${BOARD.x}, ${BOARD.y})`}>
          <rect
            width={BOARD.w}
            height={BOARD.h}
            rx="10"
            fill={CITY.plazaLeft}
            stroke={CITY.edge}
          />
          <text
            x={24}
            y={30}
            fill={CITY.ink}
            fontSize={FONT.label}
            style={LABEL}
          >
            ZONING INSPECTION
          </text>
          <text
            x={317}
            y={30}
            fill={CITY.heading}
            fontFamily={FONTS.heading}
            fontSize={FONT.caption}
          >
            composition
          </text>
          {CHECKS.map((check, i) => (
            <CheckRow
              key={check.label}
              check={check}
              phase={phase}
              y={70 + i * 32}
            />
          ))}
        </g>
      </svg>
    </div>
  );
}
