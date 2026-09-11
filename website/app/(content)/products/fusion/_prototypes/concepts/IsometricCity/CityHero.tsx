"use client";

import type { CSSProperties } from "react";

import { FONTS } from "../../brand";
import { useCityMotion } from "./hooks";
import {
  BLOCKS,
  CITY,
  HERO_FONT,
  LABEL,
  SOURCE_BLOCKS,
  VEHICLES,
  box,
  iso,
  poly,
  rightQuad,
  tile,
} from "./palette";

/**
 * Hero backdrop: the city assembles block by block. The plaza with its one
 * door is laid first, then the five subgraph buildings and the two
 * non-GraphQL facades rise out of the ground in turn, while a slow day-night
 * cycle washes the sky and lights the windows and the vehicles roll in along
 * the ring road.
 *
 * Rest state (reduced motion, off-screen, or before hydration): the finished
 * city at dusk with every building standing and the vehicles at the plaza.
 *
 * The city is drawn 1200 x 700 and sliced into a band at least 88svh tall, so
 * its signage is set in `HERO_FONT` units, which hold the site's 11px label
 * floor even on the shortest phone.
 */

const CSS = `
.ic-hero[data-run="false"] [class*="ic-h-"] { animation: none; }
.ic-hero .ic-h-rise { animation: ic-h-rise 22s ease-out backwards infinite; }
.ic-hero .ic-h-day { animation: ic-h-day 22s ease-in-out infinite; }
.ic-hero .ic-h-win { animation: ic-h-win 22s ease-in-out backwards infinite; }
.ic-hero .ic-h-drive { animation: ic-h-drive 22s ease-in-out backwards infinite; }
.ic-hero .ic-h-beacon { animation: ic-h-beacon 5s ease-in-out infinite; }
@keyframes ic-h-rise {
  0% { transform: translateY(46px); opacity: 0; }
  9%, 100% { transform: translateY(0); opacity: 1; }
}
@keyframes ic-h-day {
  0%, 14% { opacity: 0.9; }
  46%, 72% { opacity: 0; }
  100% { opacity: 0.9; }
}
@keyframes ic-h-win {
  0%, 18% { opacity: 0.2; }
  50%, 76% { opacity: 1; }
  100% { opacity: 0.2; }
}
@keyframes ic-h-drive {
  0% { transform: translate(var(--dx), var(--dy)); }
  26%, 100% { transform: translate(0, 0); }
}
@keyframes ic-h-beacon {
  0%, 100% { opacity: 0.45; }
  50% { opacity: 1; }
}
`;

/** The plaza: the gateway, three tiles square, with one door on the near face. */
const PLAZA = box(3, 3, 3, 3, 40);

/** Ring road tiles laid before anything is built. */
const ROADS = [
  { gx: -4, gy: 3.1, sx: 15, sy: 0.8 },
  { gx: 3.1, gy: -1, sx: 0.8, sy: 11 },
];

/** Deterministic window grid; no Math.random, so SSR and client agree. */
function windows(height: number): readonly string[] {
  const rows = Math.min(3, Math.max(1, Math.floor(height / 24)));
  const out: string[] = [];
  for (let r = 0; r < rows; r++) {
    for (let c = 0; c < 2; c++) {
      out.push(rightQuad(2.5, 0.5 + c * 1.1, 10 + r * 22, 0.7, 9));
    }
  }
  return out;
}

interface CityBuildingProps {
  readonly gx: number;
  readonly gy: number;
  readonly height: number;
  readonly name: string;
  readonly tag: string;
  readonly flag: string | null;
  readonly delay: number;
  readonly muted: boolean;
}

function CityBuilding({
  gx,
  gy,
  height,
  name,
  tag,
  flag,
  delay,
  muted,
}: CityBuildingProps) {
  const faces = box(gx, gy, 2.5, 2.5, height);
  const [rx, ry] = faces.roof;
  const stroke = CITY.edge;

  return (
    <g className="ic-h-rise" style={{ animationDelay: `${delay}s` }}>
      <polygon
        points={faces.left}
        fill={muted ? CITY.facadeLeft : CITY.blockLeft}
        stroke={stroke}
      />
      <polygon
        points={faces.right}
        fill={muted ? CITY.facadeRight : CITY.blockRight}
        stroke={stroke}
      />
      <polygon
        points={faces.top}
        fill={muted ? CITY.facadeTop : CITY.blockTop}
        stroke={stroke}
      />

      <g transform={`translate(${(gx - gy) * 32}, ${(gx + gy) * 16})`}>
        {windows(height).map((points, i) => (
          <polygon
            key={points}
            className="ic-h-win"
            points={points}
            fill={CITY.window}
            opacity="0.85"
            style={{ animationDelay: `${delay + i * 0.15}s` }}
          />
        ))}
      </g>

      {flag ? (
        <g>
          <line
            x1={rx}
            y1={ry}
            x2={rx}
            y2={ry - 26}
            stroke={CITY.edge}
            strokeWidth={1.5}
          />
          <polygon
            points={`${rx},${ry - 26} ${rx + 24},${ry - 21} ${rx},${ry - 16}`}
            fill={CITY.accent}
            opacity="0.85"
          />
          <text
            x={rx + 30}
            y={ry - 15}
            fill={CITY.ink}
            fontSize={HERO_FONT.label}
            style={LABEL}
          >
            {flag}
          </text>
        </g>
      ) : null}

      <text
        x={rx}
        y={ry + 8}
        textAnchor="middle"
        fill={CITY.heading}
        fontFamily={FONTS.heading}
        fontSize={HERO_FONT.caption}
      >
        {name}
      </text>
      <text
        x={rx}
        y={ry + 30}
        textAnchor="middle"
        fill={CITY.ink}
        fontSize={HERO_FONT.label}
        style={LABEL}
      >
        {tag}
      </text>
    </g>
  );
}

