"use client";

import { useReducedMotionPreference, useSceneActive } from "../../Primitives";
import {
  CLIENTS,
  drawStyle,
  fadeStyle,
  HAND,
  MARKER,
  NOTES,
  PAPER,
} from "./palette";
import { Folio, PaperGround, StickyNote, Wobble } from "./Sketch";

/**
 * Hero drawing: the whole page sketched onto the cover in marker. The gateway
 * hub draws itself first, then the doodled clients and their lines, then the
 * subgraph sticky notes below, and finally the annotations in the margin.
 *
 * Rest state (reduced motion, or before hydration): the finished drawing.
 */

const CSS = `
.ed-hero .ed-hero-draw { stroke-dasharray: var(--len, 400); stroke-dashoffset: 0; }
.ed-hero[data-run="true"] .ed-hero-draw { animation: ed-hero-draw 1.1s ease-out both; }
.ed-hero[data-run="true"] .ed-hero-fade { animation: ed-hero-fade 0.8s ease-out both; }
.ed-hero[data-run="true"] .ed-hero-drop { animation: ed-hero-drop 0.7s cubic-bezier(0.2, 0.9, 0.3, 1.2) both; transform-box: fill-box; transform-origin: center; }
.ed-hero[data-run="true"] .ed-hero-flow { animation: ed-hero-flow 5s linear infinite; }
@keyframes ed-hero-draw {
  from { stroke-dashoffset: var(--len, 400); }
  to { stroke-dashoffset: 0; }
}
@keyframes ed-hero-fade {
  from { opacity: 0; }
  to { opacity: 1; }
}
@keyframes ed-hero-drop {
  from { opacity: 0; transform: translateY(-14px) rotate(-4deg); }
  to { opacity: 1; transform: translateY(0) rotate(0deg); }
}
@keyframes ed-hero-flow { to { stroke-dashoffset: -220; } }
`;

const HUB = { x: 600, y: 252, r: 84 };

/** Where each doodled client sits on the top edge of the sheet. */
const DESKS = [
  { x: 150, y: 96 },
  { x: 420, y: 70 },
  { x: 772, y: 74 },
  { x: 1040, y: 100 },
];

/** Where each subgraph note is stuck along the foot of the drawing. */
const PINS = [
  { x: 74, y: 452, tilt: -2.4 },
  { x: 292, y: 470, tilt: 1.8 },
  { x: 510, y: 452, tilt: -1.2 },
  { x: 728, y: 470, tilt: 2.6 },
  { x: 946, y: 452, tilt: -2 },
];

const NOTE_W = 176;
const NOTE_H = 84;

interface DoodleProps {
  readonly device: (typeof CLIENTS)[number]["device"];
}

/** A client, drawn the way someone sketches a device in a margin. */
function Doodle({ device }: DoodleProps) {
  const stroke = {
    fill: "none",
    stroke: PAPER.ink,
    strokeWidth: 2.4,
    strokeLinecap: "round" as const,
    strokeLinejoin: "round" as const,
  };

  if (device === "laptop") {
    return (
      <g {...stroke}>
        <path d="M-42 -30 h84 v54 h-84 Z" />
        <path d="M-56 24 h112 l-10 12 h-92 Z" />
        <path d="M-26 -14 h44 M-26 0 h30" strokeWidth={1.6} opacity={0.6} />
      </g>
    );
  }

  if (device === "phone") {
    return (
      <g {...stroke}>
        <path d="M-20 -34 h40 v68 h-40 Z" />
        <path d="M-8 -28 h16" strokeWidth={1.6} opacity={0.6} />
        <path d="M-10 24 h20" strokeWidth={1.6} opacity={0.6} />
      </g>
    );
  }

  if (device === "card") {
    return (
      <g {...stroke}>
        <path d="M-46 -26 h92 v52 h-92 Z" />
        <path d="M-34 -10 h44 M-34 4 h64" strokeWidth={1.6} opacity={0.6} />
      </g>
    );
  }

  return (
    <g {...stroke}>
      <path d="M-30 -18 h60 v44 h-60 Z" />
      <circle cx={-12} cy={2} r={6} />
      <circle cx={12} cy={2} r={6} />
      <path d="M0 -18 v-14 M-14 32 v10 M14 32 v10" />
      <path d="M-30 26 h60" />
    </g>
  );
}

/** Curve from a client desk down into the hub. */
function deskPath(i: number): string {
  const d = DESKS[i];
  const dx = HUB.x - d.x;
  return `M${d.x} ${d.y + 46} C ${d.x + dx * 0.16} ${d.y + 150}, ${HUB.x - dx * 0.3} ${HUB.y - 150}, ${HUB.x - Math.sign(dx) * 26} ${HUB.y - HUB.r + 6}`;
}

/** Curve from the hub down to a subgraph note. */
function pinPath(i: number): string {
  const p = PINS[i];
  const tx = p.x + NOTE_W / 2;
  return `M${HUB.x} ${HUB.y + HUB.r - 4} C ${HUB.x} ${HUB.y + 110}, ${tx} ${p.y - 110}, ${tx} ${p.y - 6}`;
}

