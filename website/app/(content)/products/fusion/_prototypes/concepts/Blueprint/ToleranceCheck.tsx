"use client";

import { useReducedMotionPreference, useSceneActive } from "../../Primitives";
import { BP, PARTS } from "./palette";
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
  { label: "KEYS DECLARED IN THE SOURCE SCHEMA", state: "ok" },
  { label: "LOOKUPS RESOLVE ACROSS DRAWINGS", state: "ok" },
  { label: "FIELD TYPES WITHIN TOLERANCE", state: "fail" },
  { label: "ENUM VALUES COMPATIBLE", state: "skipped" },
] as const;

const LANGUAGES = PARTS.map((part) => part.language);

/** A red-line revision cloud around the mismatched dimension. */
function cloud(x: number, y: number, w: number, h: number, r = 7): string {
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
${fade("bp-c-lang-note", 32)}
${draw("bp-c-schedule", 34, 5)}
${CHECKS.map((_, i) => fade(`bp-c-check${i}`, 38 + i * 4)).join("\n")}
${draw("bp-c-cloud", 54, 8)}
${fade("bp-c-strike", 60)}
${stamp("bp-c-stamp", 68)}
${fade("bp-c-note", 76)}
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
        d={`M${x} 26h208v124h-208Z`}
        pathLength={1}
        fill={BP.plate}
        stroke={BP.ink}
        strokeWidth={1.3}
      />
      <g className={labelClass}>
        <text x={x + 12} y={44} fontSize={9}>
          {name.toUpperCase()}
        </text>
        <text
          className="bp-t-dim"
          x={x + 196}
          y={44}
          textAnchor="end"
          fontSize={6}
        >
          {`${language} · REV ${rev}`}
        </text>
        <text className="bp-t-dim" x={x + 12} y={68} fontSize={7}>
          type Product
        </text>
        <text className="bp-t-dim" x={x + 12} y={82} fontSize={7}>
          {'@key(fields: "id")'}
        </text>
        <text x={x + 12} y={112} fontSize={9}>
          {field}
        </text>
        <text className="bp-t-dim" x={x + 12} y={136} fontSize={6}>
          ORDINARY GRAPHQL SERVER · NO RUNTIME PACKAGE
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
        viewBox="0 0 480 360"
        preserveAspectRatio="xMidYMid meet"
        className="absolute inset-0 h-full w-full"
      >
        <style>{CSS}</style>

        <text className="bp-c-head bp-t-dim" x={14} y={16} fontSize={6.5}>
          TOLERANCE CHECK ACROSS TWO SOURCE DRAWINGS
        </text>

        <Drawing
          x={14}
          frameClass="bp-c-sheet-a"
          labelClass="bp-c-label-a"
          name="Catalog"
          language="JS/TS"
          rev="4"
          field="price : Float!"
        />
        <Drawing
          x={258}
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
          d={cloud(262, 96, 106, 24)}
          pathLength={1}
          fill="none"
          stroke={BP.redline}
          strokeWidth={1.2}
        />
        <g className="bp-c-strike">
          <path
            d="M270 112h96"
            fill="none"
            stroke={BP.redline}
            strokeWidth={1}
          />
          <text className="bp-t-red" x={380} y={112} fontSize={6.5}>
            ≠ Float!
          </text>
        </g>

        {/* Language stamps: any server, no plugin */}
        {LANGUAGES.map((language, i) => (
          <g
            key={language}
            className={`bp-c-lang${i}`}
            style={{ transformOrigin: `${58 + i * 92}px 176px` }}
          >
            <path
              d={`M${14 + i * 92} 164h88v24h-88Z`}
              fill="none"
              stroke={BP.ink}
              strokeWidth={0.9}
            />
            <text x={58 + i * 92} y={179} textAnchor="middle" fontSize={7}>
              {language}
            </text>
          </g>
        ))}
        <text className="bp-c-lang-note bp-t-dim" x={14} y={202} fontSize={6.2}>
          NO DISTRIBUTED-RUNTIME PACKAGE · NO VENDOR PROTOCOL LAYER
        </text>

        {/* The check schedule */}
        <path
          className="bp-c-schedule"
          d="M14 212h292v112h-292Z"
          pathLength={1}
          fill="none"
          stroke={BP.inkFaint}
          strokeWidth={0.9}
        />
        {CHECKS.map((check, i) => {
          const y = 234 + i * 26;
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
              <text className={colour} x={26} y={y} fontSize={9}>
                {mark}
              </text>
              <text
                className={check.state === "skipped" ? "bp-t-dim" : undefined}
                x={44}
                y={y}
                fontSize={6.6}
              >
                {check.label}
              </text>
              {check.state === "skipped" ? (
                <text
                  className="bp-t-dim"
                  x={296}
                  y={y}
                  textAnchor="end"
                  fontSize={6}
                >
                  NOT REACHED
                </text>
              ) : null}
            </g>
          );
        })}

        {/* The drawing set is rejected */}
        <g className="bp-c-stamp" style={{ transformOrigin: "392px 258px" }}>
          <path
            d="M322 228h140v60h-140Z"
            fill="none"
            stroke={BP.redline}
            strokeWidth={1.6}
          />
          <text
            className="bp-t-red"
            x={392}
            y={254}
            textAnchor="middle"
            fontSize={13}
            style={{ letterSpacing: "0.16em" }}
          >
            REJECTED
          </text>
          <text
            className="bp-t-red"
            x={392}
            y={272}
            textAnchor="middle"
            fontSize={6}
          >
            REV D · TYPE CONFLICT
          </text>
        </g>
        <text className="bp-c-note bp-t-dim" x={322} y={306} fontSize={6.2}>
          BUILD STOPPED
        </text>
        <text className="bp-c-note bp-t-dim" x={322} y={318} fontSize={6.2}>
          GATEWAY UNCHANGED
        </text>
      </svg>
    </Sheet>
  );
}
