"use client";

import { useReducedMotionPreference, useSceneActive } from "../../Primitives";
import { ASSEMBLY, BOUGHT_IN, BP, PARTS } from "./palette";
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
const CY = 180;
const RX = 150;
const RY = 112;
/** How far a part lifts off the assembly on its explosion axis. */
const LIFT = 18;

interface Placed {
  readonly no: string;
  readonly name: string;
  readonly meta: string;
  readonly x: number;
  readonly y: number;
  readonly dx: number;
  readonly dy: number;
  readonly angle: number;
  readonly boughtIn: boolean;
}

const RING: readonly Placed[] = [...PARTS, ...BOUGHT_IN].map((part, i, all) => {
  const angle = (-90 + (360 / all.length) * i) * (Math.PI / 180);
  const boughtIn = !("language" in part);

  return {
    no: part.no,
    name: part.name.toUpperCase(),
    meta: boughtIn
      ? `${(part as (typeof BOUGHT_IN)[number]).kind} · BOUGHT-IN`
      : (part as (typeof PARTS)[number]).language,
    x: CX + RX * Math.cos(angle),
    y: CY + RY * Math.sin(angle),
    dx: LIFT * Math.cos(angle),
    dy: LIFT * Math.sin(angle),
    angle,
    boughtIn,
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
  return (
    <g className={`bp-lift${index}`}>
      <g transform={`translate(${part.x.toFixed(1)} ${part.y.toFixed(1)})`}>
        <g className="bp-upright">
          {part.boughtIn ? (
            <path
              className={`bp-h-label${index}`}
              d={box(84, 34)}
              fill={BP.plate}
              stroke={BP.ink}
              strokeWidth={1}
              strokeDasharray="5 3"
            />
          ) : (
            <path
              className={`bp-h-part${index}`}
              d={box(84, 34)}
              pathLength={1}
              fill={BP.plate}
              stroke={BP.ink}
              strokeWidth={1.2}
            />
          )}

          <g className={`bp-h-label${index}`}>
            <text textAnchor="middle" y={-1} fontSize={10}>
              {part.name}
            </text>
            <text
              className="bp-t-dim"
              textAnchor="middle"
              y={10}
              fontSize={6.5}
            >
              {`${part.no} · ${part.meta}`}
            </text>
          </g>

          <g className={`bp-h-balloon${index}`}>
            <line
              x1={0}
              y1={-24}
              x2={0}
              y2={-17}
              stroke={BP.dim}
              strokeWidth={0.8}
            />
            <circle
              cx={0}
              cy={-31}
              r={7}
              fill={BP.plate}
              stroke={BP.dim}
              strokeWidth={0.8}
            />
            <text
              className="bp-t-cyan"
              textAnchor="middle"
              x={0}
              y={-28.5}
              fontSize={7.5}
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
      title={`${ASSEMBLY.name} · general arrangement`}
      no="DWG-100"
      rev="C"
      field="1:1"
      run={active && !reduced}
    >
      <svg
        viewBox="0 0 480 360"
        preserveAspectRatio="xMidYMid meet"
        className="absolute inset-0 h-full w-full"
      >
        <style>{CSS}</style>

        {/* Centre lines through the assembly datum */}
        <g className="bp-h-centre" stroke={BP.inkFaint} strokeWidth={0.8}>
          <line x1={8} y1={CY} x2={472} y2={CY} strokeDasharray="14 4 2 4" />
          <line x1={CX} y1={8} x2={CX} y2={352} strokeDasharray="14 4 2 4" />
        </g>

        {/* Explosion axes, drawn only while the assembly is apart */}
        <g className="bp-turn">
          <g className="bp-h-axis" stroke={BP.dim} strokeWidth={0.7}>
            {RING.map((part) => (
              <line
                key={part.no}
                x1={CX + 62 * Math.cos(part.angle)}
                y1={CY + 46 * Math.sin(part.angle)}
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
            d={box(172, 96)}
            pathLength={1}
            fill={BP.plate}
            stroke={BP.ink}
            strokeWidth={1.6}
          />
          <path
            className="bp-h-core"
            d={box(150, 74)}
            pathLength={1}
            fill="none"
            stroke={BP.inkFaint}
            strokeWidth={0.8}
          />
          <g className="bp-h-assy-label">
            <text textAnchor="middle" y={-14} fontSize={15}>
              FUSION
            </text>
            <text textAnchor="middle" y={-1} fontSize={8.5}>
              GATEWAY
            </text>
            <text className="bp-t-dim" textAnchor="middle" y={12} fontSize={7}>
              {ASSEMBLY.no}
            </text>
            <text className="bp-t-dim" textAnchor="middle" y={25} fontSize={6}>
              COMPOSITE SCHEMA · DISTRIBUTED EXECUTOR
            </text>
          </g>
        </g>

        {/* Sheet notes */}
        <text className="bp-assembled bp-t-dim" x={14} y={22} fontSize={7.5}>
          ASSEMBLED VIEW
        </text>
        <text className="bp-h-exploded bp-t-cyan" x={14} y={22} fontSize={7.5}>
          EXPLODED VIEW · TURNTABLE
        </text>
        <text className="bp-t-dim" x={14} y={348} fontSize={7}>
          {`${RING.length} SUB-ASSEMBLIES · 1 COMPOSITE SCHEMA`}
        </text>

        <g className="bp-h-stamp" style={{ transformOrigin: "398px 336px" }}>
          <path
            d="M340 322h116v28H340Z"
            fill="none"
            stroke={BP.ok}
            strokeWidth={1.2}
          />
          <text
            x={398}
            y={340}
            className="bp-t-ok"
            textAnchor="middle"
            fontSize={9}
            style={{ letterSpacing: "0.14em" }}
          >
            ASSEMBLED
          </text>
        </g>
      </svg>
    </Sheet>
  );
}
