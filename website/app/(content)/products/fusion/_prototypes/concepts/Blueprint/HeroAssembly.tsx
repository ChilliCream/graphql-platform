"use client";

import { useReducedMotionPreference, useSceneActive } from "../../Primitives";
import { ASSEMBLY, BOUGHT_IN, BP, FONT, PARTS } from "./palette";
import { DUR, draw, fade, flash, Sheet, stamp } from "./Sheet";

/**
 * Hero plate: the general arrangement drawing of the gateway. The main
 * assembly is drafted in the middle, the five GraphQL sub-assemblies and the
 * two bought-in parts are ballooned around it on the centre ring, and the
 * whole set then lifts off its axes into an exploded view that turns on the
 * table before it settles back together and the sheet is stamped assembled.
 *
 * Rest state: the finished general arrangement - every outline drawn, every
 * balloon numbered, the parts assembled and the sheet stamped.
 */

const CX = 240;
const CY = 190;
const RX = 140;
const RY = 132;
/** How far a part lifts off the assembly on its explosion axis. */
const LIFT = 8;

/** Half the width and height of a sub-assembly box, lettered at the floor. */
const PART_W = 58;
const PART_H = 14;
/** Balloon radius, sized for an item number at the label floor. */
const BALLOON_R = 13;
/** Clearance between a part box and its balloon. */
const BALLOON_GAP = 5;

/**
 * How far along its own radial axis a part's balloon sits: just clear of
 * whichever edge of the box that axis leaves through, so the balloon never
 * lands on the line-work it numbers.
 */
function balloonOffset(cos: number, sin: number): number {
  const across =
    Math.abs(cos) < 1e-6
      ? Infinity
      : (PART_W + BALLOON_R + BALLOON_GAP) / Math.abs(cos);
  const down =
    Math.abs(sin) < 1e-6
      ? Infinity
      : (PART_H + BALLOON_R + BALLOON_GAP) / Math.abs(sin);
  return Math.min(across, down);
}

interface Placed {
  readonly no: string;
  readonly name: string;
  readonly x: number;
  readonly y: number;
  readonly dx: number;
  readonly dy: number;
  /** Balloon centre, relative to the part box. */
  readonly bx: number;
  readonly by: number;
  readonly angle: number;
  readonly boughtIn: boolean;
}

const RING: readonly Placed[] = [...PARTS, ...BOUGHT_IN].map((part, i, all) => {
  const angle = (-90 + (360 / all.length) * i) * (Math.PI / 180);
  const cos = Math.cos(angle);
  const sin = Math.sin(angle);
  const offset = balloonOffset(cos, sin);

  return {
    no: part.no,
    name: part.name.toUpperCase(),
    x: CX + RX * cos,
    y: CY + RY * sin,
    dx: LIFT * cos,
    dy: LIFT * sin,
    bx: offset * cos,
    by: offset * sin,
    angle,
    boughtIn: !("language" in part),
  };
});

/** A rectangle as a path, so `pathLength` and the draw-on animation work everywhere. */
function box(w: number, h: number): string {
  return `M${-w / 2} ${-h / 2}h${w}v${h}h${-w}Z`;
}

const TURN = `
.bp-turn{transform:none;transform-origin:${CX}px ${CY}px;animation:bp-turn ${DUR}s ease-in-out infinite}
@keyframes bp-turn{0%,46%{transform:rotate(0deg)}60%{transform:rotate(-26deg)}74%,100%{transform:rotate(0deg)}}
.bp-upright{transform:none;transform-box:fill-box;transform-origin:center;animation:bp-upright ${DUR}s ease-in-out infinite}
@keyframes bp-upright{0%,46%{transform:rotate(0deg)}60%{transform:rotate(26deg)}74%,100%{transform:rotate(0deg)}}
.bp-assembled{opacity:1;animation:bp-assembled ${DUR}s linear infinite}
@keyframes bp-assembled{0%,40%{opacity:1}44%,76%{opacity:0}80%,100%{opacity:1}}
`;

const LIFTS = RING.map(
  (part, i) => `
.bp-lift${i}{transform:none;animation:bp-lift${i} ${DUR}s ease-in-out infinite}
@keyframes bp-lift${i}{
0%,38%{transform:none}
46%,74%{transform:translate(${part.dx.toFixed(1)}px,${part.dy.toFixed(1)}px)}
82%,100%{transform:none}}`,
).join("\n");

const CSS = `
${TURN}
${LIFTS}
${fade("bp-h-centre", 4)}
${draw("bp-h-assy", 6, 8)}
${fade("bp-h-assy-label", 14)}
${draw("bp-h-core", 16, 4)}
${RING.map((_, i) => draw(`bp-h-part${i}`, 18 + i * 2, 5)).join("\n")}
${RING.map((_, i) => fade(`bp-h-label${i}`, 22 + i * 2)).join("\n")}
${RING.map((_, i) => fade(`bp-h-balloon${i}`, 30 + i * 1.4)).join("\n")}
${flash("bp-h-axis", 40, 76)}
${flash("bp-h-exploded", 42, 76)}
${stamp("bp-h-stamp", 84)}
`;

interface PartGroupProps {
  readonly part: Placed;
  readonly index: number;
}

