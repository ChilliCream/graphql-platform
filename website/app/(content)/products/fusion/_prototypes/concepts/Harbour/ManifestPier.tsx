"use client";

import type { CSSProperties } from "react";

import { useReducedMotionPreference, useSceneActive } from "../../Primitives";
import { DUSK, FONT, LABEL, WAREHOUSES } from "./palette";

/**
 * "What is Fusion?": one manifest, one pier, one ship. The four lines of the
 * manifest are loaded as containers out of four different warehouses along the
 * quay and stacked on the single ship waiting at the pier.
 *
 * Rest state: every container already sits on the deck, so the still frame
 * still reads as one query answered from several subgraphs.
 */

const CSS = `
.hbr-pier [class*="hbr-p-"] { animation-play-state: running; }
.hbr-pier[data-run="false"] [class*="hbr-p-"] { animation: none; }
.hbr-pier .hbr-p-load { animation: hbr-p-load 9s cubic-bezier(0.37, 0, 0.63, 1) infinite; }
.hbr-pier .hbr-p-cable { animation: hbr-p-cable 3s linear infinite; }
.hbr-pier .hbr-p-row { animation: hbr-p-row 9s ease-in-out infinite; }
.hbr-pier .hbr-p-bob { animation: hbr-p-bob 7s ease-in-out infinite; }
@keyframes hbr-p-load {
  0% { transform: translate(var(--dx), var(--dy)); }
  8% { transform: translate(var(--dx), var(--dy)); }
  48%, 100% { transform: translate(0, 0); }
}
@keyframes hbr-p-cable { to { stroke-dashoffset: -36; } }
@keyframes hbr-p-row {
  0%, 6% { opacity: 0.35; }
  20%, 100% { opacity: 1; }
}
@keyframes hbr-p-bob {
  0%, 100% { transform: translate(0, 0); }
  50% { transform: translate(0, -5px); }
}
`;

const DOOR_Y = 120;
const DECK_Y = 322;
const CRATE_W = 60;

const CARGO = [
  { color: DUSK.accent, field: "product" },
  { color: DUSK.lamp, field: "price" },
  { color: DUSK.ok, field: "delivery" },
  { color: DUSK.sun, field: "buyer" },
];

/** Warehouse doors along the quay, evenly spaced across the viewBox. */
const DOORS = WAREHOUSES.map((w, i) => ({
  warehouse: w,
  x: 12 + i * 126,
  w: 110,
}));

/** Deck slots on the one ship; container i is loaded into slot i. */
const SLOTS = CARGO.map((_, i) => ({ x: 252 + i * 72 }));

function cargoStyle(index: number): CSSProperties {
  const door = DOORS[index];
  const slot = SLOTS[index];
  return {
    "--dx": `${door.x + door.w / 2 - CRATE_W / 2 - slot.x}px`,
    "--dy": `${DOOR_Y - DECK_Y}px`,
    animationDelay: `${index * 0.45}s`,
  } as CSSProperties;
}

export function ManifestPier() {
  const active = useSceneActive();
  const reduced = useReducedMotionPreference();
  const run = active && !reduced;

  return (
    <div
      className="hbr-pier absolute inset-0"
      data-run={run ? "true" : "false"}
    >
      <style>{CSS}</style>
      <svg viewBox="0 0 640 480" className="h-full w-full" aria-hidden="true">
        <rect width="640" height="480" fill={DUSK.skyTop} />
        <rect x="0" y="372" width="640" height="108" fill={DUSK.waterDeep} />

        {/* Quay with one warehouse per subgraph */}
        <rect x="0" y="120" width="640" height="16" fill={DUSK.quay} />
        {DOORS.map(({ warehouse, x, w }, i) => (
          <g key={warehouse.name}>
            <path
              d={`M${x} 56 L${x + w / 2} 28 L${x + w} 56 Z`}
              fill={DUSK.quayTop}
            />
            <rect
              x={x}
              y={56}
              width={w}
              height={64}
              fill={DUSK.quay}
              stroke={DUSK.edge}
            />
            <text
              x={x + w / 2}
              y={86}
              textAnchor="middle"
              fill={DUSK.heading}
              fontSize={FONT.label}
            >
              {warehouse.name}
            </text>
            <text
              x={x + w / 2}
              y={112}
              textAnchor="middle"
              fill={DUSK.ink}
              fontSize={FONT.label}
              style={LABEL}
            >
              {warehouse.language}
            </text>
            {i < CARGO.length && (
              <line
                className="hbr-p-cable"
                x1={x + w / 2}
                y1={136}
                x2={SLOTS[i].x + CRATE_W / 2}
                y2={DECK_Y}
                stroke={CARGO[i].color}
                strokeWidth={1.5}
                strokeDasharray="6 12"
                opacity="0.45"
              />
            )}
          </g>
        ))}

        {/* The manifest: one query, four lines */}
        <g>
          <rect
            x="16"
            y="176"
            width="216"
            height="140"
            rx="10"
            fill={DUSK.quay}
            stroke={DUSK.edge}
          />
          <text
            x="34"
            y="206"
            fill={DUSK.ink}
            fontSize={FONT.label}
            style={LABEL}
          >
            ONE QUERY
          </text>
          {CARGO.map((c, i) => (
            <g
              key={c.field}
              className="hbr-p-row"
              style={{ animationDelay: `${i * 0.45}s` }}
            >
              <rect
                x={34}
                y={222 + i * 28}
                width={14}
                height={14}
                rx={3}
                fill={c.color}
              />
              <text
                x={60}
                y={234 + i * 28}
                fill={DUSK.heading}
                fontSize={FONT.label}
                style={LABEL}
              >
                {c.field}
              </text>
            </g>
          ))}
        </g>

        {/* The one pier */}
        <rect
          x="206"
          y="352"
          width="392"
          height="10"
          rx="4"
          fill={DUSK.quayTop}
        />
        <rect x="212" y="362" width="8" height="26" fill={DUSK.quay} />
        <rect x="576" y="362" width="8" height="26" fill={DUSK.quay} />
        <text
          x="206"
          y="416"
          fill={DUSK.ink}
          fontSize={FONT.label}
          style={LABEL}
        >
          ONE PIER
        </text>

        {/* The one ship, with the loaded containers on deck */}
        <g className="hbr-p-bob">
          <path
            d="M212 352 H596 L576 384 H232 Z"
            fill={DUSK.hull}
            opacity="0.92"
          />
          {CARGO.map((c, i) => (
            <g key={c.field} className="hbr-p-load" style={cargoStyle(i)}>
              <rect
                x={SLOTS[i].x}
                y={DECK_Y}
                width={CRATE_W}
                height={30}
                rx={3}
                fill={c.color}
                opacity="0.85"
              />
              <rect
                x={SLOTS[i].x}
                y={DECK_Y}
                width={CRATE_W}
                height={30}
                rx={3}
                fill="none"
                stroke={DUSK.skyTop}
                strokeWidth={1.5}
              />
            </g>
          ))}
          <rect x="212" y="330" width="30" height="22" fill={DUSK.hull} />
          <text
            x="596"
            y="344"
            textAnchor="end"
            fill={DUSK.ink}
            fontSize={FONT.label}
            style={LABEL}
          >
            ONE RESPONSE
          </text>
        </g>
      </svg>
    </div>
  );
}
