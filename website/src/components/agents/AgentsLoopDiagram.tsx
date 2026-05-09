"use client";

import React, { FC } from "react";

import { LOOP_STAGES } from "@/data/agents/loop-stages";

// Section 03: the Loop diagram. Five circular nodes connected by an arrow
// path with a single pulsing amber dot traveling along it. Below the SVG,
// the same five stages are repeated as text cards (stage label + primitive
// + body) so the diagram and the descriptions stay in lockstep without
// fighting for vertical real estate inside the SVG.
//
// Geometry: nodes are evenly spaced across a 1080x180 viewBox. The arrow
// path goes node-to-node with a small offset on each segment so arrowheads
// land just before the next node, not on top of it. The traveling dot uses
// `<animateMotion>` along the same path, repeating indefinitely.

const W = 1080;
const H = 180;
const PAD_X = 80;
const NODE_R = 26;
const NODE_Y = 80;

export const AgentsLoopDiagram: FC = () => {
  const n = LOOP_STAGES.length;
  const nodeXs = LOOP_STAGES.map(
    (_, i) => PAD_X + ((W - PAD_X * 2) * i) / (n - 1)
  );

  // Path goes node center to next node center, with a slight outward arc
  // for visual interest on each segment. Stored as a single `d` so the
  // pulsing dot can ride it via animateMotion.
  const pathSegments: string[] = [];
  for (let i = 0; i < nodeXs.length - 1; i++) {
    const x1 = nodeXs[i] + NODE_R;
    const x2 = nodeXs[i + 1] - NODE_R;
    const cx = (x1 + x2) / 2;
    pathSegments.push(
      `M ${x1} ${NODE_Y} Q ${cx} ${NODE_Y - 14}, ${x2} ${NODE_Y}`
    );
  }
  const fullPath = pathSegments.join(" ");

  return (
    <section className="cc-ag-section cc-ag-feature">
      <div className="cc-section-label">
        <span className="num">03</span> The Loop
      </div>
      <div className="cc-ag-feature-inner">
        <div className="cc-ag-feature-header">
          <div className="eyebrow">The Loop</div>
          <h2 className="display">Observe → Reason → Act → Compose → Ship.</h2>
          <p>
            One feedback loop, every primitive accounted for. The agent doesn't
            just propose a fix. It reads the system, reasons in your
            conventions, makes the change, composes it across services, and
            ships the regenerated client.
          </p>
        </div>

        <div className="cc-ag-loop">
          <svg
            className="cc-ag-loop-svg"
            viewBox={`0 0 ${W} ${H}`}
            preserveAspectRatio="xMidYMid meet"
            aria-hidden
          >
            <defs>
              <marker
                id="cc-ag-loop-arrow"
                viewBox="0 0 8 8"
                refX="6"
                refY="4"
                markerWidth="6"
                markerHeight="6"
                orient="auto"
              >
                <path
                  d="M 0 0 L 8 4 L 0 8 Z"
                  fill="var(--cc-amber)"
                  fillOpacity="0.7"
                />
              </marker>
              <radialGradient id="cc-ag-loop-glow" cx="0.5" cy="0.5" r="0.5">
                <stop
                  offset="0"
                  stopColor="var(--cc-amber)"
                  stopOpacity="0.7"
                />
                <stop offset="1" stopColor="var(--cc-amber)" stopOpacity="0" />
              </radialGradient>
            </defs>

            {/* arrowed path (one segment per stage transition) */}
            {pathSegments.map((d, i) => (
              <path
                key={i}
                d={d}
                fill="none"
                stroke="var(--cc-amber)"
                strokeOpacity="0.55"
                strokeWidth="1.4"
                markerEnd="url(#cc-ag-loop-arrow)"
              />
            ))}

            {/* pulsing amber dot traveling along the full path */}
            <circle r="14" fill="url(#cc-ag-loop-glow)">
              <animateMotion
                dur="9s"
                repeatCount="indefinite"
                path={fullPath}
              />
            </circle>
            <circle r="4" fill="var(--cc-amber)">
              <animateMotion
                dur="9s"
                repeatCount="indefinite"
                path={fullPath}
              />
            </circle>

            {/* nodes */}
            {LOOP_STAGES.map((stage, i) => {
              const cx = nodeXs[i];
              return (
                <g key={stage.key}>
                  {/* halo */}
                  <circle
                    cx={cx}
                    cy={NODE_Y}
                    r={NODE_R + 8}
                    fill="var(--cc-amber)"
                    fillOpacity="0.05"
                  />
                  {/* node ring */}
                  <circle
                    cx={cx}
                    cy={NODE_Y}
                    r={NODE_R}
                    fill="#0c1322"
                    stroke="var(--cc-amber)"
                    strokeOpacity="0.55"
                    strokeWidth="1.4"
                  />
                  {/* index */}
                  <text
                    x={cx}
                    y={NODE_Y + 5}
                    textAnchor="middle"
                    fontFamily="var(--cc-font-mono), monospace"
                    fontSize="13"
                    letterSpacing="0.06em"
                    fill="var(--cc-amber)"
                  >
                    0{i + 1}
                  </text>
                  {/* label above the node */}
                  <text
                    x={cx}
                    y={NODE_Y - NODE_R - 16}
                    textAnchor="middle"
                    fontFamily="var(--cc-font-sans), sans-serif"
                    fontSize="15"
                    fontWeight="500"
                    fill="var(--cc-ink)"
                    letterSpacing="-0.01em"
                  >
                    {stage.label}
                  </text>
                  {/* primitive below the node */}
                  <text
                    x={cx}
                    y={NODE_Y + NODE_R + 22}
                    textAnchor="middle"
                    fontFamily="var(--cc-font-mono), monospace"
                    fontSize="9.5"
                    letterSpacing="0.14em"
                    fill="var(--cc-ink-dim)"
                    style={{ textTransform: "uppercase" }}
                  >
                    {stage.primitive.split("·")[0].trim()}
                  </text>
                </g>
              );
            })}
          </svg>
        </div>

        <div className="cc-ag-loop-stages">
          {LOOP_STAGES.map((stage, i) => (
            <div key={stage.key} className="cc-ag-loop-stage">
              <span className="step">Step 0{i + 1}</span>
              <h4>{stage.label}</h4>
              <span className="primitive">{stage.primitive}</span>
              <p>{stage.body}</p>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
};
