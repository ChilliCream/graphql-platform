"use client";

import { useReducedMotionPreference, useSceneActive } from "../../Primitives";
import { BOUGHT_IN, BP, PARTS } from "./palette";
import { draw, fade, Sheet, stamp } from "./Sheet";

/**
 * "Both specifications, one gateway": the title block of every source schema
 * on one sheet. Each sub-assembly is stamped with the specification its schema
 * is written to - the two stamps are drafted identically, because neither is
 * the default - and every stamped drawing runs down the same trunk into one
 * composite schema. Underneath, two bought-in parts that are not GraphQL
 * servers at all join the same trunk through their adapter drawings.
 *
 * Rest state: every row stamped, both adapters drawn and the trunk closed into
 * the composite schema.
 */

const ROW_X = 14;
const ROW_W = 286;
const ROW_H = 28;
const TRUNK_X = 320;
const STAMP_X = 196;

const ROWS = PARTS.map((part, i) => ({ ...part, y: 26 + i * 34 }));
const SOURCES = BOUGHT_IN.map((part, i) => ({ ...part, y: 200 + i * 34 }));

const CSS = `
${fade("bp-s-head", 1)}
${ROWS.map((_, i) => draw(`bp-s-row${i}`, 4 + i * 3, 5)).join("\n")}
${ROWS.map((_, i) => fade(`bp-s-label${i}`, 8 + i * 3)).join("\n")}
${ROWS.map((_, i) => stamp(`bp-s-stamp${i}`, 26 + i * 3.6)).join("\n")}
${SOURCES.map((_, i) => fade(`bp-s-src${i}`, 46 + i * 4)).join("\n")}
${SOURCES.map((_, i) => draw(`bp-s-adapter${i}`, 50 + i * 4, 5)).join("\n")}
${SOURCES.map((_, i) => fade(`bp-s-adapter-label${i}`, 55 + i * 4)).join("\n")}
${draw("bp-s-trunk", 60, 8)}
${[...ROWS, ...SOURCES].map((_, i) => draw(`bp-s-link${i}`, 62 + i * 1.4, 3)).join("\n")}
${draw("bp-s-out", 74, 6)}
${fade("bp-s-out-label", 80)}
${fade("bp-s-note", 86)}
`;

interface StampMarkProps {
  readonly className: string;
  readonly y: number;
  readonly label: string;
}

/** The specification stamp: one box, one line of lettering, both specs alike. */
function StampMark({ className, y, label }: StampMarkProps) {
  return (
    <g
      className={className}
      style={{ transformOrigin: `${STAMP_X + 49}px ${y + 14}px` }}
    >
      <path
        d={`M${STAMP_X} ${y + 4}h98v20h-98Z`}
        fill="none"
        stroke={BP.ink}
        strokeWidth={1}
      />
      <text
        x={STAMP_X + 49}
        y={y + 18}
        textAnchor="middle"
        fontSize={6.2}
        style={{ letterSpacing: "0.1em" }}
      >
        {label.toUpperCase()}
      </text>
    </g>
  );
}

interface AdapterProps {
  readonly className: string;
  /** Fade class for the lettering, which has no stroke to draw on. */
  readonly labelClassName: string;
  readonly y: number;
  readonly label: string;
}

/** The adapter drawing a bought-in part mates to the assembly with. */
function Adapter({ className, labelClassName, y, label }: AdapterProps) {
  return (
    <g className={className}>
      <path
        d={`M${STAMP_X} ${y + 4}h98v20h-98Z`}
        pathLength={1}
        fill="none"
        stroke={BP.dim}
        strokeWidth={1}
      />
      <path
        d={`M${STAMP_X + 18} ${y + 4}v20M${STAMP_X + 80} ${y + 4}v20`}
        pathLength={1}
        fill="none"
        stroke={BP.dim}
        strokeWidth={0.8}
      />
      <text
        className={`${labelClassName} bp-t-cyan`}
        x={STAMP_X + 49}
        y={y + 18}
        textAnchor="middle"
        fontSize={6.2}
      >
        {label}
      </text>
    </g>
  );
}

