"use client";

import { useRef } from "react";

import { TYPE } from "../../brand";
import { anim, useElementMotion } from "./hooks";
import { AP, CLIENTS, GATES } from "./palette";

/**
 * Hero backdrop: the apron seen from the tower. Client aircraft land on the
 * one runway at the bottom, taxi along the lit centre line and roll to the
 * gates that stand for the source schemas, while the tower beacon turns. At
 * rest the aircraft park where the layout reads as one runway serving every
 * gate.
 *
 * The apron fills the hero with `slice` rather than shrinking to fit it: a
 * 1200-unit apron letterboxed into a 375px screen would render its lettering
 * at about 4px, while filling the hero keeps the scale at or above 1x there,
 * so the smallest sign (`TYPE.caption`) stays above the 11px floor and the
 * viewport simply shows less of the apron.
 */

const W = 1200;
const H = 620;
const RUNWAY_Y = 500;
const TAXI_Y = 392;

const KEYFRAMES = `
@keyframes ap-apron-roll {
  0% { transform: translateX(-180px); }
  100% { transform: translateX(1320px); }
}
@keyframes ap-apron-taxi {
  0% { transform: translateX(1240px); }
  100% { transform: translateX(-220px); }
}
@keyframes ap-apron-beacon { to { transform: rotate(360deg); } }
@keyframes ap-apron-lights { 0%, 100% { opacity: 0.85; } 50% { opacity: 0.25; } }
@keyframes ap-apron-bridge { 0%, 100% { opacity: 0.5; } 50% { opacity: 1; } }
`;

interface PlaneProps {
  readonly tail: string;
  readonly scale: number;
  readonly color: string;
}

/** Top-down aircraft, nose to the right, with its client name on the tail. */
function Plane({ tail, scale, color }: PlaneProps) {
  return (
    <g transform={`scale(${scale})`}>
      <ellipse cx="0" cy="0" rx="20" ry="3.4" fill={color} />
      <path d="M3,-1 L-11,-15 L-3,-15 L11,-1 Z" fill={color} opacity="0.9" />
      <path d="M3,1 L-11,15 L-3,15 L11,1 Z" fill={color} opacity="0.9" />
      <path d="M-15,-1 L-21,-8 L-17,-8 L-10,-1 Z" fill={color} opacity="0.7" />
      <path d="M-15,1 L-21,8 L-17,8 L-10,1 Z" fill={color} opacity="0.7" />
      <text
        x="-24"
        y="-11"
        fill={AP.ink}
        fontFamily={AP.mono}
        fontSize={TYPE.caption}
        textAnchor="end"
        opacity="0.7"
      >
        {tail}
      </text>
    </g>
  );
}

