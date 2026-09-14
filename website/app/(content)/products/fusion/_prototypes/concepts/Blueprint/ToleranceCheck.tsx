"use client";

import { useReducedMotionPreference, useSceneActive } from "../../Primitives";
import { BP, FONT, PARTS } from "./palette";
import { draw, fade, Sheet, stamp } from "./Sheet";

/**
 * "Any GraphQL server, no plugin": composition drawn as the tolerance check
 * between two drawings. Two ordinary GraphQL servers, written in two
 * languages, declare the same feature to two different types. The check walks
 * the schedule - keys, lookups, field types, enum values - and on the third
 * line the drawings disagree, so a red-line revision cloud goes round the
 * mismatch and the drawing set is rejected before anything is deployed.
 *
 * Rest state: the completed check with its failing line, the revision cloud
 * round the conflict and the rejected stamp on the sheet.
 */

const CHECKS = [
  { label: "KEYS DECLARED", state: "ok" },
  { label: "LOOKUPS RESOLVE", state: "ok" },
  { label: "FIELD TYPES", state: "fail" },
  { label: "ENUM VALUES", state: "skipped" },
] as const;

/** Width of one source drawing; the pair fills the sheet. */
const DRAWING_W = 226;

const LANGUAGES = PARTS.map((part) => part.language);

/** A red-line revision cloud around the mismatched dimension. */
function cloud(x: number, y: number, w: number, h: number, r = 9): string {
  const nx = Math.max(2, Math.round(w / (r * 2)));
  const ny = Math.max(1, Math.round(h / (r * 2)));
  const sx = w / nx;
  const sy = h / ny;
  const parts = [`M${x} ${y}`];

  for (let i = 0; i < nx; i++) {
    parts.push(
      `A${sx / 2} ${sx / 2} 0 0 1 ${(x + sx * (i + 1)).toFixed(1)} ${y}`,
    );
  }
  for (let i = 0; i < ny; i++) {
    parts.push(
      `A${sy / 2} ${sy / 2} 0 0 1 ${x + w} ${(y + sy * (i + 1)).toFixed(1)}`,
    );
  }
  for (let i = 0; i < nx; i++) {
    parts.push(
      `A${sx / 2} ${sx / 2} 0 0 1 ${(x + w - sx * (i + 1)).toFixed(1)} ${y + h}`,
    );
  }
  for (let i = 0; i < ny; i++) {
    parts.push(
      `A${sy / 2} ${sy / 2} 0 0 1 ${x} ${(y + h - sy * (i + 1)).toFixed(1)}`,
    );
  }

  return `${parts.join("")}Z`;
}

const CSS = `
${fade("bp-c-head", 1)}
${draw("bp-c-sheet-a", 4, 6)}
${fade("bp-c-label-a", 10)}
${draw("bp-c-sheet-b", 9, 6)}
${fade("bp-c-label-b", 15)}
${LANGUAGES.map((_, i) => stamp(`bp-c-lang${i}`, 20 + i * 2.4)).join("\n")}
${draw("bp-c-schedule", 34, 5)}
${CHECKS.map((_, i) => fade(`bp-c-check${i}`, 38 + i * 4)).join("\n")}
${draw("bp-c-cloud", 54, 8)}
${fade("bp-c-strike", 60)}
${stamp("bp-c-stamp", 68)}
`;

interface DrawingProps {
  readonly x: number;
  readonly frameClass: string;
  readonly labelClass: string;
  readonly name: string;
  readonly language: string;
  readonly rev: string;
  readonly field: string;
}

/** One source drawing: an ordinary GraphQL server, drafted as a part sheet. */
function Drawing({
  x,
  frameClass,
  labelClass,
  name,
  language,
  rev,
  field,
}: DrawingProps) {
  return (
    <g>
      <path
        className={frameClass}
        d={`M${x} 32h${DRAWING_W}v136h${-DRAWING_W}Z`}
        pathLength={1}
        fill={BP.plate}
        stroke={BP.ink}
        strokeWidth={1.3}
      />
      <g className={labelClass}>
        <text x={x + 10} y={58} fontSize={FONT.label}>
          {name.toUpperCase()}
        </text>
        <text className="bp-t-dim" x={x + 10} y={80} fontSize={FONT.label}>
          {`${language} · REV ${rev}`}
        </text>
        <text className="bp-t-dim" x={x + 10} y={108} fontSize={FONT.label}>
          type Product
        </text>
        <text className="bp-t-dim" x={x + 10} y={130} fontSize={FONT.label}>
          {'@key(fields: "id")'}
        </text>
        <text x={x + 10} y={158} fontSize={FONT.label}>
          {field}
        </text>
      </g>
    </g>
  );
}

