"use client";

import { FONTS } from "../../brand";
import { useCityMotion } from "./hooks";
import {
  CITY,
  FONT,
  LABEL,
  SCENE_H,
  SCENE_W,
  box,
  iso,
  leftQuad,
  tile,
} from "./palette";

/**
 * "Both specifications, one gateway": one street, two kinds of flag. Every
 * GraphQL building on the road flies a specification flag on its roof - two
 * GraphQL Federation, two Apollo Federation - and the corner building flies
 * both while it moves across. The OpenAPI and gRPC buildings have a different
 * facade but stand on the same road into the same plaza.
 *
 * Rest state: the finished street with every flag flying and every road
 * connected, so the still frame reads as one gateway serving both
 * specifications and the non-GraphQL sources alike.
 *
 * Signage is set in `FONT` units so the smallest pennant still reads at the
 * site's 11px label on a 375px screen. At that size the street holds two ranks
 * of three blocks five cells apart - the pitch that keeps one block's signage
 * clear of the next one's - and the composition caption sits under the map.
 */

const CSS = `
.ic-flags[data-run="false"] [class*="ic-f-"] { animation: none; }
.ic-flags .ic-f-flow { animation: ic-f-flow 4s linear infinite; }
.ic-flags .ic-f-wave { animation: ic-f-wave 5s ease-in-out infinite; transform-box: fill-box; transform-origin: left center; }
.ic-flags .ic-f-swap-a { animation: ic-f-swap-a 9s ease-in-out infinite; }
.ic-flags .ic-f-swap-b { animation: ic-f-swap-b 9s ease-in-out infinite; }
@keyframes ic-f-flow { to { stroke-dashoffset: -40; } }
@keyframes ic-f-wave {
  0%, 100% { transform: scaleX(1); }
  50% { transform: scaleX(0.82); }
}
@keyframes ic-f-swap-a {
  0%, 40% { opacity: 1; }
  60%, 100% { opacity: 0.22; }
}
@keyframes ic-f-swap-b {
  0%, 40% { opacity: 0.22; }
  60%, 100% { opacity: 1; }
}
`;

const ORIGIN = "translate(216, 70)";
const SIZE = 2.2;
/** Cells between two blocks in the same rank: the signage pitch. */
const PITCH = 5;

/** The plaza sits at the end of the street, straddling the road. */
const PLAZA = box(13, 2.5, 2.8, 2.8, 40);
const PLAZA_DOOR = iso(13.95, 5.3);

interface StreetBlock {
  readonly name: string;
  /** Language painted on the facade, or the source kind for a non-GraphQL block. */
  readonly tag: string;
  /** Roof flag text, or `null` for a source that carries no federation flag. */
  readonly flag: string | null;
  /** The corner block that flies both flags while it moves across. */
  readonly both?: boolean;
  readonly cell: readonly [number, number];
  readonly height: number;
  /** Non-GraphQL sources get the hatched facade. */
  readonly facade?: boolean;
}

const STREET: readonly StreetBlock[] = [
  {
    name: "Catalog",
    tag: "JS/TS",
    flag: "GraphQL Fed",
    cell: [0, 0],
    height: 52,
  },
  {
    name: "Billing",
    tag: "Java",
    flag: "Apollo Fed",
    cell: [PITCH, 0],
    height: 44,
  },
  {
    name: "Inventory",
    tag: "gRPC",
    flag: null,
    cell: [2 * PITCH, 0],
    height: 42,
    facade: true,
  },
  {
    name: "Shipping",
    tag: "Ruby",
    flag: "Apollo Fed",
    both: true,
    cell: [1, PITCH],
    height: 46,
  },
  {
    name: "Accounts",
    tag: "C#",
    flag: "GraphQL Fed",
    cell: [1 + PITCH, PITCH],
    height: 54,
  },
  {
    name: "Payments",
    tag: "OpenAPI",
    flag: null,
    cell: [1 + 2 * PITCH, PITCH],
    height: 40,
    facade: true,
  },
];

interface PennantProps {
  readonly x: number;
  readonly y: number;
  readonly label: string;
  readonly className?: string;
  readonly opacity: number;
}

/** Both specifications fly the same pennant on purpose; only the text differs. */
function Pennant({ x, y, label, className, opacity }: PennantProps) {
  return (
    <g className={className} opacity={opacity}>
      <polygon
        className="ic-f-wave"
        points={`${x},${y} ${x + 30},${y + 7} ${x},${y + 14}`}
        fill={CITY.accent}
      />
      <text
        x={x + 38}
        y={y + 13}
        fill={CITY.ink}
        fontSize={FONT.label}
        style={LABEL}
      >
        {label}
      </text>
    </g>
  );
}

interface StreetHouseProps {
  readonly block: StreetBlock;
}

