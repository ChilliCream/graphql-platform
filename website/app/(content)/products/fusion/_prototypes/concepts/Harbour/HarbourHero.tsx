"use client";

import { useReducedMotionPreference, useSceneActive } from "../../Primitives";
import { DUSK, LABEL, SHIPS } from "./palette";

/**
 * Hero backdrop: a flat-vector harbour at dusk. The water shimmers, the
 * harbour master's tower sweeps its beam across the bay, and the ships of the
 * four clients ride at anchor in front of the lit warehouses on the quay.
 *
 * Rest state (reduced motion, or before hydration): the same harbour, still.
 */

const CSS = `
.hbr-hero [class*="hbr-a-"] { animation-play-state: running; }
.hbr-hero[data-run="false"] [class*="hbr-a-"] { animation: none; }
.hbr-hero .hbr-a-bob { animation: hbr-bob 7s ease-in-out infinite; }
.hbr-hero .hbr-a-wake { animation: hbr-wake 3.4s linear infinite; }
.hbr-hero .hbr-a-shimmer { animation: hbr-shimmer 6s ease-in-out infinite; }
.hbr-hero .hbr-a-beam { animation: hbr-beam 12s ease-in-out infinite; transform-box: view-box; transform-origin: 742px 286px; }
.hbr-hero .hbr-a-lamp { animation: hbr-lamp 4.5s ease-in-out infinite; }
@keyframes hbr-bob {
  0%, 100% { transform: translate(0, 0) rotate(0deg); }
  50% { transform: translate(-9px, -5px) rotate(-0.5deg); }
}
@keyframes hbr-wake { to { stroke-dashoffset: -48; } }
@keyframes hbr-shimmer {
  0%, 100% { opacity: 0.18; transform: translateX(0); }
  50% { opacity: 0.6; transform: translateX(14px); }
}
@keyframes hbr-beam {
  0%, 100% { transform: rotate(-16deg); opacity: 0.5; }
  50% { transform: rotate(14deg); opacity: 0.85; }
}
@keyframes hbr-lamp {
  0%, 100% { opacity: 0.55; }
  45% { opacity: 1; }
}
`;

/** Deterministic shimmer rows: no Math.random, so SSR and client agree. */
const SHIMMER = Array.from({ length: 16 }, (_, i) => ({
  x: 40 + ((i * 173) % 1040),
  y: 372 + i * 19,
  w: 60 + ((i * 97) % 190),
  delay: (i % 7) * 0.8,
}));

const WAREHOUSE_ROOF = [
  { x: 66, w: 118, h: 74 },
  { x: 196, w: 96, h: 58 },
  { x: 304, w: 132, h: 86 },
  { x: 448, w: 88, h: 62 },
  { x: 548, w: 120, h: 70 },
];

interface HullProps {
  readonly kind: (typeof SHIPS)[number]["kind"];
  readonly length: number;
}

/** Flat hull silhouette; each client gets a different profile. */
function Hull({ kind, length }: HullProps) {
  const l = length;
  const deck = kind === "speedboat" ? 8 : kind === "drone" ? 6 : 14;

  return (
    <g>
      <path
        d={`M0 0 H${l} L${l - 14} ${deck + 10} H12 Z`}
        fill={DUSK.hull}
        opacity={0.92}
      />
      {kind === "liner" && (
        <>
          <rect x={22} y={-22} width={l - 52} height={22} fill={DUSK.hull} />
          <rect
            x={l * 0.44}
            y={-40}
            width={20}
            height={18}
            fill={DUSK.hull}
            opacity={0.8}
          />
        </>
      )}
      {kind === "freighter" && (
        <>
          <rect x={10} y={-16} width={l * 0.6} height={16} fill={DUSK.lamp} />
          <rect
            x={l * 0.68}
            y={-30}
            width={26}
            height={30}
            fill={DUSK.hull}
            opacity={0.85}
          />
        </>
      )}
      {kind === "speedboat" && (
        <rect
          x={l * 0.3}
          y={-14}
          width={l * 0.4}
          height={14}
          fill={DUSK.hull}
          opacity={0.85}
        />
      )}
      {kind === "drone" && (
        <>
          <rect
            x={l * 0.34}
            y={-10}
            width={l * 0.3}
            height={10}
            fill={DUSK.accent}
          />
          <circle cx={l / 2} cy={-20} r={4} fill={DUSK.accent} />
        </>
      )}
    </g>
  );
}

const FLEET = [
  { ship: SHIPS[0], x: 296, y: 556, delay: 0 },
  { ship: SHIPS[1], x: 690, y: 502, delay: 1.6 },
  { ship: SHIPS[2], x: 852, y: 622, delay: 0.8 },
  { ship: SHIPS[3], x: 1024, y: 470, delay: 2.4 },
];

