import { Fragment } from "react";
import type { CSSProperties, ReactNode } from "react";

import {
  CANON,
  GatewayChip,
  HorizonRule,
  INK_DIM,
  NodeCaption,
} from "../Primitives";
import { CHAPTERS, ProtoCodeBox } from "../story";
import type { Chapter } from "../story";

const W = 1024;
const H = 4900;

const MONO = "ui-monospace, SFMono-Regular, Menlo, monospace";

/** Five service markers, always Catalog -> User left to right: reused as the
 * legend at the top of the map and as the canonical x anchors every cloud
 * quintet returns to at every beat. */
const MARKERS = [
  { s: 0, x: 150, y: 100 },
  { s: 1, x: 320, y: 140 },
  { s: 2, x: 512, y: 180 },
  { s: 3, x: 704, y: 220 },
  { s: 4, x: 874, y: 260 },
] as const;
const XS = MARKERS.map((m) => m.x);

const HORIZON_Y = 4300;
const CHIP = { x: 512, y: 4380 } as const;

/** Mask windows the map dims behind, one per chapter beat - unchanged from
 * the production map's current placement (current working tree values). */
const GAPS = [
  { x: 470, w: 460, y: 400, h: 320 },
  { x: 95, w: 460, y: 900, h: 320 },
  { x: 220, w: 584, y: 1390, h: 300 },
  { x: 470, w: 460, y: 1860, h: 320 },
  { x: 220, w: 584, y: 2350, h: 300 },
  { x: 220, w: 584, y: 3545, h: 360 },
  { x: 220, w: 584, y: 4450, h: 300 },
] as const;

const pct = (v: number, total: number) => `${(v / total) * 100}%`;

/** Desktop placement: the block's center in the 1024 x H map, as CSS variables. */
function placement(top: number, left: number): CSSProperties {
  return { "--top": pct(top, H), "--left": `${left}%` } as CSSProperties;
}

const SCRIM =
  "radial-gradient(ellipse 62% 58% at 50% 50%, rgba(11,15,26,0.98) 0%, rgba(11,15,26,0.94) 50%, rgba(11,15,26,0.6) 76%, rgba(11,15,26,0) 93%)";

interface Placement {
  readonly top: number;
  readonly left: number;
  readonly side?: boolean;
}

interface BoxPlacement extends Placement {
  readonly paired?: boolean;
}

/** Copy positions, unchanged from the production map's current placement. */
const COPY_PLACEMENT: readonly Placement[] = [
  { top: 560, left: 70, side: true },
  { top: 1060, left: 30, side: true },
  { top: 1540, left: 50 },
  { top: 2020, left: 70, side: true },
  { top: 2500, left: 50 },
  { top: 3740, left: 50 },
  { top: 4600, left: 50 },
];

/** Box positions per chapter; chapters with no boxes get an empty array. */
const BOX_PLACEMENT: readonly (readonly BoxPlacement[])[] = [
  [{ top: 560, left: 28 }],
  [{ top: 1060, left: 72 }],
  [],
  [{ top: 2020, left: 28 }],
  [
    { top: 2900, left: 27, paired: true },
    { top: 2900, left: 73, paired: true },
  ],
  [{ top: 4100, left: 50 }],
  [],
];

interface CopyBlockProps extends Placement {
  readonly title: string;
  readonly children: ReactNode;
}

function CopyBlock({ top, left, side, title, children }: CopyBlockProps) {
  return (
    <div
      className={`relative w-full text-center sm:absolute sm:top-(--top) sm:left-(--left) sm:z-20 sm:-translate-x-1/2 sm:-translate-y-1/2 ${
        side ? "sm:w-[min(44%,26rem)] sm:text-left" : "sm:w-[min(92%,34rem)]"
      }`}
      style={placement(top, left)}
    >
      <div
        aria-hidden="true"
        className="pointer-events-none absolute -inset-x-32 -inset-y-20 hidden sm:block"
        style={{ background: SCRIM }}
      />
      <div className="relative">
        <h3 className="font-heading text-cc-heading text-h4 sm:text-h3 text-balance">
          {title}
        </h3>
        <div className="text-cc-ink mt-4 space-y-3 text-sm sm:text-base">
          {children}
        </div>
      </div>
    </div>
  );
}

interface CodeBoxSlotProps extends BoxPlacement {
  readonly box: Chapter["boxes"][number];
}

