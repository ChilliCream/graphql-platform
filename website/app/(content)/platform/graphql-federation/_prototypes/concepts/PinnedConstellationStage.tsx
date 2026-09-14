"use client";

import { useEffect, useRef, useState } from "react";

import { CANON, GatewayChip, GlowNode, INK_DIM } from "../Primitives";
import { CHAPTERS, ProtoCodeBox } from "../story";

const MONO = "ui-monospace, SFMono-Regular, Menlo, monospace";
const DASHED = "rgba(245,241,234,0.3)";
const CENTER = { x: 190, y: 170 } as const;

type Anchor = "start" | "middle" | "end";

interface Mark {
  readonly color: string;
  readonly name: string;
  readonly x: number;
  readonly y: number;
  readonly labelX: number;
  readonly labelY: number;
  readonly anchor: Anchor;
}

/** Five fixed pentagon anchors. These never move across any of the 7 beats. */
const MARKS: readonly Mark[] = [
  {
    ...CANON[0],
    x: 70,
    y: 80,
    labelX: 90,
    labelY: 68,
    anchor: "start",
  },
  {
    ...CANON[1],
    x: 310,
    y: 80,
    labelX: 290,
    labelY: 68,
    anchor: "end",
  },
  {
    ...CANON[2],
    x: 330,
    y: 230,
    labelX: 310,
    labelY: 254,
    anchor: "end",
  },
  {
    ...CANON[3],
    x: 190,
    y: 325,
    labelX: 190,
    labelY: 349,
    anchor: "middle",
  },
  {
    ...CANON[4],
    x: 50,
    y: 230,
    labelX: 70,
    labelY: 254,
    anchor: "start",
  },
];

/** Deliberately mismatched bend points so the beat-2 elbows cross each other. */
const TANGLE_BENDS: readonly { readonly x: number; readonly y: number }[] = [
  { x: 250, y: 60 },
  { x: 130, y: 55 },
  { x: 108, y: 205 },
  { x: 285, y: 300 },
  { x: 268, y: 138 },
];

/** Fragment dock points: each schema fragment sits ~14px from its own mark. */
const DOCKS: readonly { readonly x: number; readonly y: number }[] = [
  { x: 84, y: 92 },
  { x: 296, y: 92 },
  { x: 316, y: 222 },
  { x: 190, y: 309 },
  { x: 64, y: 222 },
];

const COMPOSITE_FIELDS = [
  { label: "name", color: CANON[0].color },
  { label: "price", color: CANON[1].color },
  { label: "orders", color: CANON[2].color },
  { label: "delivery", color: CANON[3].color },
  { label: "account", color: CANON[4].color },
] as const;

const DOC_BOX = { x: 158, y: 128, w: 64, h: 84 } as const;
const COMPOSITE_BOX = { x: 154, y: 122, w: 72, h: 96 } as const;

function MarkGlyphs() {
  return (
    <g>
      {MARKS.map((m) => (
        <g key={m.name}>
          <rect
            x={m.x - 8}
            y={m.y - 8}
            width={16}
            height={16}
            rx={4}
            fill={m.color}
          />
          <text
            x={m.labelX}
            y={m.labelY}
            textAnchor={m.anchor}
            fontFamily={MONO}
            fontSize={10}
            letterSpacing="0.16em"
            fill={INK_DIM}
          >
            {m.name.toUpperCase()}
          </text>
        </g>
      ))}
    </g>
  );
}

function Beat1() {
  return (
    <g>
      {MARKS.map((m) => (
        <g key={m.name}>
          <line
            x1={CENTER.x}
            y1={CENTER.y}
            x2={m.x}
            y2={m.y}
            stroke={DASHED}
            strokeWidth={1}
            strokeDasharray="3 4"
          />
          <text
            x={(CENTER.x + m.x) / 2}
            y={(CENTER.y + m.y) / 2 - 4}
            textAnchor="middle"
            fontFamily={MONO}
            fontSize={10}
            fill={INK_DIM}
          >
            ?
          </text>
        </g>
      ))}
      <GatewayChip x={CENTER.x} y={CENTER.y} label="CLIENT" />
    </g>
  );
}

