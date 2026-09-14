"use client";

import { useRef } from "react";

import { TYPE } from "../../brand";
import { anim, useElementMotion } from "./hooks";
import { CN, PLANETS, PROBES, orbitPoint, ringDash } from "./palette";

/**
 * Hero visual: the Fusion system seen from deep space.
 *
 * A parallax starfield drifts behind three depths of sky while the star - the
 * gateway - burns at the centre of five orbits. Each subgraph enters its orbit
 * in turn and then circles at its own period, its colour the language its
 * server is written in and its ring the specification it is written to; probes
 * on the rim send light pulses inward. At rest the system is already formed,
 * so the still frame is the finished constellation rather than an empty sky.
 *
 * The sky fills the hero with `slice` rather than shrinking to fit it: a
 * 1200-unit sky letterboxed into a 375px screen would render its lettering at
 * about 4px, while filling the hero keeps the scale at or above 1x there, so
 * the smallest label (`TYPE.label`) clears the 11px floor and the viewport
 * simply shows less sky. The star sits right of the sky's centre, which the
 * `xMid` slice window crops away on a phone, so below `sm` the sky hangs off
 * the left edge and its centre - and with it the gateway star - moves back
 * into frame.
 */

const VIEW_W = 1200;
const VIEW_H = 700;
const STAR_X = 800;
const STAR_Y = 348;

interface Spark {
  readonly x: string;
  readonly y: string;
  readonly r: string;
  readonly o: string;
}