function CodeBoxSlot({ top, left, paired, box }: CodeBoxSlotProps) {
  return (
    <div
      className={`mx-auto w-[min(100%,21rem)] sm:absolute sm:top-(--top) sm:left-(--left) sm:z-30 sm:mx-0 sm:-translate-x-1/2 sm:-translate-y-1/2 ${
        paired ? "sm:w-[min(43%,21rem)]" : "sm:w-[min(88%,21rem)]"
      }`}
      style={placement(top, left)}
    >
      <ProtoCodeBox label={box.label} color={box.color} lines={box.lines} />
    </div>
  );
}

interface CloudProps {
  readonly cx: number;
  readonly cy: number;
  readonly color: string;
  readonly opacity: number;
  readonly scale?: number;
  readonly drift?: number;
}

/**
 * One of the five recurring service clouds: 2-3 overlapping ellipses, always
 * the same silhouette, at a beat-specific position, size and opacity. Given
 * an optional `drift` index so a whole quintet does not breathe in lockstep.
 */
function Cloud({ cx, cy, color, opacity, scale = 1, drift = 0 }: CloudProps) {
  return (
    <g
      className="on-cloud"
      style={{ animationDelay: `${drift * -11}s` } as CSSProperties}
    >
      <ellipse
        cx={cx - 26 * scale}
        cy={cy + 5 * scale}
        rx={108 * scale}
        ry={33 * scale}
        fill={color}
        fillOpacity={opacity}
      />
      <ellipse
        cx={cx + 22 * scale}
        cy={cy - 7 * scale}
        rx={90 * scale}
        ry={29 * scale}
        fill={color}
        fillOpacity={opacity}
      />
      <ellipse
        cx={cx}
        cy={cy + 9 * scale}
        rx={128 * scale}
        ry={37 * scale}
        fill={color}
        fillOpacity={opacity}
      />
    </g>
  );
}

/** A thin dawn-rim stroke along a cloud's lower edge only. */
function RimArc({
  cx,
  cy,
  rx,
  ry,
}: {
  readonly cx: number;
  readonly cy: number;
  readonly rx: number;
  readonly ry: number;
}) {
  return (
    <path
      d={`M${cx - rx} ${cy} A ${rx} ${ry} 0 0 0 ${cx + rx} ${cy}`}
      fill="none"
      stroke="#5eead4"
      strokeOpacity={0.35}
      strokeWidth={1.5}
    />
  );
}

const B6_YS = [3495, 3415, 3380, 3415, 3495] as const;
const B7_XS = [70, 300, 512, 724, 954] as const;

/**
 * The recurring five-cloud motif and its per-beat atmosphere: crisp at rest,
 * murk where the wrong answers merge them, one grey layer over everything,
 * clear again at the question, thinned to two at authorship, rim-lit in a
 * pre-dawn arc at composition, and returned whole below the horizon. Every
 * shape here lives inside the map's gap mask so it dims wherever a copy
 * block sits, per the ambient-layer contrast guard.
 */
