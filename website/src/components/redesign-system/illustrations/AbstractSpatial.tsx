"use client";

import React from "react";

// "Abstract spatial" illustration register: perspective rooms, hemispheres,
// orbits, ray bursts, light cones. Pages compose these into bespoke heroes.
//
// Every primitive renders inline SVG, uses `currentColor` for stroke and the
// `accent` prop (or `var(--cc-accent)` fallback) for highlights, has stroke
// widths around 1.5px, no fills, and `aria-hidden` because they are
// decorative.

const ACCENT_FALLBACK = "var(--cc-accent, currentColor)";

export interface OrbitProps {
  rings?: number;
  rotate?: number;
  accent?: string;
  className?: string;
}

/**
 * Concentric perspective-tilted ellipses. Composes well with `Hemisphere`
 * or `RayBurst` at the same focal point.
 */
export const Orbit: React.FC<OrbitProps> = ({
  rings = 3,
  rotate = 0,
  accent,
  className,
}) => {
  const cx = 240;
  const cy = 150;
  const accentColor = accent ?? ACCENT_FALLBACK;
  const ringList = Array.from({ length: rings }, (_, i) => i + 1);

  return (
    <svg
      className={className}
      viewBox="0 0 480 300"
      preserveAspectRatio="xMidYMid meet"
      aria-hidden="true"
    >
      <g transform={`rotate(${rotate} ${cx} ${cy})`} fill="none">
        {ringList.map((i) => {
          const rx = 60 + i * 60;
          const ry = 18 + i * 18;
          const isLast = i === ringList.length;
          return (
            <ellipse
              key={i}
              cx={cx}
              cy={cy}
              rx={rx}
              ry={ry}
              stroke={isLast ? accentColor : "currentColor"}
              strokeWidth={1.5}
              opacity={isLast ? 0.85 : 0.32 + i * 0.08}
            />
          );
        })}
        <circle cx={cx} cy={cy} r={3} fill={accentColor} />
      </g>
    </svg>
  );
};

export interface HemisphereProps {
  side?: "left" | "right";
  accent?: string;
  className?: string;
}

/**
 * Half-dome of soft radial light casting into the canvas. Use as ambient
 * fill behind a typographic moment or dashboard composite.
 */
export const Hemisphere: React.FC<HemisphereProps> = ({
  side = "right",
  accent,
  className,
}) => {
  const accentColor = accent ?? ACCENT_FALLBACK;
  const cx = side === "right" ? 360 : 120;
  const gradId = `cc-hemi-${side}`;
  const lat = 6;
  const lng = 8;

  // Latitude lines: nested half-ellipses with descending opacity
  const latitudes = Array.from({ length: lat }, (_, i) => i + 1);
  // Longitude lines: arcs from top to bottom of the dome at varying x offsets
  const longitudes = Array.from({ length: lng }, (_, i) => i);

  return (
    <svg
      className={className}
      viewBox="0 0 480 300"
      preserveAspectRatio="xMidYMid meet"
      aria-hidden="true"
    >
      <defs>
        <radialGradient id={gradId} cx="50%" cy="50%" r="50%">
          <stop offset="0%" stopColor={accentColor} stopOpacity="0.28" />
          <stop offset="60%" stopColor={accentColor} stopOpacity="0.06" />
          <stop offset="100%" stopColor={accentColor} stopOpacity="0" />
        </radialGradient>
        <clipPath id={`${gradId}-clip`}>
          <rect x={cx - 160} y={0} width={320} height={300} />
        </clipPath>
      </defs>
      <circle cx={cx} cy={150} r={140} fill={`url(#${gradId})`} />
      <g
        clipPath={`url(#${gradId}-clip)`}
        fill="none"
        stroke="currentColor"
        strokeWidth={1.5}
      >
        {latitudes.map((i) => {
          const r = 24 + i * 22;
          return (
            <ellipse
              key={`lat-${i}`}
              cx={cx}
              cy={150}
              rx={r}
              ry={r * 0.32}
              opacity={0.18 - i * 0.02}
            />
          );
        })}
        {longitudes.map((i) => {
          const t = (i - (lng - 1) / 2) / ((lng - 1) / 2);
          const dx = t * 130;
          const r = Math.sqrt(Math.max(0, 130 * 130 - dx * dx));
          return (
            <ellipse
              key={`lng-${i}`}
              cx={cx + dx}
              cy={150}
              rx={Math.max(8, r * 0.28)}
              ry={r}
              opacity={0.16 - Math.abs(t) * 0.06}
            />
          );
        })}
      </g>
    </svg>
  );
};

export interface PerspectiveGridProps {
  density?: "sparse" | "dense";
  accent?: string;
  className?: string;
}

/**
 * 3D perspective grid lines vanishing toward a horizon point at the canvas
 * center. Use as a hero floor or backdrop.
 */
