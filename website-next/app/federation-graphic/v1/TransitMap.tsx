/**
 * Federation transit map, v1. A Beck-style octilinear network rendered as one
 * hand-authored SVG: each colored line is a service owned by one team, regular
 * stations are fields (lowercase), and interchanges where lines meet are
 * entity types (uppercase). The teal journey is a query: it boards the coral
 * Orders line, transfers at CUSTOMER, rides Reviews north through rating and
 * terminates at the four-line PRODUCT interchange. The journey draws itself in
 * once; a train dot then loops it continuously. Purely decorative.
 */

import {
  AMBER,
  CORAL,
  CYAN,
  GREEN,
  MONO_FONT,
  NAVY,
  SLATE,
  TEAL,
  VIOLET,
} from "./palette";

const INK = "#e8eef8";
const RING = "#f5f0ea";
const CARD_BG = "rgba(12, 19, 34, 0.72)";
const CARD_BORDER = "rgba(245, 241, 234, 0.14)";

interface Pt {
  readonly x: number;
  readonly y: number;
}

/** Octilinear polyline with quadratic fillets at every interior vertex. */
function lane(pts: readonly Pt[], r = 18): string {
  let d = `M ${pts[0].x} ${pts[0].y}`;
  for (let i = 1; i < pts.length - 1; i++) {
    const p = pts[i];
    const a = pts[i - 1];
    const b = pts[i + 1];
    const la = Math.hypot(a.x - p.x, a.y - p.y);
    const lb = Math.hypot(b.x - p.x, b.y - p.y);
    const ra = Math.min(r, la / 2);
    const rb = Math.min(r, lb / 2);
    const pa = {
      x: p.x + ((a.x - p.x) / la) * ra,
      y: p.y + ((a.y - p.y) / la) * ra,
    };
    const pb = {
      x: p.x + ((b.x - p.x) / lb) * rb,
      y: p.y + ((b.y - p.y) / lb) * rb,
    };
    d += ` L ${pa.x} ${pa.y} Q ${p.x} ${p.y} ${pb.x} ${pb.y}`;
  }
  const last = pts[pts.length - 1];
  d += ` L ${last.x} ${last.y}`;
  return d;
}

type LabelSide = "above" | "below" | "left" | "right";

interface Station {
  readonly x: number;
  readonly y: number;
  readonly label?: string;
  /** Orientation of the line at this station; the tick runs perpendicular. */
  readonly horiz: boolean;
  readonly side: LabelSide;
}

interface Line {
  readonly name: string;
  readonly color: string;
  readonly pts: readonly Pt[];
  readonly stations: readonly Station[];
}

