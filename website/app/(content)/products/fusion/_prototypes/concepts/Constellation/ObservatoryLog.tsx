"use client";

import { TYPE } from "../../brand";
import { useCycle, useSceneMotion } from "./hooks";
import { CN, PLANETS } from "./palette";

/**
 * "Composition protects the graph, Nitro protects your clients": the
 * observatory log.
 *
 * A subgraph drops a field, the orbits still compose - the composition badge
 * stays green the whole way through - while the observatory beam runs down the
 * log of which probe relies on which planet and marks each published operation
 * safe, risky or breaking. The rest frame holds the finished verdict, so the
 * still image shows a green build next to a broken client.
 */

const VIEW_W = 640;
const VIEW_H = 440;
const STAR = { x: 520, y: 142 };

type Verdict = "SAFE" | "RISKY" | "BREAKING";

interface Row {
  readonly probe: string;
  readonly operation: string;
  readonly verdict: Verdict;
}

const ROWS: readonly Row[] = [
  { probe: "Web", operation: "Catalog.price", verdict: "SAFE" },
  { probe: "Mobile", operation: "Catalog.stockLevel", verdict: "BREAKING" },
  { probe: "Partner API", operation: "Billing.invoice", verdict: "SAFE" },
  { probe: "Agent", operation: "Ordering.status", verdict: "RISKY" },
];

const VERDICT_COLOUR: Readonly<Record<Verdict, string>> = {
  SAFE: CN.clear,
  RISKY: CN.caution,
  BREAKING: CN.alert,
};

const ROW_TOP = 116;
const ROW_H = 62;
const STEPS = ROWS.length + 1;

export function ObservatoryLog() {
  const running = useSceneMotion();
  const scanned = useCycle(running, STEPS, 1700, ROWS.length);

  return (
    <div className="absolute inset-0">
      <svg
        viewBox={`0 0 ${VIEW_W} ${VIEW_H}`}
        preserveAspectRatio="xMidYMid meet"
        className="h-full w-full"
      >
        <text
          x={24}
          y={40}
          fill={CN.dim}
          fontSize={TYPE.label}
          style={{ fontFamily: CN.mono, letterSpacing: "0.16em" }}
        >
          OBSERVATORY LOG · WHICH PROBE RELIES ON WHICH PLANET
        </text>

        <rect
          x={20}
          y={56}
          width={396}
          height={ROW_TOP - 56 + ROWS.length * ROW_H + 10}
          rx="10"
          fill={CN.panel}
          stroke={CN.panelEdge}
        />

        <text
          x={36}
          y={92}
          fill={CN.ink}
          fontSize={TYPE.label}
          style={{ fontFamily: CN.mono, letterSpacing: "0.1em" }}
        >
          schema change · Catalog removes stockLevel
        </text>

        <rect
          x={36}
          y={ROW_TOP - 26}
          width={364}
          height={ROW_H - 14}
          rx="6"
          fill={CN.beam}
          opacity={scanned < ROWS.length ? 0.09 : 0}
          style={{
            transform: `translate(0px, ${scanned * ROW_H}px)`,
            transition: "transform 500ms ease-out, opacity 400ms linear",
          }}
        />

        {ROWS.map((row, i) => {
          const done = i < scanned;
          const y = ROW_TOP + i * ROW_H;

          return (
            <g key={row.probe}>
              <text
                x={36}
                y={y}
                fill={CN.ink}
                fontSize={TYPE.label}
                style={{ fontFamily: CN.mono, letterSpacing: "0.08em" }}
              >
                {`${row.probe} → ${row.operation}`}
              </text>
              <text
                x={36}
                y={y + 18}
                fill={CN.dim}
                fontSize={TYPE.label}
                style={{ fontFamily: CN.mono, letterSpacing: "0.12em" }}
              >
                PUBLISHED OPERATION
              </text>
              <g
                opacity={done ? 1 : 0}
                style={{ transition: "opacity 400ms linear" }}
              >
                <rect
                  x={296}
                  y={y - 16}
                  width={104}
                  height={24}
                  rx="12"
                  fill="none"
                  stroke={VERDICT_COLOUR[row.verdict]}
                  strokeWidth="1.2"
                  opacity="0.9"
                />
                <text
                  x={348}
                  y={y}
                  fill={VERDICT_COLOUR[row.verdict]}
                  fontSize={TYPE.label}
                  textAnchor="middle"
                  style={{ fontFamily: CN.mono, letterSpacing: "0.14em" }}
                >
                  {row.verdict}
                </text>
              </g>
            </g>
          );
        })}

        {/* The build stays green the whole way through: that is the point. */}
        <g>
          <rect
            x={440}
            y={300}
            width={176}
            height={30}
            rx="15"
            fill="none"
            stroke={CN.clear}
            strokeWidth="1.2"
            opacity="0.8"
          />
          <text
            x={528}
            y={320}
            fill={CN.clear}
            fontSize={TYPE.label}
            textAnchor="middle"
            style={{ fontFamily: CN.mono, letterSpacing: "0.12em" }}
          >
            COMPOSITION PASSED
          </text>
          <text
            x={528}
            y={352}
            fill={scanned >= ROWS.length ? CN.alert : CN.dim}
            fontSize={TYPE.label}
            textAnchor="middle"
            style={{
              fontFamily: CN.mono,
              letterSpacing: "0.12em",
              transition: "fill 400ms linear",
            }}
          >
            1 BREAKING · 1 RISKY
          </text>
          <text
            x={528}
            y={372}
            fill={CN.dim}
            fontSize={TYPE.label}
            textAnchor="middle"
            style={{ fontFamily: CN.mono, letterSpacing: "0.12em" }}
          >
            FLAGGED BEFORE MERGE
          </text>
        </g>

        <g>
          <circle cx={STAR.x} cy={STAR.y} r="10" fill={CN.starCore} />
          {PLANETS.slice(0, 3).map((planet, i) => {
            const radius = 40 + i * 26;
            const changed = planet.name === "Catalog";

            return (
              <g key={planet.name}>
                <ellipse
                  cx={STAR.x}
                  cy={STAR.y}
                  rx={radius}
                  ry={radius * 0.46}
                  fill="none"
                  stroke={CN.orbitFaint}
                  strokeWidth="1"
                />
                <circle
                  cx={
                    STAR.x + radius * Math.cos(((i * 120 - 40) * Math.PI) / 180)
                  }
                  cy={
                    STAR.y +
                    radius * 0.46 * Math.sin(((i * 120 - 40) * Math.PI) / 180)
                  }
                  r={changed ? 8 : 6}
                  fill={planet.colour}
                  stroke={changed ? CN.alert : "none"}
                  strokeWidth="1.4"
                />
              </g>
            );
          })}
          <text
            x={STAR.x}
            y={STAR.y + 78}
            fill={CN.dim}
            fontSize={TYPE.label}
            textAnchor="middle"
            style={{ fontFamily: CN.mono, letterSpacing: "0.14em" }}
          >
            CATALOG · SCHEMA CHANGE
          </text>
        </g>
      </svg>
    </div>
  );
}
