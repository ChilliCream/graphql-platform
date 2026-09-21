"use client";

import { useRef } from "react";

import { TYPE, svgLabelGap, svgLabelSize } from "../tokens";
import { anim, useCycle, useSceneMotion, useSvgLabelScale } from "./hooks";
import { useSceneRatio } from "./Scene";
import { MC, STATIONS } from "../palette";

/**
 * Animated console that runs a composition build across stock GraphQL servers
 * in five languages and aborts the deployment countdown when a type conflict
 * turns a line red.
 */

const W = 640;
const H = 460;
/** The scene box mirrors the viewBox, so the checklist never letterboxes. */
export const PREFLIGHT_CHECKLIST_RATIO = `${W} / ${H}`;

/**
 * Below 1024px the roster and the checklist stack in a single column
 * instead of sitting side by side, so every label keeps its natural glyph
 * width inside a column narrow enough to hold it (see `hero-wave`'s
 * `visuals/Scene.tsx` `useSceneRatio`). `MOBILE_W` matches this panel's
 * measured rendered width at 375px.
 */
const MOBILE_W = 333;
const MOBILE_H = 906;
const MOBILE_RATIO = `${MOBILE_W} / ${MOBILE_H}`;

/** 0 arms the run; the last phase is the rest frame: the aborted build. */
const PHASES = 7;
const REST = PHASES - 1;
const BEAT = 1300;

const ROSTER = { x: 16, y: 44, w: 250 } as const;
const LIST = { x: 292, y: 44, w: 332 } as const;
const ROSTER_M = { x: 16, y: 44, w: MOBILE_W - 32 } as const;
const LIST_M = { x: 16, y: 434, w: MOBILE_W - 32 } as const;
/** The conflict line: after it the countdown holds and the build stops. */
const CONFLICT = 2;

const CHECKS = [
  "SUBGRAPHS READ",
  "KEYS AND LOOKUPS DECLARED",
  "TYPE COMPATIBILITY",
  "ENUM COMPATIBILITY",
  "COHERENT GRAPH SIGNED",
] as const;

const KEYFRAMES = `
@keyframes mc-preflight-alarm { 0%, 100% { opacity: 1; } 50% { opacity: 0.4; } }
@keyframes mc-preflight-scan { 0%, 100% { opacity: 0.25; } 50% { opacity: 0.9; } }
`;

type CheckState = "pending" | "ok" | "failed" | "stopped";

function checkState(index: number, phase: number): CheckState {
  if (phase === 0) return "pending";
  const reached = phase - 1;
  if (index < CONFLICT) return index <= reached ? "ok" : "pending";
  if (index === CONFLICT) return reached >= CONFLICT ? "failed" : "pending";
  return reached >= CONFLICT ? "stopped" : "pending";
}

const STATE_COLOR: Record<CheckState, string> = {
  pending: MC.dim,
  ok: MC.phosphor,
  failed: MC.alert,
  stopped: MC.dim,
};

const STATE_LABEL: Record<CheckState, string> = {
  pending: "· · ·",
  ok: "OK",
  failed: "CONFLICT",
  stopped: "NOT RUN",
};

/**
 * Splits `text` at every ` · ` separator into lines a `<tspan>` stack can
 * wrap onto their own rows, each line keeping its leading separator so the
 * lines' concatenated content is `text` again, byte for byte.
 */
function dotLines(text: string): readonly string[] {
  const parts = text.split(" · ");
  return parts.map((part, i) => (i === 0 ? part : ` · ${part}`));
}

interface StackedTextProps {
  readonly x: number;
  readonly y: number;
  readonly lines: readonly string[];
  readonly lineHeight: number;
  readonly fill: string;
  readonly fontSize: number;
  readonly letterSpacing?: string;
  readonly textAnchor?: "start" | "middle" | "end";
}

/** A `<text>` whose content renders as several stacked, left-aligned lines. */
function StackedText({
  x,
  y,
  lines,
  lineHeight,
  fill,
  fontSize,
  letterSpacing,
  textAnchor,
}: StackedTextProps) {
  return (
    <text
      x={x}
      y={y}
      fill={fill}
      fontFamily={MC.mono}
      fontSize={fontSize}
      letterSpacing={letterSpacing}
      textAnchor={textAnchor}
      style={{ transition: "fill 400ms ease" }}
    >
      {lines.map((line, i) => (
        <tspan key={i} x={x} dy={i === 0 ? 0 : lineHeight}>
          {line}
        </tspan>
      ))}
    </text>
  );
}

