"use client";

import React from "react";
import styled from "styled-components";

// Ambient archive-book background for the cinematic /customers variant.
// Renders a hand-placed scatter of postage-stamp shapes, each carrying a
// circular "verified" cancellation postmark, occasional date stamps, and
// a few diagonal "ANONYMOUS / CLEARED" wax-seal overprints. The whole
// composition reads as an archive of dated, sealed customer references.
//
// The SVG sits at `position: absolute; inset: 0; z-index: 0;` behind the
// page content. It is purely decorative and aria-hidden.

export interface StampArchiveProps {
  className?: string;
}

const Outer = styled.div`
  position: absolute;
  inset: 0;
  z-index: 0;
  pointer-events: none;
  overflow: hidden;

  & > svg {
    display: block;
    width: 100%;
    height: 100%;
  }
`;

// Stamp definition. Coordinates are top-left in the 1440x4000 viewBox.
// Sizes range from small (60x80) to large (120x160). Rotation is in
// degrees. `seal` adds a diagonal wax-seal overprint. `dateStamp` adds
// the small "DATE: ..." dash strip.
interface StampDef {
  x: number;
  y: number;
  w: number;
  h: number;
  rot: number;
  seal?: "ANONYMOUS" | "CLEARED";
  dateStamp?: string;
  postmark?: string;
}

// Composition: heavier scatter in upper-left, sparser bottom-right. The
// y-axis spans 0..4000, x spans 0..1440. Hand-placed; do not auto-tile.
const STAMPS: StampDef[] = [
  // Upper-left dense cluster
  {
    x: 60,
    y: 80,
    w: 110,
    h: 150,
    rot: -6,
    postmark: "VERIFIED  2026  CHILLICREAM",
    seal: "ANONYMOUS",
  },
  {
    x: 220,
    y: 220,
    w: 80,
    h: 100,
    rot: 4,
    postmark: "REFERENCE  ON FILE  CC",
    dateStamp: "MAY 09 2026",
  },
  {
    x: 110,
    y: 360,
    w: 95,
    h: 125,
    rot: -3,
    postmark: "VERIFIED  2026  CHILLICREAM",
  },
  { x: 320, y: 60, w: 70, h: 90, rot: 7, postmark: "CLEARED  2026" },
  {
    x: 410,
    y: 280,
    w: 105,
    h: 140,
    rot: -5,
    postmark: "VERIFIED  2026  CHILLICREAM",
    seal: "CLEARED",
  },

  // Upper-right
  {
    x: 1180,
    y: 140,
    w: 90,
    h: 120,
    rot: 5,
    postmark: "ON FILE  2026",
    dateStamp: "MAY 09 2026",
  },
  {
    x: 980,
    y: 320,
    w: 75,
    h: 100,
    rot: -4,
    postmark: "VERIFIED  2026  CHILLICREAM",
  },
  {
    x: 1240,
    y: 460,
    w: 100,
    h: 130,
    rot: 3,
    postmark: "REFERENCE  ON FILE  CC",
  },

  // Mid band
  {
    x: 240,
    y: 720,
    w: 120,
    h: 160,
    rot: -7,
    postmark: "VERIFIED  2026  CHILLICREAM",
    seal: "ANONYMOUS",
  },
  {
    x: 720,
    y: 640,
    w: 85,
    h: 110,
    rot: 6,
    postmark: "CLEARED  2026",
    dateStamp: "MAY 09 2026",
  },
  { x: 1080, y: 820, w: 95, h: 125, rot: -2, postmark: "ON FILE  2026" },

  // Lower mid
  {
    x: 140,
    y: 1180,
    w: 100,
    h: 130,
    rot: 4,
    postmark: "VERIFIED  2026  CHILLICREAM",
  },
  { x: 620, y: 1080, w: 70, h: 90, rot: -8, postmark: "CLEARED  2026" },
  {
    x: 1180,
    y: 1320,
    w: 110,
    h: 145,
    rot: 2,
    postmark: "REFERENCE  ON FILE  CC",
    seal: "ANONYMOUS",
  },

  // Mid-lower
  {
    x: 380,
    y: 1560,
    w: 90,
    h: 115,
    rot: 5,
    postmark: "VERIFIED  2026  CHILLICREAM",
    dateStamp: "MAY 09 2026",
  },
  { x: 880, y: 1700, w: 85, h: 110, rot: -4, postmark: "ON FILE  2026" },

  // Lower section, sparser
  {
    x: 200,
    y: 2080,
    w: 100,
    h: 135,
    rot: -3,
    postmark: "VERIFIED  2026  CHILLICREAM",
  },
  { x: 980, y: 2200, w: 75, h: 95, rot: 6, postmark: "CLEARED  2026" },
  {
    x: 540,
    y: 2440,
    w: 90,
    h: 120,
    rot: -5,
    postmark: "REFERENCE  ON FILE  CC",
    seal: "CLEARED",
  },

  // Bottom band, sparsest
  {
    x: 1100,
    y: 2720,
    w: 80,
    h: 105,
    rot: 4,
    postmark: "ON FILE  2026",
    dateStamp: "MAY 09 2026",
  },
  {
    x: 280,
    y: 2980,
    w: 95,
    h: 125,
    rot: -6,
    postmark: "VERIFIED  2026  CHILLICREAM",
  },
  { x: 820, y: 3220, w: 70, h: 90, rot: 3, postmark: "CLEARED  2026" },
  {
    x: 380,
    y: 3520,
    w: 85,
    h: 115,
    rot: -2,
    postmark: "REFERENCE  ON FILE  CC",
  },
  {
    x: 1180,
    y: 3680,
    w: 90,
    h: 120,
    rot: 5,
    postmark: "VERIFIED  2026  CHILLICREAM",
    seal: "ANONYMOUS",
  },
];

