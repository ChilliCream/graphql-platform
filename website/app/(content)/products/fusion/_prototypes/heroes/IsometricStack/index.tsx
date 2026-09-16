"use client";

import { useRef } from "react";

import { CC } from "../../../tokens";
import { anim, useElementMotion } from "../../../visuals/hooks";
import { SERVICE_SPECTRUM } from "../../spectrum";

/**
 * Five isometric slabs, one per service colour, fly in from the right and
 * settle bottom to top into one composite block capped by a single white
 * top face. A light isometric grid runs behind the stack; the rest frame
 * shows the block fully assembled.
 */

const VIEW_W = 1000;
const VIEW_H = 600;

const SCALE = 86;
const ORIGIN: readonly [number, number] = [660, 400];
const X_DEPTH = 2;
const Y_DEPTH = 2;
const SLAB_H = 0.5;

/** Isometric projection: 3D (x, y, z) to a 2D point in the viewBox. */
function project(x: number, y: number, z: number): [number, number] {
  const px = ORIGIN[0] + (x * 0.866 - y * 0.866) * SCALE;
  const py = ORIGIN[1] + (x * 0.5 + y * 0.5) * SCALE - z * SCALE;
  return [px, py];
}

function pointsStr(points: readonly (readonly [number, number])[]): string {
  return points.map(([x, y]) => `${x.toFixed(1)},${y.toFixed(1)}`).join(" ");
}

const SLABS = SERVICE_SPECTRUM.map((stop, i) => {
  const z0 = i * SLAB_H;
  const z1 = z0 + SLAB_H;

  // Face A: the y = 0 plane (lit, front-left). Face B: the x = X_DEPTH plane
  // (shaded, front-right). Together with the top cap these are the three
  // faces a standard isometric box shows the viewer.
  const a0 = project(0, 0, z0);
  const a1 = project(X_DEPTH, 0, z0);
  const a2 = project(X_DEPTH, 0, z1);
  const a3 = project(0, 0, z1);
  const b0 = project(X_DEPTH, 0, z0);
  const b1 = project(X_DEPTH, Y_DEPTH, z0);
  const b2 = project(X_DEPTH, Y_DEPTH, z1);
  const b3 = project(X_DEPTH, 0, z1);

  // Affine matrix mapping the unit square (0,0)-(1,0)-(0,1) onto face A's
  // corners, so the glyph below can be drawn in flat unit space and still
  // land skewed onto the slab's isometric face.
  const matrix = `matrix(${(a1[0] - a0[0]).toFixed(2)} ${(a1[1] - a0[1]).toFixed(2)} ${(a3[0] - a0[0]).toFixed(2)} ${(a3[1] - a0[1]).toFixed(2)} ${a0[0].toFixed(1)} ${a0[1].toFixed(1)})`;

  return {
    stop,
    faceA: pointsStr([a0, a1, a2, a3]),
    faceB: pointsStr([b0, b1, b2, b3]),
    glyphMatrix: matrix,
    delay: i * 0.32,
  };
});

const Z_TOP = SERVICE_SPECTRUM.length * SLAB_H;
const TOP_CAP = pointsStr([
  project(0, 0, Z_TOP),
  project(X_DEPTH, 0, Z_TOP),
  project(X_DEPTH, Y_DEPTH, Z_TOP),
  project(0, Y_DEPTH, Z_TOP),
]);
const GROUND_Y = project(X_DEPTH, Y_DEPTH, 0)[1];
const LAST_SLAB_DELAY = SLABS[SLABS.length - 1].delay;
const CAP_DELAY = LAST_SLAB_DELAY + 0.85;

/** Two families of parallel 30-degree lines, matching the box projection. */
const GRID_DIRS: readonly (readonly [number, number])[] = [
  [0.866, 0.5],
  [-0.866, 0.5],
];

function isoGridPaths(): readonly string[] {
  const step = 62;
  const span = 22;
  const reach = 1300;
  const paths: string[] = [];

  for (const dir of GRID_DIRS) {
    const perp: readonly [number, number] = [-dir[1], dir[0]];
    for (let i = -span; i <= span; i++) {
      const ox = 500 + perp[0] * i * step;
      const oy = 300 + perp[1] * i * step;
      const x1 = ox - dir[0] * reach;
      const y1 = oy - dir[1] * reach;
      const x2 = ox + dir[0] * reach;
      const y2 = oy + dir[1] * reach;
      paths.push(
        `M ${x1.toFixed(1)} ${y1.toFixed(1)} L ${x2.toFixed(1)} ${y2.toFixed(1)}`,
      );
    }
  }
  return paths;
}

const GRID_PATHS = isoGridPaths();