function Beat2() {
  return (
    <g>
      {MARKS.map((m, i) => {
        const bend = TANGLE_BENDS[i];
        return (
          <polyline
            key={m.name}
            points={`${CENTER.x},${CENTER.y} ${bend.x},${bend.y} ${m.x},${m.y}`}
            fill="none"
            stroke={m.color}
            strokeWidth={1.5}
            strokeLinecap="round"
            strokeLinejoin="round"
          />
        );
      })}
      <GatewayChip x={CENTER.x} y={CENTER.y} label="CLIENT" />
    </g>
  );
}

const TEAM_BOX = { x: 130, y: 102, w: 120, h: 36 } as const;
const TEAM_EDGE = { x: TEAM_BOX.x, y: TEAM_BOX.y + TEAM_BOX.h / 2 } as const;

function Beat3() {
  return (
    <g>
      {MARKS.map((m) => (
        <line
          key={m.name}
          x1={m.x}
          y1={m.y}
          x2={TEAM_EDGE.x}
          y2={TEAM_EDGE.y}
          stroke={m.color}
          strokeWidth={1.5}
          strokeOpacity={0.85}
        />
      ))}
      {[0, 1, 2, 3].map((i) => (
        <circle
          key={i}
          cx={TEAM_EDGE.x - 12}
          cy={TEAM_EDGE.y - 15 + i * 10}
          r={3}
          fill="none"
          stroke="rgba(245,241,234,0.5)"
        />
      ))}
      <rect
        x={TEAM_BOX.x}
        y={TEAM_BOX.y}
        width={TEAM_BOX.w}
        height={TEAM_BOX.h}
        rx={6}
        fill="#232a38"
        stroke="rgba(245,241,234,0.35)"
      />
      <text
        x={TEAM_BOX.x + TEAM_BOX.w / 2}
        y={TEAM_BOX.y + TEAM_BOX.h / 2 + 3.5}
        textAnchor="middle"
        fontFamily={MONO}
        fontSize={9.5}
        letterSpacing="0.16em"
        fill={INK_DIM}
      >
        ONE API TEAM
      </text>
    </g>
  );
}

function Beat4() {
  const doc = DOC_BOX;
  const docTop = { x: doc.x + doc.w / 2, y: doc.y };
  return (
    <g>
      {MARKS.map((m) => (
        <line
          key={m.name}
          x1={docTop.x}
          y1={doc.y + doc.h / 2}
          x2={m.x}
          y2={m.y}
          stroke={m.color}
          strokeWidth={1}
          strokeOpacity={0.15}
        />
      ))}
      <rect
        x={doc.x}
        y={doc.y}
        width={doc.w}
        height={doc.h}
        rx={6}
        fill="#0d1424"
        stroke="rgba(255,255,255,0.3)"
      />
      {[0, 1, 2, 3, 4].map((i) => (
        <rect
          key={i}
          x={doc.x + 10}
          y={doc.y + 16 + i * 14}
          width={doc.w - 20}
          height={3}
          rx={1.5}
          fill="rgba(201,212,232,0.4)"
        />
      ))}
      <line
        x1={docTop.x}
        y1={44}
        x2={docTop.x}
        y2={doc.y}
        stroke="#5eead4"
        strokeWidth={2}
      />
      <g transform={`translate(${docTop.x}, 32)`}>
        <rect x={-35} y={-13} width={70} height={26} rx={8} fill="#0d1424" />
        <rect
          x={-35}
          y={-13}
          width={70}
          height={26}
          rx={8}
          fill="none"
          stroke="rgba(94,234,212,0.45)"
        />
        <text
          x={0}
          y={3.5}
          textAnchor="middle"
          fontFamily={MONO}
          fontSize={9.5}
          letterSpacing="0.16em"
          fill="#5eead4"
        >
          CLIENT
        </text>
      </g>
    </g>
  );
}

function Beat5() {
  return (
    <g>
      <rect
        x={DOC_BOX.x}
        y={DOC_BOX.y}
        width={DOC_BOX.w}
        height={DOC_BOX.h}
        rx={6}
        fill="none"
        stroke="rgba(255,255,255,0.3)"
        strokeDasharray="4 5"
      />
      {MARKS.map((m, i) => {
        const d = DOCKS[i];
        return (
          <rect
            key={m.name}
            x={d.x - 22}
            y={d.y - 17}
            width={44}
            height={34}
            rx={5}
            fill="rgba(12,19,34,0.6)"
            stroke={m.color}
            strokeWidth={1.5}
          />
        );
      })}
    </g>
  );
}