export function HeroApron() {
  const ref = useRef<SVGSVGElement>(null);
  const running = useElementMotion(ref);

  return (
    <svg
      ref={ref}
      viewBox={`0 0 ${W} ${H}`}
      preserveAspectRatio="xMidYMid slice"
      aria-hidden="true"
      className="absolute inset-0 h-full w-full"
    >
      <style>{KEYFRAMES}</style>

      <rect width={W} height={H} fill={AP.bg} />
      <rect width={W} height="300" fill="url(#ap-hero-sky)" />
      <defs>
        <linearGradient id="ap-hero-sky" x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stopColor={AP.sky} />
          <stop offset="100%" stopColor={AP.bg} />
        </linearGradient>
      </defs>

      {/* Gates along the concourse: one stand per source schema. */}
      {GATES.map((gate, i) => {
        const x = 120 + i * 220;
        return (
          <g key={gate.name}>
            <rect
              x={x - 66}
              y="150"
              width="132"
              height="66"
              rx="8"
              fill={AP.panel}
              stroke={AP.panelEdge}
            />
            <text
              x={x}
              y="176"
              fill={AP.amber}
              fontFamily={AP.mono}
              fontSize={TYPE.h6}
              textAnchor="middle"
            >
              {gate.stand} {gate.name}
            </text>
            <text
              x={x}
              y="197"
              fill={AP.dim}
              fontFamily={AP.mono}
              fontSize={TYPE.caption}
              textAnchor="middle"
              letterSpacing="0.12em"
            >
              {gate.language}
            </text>
            {/* Boarding bridge down to the taxiway. */}
            <path
              d={`M${x} 216 L${x} ${TAXI_Y - 26}`}
              stroke={AP.taxi}
              strokeWidth="2"
              strokeDasharray="6 7"
              opacity="0.5"
              style={{
                animation: anim(
                  running,
                  `ap-apron-bridge 3200ms ease-in-out ${i * 280}ms infinite`,
                ),
              }}
            />
          </g>
        );
      })}

      {/* Tower with its turning beacon. */}
      <g>
        <path
          d="M1092 232 L1108 232 L1114 330 L1086 330 Z"
          fill={AP.panel}
          stroke={AP.panelEdge}
        />
        <rect
          x="1068"
          y="196"
          width="64"
          height="42"
          rx="6"
          fill={AP.panel}
          stroke={AP.panelEdge}
        />
        <text
          x="1100"
          y="222"
          fill={AP.approach}
          fontFamily={AP.mono}
          fontSize={TYPE.caption}
          textAnchor="middle"
        >
          TOWER
        </text>
        <path
          d="M1100,186 L1160,173 L1160,199 Z"
          fill={AP.approach}
          opacity="0.22"
          style={{
            transformBox: "view-box",
            transformOrigin: "1100px 186px",
            animation: anim(running, "ap-apron-beacon 5200ms linear infinite"),
          }}
        />
        <circle cx="1100" cy="186" r="4" fill={AP.approach} />
      </g>

      {/* Taxiway with its lit centre line. */}
      <rect x="0" y={TAXI_Y - 24} width={W} height="48" fill={AP.wash} />
      <line
        x1="0"
        y1={TAXI_Y}
        x2={W}
        y2={TAXI_Y}
        stroke={AP.taxi}
        strokeWidth="2"
        strokeDasharray="18 16"
        opacity="0.55"
      />

      {/* The one runway every client lands on. */}
      <rect x="0" y={RUNWAY_Y - 34} width={W} height="68" fill={AP.deck} />
      <line
        x1="40"
        y1={RUNWAY_Y}
        x2={W - 40}
        y2={RUNWAY_Y}
        stroke={AP.paint}
        strokeWidth="3"
        strokeDasharray="46 34"
      />
      <text
        x="44"
        y={RUNWAY_Y - 44}
        fill={AP.dim}
        fontFamily={AP.mono}
        fontSize={TYPE.h6}
        letterSpacing="0.24em"
      >
        RUNWAY 01 · ONE ENDPOINT
      </text>
      {Array.from({ length: 12 }, (_, i) => (
        <circle
          key={i}
          cx={60 + i * 96}
          cy={RUNWAY_Y + 46}
          r="3"
          fill={AP.approach}
          style={{
            animation: anim(
              running,
              `ap-apron-lights 1800ms ease-in-out ${i * 120}ms infinite`,
            ),
          }}
        />
      ))}

      {/* Client aircraft rolling out on the one runway. */}
      {CLIENTS.map((client, i) => (
        <g
          key={client}
          style={{
            animation: anim(
              running,
              `ap-apron-roll ${12000 + i * 1700}ms linear -${i * 2900}ms infinite`,
            ),
          }}
        >
          <g
            transform={`translate(${running ? 0 : 150 + i * 250} ${RUNWAY_Y - 12})`}
          >
            <Plane tail={client} scale={0.95} color={AP.approach} />
          </g>
        </g>
      ))}

      {/* Aircraft taxiing back along the taxiway towards the gates. */}
      {GATES.slice(0, 3).map((gate, i) => (
        <g
          key={gate.name}
          style={{
            animation: anim(
              running,
              `ap-apron-taxi ${16000 + i * 2600}ms linear -${i * 4200}ms infinite`,
            ),
          }}
        >
          <g
            transform={`translate(${running ? 0 : 300 + i * 300} ${TAXI_Y - 4}) scale(-1 1)`}
          >
            <Plane tail="" scale={0.72} color={AP.taxi} />
          </g>
        </g>
      ))}
    </svg>
  );
}