/**
 * Vehicles roll in along the ring road and queue at the plaza's one door, one
 * tile apart so their names clear each other at the label size.
 */
const ARRIVALS = VEHICLES.map((vehicle, i) => {
  const [x, y] = iso(6.4, 3.2 + i * 1.15);
  return {
    vehicle,
    x,
    y,
    dx: 300 + i * 40,
    dy: -150 - i * 20,
    delay: 2.4 + i * 0.5,
  };
});

export function CityHero() {
  const run = useCityMotion();

  return (
    <div className="ic-hero absolute inset-0" data-run={run ? "true" : "false"}>
      <style>{CSS}</style>
      <svg
        viewBox="0 0 1200 700"
        preserveAspectRatio="xMidYMid slice"
        className="h-full w-full"
        aria-hidden="true"
      >
        <defs>
          <linearGradient id="ic-sky-night" x1="0" y1="0" x2="0" y2="1">
            <stop offset="0%" stopColor={CITY.sky} />
            <stop offset="100%" stopColor={CITY.skyLow} />
          </linearGradient>
          <linearGradient id="ic-sky-day" x1="0" y1="0" x2="0" y2="1">
            <stop offset="0%" stopColor={CITY.dawnTop} />
            <stop offset="60%" stopColor={CITY.dawnMid} />
            <stop offset="100%" stopColor={CITY.dawnLow} />
          </linearGradient>
        </defs>

        <rect width="1200" height="700" fill="url(#ic-sky-night)" />
        <rect
          className="ic-h-day"
          width="1200"
          height="700"
          fill="url(#ic-sky-day)"
          opacity="0.35"
        />

        <g transform="translate(560, 250)">
          {/* Ground plate and ring road, laid before the first building */}
          <polygon
            points={tile(-5, -2, 17, 13)}
            fill={CITY.ground}
            stroke={CITY.edge}
          />
          {ROADS.map((r) => (
            <polygon
              key={`${r.gx}-${r.gy}`}
              points={tile(r.gx, r.gy, r.sx, r.sy)}
              fill={CITY.road}
            />
          ))}

          {/* The plaza: one gateway, one door */}
          <g className="ic-h-rise">
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
            <polygon
              points={PLAZA.top}
              fill={CITY.plazaTop}
              stroke={CITY.edge}
            />
            <polygon
              points={rightQuad(6, 4.1, 0, 0.9, 18)}
              fill={CITY.window}
              opacity="0.9"
            />
            <circle
              className="ic-h-beacon"
              cx={PLAZA.roof[0]}
              cy={PLAZA.roof[1] - 14}
              r="5"
              fill={CITY.accent}
            />
            <text
              x={PLAZA.roof[0]}
              y={PLAZA.roof[1] - 28}
              textAnchor="middle"
              fill={CITY.heading}
              fontFamily={FONTS.heading}
              fontSize={HERO_FONT.heading}
            >
              Gateway
            </text>
            <text
              x={PLAZA.roof[0]}
              y={PLAZA.roof[1] + 14}
              textAnchor="middle"
              fill={CITY.ink}
              fontSize={HERO_FONT.label}
              style={LABEL}
            >
              ONE DOOR
            </text>
          </g>

          {BLOCKS.map((block, i) => (
            <CityBuilding
              key={block.name}
              gx={block.cell[0]}
              gy={block.cell[1]}
              height={block.height}
              name={block.name}
              tag={block.language}
              flag={block.flag}
              delay={0.5 + i * 0.4}
              muted={false}
            />
          ))}

          {SOURCE_BLOCKS.map((source, i) => (
            <CityBuilding
              key={source.name}
              gx={source.cell[0]}
              gy={source.cell[1]}
              height={source.height}
              name={source.name}
              tag={source.kind}
              flag={null}
              delay={2.5 + i * 0.4}
              muted
            />
          ))}

          {ARRIVALS.map(({ vehicle, x, y, dx, dy, delay }) => (
            <g
              key={vehicle.name}
              className="ic-h-drive"
              style={
                {
                  "--dx": `${dx}px`,
                  "--dy": `${dy}px`,
                  animationDelay: `${delay}s`,
                } as CSSProperties
              }
            >
              <polygon
                points={poly([
                  [x, y - 10],
                  [x + vehicle.length * 0.6, y - 10 + vehicle.length * 0.3],
                  [x + vehicle.length * 0.6, y + vehicle.length * 0.3],
                  [x, y],
                ])}
                fill={vehicle.tint}
                opacity="0.85"
              />
              <text
                x={x - 10}
                y={y + 6}
                textAnchor="end"
                fill={CITY.ink}
                fontSize={HERO_FONT.label}
                style={LABEL}
              >
                {vehicle.name}
              </text>
            </g>
          ))}
        </g>
      </svg>
    </div>
  );
}