function PartGroup({ part, index }: PartGroupProps) {
  const cos = Math.cos(part.angle);
  const sin = Math.sin(part.angle);

  return (
    <g className={`bp-lift${index}`}>
      <g transform={`translate(${part.x.toFixed(1)} ${part.y.toFixed(1)})`}>
        <g className="bp-upright">
          {part.boughtIn ? (
            <path
              className={`bp-h-label${index}`}
              d={box(PART_W * 2, PART_H * 2)}
              fill={BP.plate}
              stroke={BP.ink}
              strokeWidth={1}
              strokeDasharray="5 3"
            />
          ) : (
            <path
              className={`bp-h-part${index}`}
              d={box(PART_W * 2, PART_H * 2)}
              pathLength={1}
              fill={BP.plate}
              stroke={BP.ink}
              strokeWidth={1.2}
            />
          )}

          <text
            className={`bp-h-label${index}`}
            textAnchor="middle"
            y={FONT.label * 0.35}
            fontSize={FONT.label}
          >
            {part.name}
          </text>

          <g className={`bp-h-balloon${index}`}>
            <line
              x1={(part.bx - cos * BALLOON_R).toFixed(1)}
              y1={(part.by - sin * BALLOON_R).toFixed(1)}
              x2={(part.bx - cos * (BALLOON_R + BALLOON_GAP)).toFixed(1)}
              y2={(part.by - sin * (BALLOON_R + BALLOON_GAP)).toFixed(1)}
              stroke={BP.dim}
              strokeWidth={0.8}
            />
            <circle
              cx={part.bx.toFixed(1)}
              cy={part.by.toFixed(1)}
              r={BALLOON_R}
              fill={BP.plate}
              stroke={BP.dim}
              strokeWidth={0.8}
            />
            <text
              className="bp-t-cyan"
              textAnchor="middle"
              x={part.bx.toFixed(1)}
              y={(part.by + FONT.label * 0.36).toFixed(1)}
              fontSize={FONT.label}
            >
              {index + 1}
            </text>
          </g>
        </g>
      </g>
    </g>
  );
}

export function HeroAssembly() {
  const active = useSceneActive();
  const reduced = useReducedMotionPreference();

  return (
    <Sheet
      title="General arrangement"
      no="DWG-100"
      rev="C"
      field={ASSEMBLY.note.toUpperCase()}
      run={active && !reduced}
    >
      <svg
        viewBox="0 0 480 380"
        preserveAspectRatio="xMidYMid meet"
        className="absolute inset-0 h-full w-full"
      >
        <style>{CSS}</style>

        {/* Centre lines through the assembly datum */}
        <g className="bp-h-centre" stroke={BP.inkFaint} strokeWidth={0.8}>
          <line x1={8} y1={CY} x2={472} y2={CY} strokeDasharray="14 4 2 4" />
          <line x1={CX} y1={8} x2={CX} y2={372} strokeDasharray="14 4 2 4" />
        </g>

        {/* Explosion axes, drawn only while the assembly is apart */}
        <g className="bp-turn">
          <g className="bp-h-axis" stroke={BP.dim} strokeWidth={0.7}>
            {RING.map((part) => (
              <line
                key={part.no}
                x1={CX + 66 * Math.cos(part.angle)}
                y1={CY + 44 * Math.sin(part.angle)}
                x2={CX + (RX + LIFT + 22) * Math.cos(part.angle)}
                y2={CY + (RY + LIFT + 22) * Math.sin(part.angle)}
                strokeDasharray="9 3 1.5 3"
              />
            ))}
          </g>

          {RING.map((part, i) => (
            <PartGroup key={part.no} part={part} index={i} />
          ))}
        </g>

        {/* Main assembly */}
        <g transform={`translate(${CX} ${CY})`}>
          <path
            className="bp-h-assy"
            d={box(132, 88)}
            pathLength={1}
            fill={BP.plate}
            stroke={BP.ink}
            strokeWidth={1.6}
          />
          <path
            className="bp-h-core"
            d={box(112, 68)}
            pathLength={1}
            fill="none"
            stroke={BP.inkFaint}
            strokeWidth={0.8}
          />
          <g className="bp-h-assy-label">
            <text textAnchor="middle" y={-14} fontSize={FONT.title}>
              FUSION
            </text>
            <text textAnchor="middle" y={8} fontSize={FONT.label}>
              GATEWAY
            </text>
            <text
              className="bp-t-dim"
              textAnchor="middle"
              y={30}
              fontSize={FONT.label}
            >
              {ASSEMBLY.no}
            </text>
          </g>
        </g>

        {/* Sheet notes */}
        <text
          className="bp-assembled bp-t-dim"
          x={12}
          y={22}
          fontSize={FONT.label}
        >
          ASSEMBLED VIEW
        </text>
        <text
          className="bp-h-exploded bp-t-cyan"
          x={12}
          y={22}
          fontSize={FONT.label}
        >
          EXPLODED VIEW
        </text>
        <text className="bp-t-dim" x={14} y={352} fontSize={FONT.label}>
          DASHED =
        </text>
        <text className="bp-t-dim" x={14} y={372} fontSize={FONT.label}>
          BOUGHT-IN
        </text>

        <g className="bp-h-stamp" style={{ transformOrigin: "392px 22px" }}>
          <path
            d="M320 6h145v32h-145Z"
            fill="none"
            stroke={BP.ok}
            strokeWidth={1.2}
          />
          <text
            x={392}
            y={28}
            className="bp-t-ok"
            textAnchor="middle"
            fontSize={FONT.label}
            style={{ letterSpacing: "0.14em" }}
          >
            ASSEMBLED
          </text>
        </g>
      </svg>
    </Sheet>
  );
}