export function ToleranceCheck() {
  const active = useSceneActive();
  const reduced = useReducedMotionPreference();

  return (
    <Sheet
      title="Tolerance check"
      no="DWG-103"
      rev="D"
      run={active && !reduced}
    >
      <svg
        viewBox="0 0 480 318"
        preserveAspectRatio="xMidYMid meet"
        className="absolute inset-0 h-full w-full"
      >
        <style>{CSS}</style>

        <text
          className="bp-c-head bp-t-dim"
          x={14}
          y={22}
          fontSize={FONT.label}
        >
          GRAPHQL SERVERS · NO RUNTIME PACKAGE
        </text>

        <Drawing
          x={12}
          frameClass="bp-c-sheet-a"
          labelClass="bp-c-label-a"
          name="Catalog"
          language="JS/TS"
          rev="4"
          field="price : Float!"
        />
        <Drawing
          x={242}
          frameClass="bp-c-sheet-b"
          labelClass="bp-c-label-b"
          name="Billing"
          language="Java"
          rev="7"
          field="price : Int!"
        />

        {/* The mismatch, red-lined */}
        <path
          className="bp-c-cloud"
          d={cloud(246, 138, 212, 28)}
          pathLength={1}
          fill="none"
          stroke={BP.redline}
          strokeWidth={1.2}
        />
        <g className="bp-c-strike">
          <path
            d="M252 152h150"
            fill="none"
            stroke={BP.redline}
            strokeWidth={1}
          />
          <text className="bp-t-red" x={14} y={190} fontSize={FONT.label}>
            ≠ Float! · FIELD TYPES DISAGREE
          </text>
        </g>

        {/* Language stamps: any server, no plugin */}
        {LANGUAGES.map((language, i) => (
          <g
            key={language}
            className={`bp-c-lang${i}`}
            style={{ transformOrigin: `${52 + i * 94}px 211px` }}
          >
            <path
              d={`M${8 + i * 94} 196h88v30h-88Z`}
              fill="none"
              stroke={BP.ink}
              strokeWidth={0.9}
            />
            <text
              x={52 + i * 94}
              y={217}
              textAnchor="middle"
              fontSize={FONT.label}
            >
              {language}
            </text>
          </g>
        ))}

        {/* The check schedule */}
        <path
          className="bp-c-schedule"
          d="M14 238h230v78H14Z"
          pathLength={1}
          fill="none"
          stroke={BP.inkFaint}
          strokeWidth={0.9}
        />
        {CHECKS.map((check, i) => {
          const y = 252 + i * 19;
          const colour =
            check.state === "ok"
              ? "bp-t-ok"
              : check.state === "fail"
                ? "bp-t-red"
                : "bp-t-dim";
          const mark =
            check.state === "ok" ? "✓" : check.state === "fail" ? "✗" : "—";

          return (
            <g key={check.label} className={`bp-c-check${i}`}>
              <text className={colour} x={26} y={y} fontSize={FONT.label}>
                {mark}
              </text>
              <text
                className={check.state === "skipped" ? "bp-t-dim" : undefined}
                x={52}
                y={y}
                fontSize={FONT.label}
              >
                {check.label}
              </text>
            </g>
          );
        })}

        {/* The drawing set is rejected */}
        <g className="bp-c-stamp" style={{ transformOrigin: "361px 277px" }}>
          <path
            d="M256 244h210v66H256Z"
            fill="none"
            stroke={BP.redline}
            strokeWidth={1.6}
          />
          <text
            className="bp-t-red"
            x={361}
            y={282}
            textAnchor="middle"
            fontSize={FONT.title}
            style={{ letterSpacing: "0.16em" }}
          >
            REJECTED
          </text>
          <text
            className="bp-t-red"
            x={361}
            y={304}
            textAnchor="middle"
            fontSize={FONT.label}
          >
            TYPE CONFLICT
          </text>
        </g>
      </svg>
    </Sheet>
  );
}
