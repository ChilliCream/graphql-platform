"use client";

import { useCycle, useSceneMotion } from "./hooks";
import type { PlanetSpec } from "./palette";
import { CN, MOONS, PLANETS, orbitPoint, ringDash, specTag } from "./palette";

/**
 * "Both specifications, one gateway": one system, two ring styles.
 *
 * Every planet keeps its own colour for the language its server is written in
 * and wears a ring for the specification it is written to - solid for one,
 * dashed for the other, neither drawn as the default - and the loop captures
 * an OpenAPI moon and a gRPC moon into the same system before moving Shipping
 * from one ring style to the other with no other orbit disturbed. The rest
 * frame is the settled system with both ring styles and both moons in place.
 */

const VIEW_W = 640;
const VIEW_H = 440;
const STAR = { x: 320, y: 218 };
const TILT = 0.46;
const SCALE = 0.8;

interface Placed {
  readonly x: number;
  readonly y: number;
}

function place(radius: number, angle: number): Placed {
  const point = orbitPoint(radius * SCALE, angle, TILT);
  return { x: STAR.x + point.x, y: STAR.y + point.y };
}

/** Where a moon waits before the gateway captures it, off the rim. */
const ENTRY: Readonly<Record<string, Placed>> = {
  Payments: { x: -80, y: 40 },
  Inventory: { x: 720, y: 400 },
};

export function RingSpectrum() {
  const running = useSceneMotion();
  const step = useCycle(running, 4, 2600, 3);

  return (
    <div className="absolute inset-0">
      <svg
        viewBox={`0 0 ${VIEW_W} ${VIEW_H}`}
        preserveAspectRatio="xMidYMid meet"
        className="h-full w-full"
      >
        <defs>
          <radialGradient id="cn9-rs-star" cx="50%" cy="50%" r="50%">
            <stop offset="0%" stopColor={CN.starCore} stopOpacity="0.9" />
            <stop offset="100%" stopColor={CN.star} stopOpacity="0" />
          </radialGradient>
        </defs>

        {PLANETS.map((planet) => (
          <ellipse
            key={`orbit-${planet.name}`}
            cx={STAR.x}
            cy={STAR.y}
            rx={planet.radius * SCALE}
            ry={planet.radius * SCALE * TILT}
            fill="none"
            stroke={CN.orbitFaint}
            strokeWidth="1"
          />
        ))}

        <circle
          cx={STAR.x}
          cy={STAR.y}
          r="54"
          fill="url(#cn9-rs-star)"
          opacity="0.45"
        />
        <circle cx={STAR.x} cy={STAR.y} r="12" fill={CN.starCore} />
        <text
          x={STAR.x}
          y={STAR.y - 24}
          fill={CN.dim}
          fontSize="11"
          textAnchor="middle"
          style={{ fontFamily: CN.mono, letterSpacing: "0.14em" }}
        >
          ONE COMPOSITE SCHEMA
        </text>

        {PLANETS.map((planet) => {
          const at = place(planet.radius, planet.angle);
          const crossed = planet.name === "Shipping" && step >= 3;
          const spec: PlanetSpec = crossed ? "GraphQL Federation" : planet.spec;
          const below = at.y >= STAR.y;
          const nameY = below ? at.y + 28 : at.y - 22;
          const tagY = below ? at.y + 42 : at.y - 8;

          return (
            <g key={planet.name}>
              <circle cx={at.x} cy={at.y} r="8" fill={planet.colour} />
              <circle
                cx={at.x}
                cy={at.y}
                r="16"
                fill="none"
                stroke={planet.colour}
                strokeWidth="1.4"
                strokeDasharray={ringDash(spec)}
                opacity="0.8"
              />
              {crossed ? (
                <circle
                  cx={at.x}
                  cy={at.y}
                  r="24"
                  fill="none"
                  stroke={CN.clear}
                  strokeWidth="1"
                  opacity="0.6"
                />
              ) : null}
              <text
                x={at.x}
                y={nameY}
                fill={CN.ink}
                fontSize="12"
                textAnchor="middle"
                style={{ fontFamily: CN.mono, letterSpacing: "0.08em" }}
              >
                {`${planet.name} · ${planet.language}`}
              </text>
              <text
                x={at.x}
                y={tagY}
                fill={crossed ? CN.clear : CN.dim}
                fontSize="10"
                textAnchor="middle"
                style={{
                  fontFamily: CN.mono,
                  letterSpacing: "0.14em",
                  transition: "fill 400ms linear",
                }}
              >
                {specTag(spec)}
              </text>
            </g>
          );
        })}

        {MOONS.map((moon, i) => {
          const host = PLANETS.find((planet) => planet.name === moon.around);
          const hostAt = host ? place(host.radius, host.angle) : STAR;
          const captured = step >= i + 1;
          const target = { x: hostAt.x + 30, y: hostAt.y - 16 };
          const from = ENTRY[moon.name];
          const at = captured ? target : from;

          return (
            <g key={moon.name}>
              <ellipse
                cx={hostAt.x}
                cy={hostAt.y}
                rx="30"
                ry="16"
                fill="none"
                stroke={CN.orbit}
                strokeWidth="1"
                strokeDasharray="3 4"
                opacity={captured ? 0.5 : 0}
                style={{ transition: "opacity 700ms linear" }}
              />
              <g
                style={{
                  transform: `translate(${at.x - target.x}px, ${at.y - target.y}px)`,
                  opacity: captured ? 1 : 0,
                  transition: "transform 1100ms ease-out, opacity 700ms linear",
                }}
              >
                <circle
                  cx={target.x}
                  cy={target.y}
                  r="4.5"
                  fill={CN.ink}
                  opacity="0.9"
                />
                <text
                  x={target.x + 10}
                  y={target.y - 4}
                  fill={CN.dim}
                  fontSize="10"
                  style={{ fontFamily: CN.mono, letterSpacing: "0.12em" }}
                >
                  {`${moon.name.toUpperCase()} · ${moon.kind.toUpperCase()}`}
                </text>
              </g>
            </g>
          );
        })}

        <text
          x={20}
          y={VIEW_H - 20}
          fill={CN.dim}
          fontSize="11"
          style={{ fontFamily: CN.mono, letterSpacing: "0.12em" }}
        >
          SOLID RING = GRAPHQL FEDERATION | DASHED RING = APOLLO FEDERATION
        </text>
      </svg>
    </div>
  );
}