const LINES: readonly Line[] = [
  {
    name: "catalog",
    color: CYAN,
    pts: [
      { x: -24, y: 480 },
      { x: 430, y: 480 },
      { x: 490, y: 420 },
      { x: 1340, y: 420 },
      { x: 1420, y: 340 },
      { x: 1624, y: 340 },
    ],
    stations: [
      { x: 250, y: 480, label: "brand", horiz: true, side: "below" },
      { x: 640, y: 420, horiz: true, side: "above" },
      { x: 780, y: 420, label: "variant", horiz: true, side: "above" },
      { x: 1200, y: 420, label: "description", horiz: true, side: "above" },
      { x: 1500, y: 340, horiz: true, side: "above" },
    ],
  },
  {
    name: "inventory",
    color: GREEN,
    pts: [
      { x: 1000, y: -24 },
      { x: 1000, y: 700 },
      { x: 920, y: 780 },
      { x: 536, y: 780 },
    ],
    stations: [
      { x: 1000, y: 150, label: "warehouse", horiz: false, side: "left" },
      { x: 1000, y: 290, label: "stock", horiz: false, side: "left" },
      { x: 1000, y: 560, label: "allocation", horiz: false, side: "left" },
      { x: 700, y: 780, horiz: true, side: "below" },
    ],
  },
  {
    name: "reviews",
    color: VIOLET,
    pts: [
      { x: 1624, y: 100 },
      { x: 1200, y: 100 },
      { x: 1060, y: 240 },
      { x: 1060, y: 720 },
      { x: 980, y: 800 },
      { x: -24, y: 800 },
    ],
    stations: [
      { x: 1360, y: 100, label: "author", horiz: true, side: "above" },
      { x: 1060, y: 320, label: "review", horiz: false, side: "right" },
      { x: 1060, y: 500, label: "rating", horiz: false, side: "right" },
      { x: 400, y: 800, horiz: true, side: "below" },
    ],
  },
  {
    name: "payments",
    color: AMBER,
    pts: [
      { x: 880, y: 924 },
      { x: 880, y: 260 },
      { x: 960, y: 180 },
      { x: 1624, y: 180 },
    ],
    stations: [
      { x: 880, y: 760, label: "invoice", horiz: false, side: "right" },
      { x: 880, y: 520, label: "payment", horiz: false, side: "left" },
      { x: 1300, y: 180, label: "ledger", horiz: true, side: "above" },
    ],
  },
  {
    name: "orders",
    color: CORAL,
    pts: [
      { x: -24, y: 640 },
      { x: 1240, y: 640 },
      { x: 1320, y: 560 },
      { x: 1624, y: 560 },
    ],
    stations: [
      { x: 240, y: 640, horiz: true, side: "below" },
      { x: 560, y: 640, label: "cart", horiz: true, side: "below" },
      { x: 740, y: 640, label: "checkout", horiz: true, side: "below" },
      { x: 1180, y: 640, label: "history", horiz: true, side: "above" },
      { x: 1450, y: 560, horiz: true, side: "above" },
    ],
  },
];

/** The query: it emerges from behind the hero copy (the client side), rides
 * the catalog line east and arrives at the PRODUCT interchange. */
const JOURNEY: readonly Pt[] = [
  { x: 660, y: 420 },
  { x: 975, y: 420 },
];

/** Resolution legs: after the query arrives, the interchange fans out along
 * the lines that own the requested fields (stock via inventory north, rating
 * via reviews south). Mirrors the query card in the hero copy. */
const LEGS: readonly (readonly Pt[])[] = [
  [
    { x: 1000, y: 402 },
    { x: 1000, y: 296 },
  ],
  [
    { x: 1060, y: 438 },
    { x: 1060, y: 494 },
  ],
];

const LEGEND: readonly { readonly name: string; readonly color: string }[] = [
  { name: "catalog", color: CYAN },
  { name: "orders", color: CORAL },
  { name: "inventory", color: GREEN },
  { name: "payments", color: AMBER },
  { name: "reviews", color: VIOLET },
];

function tickFor(s: Station): {
  x1: number;
  y1: number;
  x2: number;
  y2: number;
} {
  const len = 10;
  const half = 3.5; // start just outside the 7px line stroke
  if (s.horiz) {
    const dir = s.side === "above" ? -1 : 1;
    return {
      x1: s.x,
      y1: s.y + dir * half,
      x2: s.x,
      y2: s.y + dir * (half + len),
    };
  }
  const dir = s.side === "left" ? -1 : 1;
  return {
    x1: s.x + dir * half,
    y1: s.y,
    x2: s.x + dir * (half + len),
    y2: s.y,
  };
}

function labelAnchor(s: Station): {
  x: number;
  y: number;
  anchor: "start" | "middle" | "end";
} {
  switch (s.side) {
    case "above":
      return { x: s.x, y: s.y - 22, anchor: "middle" };
    case "below":
      return { x: s.x, y: s.y + 32, anchor: "middle" };
    case "left":
      return { x: s.x - 20, y: s.y + 4, anchor: "end" };
    case "right":
      return { x: s.x + 20, y: s.y + 4, anchor: "start" };
  }
}