function Beat6() {
  return (
    <g>
      {MARKS.map((m, i) => {
        const d = DOCKS[i];
        return (
          <g key={m.name}>
            <rect
              x={d.x - 22}
              y={d.y - 17}
              width={44}
              height={34}
              rx={5}
              fill="rgba(12,19,34,0.6)"
              stroke={m.color}
              strokeWidth={1.5}
            />
            <line
              x1={d.x}
              y1={d.y}
              x2={CENTER.x}
              y2={CENTER.y}
              stroke={m.color}
              strokeWidth={1}
              strokeDasharray="2 4"
              strokeOpacity={0.7}
            />
            <rect
              x={(d.x + CENTER.x) / 2 - 1.5}
              y={(d.y + CENTER.y) / 2 - 1.5}
              width={3}
              height={3}
              fill={m.color}
            />
          </g>
        );
      })}
      <GlowNode x={CENTER.x} y={CENTER.y} id="pcs-composition" r={5} />
      <rect
        x={COMPOSITE_BOX.x}
        y={COMPOSITE_BOX.y}
        width={COMPOSITE_BOX.w}
        height={COMPOSITE_BOX.h}
        rx={6}
        fill="#0d1424"
        stroke="rgba(255,255,255,0.4)"
      />
      {COMPOSITE_FIELDS.map((f, i) => (
        <g key={f.label}>
          <circle
            cx={COMPOSITE_BOX.x + 12}
            cy={COMPOSITE_BOX.y + 16 + i * 15}
            r={3}
            fill={f.color}
          />
          <text
            x={COMPOSITE_BOX.x + 20}
            y={COMPOSITE_BOX.y + 19 + i * 15}
            fontFamily={MONO}
            fontSize={8.5}
            fill="#c9d4e8"
          >
            {f.label}
          </text>
        </g>
      ))}
      <text
        x={COMPOSITE_BOX.x + COMPOSITE_BOX.w / 2}
        y={COMPOSITE_BOX.y + COMPOSITE_BOX.h + 20}
        textAnchor="middle"
        fontFamily={MONO}
        fontSize={9}
        letterSpacing="0.18em"
        fill={INK_DIM}
      >
        SCHEMA COMPOSITION
      </text>
      <line
        x1={COMPOSITE_BOX.x + COMPOSITE_BOX.w / 2 - 40}
        x2={COMPOSITE_BOX.x + COMPOSITE_BOX.w / 2 - 8}
        y1={COMPOSITE_BOX.y + COMPOSITE_BOX.h + 16}
        y2={COMPOSITE_BOX.y + COMPOSITE_BOX.h + 16}
        stroke={DASHED}
        strokeDasharray="3 3"
      />
    </g>
  );
}

const HORIZON_Y = 270;
const GATEWAY = { x: 190, y: 295 } as const;

function Beat7() {
  return (
    <g>
      <rect
        x={COMPOSITE_BOX.x}
        y={COMPOSITE_BOX.y}
        width={COMPOSITE_BOX.w}
        height={COMPOSITE_BOX.h}
        rx={6}
        fill="#0d1424"
        stroke="rgba(255,255,255,0.4)"
      />
      {COMPOSITE_FIELDS.map((f, i) => (
        <circle
          key={f.label}
          cx={COMPOSITE_BOX.x + 12}
          cy={COMPOSITE_BOX.y + 16 + i * 15}
          r={3}
          fill={f.color}
        />
      ))}

      <line
        x1={10}
        y1={HORIZON_Y}
        x2={370}
        y2={HORIZON_Y}
        stroke="rgba(245,241,234,0.22)"
        strokeDasharray="5 7"
      />
      <text
        x={16}
        y={HORIZON_Y - 10}
        fontFamily={MONO}
        fontSize={9}
        letterSpacing="0.18em"
        fill={INK_DIM}
      >
        BUILD TIME
      </text>
      <text
        x={16}
        y={HORIZON_Y + 20}
        fontFamily={MONO}
        fontSize={9}
        letterSpacing="0.18em"
        fill={INK_DIM}
        opacity={0.7}
      >
        RUNTIME
      </text>

      <line
        x1={COMPOSITE_BOX.x + COMPOSITE_BOX.w / 2}
        y1={COMPOSITE_BOX.y + COMPOSITE_BOX.h}
        x2={GATEWAY.x}
        y2={GATEWAY.y - 13}
        stroke="#5eead4"
        strokeWidth={2}
      />
      <GatewayChip x={GATEWAY.x} y={GATEWAY.y} />

      <defs>
        {MARKS.map((m, i) => (
          <marker
            key={m.name}
            id={`pcs-arrow-${i}`}
            markerWidth={4}
            markerHeight={4}
            refX={2}
            refY={2}
            orient="auto"
          >
            <path d="M0,0 L4,2 L0,4 Z" fill={m.color} />
          </marker>
        ))}
      </defs>
      {MARKS.map((m, i) => {
        const qx = (GATEWAY.x + m.x) / 2;
        const qy = (GATEWAY.y + m.y) / 2 - 36;
        return (
          <path
            key={m.name}
            d={`M${GATEWAY.x} ${GATEWAY.y} Q ${qx} ${qy}, ${m.x} ${m.y}`}
            fill="none"
            stroke={m.color}
            strokeWidth={1}
            strokeDasharray="3 4"
            markerEnd={`url(#pcs-arrow-${i})`}
          />
        );
      })}
    </g>
  );
}