const KEYFRAMES = `
@keyframes fx-isometricstack-slide-in {
  0% { transform: translate(320px, -64px); opacity: 0; }
  45% { opacity: 1; }
  100% { transform: translate(0, 0); opacity: 1; }
}
@keyframes fx-isometricstack-cap-in {
  0% { transform: translateY(-16px); opacity: 0; }
  100% { transform: translateY(0); opacity: 1; }
}
`;

/** A tiny three-node schema glyph, in unit-square coordinates. */
const GLYPH_DOTS: readonly (readonly [number, number])[] = [
  [0.22, 0.5],
  [0.5, 0.3],
  [0.78, 0.5],
];
const GLYPH_PATH = `M ${GLYPH_DOTS[0][0]} ${GLYPH_DOTS[0][1]} L ${GLYPH_DOTS[1][0]} ${GLYPH_DOTS[1][1]} L ${GLYPH_DOTS[2][0]} ${GLYPH_DOTS[2][1]}`;

interface EngravedGlyphProps {
  readonly shift: number;
  readonly stroke: string;
  readonly strokeOpacity: number;
  readonly fillOpacity: number;
}

/** One bevel pass of the schema glyph, offset so the pair reads as engraved. */
function EngravedGlyph({
  shift,
  stroke,
  strokeOpacity,
  fillOpacity,
}: EngravedGlyphProps) {
  return (
    <g
      transform={`translate(${shift} ${shift * 1.3})`}
      stroke={stroke}
      strokeOpacity={strokeOpacity}
      strokeWidth={0.02}
      fill={stroke}
      fillOpacity={fillOpacity}
    >
      <path d={GLYPH_PATH} fill="none" />
      {GLYPH_DOTS.map(([cx, cy], i) => (
        <circle key={i} cx={cx} cy={cy} r={0.05} />
      ))}
    </g>
  );
}

export default function IsometricStack() {
  const ref = useRef<HTMLDivElement>(null);
  const running = useElementMotion(ref);

  return (
    <div ref={ref} className="absolute inset-0" aria-hidden="true">
      <style>{KEYFRAMES}</style>

      <svg
        className="absolute inset-0 h-full w-full"
        viewBox={`0 0 ${VIEW_W} ${VIEW_H}`}
        preserveAspectRatio="xMidYMax meet"
      >
        <defs>
          <radialGradient
            id="isometricstack-cap-sheen"
            cx="35%"
            cy="28%"
            r="85%"
          >
            <stop offset="0%" stopColor={CC.white} stopOpacity={1} />
            <stop offset="100%" stopColor={CC.white} stopOpacity={0.85} />
          </radialGradient>
        </defs>

        <g stroke={CC.inkFaint} strokeOpacity={0.4} strokeWidth={1}>
          {GRID_PATHS.map((d, i) => (
            <path key={i} d={d} />
          ))}
        </g>

        <ellipse
          cx={ORIGIN[0]}
          cy={GROUND_Y + 14}
          rx={150}
          ry={22}
          fill={CC.black}
          opacity={0.16}
        />

        {SLABS.map((slab) => (
          <g
            key={slab.stop.label}
            style={{
              animation: anim(
                running,
                `fx-isometricstack-slide-in 0.85s cubic-bezier(0.16,0.84,0.44,1) ${slab.delay}s both`,
              ),
            }}
          >
            <polygon
              points={slab.faceB}
              fill={`color-mix(in srgb, ${slab.stop.color} 66%, ${CC.black})`}
              stroke={CC.black}
              strokeOpacity={0.22}
              strokeWidth={1.2}
            />
            <polygon
              points={slab.faceA}
              fill={slab.stop.color}
              stroke={CC.black}
              strokeOpacity={0.18}
              strokeWidth={1.2}
            />
            <g transform={slab.glyphMatrix}>
              <EngravedGlyph
                shift={0.014}
                stroke={CC.black}
                strokeOpacity={0.3}
                fillOpacity={0.24}
              />
              <EngravedGlyph
                shift={-0.014}
                stroke={CC.white}
                strokeOpacity={0.34}
                fillOpacity={0.3}
              />
            </g>
          </g>
        ))}

        <polygon
          points={TOP_CAP}
          fill="url(#isometricstack-cap-sheen)"
          stroke={CC.ink}
          strokeOpacity={0.25}
          strokeWidth={1.5}
          style={{
            animation: anim(
              running,
              `fx-isometricstack-cap-in 0.5s ease-out ${CAP_DELAY}s both`,
            ),
          }}
        />
      </svg>

      <div
        className="absolute inset-0"
        style={{
          background: `radial-gradient(60% 65% at 20% 46%, color-mix(in srgb, ${CC.bg} 92%, transparent) 0%, color-mix(in srgb, ${CC.bg} 40%, transparent) 55%, transparent 85%)`,
        }}
      />
    </div>
  );
}
