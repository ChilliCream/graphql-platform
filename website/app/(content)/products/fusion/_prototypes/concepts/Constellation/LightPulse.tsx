"use client";

import { anim, useSceneMotion } from "./hooks";
import { CN, PLANETS, PROBES } from "./palette";

/**
 * "What is Fusion?": one query as a single light pulse.
 *
 * A probe on the rim fires a pulse at the star; the star works out which
 * planets hold the data and fans the pulse onward to three of them, their
 * answers travel back along the same arcs, and one wider beam returns to the
 * probe. The rest frame keeps every arc lit at once, so a still image still
 * shows one query reaching several subgraphs and one response coming back.
 */

const VIEW_W = 640;
const VIEW_H = 440;
const STAR = { x: 306, y: 214 };
const PROBE = { x: 62, y: 352 };

/** The three planets this query touches, with the arc the light follows. */
const TARGETS = PLANETS.slice(0, 3).map((planet, i) => ({
  planet,
  at: [
    { x: 522, y: 92 },
    { x: 574, y: 256 },
    { x: 392, y: 388 },
  ][i],
  bend: [-70, 34, 58][i],
}));

/** Quadratic arc between two points, bowed by `bend` px perpendicular. */
function arc(
  from: { readonly x: number; readonly y: number },
  to: { readonly x: number; readonly y: number },
  bend: number,
): string {
  const mx = (from.x + to.x) / 2;
  const my = (from.y + to.y) / 2;
  const dx = to.x - from.x;
  const dy = to.y - from.y;
  const length = Math.max(1, Math.sqrt(dx * dx + dy * dy));
  const cx = mx + (-dy / length) * bend;
  const cy = my + (dx / length) * bend;
  return `M ${from.x} ${from.y} Q ${cx} ${cy} ${to.x} ${to.y}`;
}

const REQUEST = arc(PROBE, STAR, -46);
const CYCLE = 7.4;

