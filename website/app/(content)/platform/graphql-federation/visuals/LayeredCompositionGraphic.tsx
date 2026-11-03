"use client";

import { useId } from "react";

import { useReducedMotionPreference } from "@/src/nitro/lib/motion";

type Point = readonly [number, number];
type Link = readonly [number, number];

const VIEW_WIDTH = 560;
const VIEW_HEIGHT = 676;
const CENTER_X = 280;

const PLANE_WIDTH = 240;
const PLANE_HEIGHT = 96;
const PLANE_GAP = 104;
const FIRST_PLANE_Y = 84;
const SLAB = 3.5;

const BASE_WIDTH = 400;
const BASE_HEIGHT = 160;
const BASE_Y = 558;

const NODE_CORE = 3.2;
const NODE_HALO = 9;

const CYCLE = 7.2;
const PULSE_SPEED = 90;

const ACCENT = "var(--color-cc-accent)";
const ACCENT_HI = "var(--color-cc-accent-hover)";
const SUCCESS = "var(--color-cc-success)";
const SPARK = "var(--color-cc-heading)";
const GROUND = "var(--color-cc-bg)";

interface Frame {
  readonly cy: number;
  readonly width: number;
  readonly height: number;
}

interface PlaneSpec {
  readonly nodes: readonly Point[];
  readonly links: readonly Link[];
  readonly tone: number;
  readonly active?: boolean;
}

function at({ cy, width, height }: Frame, dx: number, dy: number): Point {
  return [CENTER_X + (dx * width) / 2, cy + (dy * height) / 2];
}

function corners({ cy, width, height }: Frame): readonly Point[] {
  return [
    [CENTER_X, cy - height / 2],
    [CENTER_X + width / 2, cy],
    [CENTER_X, cy + height / 2],
    [CENTER_X - width / 2, cy],
  ];
}

function path(points: readonly Point[], close = false): string {
  return `M${points.map(([x, y]) => `${x},${y}`).join(" L")}${close ? " Z" : ""}`;
}

function lerp(a: Point, b: Point, t: number): Point {
  return [a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t];
}

const PLANE_SPECS: readonly PlaneSpec[] = [
  {
    tone: 0.6,
    nodes: [
      [-0.69, 0.06],
      [-0.4, -0.34],
      [0.05, -0.08],
      [0.52, -0.26],
    ],
    links: [
      [0, 1],
      [1, 2],
      [2, 3],
    ],
  },
  {
    tone: 0.9,
    active: true,
    nodes: [
      [0.23, 0.12],
      [-0.4, -0.1],
      [0.02, -0.4],
      [0.58, -0.14],
    ],
    links: [
      [0, 1],
      [0, 2],
      [0, 3],
    ],
  },
  {
    tone: 0.8,
    nodes: [
      [-0.23, 0.12],
      [-0.46, -0.26],
      [0.02, -0.34],
      [0.5, 0],
    ],
    links: [
      [0, 1],
      [0, 2],
      [2, 3],
    ],
  },
  {
    tone: 1,
    nodes: [
      [0.69, 0.04],
      [0.46, -0.3],
      [0, 0.1],
      [-0.46, -0.2],
    ],
    links: [
      [0, 1],
      [1, 2],
      [2, 3],
    ],
  },
];

const BASE: Frame = { cy: BASE_Y, width: BASE_WIDTH, height: BASE_HEIGHT };

const BASE_SPECS: readonly Point[] = [
  [0, 0],
  [0, 0],
  [0, 0],
  [0, 0],
  [0, 0.12],
  [-0.8, 0.02],
  [0.8, 0.02],
  [0, 0.55],
];

const LANDING_DY = [-0.1, -0.3, -0.3, -0.1];

const BASE_LINKS: readonly Link[] = [
  [0, 4],
  [1, 4],
  [2, 4],
  [3, 4],
  [0, 5],
  [3, 6],
  [4, 7],
];