export function HeroSketch() {
  const active = useSceneActive();
  const reduced = useReducedMotionPreference();
  const run = active && !reduced;

  return (
    <div className="ed-hero absolute inset-0" data-run={run ? "true" : "false"}>
      <style>{CSS}</style>
      <svg
        viewBox="0 0 1200 700"
        preserveAspectRatio="xMidYMid slice"
        className="h-full w-full"
        aria-hidden="true"
      >
        <defs>
          <Wobble id="ed-hero-wob" scale={1.8} frequency={0.016} seed={5} />
          <Wobble id="ed-hero-wob2" scale={1.2} frequency={0.03} seed={11} />
        </defs>

        <PaperGround width={1200} height={700} />

        <g filter="url(#ed-hero-wob)">
          {/* Lines from the clients into the one endpoint */}
          {DESKS.map((_, i) => (
            <g key={CLIENTS[i].name}>
              <path
                className="ed-hero-draw"
                style={drawStyle(420, 1.2 + i * 0.14)}
                d={deskPath(i)}
                fill="none"
                stroke={PAPER.ink}
                strokeWidth={2}
                strokeLinecap="round"
                opacity={0.8}
              />
              <path
                className="ed-hero-flow"
                d={deskPath(i)}
                fill="none"
                stroke={PAPER.blue}
                strokeWidth={2.6}
                strokeDasharray="3 30"
                strokeLinecap="round"
                opacity={0.7}
                style={{ animationDelay: `${i * 0.7}s` }}
              />
            </g>
          ))}

          {/* Lines from the gateway down to the subgraphs */}
          {PINS.map((pin, i) => (
            <path
              key={NOTES[i].name}
              className="ed-hero-draw"
              style={drawStyle(400, 2.4 + i * 0.12)}
              d={pinPath(i)}
              fill="none"
              stroke={PAPER.ink}
              strokeWidth={2}
              strokeLinecap="round"
              opacity={0.7}
            />
          ))}
        </g>

        {/* The clients themselves */}
        {DESKS.map((desk, i) => (
          <g
            key={CLIENTS[i].name}
            className="ed-hero-fade"
            style={fadeStyle(1.1 + i * 0.14)}
            transform={`translate(${desk.x} ${desk.y})`}
            filter="url(#ed-hero-wob2)"
          >
            <Doodle device={CLIENTS[i].device} />
            <text
              x={0}
              y={62}
              textAnchor="middle"
              fill={PAPER.inkSoft}
              fontSize={16}
              style={HAND}
            >
              {CLIENTS[i].name}
            </text>
          </g>
        ))}

        {/* The gateway: one hub, drawn twice the way a marker doubles a circle */}
        <g filter="url(#ed-hero-wob)">
          <circle
            className="ed-hero-draw"
            style={drawStyle(560, 0.05)}
            cx={HUB.x}
            cy={HUB.y}
            r={HUB.r}
            fill={PAPER.sheet}
            stroke={PAPER.ink}
            strokeWidth={3}
          />
          <circle
            className="ed-hero-draw"
            style={drawStyle(600, 0.35)}
            cx={HUB.x}
            cy={HUB.y}
            r={HUB.r + 8}
            fill="none"
            stroke={PAPER.ink}
            strokeWidth={1.6}
            opacity={0.45}
          />
        </g>
        <text
          className="ed-hero-fade"
          style={{ ...MARKER, ...fadeStyle(0.9) }}
          x={HUB.x}
          y={HUB.y + 6}
          textAnchor="middle"
          fill={PAPER.ink}
          fontSize={30}
        >
          gateway
        </text>
        <text
          className="ed-hero-fade"
          style={{ ...HAND, ...fadeStyle(1.05) }}
          x={HUB.x}
          y={HUB.y + 32}
          textAnchor="middle"
          fill={PAPER.inkSoft}
          fontSize={15}
        >
          one endpoint
        </text>

        {/* The subgraphs, stuck on as notes */}
        {PINS.map((pin, i) => (
          <g
            key={NOTES[i].name}
            className="ed-hero-drop"
            style={fadeStyle(2.5 + i * 0.12)}
          >
            <StickyNote
              x={pin.x}
              y={pin.y}
              width={NOTE_W}
              height={NOTE_H}
              tilt={pin.tilt}
              stock={NOTES[i].stock}
              name={NOTES[i].name}
              caption={`${NOTES[i].language} · ${NOTES[i].specLabel}`}
            />
          </g>
        ))}

        {/* Margin annotations, added last */}
        <g className="ed-hero-fade" style={fadeStyle(3.5)}>
          <path
            d="M60 566 C 300 596, 900 596, 1140 566"
            fill="none"
            stroke={PAPER.pencil}
            strokeWidth={1.8}
            strokeLinecap="round"
            filter="url(#ed-hero-wob2)"
          />
          <text
            x={600}
            y={624}
            textAnchor="middle"
            fill={PAPER.pencil}
            fontSize={19}
            style={HAND}
          >
            source schemas, one per team
          </text>
        </g>
        <g className="ed-hero-fade" style={fadeStyle(3.8)}>
          <path
            d="M796 244 C 856 232, 892 246, 918 262"
            fill="none"
            stroke={PAPER.red}
            strokeWidth={2}
            strokeLinecap="round"
            filter="url(#ed-hero-wob2)"
          />
          <path
            d="M796 244 l16 -8 M796 244 l14 10"
            fill="none"
            stroke={PAPER.red}
            strokeWidth={2}
            strokeLinecap="round"
          />
          <text x={926} y={272} fill={PAPER.red} fontSize={19} style={HAND}>
            composite schema
          </text>
        </g>

        <Folio x={60} y={48}>
          Fusion, drawn from the margin up
        </Folio>
      </svg>
    </div>
  );
}