function BeatAtmosphere() {
  return (
    <>
      {/* B1 - evening: five small crisp clouds, far apart, clear air. */}
      <g>
        {XS.map((x, i) => (
          <Cloud
            key={`b1-${i}`}
            cx={x}
            cy={300}
            color={CANON[i].color}
            opacity={0.13}
            scale={0.55}
            drift={i}
          />
        ))}
      </g>

      {/* B2 - the clouds crowd behind "one screen, five calls"; a slate
          overlay turns their overlap to murk. */}
      <g>
        {[620, 685, 750, 815, 880].map((x, i) => (
          <Cloud
            key={`b2-${i}`}
            cx={x}
            cy={1060}
            color={CANON[i].color}
            opacity={0.12}
            scale={0.7}
            drift={i}
          />
        ))}
        <ellipse
          cx={750}
          cy={1060}
          rx={160}
          ry={72}
          fill="#3a3f4d"
          fillOpacity={0.5}
        />
      </g>

      {/* B3 - one monolithic stratus band; the five hues are barely-visible
          ghosts beneath it. */}
      <g>
        {XS.map((x, i) => (
          <Cloud
            key={`b3-${i}`}
            cx={x}
            cy={1540}
            color={CANON[i].color}
            opacity={0.04}
            scale={0.8}
            drift={i}
          />
        ))}
        <rect
          x={80}
          y={1540 - 180}
          width={W - 160}
          height={360}
          fill="#6b7280"
          fillOpacity={0.1}
        />
      </g>

      {/* B4 - the sky clears completely: five crisp clouds, well spaced. */}
      <g>
        {XS.map((x, i) => (
          <Cloud
            key={`b4-${i}`}
            cx={x}
            cy={2020}
            color={CANON[i].color}
            opacity={0.13}
            scale={0.55}
            drift={i}
          />
        ))}
      </g>

      {/* B5 - only Catalog (coral) and Billing (amber) remain, each with a
          faint matching pool behind its own schema card. */}
      <g>
        <ellipse
          cx={276}
          cy={2900}
          rx={190}
          ry={150}
          fill={CANON[0].color}
          fillOpacity={0.05}
        />
        <ellipse
          cx={748}
          cy={2900}
          rx={190}
          ry={150}
          fill={CANON[1].color}
          fillOpacity={0.05}
        />
        <Cloud
          cx={276}
          cy={2740}
          color={CANON[0].color}
          opacity={0.13}
          scale={0.6}
        />
        <Cloud
          cx={748}
          cy={2740}
          color={CANON[1].color}
          opacity={0.13}
          scale={0.6}
          drift={1}
        />
      </g>

      {/* B6 - pre-dawn: a loose arc of five clouds, deliberate gaps between
          them, each rim-lit along its lower edge by one light source. */}
      <g>
        <ellipse
          cx={512}
          cy={4100}
          rx={220}
          ry={130}
          fill="#eaf6f3"
          fillOpacity={0.05}
        />
        {XS.map((x, i) => (
          <Cloud
            key={`b6-${i}`}
            cx={x}
            cy={B6_YS[i]}
            color={CANON[i].color}
            opacity={0.13}
            scale={0.5}
            drift={i}
          />
        ))}
      </g>

      {/* B7 - sunrise: a teal glow at the gateway; the five clouds return,
          small and intact, spaced wide at the frame edges. */}
      <g>
        <circle cx={CHIP.x} cy={CHIP.y} r={260} fill="url(#on-sun)" />
        {B7_XS.map((x, i) => (
          <Cloud
            key={`b7-${i}`}
            cx={x}
            cy={4800}
            color={CANON[i].color}
            opacity={0.12}
            scale={0.5}
            drift={i}
          />
        ))}
      </g>
    </>
  );
}

function OneNightMap() {
  return (
    <svg
      viewBox={`0 0 ${W} ${H}`}
      aria-hidden="true"
      className="absolute inset-0 z-0 hidden h-full w-full sm:block"
    >
      <defs>
        <linearGradient id="on-sky" x1="0" y1="0" x2="0" y2="1">
          <stop offset="0" stopColor="#0c1322" />
          <stop offset="0.1837" stopColor="#0a0e19" />
          <stop offset="0.2245" stopColor="#10141f" />
          <stop offset="0.3265" stopColor="#10141f" />
          <stop offset="0.4286" stopColor="#0b1220" />
          <stop offset="0.6531" stopColor="rgba(80,60,200,0.10)" />
          <stop offset="0.8776" stopColor="#0b1220" />
          <stop offset="0.9224" stopColor="rgba(94,234,212,0.08)" />
          <stop offset="1" stopColor="rgba(22,185,228,0.12)" />
        </linearGradient>
        <linearGradient id="on-gap" x1="0" y1="0" x2="0" y2="1">
          <stop offset="0" stopColor="#fff" />
          <stop offset="0.18" stopColor="#333" />
          <stop offset="0.82" stopColor="#333" />
          <stop offset="1" stopColor="#fff" />
        </linearGradient>
        <mask
          id="on-mask"
          maskUnits="userSpaceOnUse"
          x="0"
          y="0"
          width={W}
          height={H}
        >
          <rect x="0" y="0" width={W} height={H} fill="#fff" />
          <g filter="url(#on-feather)">
            {GAPS.map((g, i) => (
              <rect
                key={i}
                x={g.x}
                y={g.y}
                width={g.w}
                height={g.h}
                fill="url(#on-gap)"
              />
            ))}
          </g>
        </mask>
        <filter id="on-blur" x="-50%" y="-50%" width="200%" height="200%">
          <feGaussianBlur stdDeviation="13" />
        </filter>
        <filter id="on-noise">
          <feTurbulence
            type="fractalNoise"
            baseFrequency="0.9"
            numOctaves="2"
            result="n"
          />
          <feColorMatrix
            in="n"
            type="matrix"
            values="0 0 0 0 1  0 0 0 0 1  0 0 0 0 1  0 0 0 0.02 0"
          />
        </filter>
        <radialGradient id="on-sun" cx="50%" cy="50%" r="50%">
          <stop offset="0" stopColor="#5eead4" stopOpacity="0.08" />
          <stop offset="1" stopColor="#5eead4" stopOpacity="0" />
        </radialGradient>
        <filter id="on-rim" x="-50%" y="-50%" width="200%" height="200%">
          <feGaussianBlur stdDeviation="1.5" />
        </filter>
        <filter id="on-feather" x="-10%" y="-10%" width="120%" height="120%">
          <feGaussianBlur stdDeviation="14" />
        </filter>
      </defs>

      <rect x="0" y="0" width={W} height={H} fill="url(#on-sky)" />
      <rect x="0" y="0" width={W} height={H} filter="url(#on-noise)" />

      <g mask="url(#on-mask)" filter="url(#on-blur)">
        <BeatAtmosphere />
      </g>
      <g mask="url(#on-mask)" filter="url(#on-rim)">
        {XS.map((x, i) => (
          <RimArc
            key={i}
            cx={x}
            cy={B6_YS[i] + 9 * 0.5}
            rx={128 * 0.5}
            ry={37 * 0.5}
          />
        ))}
      </g>

      <NodeCaption x={482} y={1250} label="Merged in the client" toX={740} />
      <NodeCaption
        x={400}
        y={1730}
        label="One layer over everything"
        toX={620}
      />

      {MARKERS.map((m) => (
        <g key={m.s}>
          <rect
            x={m.x - 8}
            y={m.y - 8}
            width={16}
            height={16}
            rx={4}
            fill={CANON[m.s].color}
          />
          <text
            x={m.x + 20}
            y={m.y + 5}
            textAnchor="start"
            fontFamily={MONO}
            fontSize={13}
            letterSpacing="0.18em"
            fill={INK_DIM}
          >
            {CANON[m.s].name.toUpperCase()}
          </text>
        </g>
      ))}

      <NodeCaption x={300} y={4100} label="Schema composition" toX={344} />

      <line
        x1={120}
        x2={904}
        y1={HORIZON_Y}
        y2={HORIZON_Y}
        stroke="rgba(245,241,234,0.22)"
        strokeDasharray="5 7"
      />
      <text
        x={140}
        y={HORIZON_Y - 16}
        fontFamily={MONO}
        fontSize={11}
        letterSpacing="0.2em"
        fill={INK_DIM}
      >
        BUILD TIME
      </text>
      <text
        x={140}
        y={HORIZON_Y + 26}
        fontFamily={MONO}
        fontSize={11}
        letterSpacing="0.2em"
        fill={INK_DIM}
        opacity={0.7}
      >
        RUNTIME
      </text>

      <GatewayChip x={CHIP.x} y={CHIP.y} />
    </svg>
  );
}