function StreetHouse({ block }: StreetHouseProps) {
  const [gx, gy] = block.cell;
  const faces = box(gx, gy, SIZE, SIZE, block.height);
  const [rx, ry] = faces.roof;
  const fill = block.facade
    ? { top: CITY.facadeTop, left: CITY.facadeLeft, right: CITY.facadeRight }
    : { top: CITY.blockTop, left: CITY.blockLeft, right: CITY.blockRight };

  return (
    <g>
      <polygon points={faces.left} fill={fill.left} stroke={CITY.edge} />
      <polygon points={faces.right} fill={fill.right} stroke={CITY.edge} />
      {block.facade ? (
        <polygon points={faces.right} fill="url(#ic-f-hatch)" opacity="0.7" />
      ) : null}
      <polygon points={faces.top} fill={fill.top} stroke={CITY.edge} />

      {block.flag ? (
        <>
          <line
            x1={rx}
            y1={ry}
            x2={rx}
            y2={ry - (block.both ? 62 : 34)}
            stroke={CITY.edge}
            strokeWidth={1.5}
          />
          <Pennant
            x={rx}
            y={ry - (block.both ? 62 : 34)}
            label={block.flag}
            className={block.both ? "ic-f-swap-a" : undefined}
            opacity={1}
          />
          {block.both ? (
            <Pennant
              x={rx}
              y={ry - 34}
              label="GraphQL Fed"
              className="ic-f-swap-b"
              opacity={1}
            />
          ) : null}
        </>
      ) : null}

      <text
        x={rx}
        y={ry + 6}
        textAnchor="middle"
        fill={CITY.heading}
        fontFamily={FONTS.heading}
        fontSize={FONT.caption}
      >
        {block.name}
      </text>
      <text
        x={rx}
        y={ry + 28}
        textAnchor="middle"
        fill={CITY.ink}
        fontSize={FONT.label}
        style={LABEL}
      >
        {block.tag}
      </text>
    </g>
  );
}

/** Every block is joined to the plaza door by the same road. */
const LINKS = STREET.map((block) => {
  const [x, y] = iso(block.cell[0] + SIZE / 2, block.cell[1] + SIZE / 2);
  return { name: block.name, x, y };
});

/** Painter's order: the block nearest the viewer is drawn last. */
const DEPTH_SORTED = [...STREET].sort(
  (a, b) => a.cell[0] + a.cell[1] - (b.cell[0] + b.cell[1]),
);

export function DistrictFlags() {
  const run = useCityMotion();

  return (
    <div
      className="ic-flags absolute inset-0"
      data-run={run ? "true" : "false"}
    >
      <style>{CSS}</style>
      <svg
        viewBox={`0 0 ${SCENE_W} ${SCENE_H}`}
        className="h-full w-full"
        aria-hidden="true"
      >
        <defs>
          <pattern
            id="ic-f-hatch"
            width="8"
            height="8"
            patternUnits="userSpaceOnUse"
            patternTransform="rotate(30)"
          >
            <line
              x1="0"
              y1="0"
              x2="0"
              y2="8"
              stroke={CITY.edge}
              strokeWidth="2"
            />
          </pattern>
        </defs>

        <rect width={SCENE_W} height={SCENE_H} fill={CITY.sky} />

        <g transform={ORIGIN}>
          <polygon
            points={tile(-1, -1, 17, 8.2)}
            fill={CITY.ground}
            stroke={CITY.edge}
          />
          <polygon points={tile(-1, 2.8, 17, 1.2)} fill={CITY.road} />

          {LINKS.map((link) => (
            <line
              key={link.name}
              className="ic-f-flow"
              x1={link.x}
              y1={link.y}
              x2={PLAZA_DOOR[0]}
              y2={PLAZA_DOOR[1]}
              stroke={CITY.accent}
              strokeWidth={1.5}
              strokeDasharray="5 15"
              opacity="0.5"
            />
          ))}

          {DEPTH_SORTED.map((block) => (
            <StreetHouse key={block.name} block={block} />
          ))}

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
            points={leftQuad(5.3, 13.5, 0, 0.9, 18)}
            fill={CITY.window}
            opacity="0.9"
          />
          <text
            x={PLAZA.roof[0]}
            y={PLAZA.roof[1] - 14}
            textAnchor="middle"
            fill={CITY.heading}
            fontFamily={FONTS.heading}
            fontSize={FONT.caption}
          >
            Gateway
          </text>
        </g>

        <text
          x={SCENE_W / 2}
          y={SCENE_H - 14}
          textAnchor="middle"
          fill={CITY.ink}
          fontSize={FONT.label}
          style={LABEL}
        >
          ONE COMPOSITE SCHEMA
        </text>
      </svg>
    </div>
  );
}
