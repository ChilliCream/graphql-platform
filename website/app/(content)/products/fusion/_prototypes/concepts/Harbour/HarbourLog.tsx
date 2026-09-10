"use client";

import { useReducedMotionPreference, useSceneActive } from "../../Primitives";
import { DUSK, LABEL } from "./palette";

/**
 * "Composition protects the graph, Nitro protects your clients": the Ordering
 * warehouse withdraws a container, the customs check still passes because the
 * source schemas still compose, and only the harbour log - every operation the
 * registered ships actually run - shows whose cargo just disappeared.
 *
 * Rest state: the withdrawn container struck through and the log verdicts
 * already rendered.
 */

const CSS = `
.hbr-log [class*="hbr-l-"] { animation-play-state: running; }
.hbr-log[data-run="false"] [class*="hbr-l-"] { animation: none; }
.hbr-log .hbr-l-drop { animation: hbr-l-drop 11s ease-in-out infinite; }
.hbr-log .hbr-l-pass { animation: hbr-l-pass 11s ease-in-out infinite; transform-box: fill-box; transform-origin: center; }
.hbr-log .hbr-l-row { animation: hbr-l-row 11s ease-in-out infinite; }
.hbr-log .hbr-l-alarm { animation: hbr-l-alarm 11s ease-in-out infinite; }
@keyframes hbr-l-drop {
  0%, 8% { opacity: 1; }
  20%, 100% { opacity: 0.28; }
}
@keyframes hbr-l-pass {
  0%, 22% { opacity: 0.15; transform: scale(0.9); }
  34%, 100% { opacity: 1; transform: scale(1); }
}
@keyframes hbr-l-row {
  0%, 42% { opacity: 0.16; }
  58%, 100% { opacity: 1; }
}
@keyframes hbr-l-alarm {
  0%, 60% { opacity: 0; }
  70% { opacity: 1; }
  82% { opacity: 0.3; }
  92%, 100% { opacity: 1; }
}
`;

interface LogRow {
  readonly ship: string;
  readonly operation: string;
  readonly verdict: "safe" | "risky" | "breaking";
}

const ROWS: readonly LogRow[] = [
  { ship: "Web", operation: "CatalogPage", verdict: "safe" },
  { ship: "Mobile", operation: "OrderTracking", verdict: "breaking" },
  { ship: "Partner API", operation: "BillingExport", verdict: "safe" },
  { ship: "Agent", operation: "ShippingLookup", verdict: "risky" },
];

const VERDICT_COLOR: Record<LogRow["verdict"], string> = {
  safe: DUSK.ok,
  risky: DUSK.lamp,
  breaking: DUSK.stop,
};

const CONTAINERS = ["orderId", "status", "deliveryEstimate"];

const ROW_TOP = 262;
const ROW_H = 46;
const ROW_GAP = 12;

const rowY = (i: number) => ROW_TOP + i * (ROW_H + ROW_GAP);

export function HarbourLog() {
  const active = useSceneActive();
  const reduced = useReducedMotionPreference();
  const run = active && !reduced;

  return (
    <div className="hbr-log absolute inset-0" data-run={run ? "true" : "false"}>
      <style>{CSS}</style>
      <svg
        viewBox="0 0 640 480"
        className="h-full w-full"
        role="img"
        aria-label="Composition still passes after a container is withdrawn; the harbour log marks the mobile client's operation as breaking."
      >
        <rect width="640" height="480" fill={DUSK.skyTop} />

        {/* The warehouse withdrawing a container */}
        <rect
          x={16}
          y={40}
          width={324}
          height={132}
          rx={10}
          fill={DUSK.quay}
          stroke={DUSK.edge}
        />
        <text x={34} y={68} fill={DUSK.heading} fontSize={13}>
          Ordering
        </text>
        <text x={34} y={86} fill={DUSK.ink} fontSize={10} style={LABEL}>
          GO · SOURCE SCHEMA
        </text>
        {CONTAINERS.map((field, i) => {
          const removed = field === "deliveryEstimate";
          return (
            <g
              key={field}
              className={removed ? "hbr-l-drop" : undefined}
              opacity={removed ? 0.28 : 1}
            >
              <rect
                x={34 + i * 100}
                y={104}
                width={92}
                height={44}
                rx={6}
                fill={removed ? DUSK.skyTop : DUSK.accent}
                fillOpacity={removed ? 1 : 0.5}
                stroke={removed ? DUSK.stop : DUSK.edge}
                strokeDasharray={removed ? "5 4" : undefined}
              />
              <text
                x={34 + i * 100 + 46}
                y={130}
                textAnchor="middle"
                fill={DUSK.heading}
                fontSize={10}
                style={LABEL}
              >
                {field}
              </text>
            </g>
          );
        })}

        {/* Customs still stamps the change through */}
        <rect
          x={356}
          y={40}
          width={268}
          height={132}
          rx={10}
          fill={DUSK.quay}
          stroke={DUSK.edge}
        />
        <text x={374} y={68} fill={DUSK.ink} fontSize={10} style={LABEL}>
          COMPOSITION
        </text>
        <g className="hbr-l-pass">
          <circle
            cx={412}
            cy={124}
            r={20}
            fill="none"
            stroke={DUSK.ok}
            strokeWidth={2.5}
          />
          <path
            d="M402 124 l7 8 l14 -17"
            fill="none"
            stroke={DUSK.ok}
            strokeWidth={3}
            strokeLinecap="round"
            strokeLinejoin="round"
          />
          <text x={444} y={130} fill={DUSK.ok} fontSize={12} style={LABEL}>
            PASS
          </text>
        </g>

        {/* The harbour log: which ship carries which container */}
        <text x={16} y={228} fill={DUSK.ink} fontSize={10} style={LABEL}>
          HARBOUR LOG · REGISTERED CLIENT OPERATIONS
        </text>
        {ROWS.map((row, i) => (
          <g
            key={row.ship}
            className="hbr-l-row"
            style={{ animationDelay: `${i * 0.5}s` }}
          >
            <rect
              x={16}
              y={rowY(i)}
              width={608}
              height={ROW_H}
              rx={8}
              fill={DUSK.quay}
              stroke={row.verdict === "breaking" ? DUSK.stop : DUSK.edge}
            />
            <text x={34} y={rowY(i) + 28} fill={DUSK.heading} fontSize={12}>
              {row.ship}
            </text>
            <text
              x={150}
              y={rowY(i) + 28}
              fill={DUSK.ink}
              fontSize={11}
              style={LABEL}
            >
              {row.operation}
            </text>
            <rect
              x={496}
              y={rowY(i) + 11}
              width={110}
              height={24}
              rx={12}
              fill={VERDICT_COLOR[row.verdict]}
              fillOpacity={0.16}
            />
            <text
              x={551}
              y={rowY(i) + 27}
              textAnchor="middle"
              fill={VERDICT_COLOR[row.verdict]}
              fontSize={11}
              style={LABEL}
            >
              {row.verdict}
            </text>
            {row.verdict === "breaking" && (
              <rect
                className="hbr-l-alarm"
                x={16}
                y={rowY(i)}
                width={608}
                height={ROW_H}
                rx={8}
                fill="none"
                stroke={DUSK.stop}
                strokeWidth={2.5}
              />
            )}
          </g>
        ))}
      </svg>
    </div>
  );
}