/** Multi-stop CSS gradient standing in for the SVG sky on mobile, where the
 * clouds and captions are hidden and only the color of the night survives. */
const MOBILE_SKY =
  "linear-gradient(180deg, #0c1322 0%, #0a0e19 12%, #10141f 18%, #10141f 30%, #0b1220 40%, rgba(80,60,200,0.10) 60%, #0b1220 82%, rgba(94,234,212,0.08) 90%, rgba(22,185,228,0.12) 100%)";

/**
 * Prototype v18 - "One Night": the section as one continuous night sky, dusk
 * at the top and dawn at the bottom. The five services are five small
 * colored clouds that recur at every beat - smeared into murk by the wrong
 * answers, clarified by GraphQL, thinned to two at authorship, rim-lit by
 * one dawn at composition, and answering the executor below the horizon,
 * still five, still separate. The backbone is weather, not geometry: no
 * strokes, no hub, nothing that reads as a connector between the clouds.
 */
export function OneNight() {
  return (
    <div className="relative mx-auto w-full max-w-5xl sm:aspect-[1024/4900]">
      <style>{`
        @keyframes on-drift {
          0%, 100% { transform: translate(0, 0); }
          50% { transform: translate(0, -10px); }
        }
        @media (prefers-reduced-motion: no-preference) {
          .on-cloud { animation: on-drift 60s ease-in-out infinite; }
        }
      `}</style>
      <div
        aria-hidden="true"
        className="absolute inset-0 -z-10 sm:hidden"
        style={{ background: MOBILE_SKY }}
      />
      <OneNightMap />
      <div className="flex flex-col gap-14 px-5 py-16 sm:contents">
        {CHAPTERS.map((chapter, i) => (
          <Fragment key={i}>
            <CopyBlock {...COPY_PLACEMENT[i]} title={chapter.title}>
              {chapter.body}
            </CopyBlock>
            {BOX_PLACEMENT[i].map((p, j) => (
              <CodeBoxSlot key={j} {...p} box={chapter.boxes[j]} />
            ))}
            {i === 5 && (
              <div className="sm:hidden">
                <HorizonRule />
              </div>
            )}
          </Fragment>
        ))}
      </div>
    </div>
  );
}