const PLANES = PLANE_SPECS.map((spec, i) => {
  const frame: Frame = {
    cy: FIRST_PLANE_Y + i * PLANE_GAP,
    width: PLANE_WIDTH,
    height: PLANE_HEIGHT,
  };
  return {
    ...spec,
    frame,
    corners: corners(frame),
    points: spec.nodes.map(([dx, dy]) => at(frame, dx, dy)),
  };
});

const LINES = PLANES.map(({ points, active }, i) => {
  const [x, y] = points[0];
  const landing = at(BASE, (x - CENTER_X) / (BASE_WIDTH / 2), LANDING_DY[i]);
  const length = landing[1] - y;
  return {
    x,
    y1: y + NODE_CORE + 1,
    y2: landing[1] - NODE_CORE - 1,
    active: Boolean(active),
    travel: Math.min(0.92, length / PULSE_SPEED / CYCLE),
    begin: -i * 1.9,
    pierce: PLANES.slice(i + 1).map(
      ({ frame }) => frame.cy + (PLANE_SPECS[i].nodes[0][1] * PLANE_HEIGHT) / 2,
    ),
  };
});

const BASE_POINTS = BASE_SPECS.map(([dx, dy], i) =>
  i < PLANES.length
    ? at(BASE, (LINES[i].x - CENTER_X) / (BASE_WIDTH / 2), LANDING_DY[i])
    : at(BASE, dx, dy),
);

const SWEEP_WIDTH = 0.26;
const ACTIVE_PLANE = PLANES.find(({ active }) => active) ?? PLANES[0];

function sweepGeometry(frame: Frame) {
  const project = (u: number, v: number): Point => [
    CENTER_X + ((u - v) * frame.width) / 2,
    frame.cy + ((u + v - 1) * frame.height) / 2,
  ];
  const norm = Math.hypot(frame.height, frame.width);
  const span = (SWEEP_WIDTH * frame.width * frame.height) / norm;
  const from = project(-SWEEP_WIDTH, 0.5);
  return {
    band: path(
      [
        project(-SWEEP_WIDTH, 0),
        project(0, 0),
        project(0, 1),
        project(-SWEEP_WIDTH, 1),
      ],
      true,
    ),
    from,
    to: [
      from[0] + (frame.height / norm) * span,
      from[1] + (frame.width / norm) * span,
    ] as Point,
    travel: [
      ((1 + SWEEP_WIDTH) * frame.width) / 2,
      ((1 + SWEEP_WIDTH) * frame.height) / 2,
    ] as Point,
  };
}

const SWEEP = sweepGeometry(ACTIVE_PLANE.frame);

function breathe(i: number): { dur: string; begin: string } {
  return { dur: `${5.4 + (i % 5) * 0.8}s`, begin: `${-((i * 1.7) % 5)}s` };
}

interface NodeDotProps {
  readonly uid: string;
  readonly x: number;
  readonly y: number;
  readonly green: boolean;
  readonly bright?: boolean;
  readonly tone: number;
  readonly index: number;
  readonly animated: boolean;
}

function NodeDot({
  uid,
  x,
  y,
  green,
  bright = false,
  tone,
  index,
  animated,
}: NodeDotProps) {
  const { dur, begin } = breathe(index);
  const color = green ? SUCCESS : ACCENT;
  return (
    <g>
      <circle
        cx={x}
        cy={y}
        r={NODE_HALO}
        fill={`url(#${uid}-halo-${green ? "success" : "accent"})`}
        fillOpacity={0.24 * tone}
      >
        {animated && (
          <animate
            attributeName="fill-opacity"
            values={`${0.14 * tone};${0.3 * tone};${0.14 * tone}`}
            dur={dur}
            begin={begin}
            repeatCount="indefinite"
          />
        )}
      </circle>
      <circle cx={x} cy={y} r={NODE_CORE} fill={color} fillOpacity={tone} />
      {bright && (
        <circle
          cx={x}
          cy={y}
          r={NODE_CORE * 0.42}
          fill={SPARK}
          fillOpacity={0.9 * tone}
        />
      )}
    </g>
  );
}

/**
 * Isometric stack of four translucent subgraph planes whose connectors drop
 * as light-lines into one base plane, where they join into a single composite
 * graph. One plane is highlighted as the agent's domain.
 */
