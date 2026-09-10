"use client";

import { useCityCycle, useCityMotion } from "./hooks";
import { CITY, LABEL, box, iso, poly, tile } from "./palette";

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
 */

const PHASES = 6;
const REST = 5;
const BEAT = 1500;

const ORIGIN = "translate(212, 128)";
const SIZE = 2.2;

interface Standing {
  readonly name: string;
  readonly language: string;
  readonly cell: readonly [number, number];
  readonly height: number;
}

/** The buildings already open: an ordinary server each, no plugin bolted on. */
const STANDING: readonly Standing[] = [
  { name: "Catalog", language: "JS/TS", cell: [0, 0], height: 54 },
  { name: "Billing", language: "Java", cell: [3.2, 0], height: 44 },
  { name: "Ordering", language: "Go", cell: [6.4, 0], height: 50 },
  { name: "Shipping", language: "Ruby", cell: [0, 3.6], height: 42 },
  { name: "Accounts", language: "Python", cell: [3.2, 3.6], height: 48 },
];

/** The lot under inspection, still fenced off. */
const LOT: readonly [number, number] = [6.4, 3.6];
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
        x={22}
        y={y - 12}
        width={14}
        height={14}
        rx={3}
        fill="none"
        stroke={colour}
        strokeWidth={1.5}
      />
      {reached ? (
        <path
          d={
            check.fails
              ? `M25 ${y - 9} L33 ${y - 1} M33 ${y - 9} L25 ${y - 1}`
              : `M25 ${y - 5} L28 ${y - 2} L34 ${y - 10}`
          }
          fill="none"
          stroke={colour}
          strokeWidth={2}
        />
      ) : null}
      <text x={48} y={y} fill={CITY.heading} fontSize={12}>
        {check.label}
      </text>
      <text
        x={168}
        y={y}
        textAnchor="end"
        fill={colour}
        fontSize={10}
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
        y={ry + 4}
        textAnchor="middle"
        fill={CITY.heading}
        fontSize={12}
      >
        {building.name}
      </text>
      <text
        x={rx}
        y={ry + 19}
        textAnchor="middle"
        fill={CITY.ink}
        fontSize={10}
        style={LABEL}
      >
        {building.language}
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
      <svg viewBox="0 0 640 480" className="h-full w-full" aria-hidden="true">
        <rect width="640" height="480" fill={CITY.sky} />

        <g transform={ORIGIN}>
          <polygon
            points={tile(-1, -1, 11, 8)}
            fill={CITY.ground}
            stroke={CITY.edge}
          />
          <polygon points={tile(-1, 3.1, 11, 0.4)} fill={CITY.road} />

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
            y={LOT_BOX.roof[1] + 4}
            textAnchor="middle"
            fill={refused ? CITY.stop : CITY.warn}
            fontSize={10}
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

        {/* The inspector's board: the composition step in the build */}
        <g transform="translate(24, 300)">
          <rect
            width="200"
            height="150"
            rx="10"
            fill={CITY.plazaLeft}
            stroke={CITY.edge}
          />
          <text x={22} y={30} fill={CITY.ink} fontSize={10} style={LABEL}>
            ZONING INSPECTION
          </text>
          <text x={22} y={50} fill={CITY.heading} fontSize={12}>
            composition
          </text>
          {CHECKS.map((check, i) => (
            <CheckRow
              key={check.label}
              check={check}
              phase={phase}
              y={82 + i * 24}
            />
          ))}
        </g>

        {/* The stamp that lands once the inspection is done */}
        <g transform="translate(236, 372)" opacity={refused ? 1 : 0.25}>
          <polygon
            points={poly([
              [0, 0],
              [150, 0],
              [150, 44],
              [0, 44],
            ])}
            fill="none"
            stroke={refused ? CITY.stop : CITY.ink}
            strokeWidth={3}
            transform="rotate(-6)"
          />
          <text
            x={16}
            y={30}
            fill={refused ? CITY.stop : CITY.ink}
            fontSize={16}
            style={LABEL}
            transform="rotate(-6)"
          >
            BUILD STOPPED
          </text>
        </g>

        <text x={236} y={442} fill={CITY.ink} fontSize={11} style={LABEL}>
          {refused
            ? "THE PIPELINE FAILS, NOT THE GATEWAY"
            : "CHECKING SOURCE SCHEMAS"}
        </text>
      </svg>
    </div>
  );
}
