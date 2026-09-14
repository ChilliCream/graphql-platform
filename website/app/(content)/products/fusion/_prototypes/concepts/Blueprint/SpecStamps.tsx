"use client";

import { useReducedMotionPreference, useSceneActive } from "../../Primitives";
import { BOUGHT_IN, BP, FONT, PARTS } from "./palette";
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
const ROW_W = 426;
const ROW_H = 42;
const ROW_PITCH = 48;
const TRUNK_X = 456;
/** Left edge of the stamp box, sized for the longer of the two specifications. */
const STAMP_X = 180;
const STAMP_W = 252;

const ROWS = PARTS.map((part, i) => ({ ...part, y: 32 + i * ROW_PITCH }));
const SOURCES = BOUGHT_IN.map((part, i) => ({
  ...part,
  y: 274 + i * ROW_PITCH,
}));

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
      style={{ transformOrigin: `${STAMP_X + STAMP_W / 2}px ${y + 21}px` }}
    >
      <path
        d={`M${STAMP_X} ${y + 8}h${STAMP_W}v26h${-STAMP_W}Z`}
        fill="none"
        stroke={BP.ink}
        strokeWidth={1}
      />
      <text
        x={STAMP_X + STAMP_W / 2}
        y={y + 27}
        textAnchor="middle"
        fontSize={FONT.label}
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
        d={`M${STAMP_X} ${y + 8}h${STAMP_W}v26h${-STAMP_W}Z`}
        pathLength={1}
        fill="none"
        stroke={BP.dim}
        strokeWidth={1}
      />
      <path
        d={`M${STAMP_X + 34} ${y + 8}v26M${STAMP_X + STAMP_W - 34} ${y + 8}v26`}
        pathLength={1}
        fill="none"
        stroke={BP.dim}
        strokeWidth={0.8}
      />
      <text
        className={`${labelClassName} bp-t-cyan`}
        x={STAMP_X + STAMP_W / 2}
        y={y + 27}
        textAnchor="middle"
        fontSize={FONT.label}
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
        viewBox="0 0 480 442"
        preserveAspectRatio="xMidYMid meet"
        className="absolute inset-0 h-full w-full"
      >
        <style>{CSS}</style>

        <text
          className="bp-s-head bp-t-dim"
          x={ROW_X}
          y={22}
          fontSize={FONT.label}
        >
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
              <text x={ROW_X + 8} y={row.y + 18} fontSize={FONT.label}>
                {row.name.toUpperCase()}
              </text>
              <text
                className="bp-t-dim"
                x={ROW_X + 8}
                y={row.y + 36}
                fontSize={FONT.label}
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
              <text x={ROW_X + 8} y={source.y + 18} fontSize={FONT.label}>
                {source.name.toUpperCase()}
              </text>
              <text
                className="bp-t-dim"
                x={ROW_X + 8}
                y={source.y + 36}
                fontSize={FONT.label}
              >
                {`${source.no} · ${source.kind.toUpperCase()}`}
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
          d={`M${TRUNK_X} 48v328`}
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
          d={`M${ROW_X} 376h452v48h-452Z`}
          pathLength={1}
          fill={BP.plate}
          stroke={BP.ink}
          strokeWidth={1.6}
        />
        <g className="bp-s-out-label">
          <text x={240} y={400} textAnchor="middle" fontSize={FONT.label}>
            ONE COMPOSITE SCHEMA
          </text>
          <text
            className="bp-t-dim"
            x={240}
            y={418}
            textAnchor="middle"
            fontSize={FONT.label}
          >
            SEVEN SOURCE SCHEMAS
          </text>
        </g>

        <text
          className="bp-s-note bp-t-dim"
          x={ROW_X}
          y={438}
          fontSize={FONT.label}
        >
          DASHED = BOUGHT-IN
        </text>
        <text
          className="bp-s-note bp-t-dim"
          x={466}
          y={438}
          textAnchor="end"
          fontSize={FONT.label}
        >
          ONE COMPOSITION STEP
        </text>
      </svg>
    </Sheet>
  );
}