export function LayeredCompositionGraphic() {
  const uid = `layered-composition-${useId().replace(/[^a-zA-Z0-9_-]/g, "")}`;
  const reducedMotion = useReducedMotionPreference();
  const animated = !reducedMotion;
  const baseCorners = corners(BASE);

  return (
    <svg
      viewBox={`0 0 ${VIEW_WIDTH} ${VIEW_HEIGHT}`}
      aria-hidden="true"
      className="mx-auto h-auto w-full max-w-[35rem]"
      fill="none"
    >
      <defs>
        <radialGradient id={`${uid}-halo-accent`}>
          <stop offset="0%" stopColor={ACCENT} stopOpacity={1} />
          <stop offset="100%" stopColor={ACCENT} stopOpacity={0} />
        </radialGradient>
        <radialGradient id={`${uid}-halo-success`}>
          <stop offset="0%" stopColor={SUCCESS} stopOpacity={1} />
          <stop offset="100%" stopColor={SUCCESS} stopOpacity={0} />
        </radialGradient>
        <radialGradient id={`${uid}-shadow`}>
          <stop offset="0%" stopColor={GROUND} stopOpacity={0.7} />
          <stop offset="100%" stopColor={GROUND} stopOpacity={0} />
        </radialGradient>
        <linearGradient id={`${uid}-face`} x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stopColor={ACCENT} stopOpacity={0.02} />
          <stop offset="100%" stopColor={ACCENT} stopOpacity={0.13} />
        </linearGradient>
        <linearGradient id={`${uid}-face-active`} x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stopColor={SUCCESS} stopOpacity={0.02} />
          <stop offset="100%" stopColor={SUCCESS} stopOpacity={0.08} />
        </linearGradient>
        <linearGradient id={`${uid}-slab`} x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stopColor={ACCENT_HI} stopOpacity={0.34} />
          <stop offset="100%" stopColor={ACCENT} stopOpacity={0.05} />
        </linearGradient>
        <linearGradient id={`${uid}-slab-active`} x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stopColor={SUCCESS} stopOpacity={0.4} />
          <stop offset="100%" stopColor={SUCCESS} stopOpacity={0.05} />
        </linearGradient>
        <linearGradient id={`${uid}-edge`} x1="0" y1="0" x2="1" y2="0">
          <stop offset="0%" stopColor={ACCENT_HI} stopOpacity={0.1} />
          <stop offset="50%" stopColor={ACCENT_HI} stopOpacity={1} />
          <stop offset="100%" stopColor={ACCENT_HI} stopOpacity={0.1} />
        </linearGradient>
        <linearGradient id={`${uid}-edge-active`} x1="0" y1="0" x2="1" y2="0">
          <stop offset="0%" stopColor={SUCCESS} stopOpacity={0.1} />
          <stop offset="50%" stopColor={SUCCESS} stopOpacity={1} />
          <stop offset="100%" stopColor={SUCCESS} stopOpacity={0.1} />
        </linearGradient>
        <radialGradient id={`${uid}-inner`} cx="0.5" cy="0.5" r="0.5">
          <stop offset="0%" stopColor={SUCCESS} stopOpacity={0.3} />
          <stop offset="70%" stopColor={SUCCESS} stopOpacity={0.06} />
          <stop offset="100%" stopColor={SUCCESS} stopOpacity={0} />
        </radialGradient>
        <radialGradient id={`${uid}-base-inner`} cx="0.5" cy="0.55" r="0.5">
          <stop offset="0%" stopColor={ACCENT} stopOpacity={0.24} />
          <stop offset="100%" stopColor={ACCENT} stopOpacity={0} />
        </radialGradient>
        <linearGradient id={`${uid}-trail`} x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stopColor={ACCENT} stopOpacity={0} />
          <stop offset="100%" stopColor={ACCENT} stopOpacity={0.9} />
        </linearGradient>
        <linearGradient id={`${uid}-trail-active`} x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stopColor={SUCCESS} stopOpacity={0} />
          <stop offset="100%" stopColor={SUCCESS} stopOpacity={0.9} />
        </linearGradient>
        {LINES.map(({ x, y1, y2, active }, i) => (
          <linearGradient
            key={i}
            id={`${uid}-line-${i}`}
            gradientUnits="userSpaceOnUse"
            x1={x}
            y1={y1}
            x2={x}
            y2={y2}
          >
            <stop
              offset="0%"
              stopColor={active ? SUCCESS : ACCENT}
              stopOpacity={0.15}
            />
            <stop
              offset="55%"
              stopColor={active ? SUCCESS : ACCENT}
              stopOpacity={0.4}
            />
            <stop
              offset="100%"
              stopColor={active ? SUCCESS : ACCENT}
              stopOpacity={0.75}
            />
          </linearGradient>
        ))}
        <linearGradient
          id={`${uid}-sweep`}
          gradientUnits="userSpaceOnUse"
          x1={SWEEP.from[0]}
          y1={SWEEP.from[1]}
          x2={SWEEP.to[0]}
          y2={SWEEP.to[1]}
        >
          <stop offset="0%" stopColor={SUCCESS} stopOpacity={0} />
          <stop offset="82%" stopColor={SUCCESS} stopOpacity={0.14} />
          <stop offset="96%" stopColor={SUCCESS} stopOpacity={0.32} />
          <stop offset="100%" stopColor={SUCCESS} stopOpacity={0} />
        </linearGradient>
        <clipPath id={`${uid}-clip-active`}>
          <path d={path(ACTIVE_PLANE.corners, true)} />
        </clipPath>
      </defs>

      <ellipse
        cx={CENTER_X}
        cy={BASE_Y + 18}
        rx={BASE_WIDTH / 2 + 36}
        ry={BASE_HEIGHT / 2 + 30}
        fill={`url(#${uid}-halo-accent)`}
        fillOpacity={0.14}
      />
      <path
        d={path(
          [
            baseCorners[3],
            baseCorners[2],
            [baseCorners[2][0], baseCorners[2][1] + SLAB + 1],
            [baseCorners[3][0], baseCorners[3][1] + SLAB + 1],
          ],
          true,
        )}
        fill={`url(#${uid}-slab)`}
      />
      <path
        d={path(
          [
            baseCorners[2],
            baseCorners[1],
            [baseCorners[1][0], baseCorners[1][1] + SLAB + 1],
            [baseCorners[2][0], baseCorners[2][1] + SLAB + 1],
          ],
          true,
        )}
        fill={`url(#${uid}-slab)`}
        fillOpacity={0.6}
      />
      <path d={path(baseCorners, true)} fill={`url(#${uid}-face)`} />
      <path d={path(baseCorners, true)} fill={`url(#${uid}-base-inner)`} />
      {[1, 2, 3, 4, 5].flatMap((n) => {
        const t = n / 6;
        return [
          [
            lerp(baseCorners[3], baseCorners[0], t),
            lerp(baseCorners[2], baseCorners[1], t),
          ],
          [
            lerp(baseCorners[3], baseCorners[2], t),
            lerp(baseCorners[0], baseCorners[1], t),
          ],
        ].map(([a, b], k) => (
          <line
            key={`${n}-${k}`}
            x1={a[0]}
            y1={a[1]}
            x2={b[0]}
            y2={b[1]}
            stroke={ACCENT}
            strokeOpacity={0.07}
            strokeWidth={1}
            vectorEffect="non-scaling-stroke"
          />
        ));
      })}
      <path
        d={path([baseCorners[3], baseCorners[0], baseCorners[1]])}
        stroke={ACCENT}
        strokeOpacity={0.3}
        strokeWidth={1}
        strokeLinejoin="round"
        vectorEffect="non-scaling-stroke"
      />
      <path
        d={path([baseCorners[3], baseCorners[2], baseCorners[1]])}
        stroke={`url(#${uid}-edge)`}
        strokeWidth={1}
        strokeLinejoin="round"
        vectorEffect="non-scaling-stroke"
      />

      {BASE_LINKS.map(([a, b], i) => (
        <line
          key={i}
          x1={BASE_POINTS[a][0]}
          y1={BASE_POINTS[a][1]}
          x2={BASE_POINTS[b][0]}
          y2={BASE_POINTS[b][1]}
          stroke={ACCENT}
          strokeOpacity={0.55}
          strokeWidth={1}
          strokeLinecap="round"
          vectorEffect="non-scaling-stroke"
        />
      ))}
      {BASE_POINTS.map(([x, y], i) => (
        <NodeDot
          key={i}
          uid={uid}
          x={x}
          y={y}
          green={i === 1 || i === 4}
          bright={i >= PLANES.length ? i === 4 : true}
          tone={1}
          index={i}
          animated={animated}
        />
      ))}

      {[...PLANES].reverse().map((plane) => {
        const { corners: c, points, links, active, frame, tone } = plane;
        const index = PLANES.indexOf(plane);
        const suffix = active ? "-active" : "";
        const color = active ? SUCCESS : ACCENT;
        return (
          <g key={index}>
            <ellipse
              cx={CENTER_X}
              cy={frame.cy + PLANE_GAP * 0.5}
              rx={PLANE_WIDTH * 0.4}
              ry={PLANE_HEIGHT * 0.5}
              fill={`url(#${uid}-shadow)`}
            />
            <ellipse
              cx={CENTER_X}
              cy={frame.cy + PLANE_GAP * 0.5}
              rx={PLANE_WIDTH * 0.46}
              ry={PLANE_HEIGHT * 0.56}
              fill={`url(#${uid}-halo-${active ? "success" : "accent"})`}
              fillOpacity={(active ? 0.13 : 0.09) * tone}
            >
              {animated && active && (
                <animate
                  attributeName="fill-opacity"
                  values="0.08;0.17;0.08"
                  dur="6s"
                  repeatCount="indefinite"
                />
              )}
            </ellipse>
            <g opacity={tone}>
              <path
                d={path(
                  [
                    c[3],
                    c[2],
                    [c[2][0], c[2][1] + SLAB],
                    [c[3][0], c[3][1] + SLAB],
                  ],
                  true,
                )}
                fill={`url(#${uid}-slab${suffix})`}
              />
              <path
                d={path(
                  [
                    c[2],
                    c[1],
                    [c[1][0], c[1][1] + SLAB],
                    [c[2][0], c[2][1] + SLAB],
                  ],
                  true,
                )}
                fill={`url(#${uid}-slab${suffix})`}
                fillOpacity={0.55}
              />
              <path d={path(c, true)} fill={`url(#${uid}-face${suffix})`} />
              {active && (
                <path d={path(c, true)} fill={`url(#${uid}-inner)`}>
                  {animated && (
                    <animate
                      attributeName="fill-opacity"
                      values="0.6;1;0.6"
                      dur="6s"
                      repeatCount="indefinite"
                    />
                  )}
                </path>
              )}
              {active && animated && (
                <g clipPath={`url(#${uid}-clip-active)`}>
                  <path d={SWEEP.band} fill={`url(#${uid}-sweep)`}>
                    <animateTransform
                      attributeName="transform"
                      type="translate"
                      values={`0 0; ${SWEEP.travel[0]} ${SWEEP.travel[1]}; ${SWEEP.travel[0]} ${SWEEP.travel[1]}`}
                      keyTimes="0;0.5;1"
                      calcMode="spline"
                      keySplines="0.45 0 0.55 1; 0 0 1 1"
                      dur="9s"
                      repeatCount="indefinite"
                    />
                  </path>
                </g>
              )}
              <path
                d={path([c[3], c[0], c[1]])}
                stroke={color}
                strokeOpacity={0.26}
                strokeWidth={1}
                strokeLinejoin="round"
                vectorEffect="non-scaling-stroke"
              />
              <path
                d={path([c[3], c[2], c[1]])}
                stroke={`url(#${uid}-edge${suffix})`}
                strokeWidth={1}
                strokeLinejoin="round"
                vectorEffect="non-scaling-stroke"
              />
              {links.map(([a, b], l) => (
                <line
                  key={l}
                  x1={points[a][0]}
                  y1={points[a][1]}
                  x2={points[b][0]}
                  y2={points[b][1]}
                  stroke={color}
                  strokeOpacity={0.5}
                  strokeWidth={1}
                  strokeLinecap="round"
                  vectorEffect="non-scaling-stroke"
                />
              ))}
            </g>
            {points.map(([x, y], n) => (
              <NodeDot
                key={n}
                uid={uid}
                x={x}
                y={y}
                green={Boolean(active)}
                bright={n === 0}
                tone={tone}
                index={index * 4 + n + 8}
                animated={animated}
              />
            ))}
          </g>
        );
      })}

      {LINES.map(({ x, y1, y2, active, pierce }, i) => (
        <g key={i}>
          <line
            x1={x}
            y1={y1}
            x2={x}
            y2={y2}
            stroke={`url(#${uid}-line-${i})`}
            strokeWidth={1}
            strokeLinecap="round"
            vectorEffect="non-scaling-stroke"
          />
          {pierce.map((py, k) => (
            <ellipse
              key={k}
              cx={x}
              cy={py}
              rx={4.6}
              ry={1.8}
              stroke={active ? SUCCESS : ACCENT}
              strokeOpacity={0.5}
              strokeWidth={1}
              vectorEffect="non-scaling-stroke"
            />
          ))}
        </g>
      ))}

      {animated &&
        LINES.map(({ x, y1, y2, active, travel, begin }, i) => {
          const f = travel;
          const target = BASE_POINTS[i];
          return (
            <g key={i}>
              <g opacity={0}>
                <rect
                  x={x - 0.6}
                  y={-16}
                  width={1.2}
                  height={16}
                  fill={`url(#${uid}-trail${active ? "-active" : ""})`}
                />
                <circle
                  cx={x}
                  cy={0}
                  r={NODE_HALO * 0.55}
                  fill={`url(#${uid}-halo-${active ? "success" : "accent"})`}
                  fillOpacity={0.7}
                />
                <circle cx={x} cy={0} r={1.6} fill={active ? SUCCESS : SPARK} />
                <animateTransform
                  attributeName="transform"
                  type="translate"
                  values={`0 ${y1}; 0 ${y2}; 0 ${y2}`}
                  keyTimes={`0;${f};1`}
                  dur={`${CYCLE}s`}
                  begin={`${begin}s`}
                  repeatCount="indefinite"
                />
                <animate
                  attributeName="opacity"
                  values="0;1;1;0;0"
                  keyTimes={`0;0.05;${f * 0.9};${f};1`}
                  dur={`${CYCLE}s`}
                  begin={`${begin}s`}
                  repeatCount="indefinite"
                />
              </g>
              <ellipse
                cx={target[0]}
                cy={target[1]}
                rx={NODE_CORE}
                ry={NODE_CORE * 0.45}
                stroke={active ? SUCCESS : ACCENT_HI}
                strokeWidth={1}
                strokeOpacity={0}
                vectorEffect="non-scaling-stroke"
              >
                <animate
                  attributeName="rx"
                  values={`${NODE_CORE};${NODE_CORE};${NODE_CORE * 4.4};${NODE_CORE}`}
                  keyTimes={`0;${f};${Math.min(0.99, f + 0.16)};1`}
                  dur={`${CYCLE}s`}
                  begin={`${begin}s`}
                  repeatCount="indefinite"
                />
                <animate
                  attributeName="ry"
                  values={`${NODE_CORE * 0.45};${NODE_CORE * 0.45};${NODE_CORE * 2};${NODE_CORE * 0.45}`}
                  keyTimes={`0;${f};${Math.min(0.99, f + 0.16)};1`}
                  dur={`${CYCLE}s`}
                  begin={`${begin}s`}
                  repeatCount="indefinite"
                />
                <animate
                  attributeName="stroke-opacity"
                  values="0;0;0.8;0;0"
                  keyTimes={`0;${f};${Math.min(0.97, f + 0.03)};${Math.min(0.99, f + 0.16)};1`}
                  dur={`${CYCLE}s`}
                  begin={`${begin}s`}
                  repeatCount="indefinite"
                />
              </ellipse>
            </g>
          );
        })}
    </svg>
  );
}
