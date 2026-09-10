"use client";

import { useCycle, useSceneMotion } from "./hooks";
import { CN, PLANETS, orbitPoint } from "./palette";

/**
 * "Any GraphQL server, no plugin": composition as the gravitational check.
 *
 * Five ordinary servers sit on their own orbits, each planet coloured by the
 * language it is written in and carrying nothing but its own schema; a sweep
 * leaves the star and clears one orbit at a time, and the run ends either with
 * every orbit clear or - on the last pass - with Shipping's orbit crossing
 * Accounts', a collision that stops the build before anything is deployed. The
 * rest frame is the cleared system with composition passed.
 */

const VIEW_W = 640;
const VIEW_H = 440;
const STAR = { x: 320, y: 214 };
const TILT = 0.46;
const SCALE = 0.78;

const STEPS = 6;
const CONFLICT = 5;
const CLEARED = 4;

/** The two orbits the conflict pass pushes into each other. */
const OFFENDER = "Shipping";
const NEIGHBOUR = "Accounts";

export function GravityCheck() {
  const running = useSceneMotion();
  const step = useCycle(running, STEPS, 1500, CLEARED);
  const conflict = step === CONFLICT;

  const neighbourRadius =
    (PLANETS.find((planet) => planet.name === NEIGHBOUR)?.radius ?? 0) * SCALE;

  const sweepRadius = conflict
    ? neighbourRadius
    : PLANETS[Math.min(step, PLANETS.length - 1)].radius * SCALE;

  const status = conflict
    ? "ORBIT COLLISION · TYPE CONFLICT · COMPOSITION STOPPED"
    : step >= CLEARED
      ? "COMPOSITION PASSED · COMPOSITE SCHEMA BUILT"
      : "COMPOSITION · CHECKING SOURCE SCHEMAS";

  return (
    <div className="absolute inset-0">
      <svg
        viewBox={`0 0 ${VIEW_W} ${VIEW_H}`}
        preserveAspectRatio="xMidYMid meet"
        className="h-full w-full"
      >
        <g
          style={{
            transformBox: "view-box",
            transformOrigin: `${STAR.x}px ${STAR.y}px`,
            transform: `scale(${Math.max(sweepRadius, 1)})`,
            transition: "transform 900ms ease-out",
          }}
        >
          <ellipse
            cx={STAR.x}
            cy={STAR.y}
            rx="1"
            ry={TILT}
            fill="none"
            stroke={conflict ? CN.alert : CN.clear}
            strokeWidth="1.5"
            opacity="0.55"
            vectorEffect="non-scaling-stroke"
          />
        </g>

        {PLANETS.map((planet, i) => {
          const offending = conflict && planet.name === OFFENDER;
          const radius = planet.radius * SCALE;
          const cleared = !conflict && step >= i;

          return (
            <g
              key={`orbit-${planet.name}`}
              style={{
                transformBox: "view-box",
                transformOrigin: `${STAR.x}px ${STAR.y}px`,
                transform: `scale(${offending ? neighbourRadius / radius : 1})`,
                transition: "transform 900ms ease-out",
              }}
            >
              <ellipse
                cx={STAR.x}
                cy={STAR.y}
                rx={radius}
                ry={radius * TILT}
                fill="none"
                stroke={
                  offending ? CN.alert : cleared ? CN.clear : CN.orbitFaint
                }
                strokeWidth={offending ? 1.6 : 1}
                opacity={offending ? 0.9 : cleared ? 0.45 : 0.35}
                vectorEffect="non-scaling-stroke"
                style={{
                  transition: "stroke 500ms linear, opacity 500ms linear",
                }}
              />
            </g>
          );
        })}

        <circle cx={STAR.x} cy={STAR.y} r="12" fill={CN.starCore} />
        <text
          x={STAR.x}
          y={STAR.y - 22}
          fill={CN.dim}
          fontSize="11"
          textAnchor="middle"
          style={{ fontFamily: CN.mono, letterSpacing: "0.14em" }}
        >
          GATEWAY
        </text>

        {PLANETS.map((planet, i) => {
          const offending = conflict && planet.name === OFFENDER;
          const point = orbitPoint(planet.radius * SCALE, planet.angle, TILT);
          const crossed = orbitPoint(neighbourRadius, planet.angle, TILT);
          const at = { x: STAR.x + point.x, y: STAR.y + point.y };
          const cleared = !conflict && step >= i;
          const below = point.y >= 0;

          return (
            <g
              key={planet.name}
              opacity={conflict && !offending ? 0.55 : 1}
              style={{
                transform: offending
                  ? `translate(${crossed.x - point.x}px, ${crossed.y - point.y}px)`
                  : "translate(0px, 0px)",
                transition: "transform 900ms ease-out, opacity 400ms linear",
              }}
            >
              <circle cx={at.x} cy={at.y} r="8" fill={planet.colour} />
              {offending ? (
                <circle
                  cx={at.x}
                  cy={at.y}
                  r="20"
                  fill="none"
                  stroke={CN.alert}
                  strokeWidth="1.4"
                  opacity="0.9"
                />
              ) : null}
              <text
                x={at.x}
                y={below ? at.y + 26 : at.y - 20}
                fill={offending ? CN.alert : CN.ink}
                fontSize="12"
                textAnchor="middle"
                style={{ fontFamily: CN.mono, letterSpacing: "0.08em" }}
              >
                {planet.name}
              </text>
              <text
                x={at.x}
                y={below ? at.y + 40 : at.y - 6}
                fill={cleared ? CN.clear : CN.dim}
                fontSize="10"
                textAnchor="middle"
                style={{ fontFamily: CN.mono, letterSpacing: "0.14em" }}
              >
                {`${planet.language}${cleared ? " ✓" : ""}`}
              </text>
            </g>
          );
        })}

        <text
          x={20}
          y={30}
          fill={CN.dim}
          fontSize="11"
          style={{ fontFamily: CN.mono, letterSpacing: "0.12em" }}
        >
          ORDINARY GRAPHQL SERVERS · NO PLUGIN ATTACHED
        </text>

        <text
          x={20}
          y={VIEW_H - 20}
          fill={conflict ? CN.alert : CN.clear}
          fontSize="11"
          style={{
            fontFamily: CN.mono,
            letterSpacing: "0.12em",
            transition: "fill 400ms linear",
          }}
        >
          {status}
        </text>
      </svg>
    </div>
  );
}