export function HarbourHero() {
  const active = useSceneActive();
  const reduced = useReducedMotionPreference();
  const run = active && !reduced;

  return (
    <div
      className="hbr-hero absolute inset-0"
      data-run={run ? "true" : "false"}
    >
      <style>{CSS}</style>
      <svg
        viewBox="0 0 1200 700"
        preserveAspectRatio="xMidYMid slice"
        className="h-full w-full"
        aria-hidden="true"
      >
        <defs>
          <linearGradient id="hbr-sky" x1="0" y1="0" x2="0" y2="1">
            <stop offset="0%" stopColor={DUSK.skyTop} />
            <stop offset="52%" stopColor={DUSK.skyMid} />
            <stop offset="100%" stopColor={DUSK.skyGlow} />
          </linearGradient>
          <linearGradient id="hbr-sea" x1="0" y1="0" x2="0" y2="1">
            <stop offset="0%" stopColor={DUSK.water} />
            <stop offset="100%" stopColor={DUSK.waterDeep} />
          </linearGradient>
          <radialGradient id="hbr-sun" cx="0.5" cy="0.5" r="0.5">
            <stop offset="0%" stopColor={DUSK.sun} stopOpacity="0.85" />
            <stop offset="100%" stopColor={DUSK.sun} stopOpacity="0" />
          </radialGradient>
        </defs>

        <rect width="1200" height="700" fill="url(#hbr-sky)" />
        <circle cx="982" cy="352" r="180" fill="url(#hbr-sun)" />
        <circle cx="982" cy="352" r="38" fill={DUSK.sun} opacity="0.75" />

        {/* Far headland */}
        <path
          d="M0 336 L120 312 L228 334 L352 300 L470 332 L604 306 L742 330 L900 316 L1060 334 L1200 310 L1200 360 L0 360 Z"
          fill={DUSK.skyTop}
          opacity="0.85"
        />

        {/* Water */}
        <rect x="0" y="358" width="1200" height="342" fill="url(#hbr-sea)" />
        {SHIMMER.map((s) => (
          <rect
            key={`${s.x}-${s.y}`}
            className="hbr-a-shimmer"
            x={s.x}
            y={s.y}
            width={s.w}
            height={2}
            rx={1}
            fill={DUSK.shimmer}
            opacity={0.18}
            style={{ animationDelay: `${s.delay}s` }}
          />
        ))}
        <rect
          className="hbr-a-shimmer"
          x="946"
          y="366"
          width="72"
          height="330"
          fill={DUSK.sun}
          opacity="0.12"
        />

        {/* Quay and warehouses */}
        <rect
          x="0"
          y="300"
          width="700"
          height="72"
          fill={DUSK.quayTop}
          opacity="0.9"
        />
        <rect x="0" y="358" width="700" height="24" fill={DUSK.quay} />
        {WAREHOUSE_ROOF.map((w) => (
          <g key={w.x}>
            <path
              d={`M${w.x} ${300} L${w.x + w.w / 2} ${300 - w.h} L${w.x + w.w} ${300} Z`}
              fill={DUSK.quay}
            />
            <rect
              x={w.x}
              y={300 - w.h * 0.1}
              width={w.w}
              height={w.h * 0.1 + 60}
              fill={DUSK.quayTop}
            />
            <rect
              className="hbr-a-lamp"
              x={w.x + 16}
              y={318}
              width={12}
              height={14}
              fill={DUSK.lamp}
              opacity={0.55}
              style={{ animationDelay: `${(w.x % 5) * 0.7}s` }}
            />
            <rect
              className="hbr-a-lamp"
              x={w.x + w.w - 30}
              y={318}
              width={12}
              height={14}
              fill={DUSK.lamp}
              opacity={0.55}
              style={{ animationDelay: `${(w.x % 3) * 1.1}s` }}
            />
          </g>
        ))}

        {/* Harbour master's tower */}
        <g>
          <path d="M726 372 L732 300 L752 300 L758 372 Z" fill={DUSK.quayTop} />
          <rect
            x="726"
            y="282"
            width="32"
            height="20"
            rx="4"
            fill={DUSK.quay}
          />
          <circle cx="742" cy="286" r="6" fill={DUSK.lamp} />
          <path
            className="hbr-a-beam"
            d="M742 286 L1188 196 L1188 350 Z"
            fill={DUSK.lamp}
            opacity="0.5"
          />
        </g>

        {/* The pier every ship docks at */}
        <rect
          x="486"
          y="404"
          width="240"
          height="12"
          rx="4"
          fill={DUSK.quayTop}
        />
        <rect x="486" y="416" width="10" height="34" fill={DUSK.quay} />
        <rect x="712" y="416" width="10" height="34" fill={DUSK.quay} />

        {/* The fleet */}
        {FLEET.map(({ ship, x, y, delay }) => (
          <g key={ship.name} transform={`translate(${x} ${y})`}>
            <g className="hbr-a-bob" style={{ animationDelay: `${delay}s` }}>
              <Hull kind={ship.kind} length={ship.length} />
              <line
                className="hbr-a-wake"
                x1={ship.length + 8}
                y1={12}
                x2={ship.length + 116}
                y2={12}
                stroke={DUSK.shimmer}
                strokeWidth={2}
                strokeDasharray="10 14"
                opacity="0.5"
              />
              <text
                x={0}
                y={34}
                fill={DUSK.ink}
                fontSize={13}
                style={LABEL}
                opacity="0.85"
              >
                {ship.name.toUpperCase()}
              </text>
            </g>
          </g>
        ))}
      </svg>
    </div>
  );
}