export function SpecStamps() {
  const active = useSceneActive();
  const reduced = useReducedMotionPreference();

  return (
    <Sheet
      title="Specification stamps"
      no="DWG-102"
      rev="B"
      run={active && !reduced}
    >
      <svg
        viewBox="0 0 400 400"
        preserveAspectRatio="xMidYMid meet"
        className="absolute inset-0 h-full w-full"
      >
        <style>{CSS}</style>

        <text className="bp-s-head bp-t-dim" x={ROW_X} y={16} fontSize={6.5}>
          SOURCE SCHEMA · SPECIFICATION STAMP
        </text>

        {ROWS.map((row, i) => (
          <g key={row.no}>
            <path
              className={`bp-s-row${i}`}
              d={`M${ROW_X} ${row.y}h${ROW_W}v${ROW_H}h${-ROW_W}Z`}
              pathLength={1}
              fill={BP.plate}
              stroke={BP.ink}
              strokeWidth={1.1}
            />
            <g className={`bp-s-label${i}`}>
              <text x={ROW_X + 10} y={row.y + 13} fontSize={9}>
                {row.name.toUpperCase()}
              </text>
              <text
                className="bp-t-dim"
                x={ROW_X + 10}
                y={row.y + 23}
                fontSize={6}
              >
                {`${row.no} · ${row.language}`}
              </text>
            </g>
            <StampMark
              className={`bp-s-stamp${i}`}
              y={row.y}
              label={row.spec}
            />
          </g>
        ))}

        {SOURCES.map((source, i) => (
          <g key={source.no}>
            <g className={`bp-s-src${i}`}>
              <path
                d={`M${ROW_X} ${source.y}h${ROW_W}v${ROW_H}h${-ROW_W}Z`}
                fill={BP.plate}
                stroke={BP.ink}
                strokeWidth={1}
                strokeDasharray="5 3"
              />
              <text x={ROW_X + 10} y={source.y + 13} fontSize={9}>
                {source.name.toUpperCase()}
              </text>
              <text
                className="bp-t-dim"
                x={ROW_X + 10}
                y={source.y + 23}
                fontSize={6}
              >
                {`${source.no} · ${source.kind} · BOUGHT-IN PART`}
              </text>
            </g>
            <Adapter
              className={`bp-s-adapter${i}`}
              labelClassName={`bp-s-adapter-label${i}`}
              y={source.y}
              label={source.adapter}
            />
          </g>
        ))}

        {/* The trunk every stamped drawing runs down */}
        <path
          className="bp-s-trunk"
          d={`M${TRUNK_X} 40v280`}
          pathLength={1}
          fill="none"
          stroke={BP.ink}
          strokeWidth={1.2}
        />
        {[...ROWS, ...SOURCES].map((row, i) => (
          <path
            key={`link-${row.no}`}
            className={`bp-s-link${i}`}
            d={`M${ROW_X + ROW_W} ${row.y + ROW_H / 2}h${TRUNK_X - ROW_X - ROW_W}`}
            pathLength={1}
            fill="none"
            stroke={BP.inkFaint}
            strokeWidth={0.9}
          />
        ))}

        {/* One composite schema */}
        <path
          className="bp-s-out"
          d={`M${ROW_X} 320h372v42h-372Z`}
          pathLength={1}
          fill={BP.plate}
          stroke={BP.ink}
          strokeWidth={1.6}
        />
        <g className="bp-s-out-label">
          <text x={200} y={340} textAnchor="middle" fontSize={10}>
            ONE COMPOSITE SCHEMA
          </text>
          <text
            className="bp-t-dim"
            x={200}
            y={353}
            textAnchor="middle"
            fontSize={6}
          >
            ASSY-100 · ONE GATEWAY · SEVEN SOURCE SCHEMAS
          </text>
        </g>

        <text className="bp-s-note bp-t-dim" x={ROW_X} y={382} fontSize={6.2}>
          EVERY CONTRACT VALIDATED IN THE SAME COMPOSITION STEP
        </text>
      </svg>
    </Sheet>
  );
}