export function TransitMap() {
  return (
    <div
      aria-hidden="true"
      className="absolute inset-0 opacity-35 md:opacity-95"
      style={{
        maskImage:
          "linear-gradient(90deg, rgba(0,0,0,0.07), rgba(0,0,0,0.13) 38%, rgba(0,0,0,0.78) 56%, #000 72%)",
        WebkitMaskImage:
          "linear-gradient(90deg, rgba(0,0,0,0.07), rgba(0,0,0,0.13) 38%, rgba(0,0,0,0.78) 56%, #000 72%)",
      }}
    >
      <svg
        id="ftm-svg"
        className="h-full w-full"
        viewBox="0 0 1600 900"
        preserveAspectRatio="xMidYMid slice"
      >
        <style>{`
          .ftm-journey {
            stroke-dasharray: 1;
            stroke-dashoffset: 1;
            animation: ftm-draw 1.7s cubic-bezier(0.6, 0, 0.2, 1) 0.6s forwards;
          }
          @keyframes ftm-draw {
            to { stroke-dashoffset: 0; }
          }
          .ftm-leg {
            stroke-dasharray: 1;
            stroke-dashoffset: 1;
            animation: ftm-draw 0.7s cubic-bezier(0.6, 0, 0.2, 1) 2.2s forwards;
          }
          @media (prefers-reduced-motion: reduce) {
            .ftm-journey, .ftm-leg { animation: none; stroke-dashoffset: 0; }
            .ftm-train { display: none; }
          }
          @media (max-width: 767px) {
            #ftm-svg text,
            #ftm-svg .ftm-legend { display: none; }
          }
        `}</style>
        <defs>
          <filter id="ftm-glow" x="-80%" y="-80%" width="260%" height="260%">
            <feGaussianBlur stdDeviation="6" result="b" />
            <feMerge>
              <feMergeNode in="b" />
              <feMergeNode in="SourceGraphic" />
            </feMerge>
          </filter>
        </defs>

        {/* The river: a quiet nod to the homepage FusionFlow current. */}
        <path
          d="M 1560 -60 C 1380 260 1160 300 980 470 C 830 610 760 740 620 940"
          fill="none"
          stroke={SLATE}
          strokeOpacity="0.05"
          strokeWidth="110"
        />

        {/* The network sits right of the hero copy; the left third only sees
            ghost lines passing under the mask. */}
        <g transform="translate(180 0)">
          {/* Service lines. */}
          {LINES.map((line) => (
            <path
              key={line.name}
              d={lane(line.pts)}
              fill="none"
              stroke={line.color}
              strokeWidth="7"
              strokeOpacity="0.82"
              strokeLinecap="round"
            />
          ))}

          {/* The journey: soft under-glow, navy casing so the route reads as an
            overlay on the lines it rides, then the drawn route. */}
          <path
            d={lane(JOURNEY, 14)}
            fill="none"
            stroke={TEAL}
            strokeOpacity="0.1"
            strokeWidth="14"
            strokeLinecap="round"
          />
          <path
            d={lane(JOURNEY, 14)}
            fill="none"
            stroke={NAVY}
            strokeWidth="13"
            strokeLinecap="round"
          />
          <path
            className="ftm-journey"
            d={lane(JOURNEY, 14)}
            pathLength={1}
            fill="none"
            stroke={TEAL}
            strokeWidth="7.5"
            strokeLinecap="round"
          />

          {/* Resolution legs fan out of the interchange once the query lands. */}
          {LEGS.map((leg, i) => (
            <g key={i}>
              <path
                d={lane(leg)}
                fill="none"
                stroke={NAVY}
                strokeWidth="10"
                strokeLinecap="round"
              />
              <path
                className="ftm-leg"
                d={lane(leg)}
                pathLength={1}
                fill="none"
                stroke={TEAL}
                strokeWidth="5"
                strokeLinecap="round"
              />
            </g>
          ))}

          {/* Station ticks and labels. */}
          {LINES.map((line) =>
            line.stations.map((s, i) => {
              const t = tickFor(s);
              const l = labelAnchor(s);
              return (
                <g key={`${line.name}-${i}`}>
                  <line
                    x1={t.x1}
                    y1={t.y1}
                    x2={t.x2}
                    y2={t.y2}
                    stroke={line.color}
                    strokeWidth="3"
                    strokeLinecap="round"
                  />
                  {s.label && (
                    <text
                      x={l.x}
                      y={l.y}
                      textAnchor={l.anchor}
                      fill={SLATE}
                      fontFamily={MONO_FONT}
                      fontSize="12.5"
                    >
                      {s.label}
                    </text>
                  )}
                </g>
              );
            }),
          )}

          {/* Interchanges: entities. PRODUCT is a four-line capsule; ORDER and
            CUSTOMER are two-line roundels. */}
          <g>
            {/* Destination halo around PRODUCT. */}
            <rect
              x={982}
              y={402}
              width={96}
              height={36}
              rx={18}
              fill="none"
              stroke={TEAL}
              strokeOpacity="0.35"
              strokeWidth="2.5"
            />
            <rect
              x={988}
              y={408}
              width={84}
              height={24}
              rx={12}
              fill={NAVY}
              stroke={RING}
              strokeWidth="3.5"
            />
            <text
              x={1030}
              y={388}
              textAnchor="middle"
              fill={INK}
              fontFamily={MONO_FONT}
              fontSize="13"
              fontWeight="600"
              letterSpacing="0.08em"
            >
              PRODUCT
            </text>

            <circle
              cx={880}
              cy={640}
              r={11.5}
              fill={NAVY}
              stroke={RING}
              strokeWidth="3.5"
            />
            <text
              x={880}
              y={676}
              textAnchor="middle"
              fill={INK}
              fontFamily={MONO_FONT}
              fontSize="13"
              fontWeight="600"
              letterSpacing="0.08em"
            >
              ORDER
            </text>

            <circle
              cx={1060}
              cy={640}
              r={11.5}
              fill={NAVY}
              stroke={RING}
              strokeWidth="3.5"
            />
            <text
              x={1060}
              y={676}
              textAnchor="middle"
              fill={INK}
              fontFamily={MONO_FONT}
              fontSize="13"
              fontWeight="600"
              letterSpacing="0.08em"
            >
              CUSTOMER
            </text>
          </g>

          {/* The train: one query looping the journey. */}
          <circle
            className="ftm-train"
            r="5.5"
            fill={TEAL}
            filter="url(#ftm-glow)"
          >
            <animateMotion
              dur="9s"
              begin="2.5s"
              repeatCount="indefinite"
              path={lane(JOURNEY, 14)}
            />
          </circle>
        </g>

        {/* Legend. */}
        <g className="ftm-legend">
          <rect
            x={1316}
            y={664}
            width={252}
            height={196}
            rx={14}
            fill={CARD_BG}
            stroke={CARD_BORDER}
          />
          <text
            x={1340}
            y={694}
            fill={INK}
            fontFamily={MONO_FONT}
            fontSize="11"
            fontWeight="600"
            letterSpacing="0.24em"
          >
            ONE NETWORK
          </text>
          {LEGEND.map((entry, i) => (
            <g key={entry.name}>
              <rect
                x={1340}
                y={708 + i * 24}
                width={20}
                height={5}
                rx={2.5}
                fill={entry.color}
              />
              <text
                x={1372}
                y={716 + i * 24}
                fill={SLATE}
                fontFamily={MONO_FONT}
                fontSize="11.5"
              >
                {entry.name}
              </text>
            </g>
          ))}
          <line x1={1340} x2={1544} y1={832} y2={832} stroke={CARD_BORDER} />
          <circle cx={1350} cy={846} r={4} fill={TEAL} />
          <text
            x={1372}
            y={850}
            fill={TEAL}
            fontFamily={MONO_FONT}
            fontSize="11.5"
          >
            your query
          </text>
        </g>
      </svg>
    </div>
  );
}
