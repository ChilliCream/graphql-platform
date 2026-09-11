"use client";

import { TYPE } from "../../brand";
import { anim, useCycle, useSceneMotion } from "./hooks";
import { AP } from "./palette";

/**
 * "Composition protects the graph, Nitro protects your clients": the boarding
 * check. Composition clears the change - the plan still composes - so the
 * board would read ON TIME, and Nitro cross-checks it against the operations
 * the registered clients actually booked, flipping each row to safe, risky or
 * breaking. At rest all four verdicts stand.
 */

const W = 640;
const H = 440;
const PHASES = 7;
const REST = 0;
const BEAT = 1300;

type Verdict = "SAFE" | "RISKY" | "BREAKING";

interface Booking {
  readonly client: string;
  readonly operation: string;
  readonly verdict: Verdict;
}

const BOOKINGS: readonly Booking[] = [
  { client: "Web", operation: "CheckoutSummary", verdict: "SAFE" },
  { client: "Mobile 4.2", operation: "OrderStatus", verdict: "BREAKING" },
  { client: "Partner API", operation: "ShipmentFeed", verdict: "RISKY" },
  { client: "Agent", operation: "OrderLookup", verdict: "SAFE" },
];

const VERDICT_COLOR: Readonly<Record<Verdict, string>> = {
  SAFE: AP.taxi,
  RISKY: AP.amber,
  BREAKING: AP.stop,
};

const KEYFRAMES = `
@keyframes ap-book-flip {
  0% { transform: rotateX(-84deg); opacity: 0.2; }
  100% { transform: rotateX(0deg); opacity: 1; }
}
@keyframes ap-book-scan { 0%, 100% { opacity: 0.25; } 50% { opacity: 0.85; } }
`;

const rowY = (i: number) => 168 + i * 58;

export function BoardingCheck() {
  const running = useSceneMotion();
  const phase = useCycle(running, PHASES, BEAT, REST);
  const shown = (i: number) => phase === REST || phase >= i + 2;

  return (
    <svg viewBox={`0 0 ${W} ${H}`} className="h-full w-full">
      <style>{KEYFRAMES}</style>
      <rect width={W} height={H} fill={AP.bg} />

      {/* The change itself: composition is happy with it. */}
      <rect
        x="24"
        y="32"
        width={W - 48}
        height="94"
        rx="9"
        fill={AP.panel}
        stroke={AP.panelEdge}
      />
      <text
        x="42"
        y="58"
        fill={AP.dim}
        fontFamily={AP.mono}
        fontSize={TYPE.label}
        letterSpacing="0.2em"
      >
        SCHEMA CHANGE · ORDERING SUBGRAPH
      </text>
      <text
        x="42"
        y="82"
        fill={AP.ink}
        fontFamily={AP.mono}
        fontSize={TYPE.caption}
      >
        - Order.trackingUrl
      </text>
      <text
        x="42"
        y="104"
        fill={AP.taxi}
        fontFamily={AP.mono}
        fontSize={TYPE.label}
        letterSpacing="0.14em"
      >
        COMPOSITION: SOURCE SCHEMAS STILL COMPOSE · BUILD GREEN
      </text>
      <text
        x={W - 42}
        y="58"
        fill={AP.approach}
        fontFamily={AP.mono}
        fontSize={TYPE.label}
        textAnchor="end"
        letterSpacing="0.18em"
        style={{
          animation: anim(running, "ap-book-scan 2000ms ease-in-out infinite"),
        }}
      >
        NITRO · BOARDED PASSENGERS
      </text>

      <text
        x="24"
        y="150"
        fill={AP.dim}
        fontFamily={AP.mono}
        fontSize={TYPE.label}
        letterSpacing="0.18em"
      >
        REGISTERED CLIENT · PUBLISHED OPERATION · VERDICT
      </text>

      {BOOKINGS.map((booking, i) => {
        const y = rowY(i);
        const lit = shown(i);
        const color = lit ? VERDICT_COLOR[booking.verdict] : AP.dim;
        const label = lit ? booking.verdict : "CHECKING";

        return (
          <g key={booking.client}>
            <rect
              x="24"
              y={y}
              width={W - 48}
              height="46"
              rx="7"
              fill={AP.wash}
              stroke={lit ? color : AP.panelEdge}
              strokeOpacity={lit ? 0.6 : 1}
              style={{ transition: "stroke 400ms ease" }}
            />
            <circle cx="46" cy={y + 23} r="5" fill={color} />
            <text
              x="64"
              y={y + 20}
              fill={AP.ink}
              fontFamily={AP.mono}
              fontSize={TYPE.caption}
            >
              {booking.client}
            </text>
            <text
              x="64"
              y={y + 36}
              fill={AP.dim}
              fontFamily={AP.mono}
              fontSize={TYPE.label}
              letterSpacing="0.1em"
            >
              {booking.operation}
            </text>
            <text
              x="300"
              y={y + 29}
              fill={AP.dim}
              fontFamily={AP.mono}
              fontSize={TYPE.label}
              letterSpacing="0.1em"
            >
              {booking.verdict === "SAFE"
                ? "no selection of the field"
                : "selects trackingUrl"}
            </text>
            <text
              key={label}
              x={W - 44}
              y={y + 29}
              fill={color}
              fontFamily={AP.mono}
              fontSize={TYPE.caption}
              textAnchor="end"
              letterSpacing="0.16em"
              style={{
                transformBox: "fill-box",
                transformOrigin: "top center",
                animation: anim(running, "ap-book-flip 420ms ease-out"),
              }}
            >
              {label}
            </text>
          </g>
        );
      })}

      <text
        x="24"
        y="422"
        fill={AP.dim}
        fontFamily={AP.mono}
        fontSize={TYPE.label}
        letterSpacing="0.14em"
      >
        BEFORE THE CHANGE IS MERGED · NOT IN THE HANDS OF A CLIENT
      </text>
    </svg>
  );
}