export const PerspectiveGrid: React.FC<PerspectiveGridProps> = ({
  density = "sparse",
  accent,
  className,
}) => {
  const accentColor = accent ?? ACCENT_FALLBACK;
  const w = 480;
  const h = 300;
  const vx = w / 2;
  const vy = h * 0.46;

  const verticalCount = density === "dense" ? 18 : 10;
  const horizonCount = density === "dense" ? 9 : 6;

  const verticals = Array.from({ length: verticalCount + 1 }, (_, i) => {
    const t = i / verticalCount;
    const x = t * w;
    return { x };
  });
  const horizons = Array.from({ length: horizonCount }, (_, i) => i + 1);

  return (
    <svg
      className={className}
      viewBox={`0 0 ${w} ${h}`}
      preserveAspectRatio="xMidYMid meet"
      aria-hidden="true"
    >
      <g fill="none" stroke="currentColor" strokeWidth={1.5}>
        {verticals.map((v, i) => (
          <line key={`v-${i}`} x1={v.x} y1={h} x2={vx} y2={vy} opacity={0.22} />
        ))}
        {horizons.map((i) => {
          // Distance lines parallel to horizon, denser near the horizon.
          const t = i / (horizonCount + 1);
          const y = vy + (h - vy) * Math.pow(t, 1.6);
          return (
            <line
              key={`h-${i}`}
              x1={0}
              y1={y}
              x2={w}
              y2={y}
              opacity={0.16 + t * 0.18}
            />
          );
        })}
      </g>
      <line
        x1={vx}
        y1={vy - 8}
        x2={vx}
        y2={vy + 8}
        stroke={accentColor}
        strokeWidth={2}
      />
      <line
        x1={vx - 8}
        y1={vy}
        x2={vx + 8}
        y2={vy}
        stroke={accentColor}
        strokeWidth={2}
      />
    </svg>
  );
};

export interface RayBurstProps {
  rayCount?: number;
  accent?: string;
  className?: string;
}

/**
 * Radial rays from a center point. Use as an accent burst on numerical
 * proof or hero punctuation.
 */
export const RayBurst: React.FC<RayBurstProps> = ({
  rayCount = 16,
  accent,
  className,
}) => {
  const accentColor = accent ?? ACCENT_FALLBACK;
  const cx = 240;
  const cy = 150;
  const inner = 22;
  const outer = 130;

  return (
    <svg
      className={className}
      viewBox="0 0 480 300"
      preserveAspectRatio="xMidYMid meet"
      aria-hidden="true"
    >
      <g fill="none" stroke="currentColor" strokeWidth={1.5}>
        {Array.from({ length: rayCount }, (_, i) => {
          const a = (i * 2 * Math.PI) / rayCount;
          const x1 = cx + Math.cos(a) * inner;
          const y1 = cy + Math.sin(a) * inner;
          const len = outer * (0.62 + (i % 3) * 0.16);
          const x2 = cx + Math.cos(a) * len;
          const y2 = cy + Math.sin(a) * len;
          return (
            <line
              key={i}
              x1={x1}
              y1={y1}
              x2={x2}
              y2={y2}
              opacity={0.22 + (i % 4) * 0.1}
            />
          );
        })}
      </g>
      <circle cx={cx} cy={cy} r={6} fill={accentColor} />
    </svg>
  );
};

export interface LightConeProps {
  angle?: number;
  accent?: string;
  className?: string;
}

/**
 * Directional cone of soft light radiating from a single source. Use to
 * imply attention or focus on a downstream element.
 */
export const LightCone: React.FC<LightConeProps> = ({
  angle = 28,
  accent,
  className,
}) => {
  const accentColor = accent ?? ACCENT_FALLBACK;
  const w = 480;
  const h = 300;
  const sx = 80;
  const sy = 60;
  const rad = (angle * Math.PI) / 180;
  const reach = 380;
  // Cone direction: from source toward bottom-right by default.
  const dirAngle = Math.PI / 4;
  const a1 = dirAngle - rad / 2;
  const a2 = dirAngle + rad / 2;
  const p1x = sx + Math.cos(a1) * reach;
  const p1y = sy + Math.sin(a1) * reach;
  const p2x = sx + Math.cos(a2) * reach;
  const p2y = sy + Math.sin(a2) * reach;
  const gradId = "cc-cone-grad";

  return (
    <svg
      className={className}
      viewBox={`0 0 ${w} ${h}`}
      preserveAspectRatio="xMidYMid meet"
      aria-hidden="true"
    >
      <defs>
        <radialGradient
          id={gradId}
          cx={`${(sx / w) * 100}%`}
          cy={`${(sy / h) * 100}%`}
          r="80%"
        >
          <stop offset="0%" stopColor={accentColor} stopOpacity="0.42" />
          <stop offset="50%" stopColor={accentColor} stopOpacity="0.12" />
          <stop offset="100%" stopColor={accentColor} stopOpacity="0" />
        </radialGradient>
      </defs>
      <polygon
        points={`${sx},${sy} ${p1x},${p1y} ${p2x},${p2y}`}
        fill={`url(#${gradId})`}
      />
      <g fill="none" stroke="currentColor" strokeWidth={1.5} opacity={0.42}>
        <line x1={sx} y1={sy} x2={p1x} y2={p1y} />
        <line x1={sx} y1={sy} x2={p2x} y2={p2y} />
      </g>
      <circle cx={sx} cy={sy} r={4} fill={accentColor} />
    </svg>
  );
};