const STROKE = "rgba(245, 241, 234, 0.10)";
const POSTMARK = "rgba(220, 200, 160, 0.12)";

// Renders a single stamp at the origin (0,0) sized w x h. Caller wraps
// in a `<g transform>` to position and rotate.
const Stamp: React.FC<{
  w: number;
  h: number;
  postmark?: string;
  dateStamp?: string;
  seal?: "ANONYMOUS" | "CLEARED";
  pathId: string;
}> = ({ w, h, postmark, dateStamp, seal, pathId }) => {
  // Perforation: small circles around the perimeter, ~10px spacing.
  const perfSpacing = 10;
  const perfRadius = 1.6;
  const perfs: React.ReactElement[] = [];
  let key = 0;
  // Top + bottom edges
  for (let x = perfSpacing / 2; x <= w - perfSpacing / 2; x += perfSpacing) {
    perfs.push(
      <circle
        key={`t${key++}`}
        cx={x}
        cy={0}
        r={perfRadius}
        fill="none"
        stroke={STROKE}
        strokeWidth={1}
      />
    );
    perfs.push(
      <circle
        key={`b${key++}`}
        cx={x}
        cy={h}
        r={perfRadius}
        fill="none"
        stroke={STROKE}
        strokeWidth={1}
      />
    );
  }
  // Left + right edges
  for (let y = perfSpacing / 2; y <= h - perfSpacing / 2; y += perfSpacing) {
    perfs.push(
      <circle
        key={`l${key++}`}
        cx={0}
        cy={y}
        r={perfRadius}
        fill="none"
        stroke={STROKE}
        strokeWidth={1}
      />
    );
    perfs.push(
      <circle
        key={`r${key++}`}
        cx={w}
        cy={y}
        r={perfRadius}
        fill="none"
        stroke={STROKE}
        strokeWidth={1}
      />
    );
  }

  // Inner stamp body inset from the perforated outline.
  const inset = 6;
  const innerW = w - inset * 2;
  const innerH = h - inset * 2;

  // Postmark: concentric circles centered in the stamp body. Radius
  // sized to the smaller axis so the rings always fit.
  const cx = w / 2;
  const cy = h / 2;
  const rOuter = Math.min(innerW, innerH) * 0.36;
  const rMid = rOuter * 0.78;
  const rInner = rOuter * 0.6;

  return (
    <g>
      {/* Stamp outline */}
      <rect
        x={0}
        y={0}
        width={w}
        height={h}
        fill="none"
        stroke={STROKE}
        strokeWidth={1}
      />
      {/* Inner border to evoke the printed inner frame */}
      <rect
        x={inset}
        y={inset}
        width={innerW}
        height={innerH}
        fill="none"
        stroke={STROKE}
        strokeWidth={1}
      />
      {/* Perforations */}
      {perfs}

      {/* Postmark: concentric rings */}
      <circle
        cx={cx}
        cy={cy}
        r={rOuter}
        fill="none"
        stroke={POSTMARK}
        strokeWidth={1}
      />
      <circle
        cx={cx}
        cy={cy}
        r={rMid}
        fill="none"
        stroke={POSTMARK}
        strokeWidth={0.75}
      />
      <circle
        cx={cx}
        cy={cy}
        r={rInner}
        fill="none"
        stroke={POSTMARK}
        strokeWidth={0.75}
      />

      {/* Postmark text along the inner ring */}
      {postmark ? (
        <>
          <defs>
            <path
              id={pathId}
              d={`M ${cx - rMid} ${cy} a ${rMid} ${rMid} 0 1 1 ${
                rMid * 2
              } 0 a ${rMid} ${rMid} 0 1 1 ${-rMid * 2} 0`}
            />
          </defs>
          <text
            fill={POSTMARK}
            fontFamily="ui-monospace, SFMono-Regular, monospace"
            fontSize={Math.max(5, rMid * 0.22)}
            letterSpacing="1.5"
          >
            <textPath href={`#${pathId}`} startOffset="0">
              {postmark}
            </textPath>
          </text>
        </>
      ) : null}

      {/* Date-stamp dashes: a small horizontal label overprint near the
          top of the stamp, simulating a hand-stamped date strip. */}
      {dateStamp ? (
        <g transform={`translate(${inset + 4}, ${inset + 10})`}>
          <line
            x1={0}
            y1={0}
            x2={innerW * 0.6}
            y2={0}
            stroke={POSTMARK}
            strokeWidth={0.75}
          />
          <text
            x={2}
            y={-2}
            fill={POSTMARK}
            fontFamily="ui-monospace, SFMono-Regular, monospace"
            fontSize={5.5}
            letterSpacing="1"
          >
            {`DATE: ${dateStamp}`}
          </text>
          <line
            x1={0}
            y1={4}
            x2={innerW * 0.45}
            y2={4}
            stroke={POSTMARK}
            strokeWidth={0.5}
          />
        </g>
      ) : null}

      {/* Wax-seal-style diagonal overstamp */}
      {seal ? (
        <g transform={`translate(${cx}, ${cy}) rotate(-22)`}>
          <rect
            x={-innerW * 0.45}
            y={-9}
            width={innerW * 0.9}
            height={18}
            fill="none"
            stroke={POSTMARK}
            strokeWidth={1}
          />
          <text
            x={0}
            y={3.5}
            textAnchor="middle"
            fill={POSTMARK}
            fontFamily="ui-monospace, SFMono-Regular, monospace"
            fontSize={9}
            letterSpacing="3"
            fontWeight={600}
          >
            {seal}
          </text>
        </g>
      ) : null}
    </g>
  );
};

/**
 * Decorative archival background for the cinematic /customers variant.
 * Renders a scatter of postage-stamp shapes with verification postmarks,
 * date strips, and occasional wax-seal overstamps. Hidden from
 * assistive tech and never receives pointer events.
 */
export const StampArchive: React.FC<StampArchiveProps> = ({ className }) => {
  const baseId = React.useId();

  return (
    <Outer className={className} aria-hidden="true">
      <svg
        xmlns="http://www.w3.org/2000/svg"
        viewBox="0 0 1440 4000"
        preserveAspectRatio="xMidYMid slice"
      >
        {STAMPS.map((s, i) => (
          <g
            key={i}
            transform={`translate(${s.x}, ${s.y}) rotate(${s.rot}, ${
              s.w / 2
            }, ${s.h / 2})`}
          >
            <Stamp
              w={s.w}
              h={s.h}
              postmark={s.postmark}
              dateStamp={s.dateStamp}
              seal={s.seal}
              pathId={`${baseId}-pm-${i}`}
            />
          </g>
        ))}
      </svg>
    </Outer>
  );
};