export function PreflightChecklist() {
  const running = useSceneMotion();
  const phase = useCycle(running, PHASES, BEAT, REST);
  const aborted = phase - 1 >= CONFLICT;
  const countdown = Math.max(5 - Math.min(phase, CONFLICT + 1), 0);
  const svgRef = useRef<SVGSVGElement>(null);
  const desktopScale = useSvgLabelScale(svgRef, W);
  const mobile = desktopScale < 1;
  const mobileScale = useSvgLabelScale(svgRef, MOBILE_W);
  const scale = mobile ? mobileScale : desktopScale;
  useSceneRatio(mobile ? MOBILE_RATIO : null);
  const label = svgLabelSize(TYPE.label, scale);
  const caption = svgLabelSize(TYPE.caption, scale);
  const h5 = svgLabelSize(TYPE.h5, scale);
  /**
   * The roster row name's size: `caption` everywhere there's room, capped at
   * `label`'s floor inside the five-row roster, whose fixed-height column
   * has no slack for `caption`'s full, proportionally larger boost.
   */
  const rosterNameSize = scale >= 1 ? caption : label;
  /** Gap between a roster row's name and its meta line, below the boost. */
  const rosterGap = scale >= 1 ? 16 : svgLabelGap(16, label, TYPE.label, 1.3);
  const rosterStride = 54 + (rosterGap - 16);
  /** Gap between a check row's label baseline and its divider rule. */
  const checkDividerGap = svgLabelGap(10, label, TYPE.label, 0.6);
  const checkStride = 40 + (checkDividerGap - 10);

  const roster = mobile ? ROSTER_M : ROSTER;
  const list = mobile ? LIST_M : LIST;
  const rosterHeight = 332;
  const listHeight = 332;

  const bannerText = aborted
    ? "COMPOSITION FAILED · PIPELINE STOPPED · NOTHING DEPLOYED"
    : "COMPOSITION RUNNING · IN THE BUILD, NOT AT RUNTIME";
  const conflictCaption = "Product.price: Float (Catalog) vs String (Billing)";
  const conflictLines = [
    "Product.price: Float (Catalog)",
    " vs String (Billing)",
  ];
  const restingCaption = "validating subgraphs against one another";

  const checksStartY = list.y + 74 + (svgLabelGap(0, h5, TYPE.h5) - 0);
  const captionY =
    checksStartY +
    (CHECKS.length - 1) * checkStride +
    checkDividerGap +
    svgLabelGap(48, label, TYPE.label, 0.8);

  const footerY = mobile ? list.y + listHeight + 40 : H - 52;
  const footerH = mobile ? (aborted ? 84 : 60) : 36;
  const footerTextY = mobile ? footerY + (aborted ? 30 : 34) : H - 29;

  return (
    <svg
      ref={svgRef}
      viewBox={`0 0 ${mobile ? MOBILE_W : W} ${mobile ? MOBILE_H : H}`}
      className="h-full w-full"
    >
      <style>{KEYFRAMES}</style>
      <rect
        width={mobile ? MOBILE_W : W}
        height={mobile ? MOBILE_H : H}
        fill={MC.bg}
      />

      <text
        x={roster.x}
        y={26}
        fill={MC.dim}
        fontFamily={MC.mono}
        fontSize={label}
        letterSpacing="0.2em"
      >
        STATION ROSTER
      </text>
      <rect
        x={roster.x}
        y={roster.y}
        width={roster.w}
        height={rosterHeight}
        rx="9"
        fill={MC.panel}
        stroke={MC.panelEdge}
      />
      <text
        x={roster.x + 10}
        y={roster.y + 24}
        fill={MC.dim}
        fontFamily={MC.mono}
        fontSize={label}
      >
        SUBGRAPH · LANGUAGE
      </text>
      <text
        x={roster.x + roster.w - 10}
        y={roster.y + 24}
        fill={MC.dim}
        fontFamily={MC.mono}
        fontSize={label}
        textAnchor="end"
      >
        RUNTIME PLUGIN
      </text>
      {STATIONS.map((station, i) => {
        const y = roster.y + 56 + i * rosterStride;
        const nameText = station.name.toUpperCase();
        const metaText = `${station.language} · STOCK GRAPHQL SERVER`;
        return (
          <g key={station.name}>
            <text
              x={roster.x + 14}
              y={y}
              fill={MC.ink}
              fontFamily={MC.mono}
              fontSize={rosterNameSize}
              letterSpacing="0.12em"
            >
              {nameText}
            </text>
            <text
              x={roster.x + 14}
              y={y + rosterGap}
              fill={MC.dim}
              fontFamily={MC.mono}
              fontSize={label}
              letterSpacing="0.06em"
            >
              {metaText}
            </text>
            <text
              x={roster.x + roster.w - 14}
              y={y}
              fill={MC.phosphor}
              fontFamily={MC.mono}
              fontSize={label}
              letterSpacing="0.14em"
              textAnchor="end"
            >
              NONE
            </text>
          </g>
        );
      })}

      <text
        x={list.x}
        y={mobile ? roster.y + rosterHeight + 22 : 26}
        fill={MC.dim}
        fontFamily={MC.mono}
        fontSize={label}
        letterSpacing="0.2em"
      >
        COMPOSITION PRE-FLIGHT
      </text>
      <rect
        x={list.x}
        y={list.y}
        width={list.w}
        height={listHeight}
        rx="9"
        fill={MC.panel}
        stroke={aborted ? MC.alert : MC.panelEdge}
        strokeOpacity={aborted ? 0.7 : 1}
        style={{ transition: "stroke 400ms ease" }}
      />
      <text
        x={list.x + 16}
        y={list.y + 32}
        fill={aborted ? MC.alert : MC.phosphor}
        fontFamily={MC.mono}
        fontSize={h5}
        letterSpacing="0.14em"
        style={{
          transition: "fill 400ms ease",
          animation: anim(
            running && aborted,
            "mc-preflight-alarm 1100ms steps(1, end) infinite",
          ),
        }}
      >
        {`T-0${countdown}`}
      </text>
      <text
        x={list.x + list.w - 16}
        y={list.y + 32}
        fill={aborted ? MC.alert : MC.dim}
        fontFamily={MC.mono}
        fontSize={label}
        letterSpacing="0.16em"
        textAnchor="end"
        style={{ transition: "fill 400ms ease" }}
      >
        {aborted ? "COUNTDOWN HELD" : "BUILD STEP RUNNING"}
      </text>

      {CHECKS.map((check, i) => {
        const state = checkState(i, phase);
        const y = checksStartY + i * checkStride;
        return (
          <g key={check}>
            <text
              x={list.x + 16}
              y={y}
              fill={state === "pending" ? MC.dim : MC.ink}
              fontFamily={MC.mono}
              fontSize={label}
              letterSpacing="0.1em"
              style={{ transition: "fill 400ms ease" }}
            >
              {check}
            </text>
            <text
              x={list.x + list.w - 16}
              y={y}
              fill={STATE_COLOR[state]}
              fontFamily={MC.mono}
              fontSize={label}
              letterSpacing="0.14em"
              textAnchor="end"
              style={{ transition: "fill 400ms ease" }}
            >
              {STATE_LABEL[state]}
            </text>
            <path
              d={`M${list.x + 16} ${y + checkDividerGap}H${list.x + list.w - 16}`}
              stroke={state === "failed" ? MC.alert : MC.panelEdge}
              strokeOpacity={state === "failed" ? 0.6 : 0.7}
              style={{
                transition: "stroke 400ms ease",
                animation: anim(
                  running && state === "pending",
                  `mc-preflight-scan 2000ms ease-in-out ${i * 160}ms infinite`,
                ),
              }}
            />
          </g>
        );
      })}

      {aborted && mobile ? (
        <StackedText
          x={list.x + list.w / 2}
          y={captionY}
          lines={conflictLines}
          lineHeight={16}
          fill={MC.alert}
          fontSize={label}
          textAnchor="middle"
        />
      ) : (
        <text
          x={list.x + list.w / 2}
          y={captionY}
          fill={aborted ? MC.alert : MC.dim}
          fontFamily={MC.mono}
          fontSize={label}
          textAnchor="middle"
          style={{ transition: "fill 400ms ease" }}
        >
          {aborted ? conflictCaption : restingCaption}
        </text>
      )}

      <rect
        x={mobile ? list.x : 16}
        y={footerY}
        width={mobile ? list.w : W - 32}
        height={footerH}
        rx="8"
        fill={MC.panel}
        stroke={aborted ? MC.alert : MC.phosphor}
        strokeOpacity="0.55"
        style={{ transition: "stroke 400ms ease" }}
      />
      {mobile ? (
        <StackedText
          x={(mobile ? list.x : 16) + (mobile ? list.w : W - 32) / 2}
          y={footerTextY}
          lines={dotLines(bannerText)}
          lineHeight={18}
          fill={aborted ? MC.alert : MC.phosphor}
          fontSize={label}
          letterSpacing="0.2em"
          textAnchor="middle"
        />
      ) : (
        <text
          x={W / 2}
          y={footerTextY}
          fill={aborted ? MC.alert : MC.phosphor}
          fontFamily={MC.mono}
          fontSize={label}
          letterSpacing="0.2em"
          textAnchor="middle"
          style={{ transition: "fill 400ms ease" }}
        >
          {bannerText}
        </text>
      )}
    </svg>
  );
}