export function LightPulse() {
  const running = useSceneMotion();

  return (
    <div className="absolute inset-0">
      <style>{`
        @keyframes cn9-lp-travel {
          0% { stroke-dashoffset: 100; opacity: 0; }
          3% { opacity: 1; }
          18% { stroke-dashoffset: 0; opacity: 1; }
          25% { opacity: 0; }
          100% { stroke-dashoffset: 0; opacity: 0; }
        }
        @keyframes cn9-lp-flare {
          0%, 100% { opacity: 0.5; }
          22%, 30% { opacity: 1; }
        }
      `}</style>

      <svg
        viewBox={`0 0 ${VIEW_W} ${VIEW_H}`}
        preserveAspectRatio="xMidYMid meet"
        className="h-full w-full"
      >
        <defs>
          <radialGradient id="cn9-lp-star" cx="50%" cy="50%" r="50%">
            <stop offset="0%" stopColor={CN.starCore} stopOpacity="0.95" />
            <stop offset="100%" stopColor={CN.star} stopOpacity="0" />
          </radialGradient>
        </defs>

        {/* Rest arcs: the shape of the plan, always visible. */}
        <path
          d={REQUEST}
          fill="none"
          stroke={CN.beam}
          strokeWidth="1"
          opacity="0.2"
        />
        {TARGETS.map(({ planet, at, bend }) => (
          <path
            key={`rest-${planet.name}`}
            d={arc(STAR, at, bend)}
            fill="none"
            stroke={CN.orbit}
            strokeWidth="1"
            opacity="0.35"
          />
        ))}

        {/* 1. the query leaves the probe. */}
        <path
          d={REQUEST}
          fill="none"
          stroke={CN.beam}
          strokeWidth="2.5"
          strokeLinecap="round"
          pathLength={100}
          strokeDasharray="100 100"
          opacity={running ? 0 : 0.9}
          style={{
            animation: anim(
              running,
              `cn9-lp-travel ${CYCLE}s linear 0s infinite`,
            ),
          }}
        />

        {/* 2. the gateway fans it out to the subgraphs that hold the data. */}
        {TARGETS.map(({ planet, at, bend }, i) => (
          <path
            key={`out-${planet.name}`}
            d={arc(STAR, at, bend)}
            fill="none"
            stroke={CN.beam}
            strokeWidth="2"
            strokeLinecap="round"
            pathLength={100}
            strokeDasharray="100 100"
            opacity={running ? 0 : 0.85}
            style={{
              animation: anim(
                running,
                `cn9-lp-travel ${CYCLE}s linear ${1.5 + i * 0.16}s infinite`,
              ),
            }}
          />
        ))}

        {/* 3. each answer travels back to the star. */}
        {TARGETS.map(({ planet, at, bend }, i) => (
          <path
            key={`back-${planet.name}`}
            d={arc(at, STAR, -bend)}
            fill="none"
            stroke={planet.colour}
            strokeWidth="2"
            strokeLinecap="round"
            pathLength={100}
            strokeDasharray="100 100"
            opacity={running ? 0 : 0.7}
            style={{
              animation: anim(
                running,
                `cn9-lp-travel ${CYCLE}s linear ${3.1 + i * 0.16}s infinite`,
              ),
            }}
          />
        ))}

        {/* 4. one response returns to the probe. */}
        <path
          d={arc(STAR, PROBE, 46)}
          fill="none"
          stroke={CN.star}
          strokeWidth="4"
          strokeLinecap="round"
          pathLength={100}
          strokeDasharray="100 100"
          opacity={running ? 0 : 0.95}
          style={{
            animation: anim(
              running,
              `cn9-lp-travel ${CYCLE}s linear 4.8s infinite`,
            ),
          }}
        />

        <circle
          cx={STAR.x}
          cy={STAR.y}
          r="66"
          fill="url(#cn9-lp-star)"
          opacity="0.5"
          style={{
            animation: anim(
              running,
              `cn9-lp-flare ${CYCLE}s ease-in-out infinite`,
            ),
          }}
        />
        <circle cx={STAR.x} cy={STAR.y} r="13" fill={CN.starCore} />
        <text
          x={STAR.x}
          y={STAR.y + 40}
          fill={CN.ink}
          fontSize="12"
          textAnchor="middle"
          style={{ fontFamily: CN.mono, letterSpacing: "0.16em" }}
        >
          GATEWAY
        </text>
        <text
          x={STAR.x}
          y={STAR.y + 58}
          fill={CN.dim}
          fontSize="11"
          textAnchor="middle"
          style={{ fontFamily: CN.mono, letterSpacing: "0.1em" }}
        >
          composite schema
        </text>

        <g>
          <circle cx={PROBE.x} cy={PROBE.y} r="7" fill={CN.beam} />
          <circle
            cx={PROBE.x}
            cy={PROBE.y}
            r="15"
            fill="none"
            stroke={CN.beam}
            strokeWidth="1"
            opacity="0.45"
          />
          <text
            x={PROBE.x + 22}
            y={PROBE.y + 5}
            fill={CN.ink}
            fontSize="12"
            style={{ fontFamily: CN.mono, letterSpacing: "0.14em" }}
          >
            {PROBES[0].toUpperCase()}
          </text>
          <text
            x={PROBE.x - 4}
            y={PROBE.y + 34}
            fill={CN.dim}
            fontSize="11"
            style={{ fontFamily: CN.mono }}
          >
            one query · one response
          </text>
        </g>

        {TARGETS.map(({ planet, at }) => (
          <g key={planet.name}>
            <circle cx={at.x} cy={at.y} r="9" fill={planet.colour} />
            <circle
              cx={at.x}
              cy={at.y}
              r="17"
              fill="none"
              stroke={planet.colour}
              strokeWidth="1.2"
              opacity="0.6"
            />
            <text
              x={at.x}
              y={at.y - 26}
              fill={CN.ink}
              fontSize="12"
              textAnchor="middle"
              style={{ fontFamily: CN.mono, letterSpacing: "0.1em" }}
            >
              {planet.name}
            </text>
            <text
              x={at.x}
              y={at.y + 34}
              fill={CN.dim}
              fontSize="11"
              textAnchor="middle"
              style={{ fontFamily: CN.mono, letterSpacing: "0.12em" }}
            >
              {planet.language}
            </text>
          </g>
        ))}
      </svg>
    </div>
  );
}