/** Deterministic PRNG so the server and the client draw the same sky. */
function mulberry32(seed: number): () => number {
  let a = seed;
  return () => {
    a = (a + 0x6d2b79f5) | 0;
    let t = Math.imul(a ^ (a >>> 15), 1 | a);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

function sparkLayer(seed: number, count: number, scale: number): Spark[] {
  const rand = mulberry32(seed);
  const sparks: Spark[] = [];
  for (let i = 0; i < count; i++) {
    sparks.push({
      x: (rand() * (VIEW_W + 120) - 60).toFixed(1),
      y: (rand() * VIEW_H).toFixed(1),
      r: (scale * (0.5 + rand() * 0.9)).toFixed(2),
      o: (0.2 + rand() * 0.55).toFixed(2),
    });
  }
  return sparks;
}

const LAYERS = [
  { sparks: sparkLayer(9101, 90, 0.9), drift: 78, opacity: 0.5 },
  { sparks: sparkLayer(9102, 55, 1.3), drift: 52, opacity: 0.75 },
  { sparks: sparkLayer(9103, 26, 1.9), drift: 34, opacity: 1 },
];

/** Probe launch tracks: where a client sits on the rim and how far it reaches. */
const TRACKS = [
  { label: PROBES[0], x: 132, y: 132 },
  { label: PROBES[1], x: 96, y: 566 },
  { label: PROBES[2], x: 1108, y: 604 },
];

export function StarField() {
  const ref = useRef<HTMLDivElement>(null);
  const running = useElementMotion(ref);

  return (
    <div
      ref={ref}
      aria-hidden="true"
      className="absolute inset-y-0 left-[-53%] w-[153%] sm:left-0 sm:w-full"
    >
      <style>{`
        @keyframes cn9-hero-drift { from { transform: translateX(0); } to { transform: translateX(-46px); } }
        @keyframes cn9-hero-spin { from { transform: rotate(0deg); } to { transform: rotate(360deg); } }
        @keyframes cn9-hero-spin-back { from { transform: rotate(0deg); } to { transform: rotate(-360deg); } }
        @keyframes cn9-hero-enter { from { opacity: 0; transform: scale(0.2); } to { opacity: 1; transform: scale(1); } }
        @keyframes cn9-hero-draw { from { stroke-dashoffset: 1700; opacity: 0; } to { stroke-dashoffset: 0; opacity: 1; } }
        @keyframes cn9-hero-breathe { 0%, 100% { opacity: 0.55; transform: scale(1); } 50% { opacity: 0.9; transform: scale(1.08); } }
        @keyframes cn9-hero-pulse { 0% { stroke-dashoffset: 260; opacity: 0; } 12% { opacity: 0.9; } 88% { opacity: 0.9; } 100% { stroke-dashoffset: 0; opacity: 0; } }
      `}</style>

      <svg
        viewBox={`0 0 ${VIEW_W} ${VIEW_H}`}
        preserveAspectRatio="xMidYMid slice"
        className="h-full w-full"
      >
        <defs>
          <radialGradient id="cn9-hero-core" cx="50%" cy="50%" r="50%">
            <stop offset="0%" stopColor={CN.starCore} stopOpacity="1" />
            <stop offset="45%" stopColor={CN.star} stopOpacity="0.85" />
            <stop offset="100%" stopColor={CN.star} stopOpacity="0" />
          </radialGradient>
          <radialGradient id="cn9-hero-sky" cx="66%" cy="50%" r="72%">
            <stop offset="0%" stopColor={CN.sky} stopOpacity="0.9" />
            <stop offset="100%" stopColor={CN.bg} stopOpacity="1" />
          </radialGradient>
        </defs>

        <rect width={VIEW_W} height={VIEW_H} fill="url(#cn9-hero-sky)" />

        {LAYERS.map((layer, i) => (
          <g
            key={i}
            opacity={layer.opacity}
            style={{
              animation: anim(
                running,
                `cn9-hero-drift ${layer.drift}s ease-in-out infinite alternate`,
              ),
            }}
          >
            {layer.sparks.map((spark, j) => (
              <circle
                key={j}
                cx={spark.x}
                cy={spark.y}
                r={spark.r}
                fill={CN.ink}
                opacity={spark.o}
              />
            ))}
          </g>
        ))}

        {TRACKS.map((track, i) => {
          const dx = STAR_X - track.x;
          const dy = STAR_Y - track.y;
          const length = Math.sqrt(dx * dx + dy * dy);
          const tx = track.x + (dx / length) * (length - 60);
          const ty = track.y + (dy / length) * (length - 60);

          return (
            <g key={track.label}>
              <line
                x1={track.x}
                y1={track.y}
                x2={tx}
                y2={ty}
                stroke={CN.beam}
                strokeWidth="1"
                opacity="0.16"
              />
              <line
                x1={track.x}
                y1={track.y}
                x2={tx}
                y2={ty}
                stroke={CN.beam}
                strokeWidth="2"
                strokeLinecap="round"
                strokeDasharray="70 260"
                strokeDashoffset={running ? undefined : 130}
                opacity={running ? 0 : 0.7}
                style={{
                  animation: anim(
                    running,
                    `cn9-hero-pulse ${5.5 + i * 1.3}s linear ${i * 1.1}s infinite`,
                  ),
                }}
              />
              <circle cx={track.x} cy={track.y} r="4" fill={CN.beam} />
              <text
                x={track.x}
                y={track.y - 14}
                fill={CN.dim}
                fontSize={TYPE.caption}
                textAnchor="middle"
                style={{ fontFamily: CN.mono, letterSpacing: "0.14em" }}
              >
                {track.label.toUpperCase()}
              </text>
            </g>
          );
        })}

        {PLANETS.map((planet, i) => (
          <circle
            key={`orbit-${planet.name}`}
            cx={STAR_X}
            cy={STAR_Y}
            r={planet.radius}
            fill="none"
            stroke={CN.orbitFaint}
            strokeWidth="1"
            strokeDasharray="1700"
            style={{
              animation: anim(
                running,
                `cn9-hero-draw 1.5s ease-out ${0.15 * i}s backwards`,
              ),
            }}
          />
        ))}

        <g>
          <circle
            cx={STAR_X}
            cy={STAR_Y}
            r="120"
            fill="url(#cn9-hero-core)"
            opacity="0.55"
            style={{
              transformBox: "fill-box",
              transformOrigin: "center",
              animation: anim(
                running,
                "cn9-hero-breathe 7s ease-in-out infinite",
              ),
            }}
          />
          <circle cx={STAR_X} cy={STAR_Y} r="17" fill={CN.starCore} />
          <text
            x={STAR_X}
            y={STAR_Y + 148}
            fill={CN.dim}
            fontSize={TYPE.caption}
            textAnchor="middle"
            style={{ fontFamily: CN.mono, letterSpacing: "0.18em" }}
          >
            FUSION GATEWAY
          </text>
        </g>

        {PLANETS.map((planet, i) => {
          const rest = orbitPoint(planet.radius, planet.angle);
          const px = STAR_X + rest.x;
          const py = STAR_Y + rest.y;

          return (
            <g
              key={planet.name}
              style={{
                transformBox: "view-box",
                transformOrigin: `${STAR_X}px ${STAR_Y}px`,
                animation: anim(
                  running,
                  `cn9-hero-spin ${planet.period}s linear infinite`,
                ),
              }}
            >
              <g
                style={{
                  transformBox: "view-box",
                  transformOrigin: `${px}px ${py}px`,
                  animation: anim(
                    running,
                    `cn9-hero-enter 1.3s ease-out ${0.35 + 0.18 * i}s backwards`,
                  ),
                }}
              >
                <circle
                  cx={px}
                  cy={py}
                  r={planet.radius > 180 ? 9 : 7.5}
                  fill={planet.colour}
                />
                <circle
                  cx={px}
                  cy={py}
                  r={planet.radius > 180 ? 16 : 14}
                  fill="none"
                  stroke={planet.colour}
                  strokeWidth="1.2"
                  strokeDasharray={ringDash(planet.spec)}
                  opacity="0.75"
                />
                <g
                  style={{
                    transformBox: "view-box",
                    transformOrigin: `${px}px ${py}px`,
                    animation: anim(
                      running,
                      `cn9-hero-spin-back ${planet.period}s linear infinite`,
                    ),
                  }}
                >
                  <text
                    x={px}
                    y={py - 24}
                    fill={CN.ink}
                    fontSize={TYPE.caption}
                    textAnchor="middle"
                    style={{ fontFamily: CN.mono, letterSpacing: "0.1em" }}
                  >
                    {planet.name}
                  </text>
                  <text
                    x={px}
                    y={py + 30}
                    fill={CN.dim}
                    fontSize={TYPE.label}
                    textAnchor="middle"
                    style={{ fontFamily: CN.mono, letterSpacing: "0.14em" }}
                  >
                    {planet.language}
                  </text>
                </g>
              </g>
            </g>
          );
        })}
      </svg>
    </div>
  );
}