const BEATS = [Beat1, Beat2, Beat3, Beat4, Beat5, Beat6, Beat7];

function Stage({ beat }: { readonly beat: number }) {
  return (
    <svg
      viewBox="0 0 380 380"
      width={380}
      height={380}
      aria-hidden="true"
      className="mx-auto"
    >
      <MarkGlyphs />
      {BEATS.map((Beat, i) => {
        const n = i + 1;
        return (
          <g
            key={n}
            className={`transition-opacity duration-300 motion-reduce:transition-none ${
              beat === n ? "opacity-100" : "pointer-events-none opacity-0"
            }`}
          >
            <Beat />
          </g>
        );
      })}
    </svg>
  );
}

/**
 * v5 "Pinned Constellation Stage": the five service marks sit at fixed
 * pentagon anchors for the whole story and never move. Only the content
 * between them (a client chip, a tangle of lines, a team box, a document, its
 * schema fragments, and finally a gateway) appears, travels and recomposes,
 * so the merge can never be misread as a shrinking service count.
 */
export function PinnedConstellationStage() {
  const [beat, setBeat] = useState(1);
  const chapterRefs = useRef<(HTMLDivElement | null)[]>([]);

  useEffect(() => {
    if (typeof IntersectionObserver === "undefined") {
      return;
    }
    const observer = new IntersectionObserver(
      (entries) => {
        for (const entry of entries) {
          if (!entry.isIntersecting) {
            continue;
          }
          const index = chapterRefs.current.indexOf(
            entry.target as HTMLDivElement,
          );
          if (index !== -1) {
            setBeat(index + 1);
          }
        }
      },
      { rootMargin: "-45% 0px -45% 0px", threshold: 0 },
    );

    for (const node of chapterRefs.current) {
      if (node) {
        observer.observe(node);
      }
    }

    return () => observer.disconnect();
  }, []);

  return (
    <div
      data-beat={beat}
      className="grid gap-10 sm:grid-cols-[minmax(0,26rem)_1fr] sm:items-start sm:gap-16"
    >
      <div className="flex flex-col gap-16 sm:gap-0">
        {CHAPTERS.map((chapter, i) => (
          <div
            key={chapter.title}
            ref={(node) => {
              chapterRefs.current[i] = node;
            }}
            className="flex min-h-[70vh] flex-col justify-center gap-6 py-8"
          >
            <h3 className="font-heading text-cc-heading text-h4 sm:text-h3 text-balance">
              {chapter.title}
            </h3>
            <div className="text-cc-ink space-y-3 text-sm sm:text-base">
              {chapter.body}
            </div>
            {chapter.boxes.length > 0 && (
              <div
                className={
                  chapter.boxes.length > 1
                    ? "grid gap-4 sm:grid-cols-2"
                    : "grid gap-4"
                }
              >
                {chapter.boxes.map((box) => (
                  <ProtoCodeBox key={box.label} {...box} />
                ))}
              </div>
            )}
          </div>
        ))}
      </div>

      <div className="hidden sm:sticky sm:top-[calc(50vh-190px)] sm:block">
        <Stage beat={beat} />
      </div>
    </div>
  );
}
