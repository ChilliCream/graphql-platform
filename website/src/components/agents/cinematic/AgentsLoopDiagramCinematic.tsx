"use client";

import React, { FC, useEffect, useRef, useState } from "react";

import { Band } from "@/components/redesign-system/Band";
import {
  ActLabel,
  Anchor,
  ConnectorLine,
} from "@/components/redesign-system/cinematic";
import { LOOP_STAGES } from "@/data/agents/loop-stages";

// Cinematic Section 03: the Loop. Mirrors `AgentsLoopDiagram` 1:1 in
// content, palette, and animation: same arcs geometry helpers, same
// stage strip, same pulse, same legend. The cinematic difference is
// that the forward arcs (the SVG curves between stage nodes) are
// suppressed via a class on the `.cc-ag-loop` container; explicit
// `<Anchor>` markers are placed at each stage node's screen position
// and four `<ConnectorLine>` Bezier curves thread between them. This
// mirrors the homepage's Act 1 -> Act 2 -> Act 3 -> Act 4 piping
// vocabulary.
//
// The pulse animation is intentionally preserved so the page's
// intellectual property (an active loop) keeps animating; the
// connectors are the chrome layer on top.

const W = 1180;
const H = 320;
const PAD_X = 110;
const NODE_R = 36;
const NODE_Y = 200;

type StageIconKind = "observe" | "reason" | "act" | "compose" | "ship";

const STAGE_ICONS: Record<StageIconKind, () => React.ReactElement> = {
  observe: () => (
    <g
      fill="none"
      stroke="currentColor"
      strokeWidth="1.6"
      strokeLinecap="round"
    >
      <circle cx="0" cy="0" r="6" opacity="0.85" />
      <circle cx="0" cy="0" r="11" opacity="0.5" />
      <circle cx="0" cy="0" r="16" opacity="0.25" />
      <circle cx="0" cy="0" r="2.4" fill="currentColor" stroke="none" />
    </g>
  ),
  reason: () => (
    <g
      fill="none"
      stroke="currentColor"
      strokeWidth="1.6"
      strokeLinecap="round"
    >
      <line x1="-12" y1="-8" x2="0" y2="6" strokeDasharray="2 3" />
      <line x1="12" y1="-8" x2="0" y2="6" strokeDasharray="2 3" />
      <line x1="-12" y1="-8" x2="12" y2="-8" strokeDasharray="2 3" />
      <circle cx="-12" cy="-8" r="3" fill="currentColor" stroke="none" />
      <circle cx="12" cy="-8" r="3" fill="currentColor" stroke="none" />
      <circle cx="0" cy="6" r="3" fill="currentColor" stroke="none" />
    </g>
  ),
  act: () => (
    <g
      fill="none"
      stroke="currentColor"
      strokeWidth="1.8"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <path d="M -14 0 L 10 0" />
      <path d="M 4 -7 L 14 0 L 4 7" />
      <circle cx="-14" cy="0" r="2.2" fill="currentColor" stroke="none" />
    </g>
  ),
  compose: () => (
    <g
      fill="none"
      stroke="currentColor"
      strokeWidth="1.6"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <rect x="-12" y="-10" width="20" height="6" rx="1.5" opacity="0.85" />
      <rect x="-9" y="-2" width="20" height="6" rx="1.5" opacity="0.6" />
      <rect x="-6" y="6" width="20" height="6" rx="1.5" opacity="0.4" />
    </g>
  ),
  ship: () => (
    <g
      fill="none"
      stroke="currentColor"
      strokeWidth="1.6"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <path d="M 0 -14 L 8 6 L 0 2 L -8 6 Z" />
      <line x1="-6" y1="11" x2="6" y2="11" />
    </g>
  ),
};

const STAGE_KEY_TO_ICON: Record<string, StageIconKind> = {
  observe: "observe",
  reason: "reason",
  act: "act",
  compose: "compose",
  ship: "ship",
};

const stageAnchorId = (key: string) => `loop-stage-${key}`;

export const AgentsLoopDiagramCinematic: FC = () => {
  const sectionRef = useRef<HTMLDivElement | null>(null);
  const [active, setActive] = useState(false);

  useEffect(() => {
    const node = sectionRef.current;
    if (!node) {
      return;
    }
    if (typeof IntersectionObserver === "undefined") {
      setActive(true);
      return;
    }
    const obs = new IntersectionObserver(
      (entries) => {
        for (const entry of entries) {
          if (entry.isIntersecting) {
            setActive(true);
            obs.disconnect();
            break;
          }
        }
      },
      { threshold: 0.25 }
    );
    obs.observe(node);
    return () => obs.disconnect();
  }, []);

  const n = LOOP_STAGES.length;
  const nodeXs = LOOP_STAGES.map(
    (_, i) => PAD_X + ((W - PAD_X * 2) * i) / (n - 1)
  );

  const ARC_HEIGHT = 64;
  const arcSegments: string[] = [];
  for (let i = 0; i < nodeXs.length - 1; i++) {
    const x1 = nodeXs[i] + NODE_R;
    const x2 = nodeXs[i + 1] - NODE_R;
    const cx = (x1 + x2) / 2;
    arcSegments.push(
      `M ${x1} ${NODE_Y} Q ${cx} ${NODE_Y - ARC_HEIGHT}, ${x2} ${NODE_Y}`
    );
  }
  const forwardPath = arcSegments.join(" ");

  const RETURN_DIP = 80;
  const returnPath = `M ${nodeXs[n - 1]} ${NODE_Y + NODE_R} Q ${
    (nodeXs[0] + nodeXs[n - 1]) / 2
  } ${NODE_Y + RETURN_DIP}, ${nodeXs[0]} ${NODE_Y + NODE_R}`;

  // Anchor x positions are expressed as percentages of the SVG's intrinsic
  // 1180-wide viewBox so the HTML anchors track the SVG nodes regardless of
  // how the SVG scales horizontally. The anchors land vertically on the
  // node baseline (NODE_Y / H).
  const anchorXPct = nodeXs.map((x) => (x / W) * 100);
  const anchorYPct = (NODE_Y / H) * 100;

  return (
    <Band variant="accent" ariaLabel="The Loop" id="loop">
      <ActLabel n="03" name="Agent loop" />
      <div ref={sectionRef} className="cc-ag-loop-band">
        <div className="cc-ag-loop-header">
          <div className="eyebrow">The Loop</div>
          <h2 className="display">
            Observe <span className="sep">/</span> Reason{" "}
            <span className="sep">/</span> Act <span className="sep">/</span>{" "}
            Compose <span className="sep">/</span>{" "}
            <span className="accent-amber">Ship.</span>
          </h2>
          <p>
            One feedback loop, every primitive accounted for. The agent reads
            the system, reasons in your conventions, makes the change, composes
            it across services, and ships the regenerated client.
          </p>
        </div>

        <div className="cc-ag-loop-stage-strip">
          {LOOP_STAGES.map((stage, i) => (
            <div key={stage.key} className="cc-ag-loop-strip-cell">
              <span className="step">0{i + 1}</span>
              <div className="meta">
                <h4>{stage.label}</h4>
                <p>{stage.body}</p>
                <span className="primitive">{stage.primitive}</span>
              </div>
            </div>
          ))}
        </div>

        <div
          className="cc-ag-loop cc-ag-loop-cinematic"
          data-active={active ? "true" : "false"}
        >
          <svg
            className="cc-ag-loop-svg"
            viewBox={`0 0 ${W} ${H}`}
            preserveAspectRatio="xMidYMid meet"
            aria-hidden
          >
            <defs>
              <radialGradient id="cc-ag-loop-pulse" cx="0.5" cy="0.5" r="0.5">
                <stop
                  offset="0"
                  stopColor="var(--cc-amber)"
                  stopOpacity="0.85"
                />
                <stop offset="1" stopColor="var(--cc-amber)" stopOpacity="0" />
              </radialGradient>
              <linearGradient
                id="cc-ag-loop-arc-grad"
                x1="0"
                y1="0"
                x2="1"
                y2="0"
              >
                <stop
                  offset="0"
                  stopColor="var(--cc-amber)"
                  stopOpacity="0.15"
                />
                <stop
                  offset="0.5"
                  stopColor="var(--cc-amber)"
                  stopOpacity="0.6"
                />
                <stop
                  offset="1"
                  stopColor="var(--cc-amber)"
                  stopOpacity="0.15"
                />
              </linearGradient>
            </defs>

            {/* return arc (closes the loop) - kept as the cinematic variant
                still completes the loop, just without the forward arcs that
                the connectors replace. */}
            <path
              d={returnPath}
              fill="none"
              stroke="var(--cc-amber)"
              strokeOpacity="0.18"
              strokeWidth="1.4"
              strokeDasharray="3 5"
            />

            {/* forward arcs are suppressed in the cinematic variant via the
                .cc-ag-loop-cinematic class; the dedicated marker class lets
                AgentsCinematicRoot hide just these without touching the
                return arc or the pulse. */}
            {arcSegments.map((d, i) => (
              <path
                key={i}
                className="cc-ag-loop-forward-arc"
                d={d}
                fill="none"
                stroke="url(#cc-ag-loop-arc-grad)"
                strokeWidth="1.6"
              />
            ))}

            <g className="cc-ag-loop-pulse">
              <circle r="22" fill="url(#cc-ag-loop-pulse)">
                <animateMotion
                  dur="7s"
                  repeatCount="indefinite"
                  path={forwardPath}
                />
              </circle>
              <circle r="5" fill="var(--cc-amber)">
                <animateMotion
                  dur="7s"
                  repeatCount="indefinite"
                  path={forwardPath}
                />
              </circle>
            </g>

            {LOOP_STAGES.map((stage, i) => {
              const cx = nodeXs[i];
              const iconKind = STAGE_KEY_TO_ICON[stage.key];
              const Icon = iconKind ? STAGE_ICONS[iconKind] : null;
              return (
                <g key={stage.key}>
                  <circle
                    cx={cx}
                    cy={NODE_Y}
                    r={NODE_R + 12}
                    fill="var(--cc-amber)"
                    fillOpacity="0.05"
                  />
                  <circle
                    cx={cx}
                    cy={NODE_Y}
                    r={NODE_R}
                    fill="#0c1322"
                    stroke="var(--cc-amber)"
                    strokeOpacity="0.65"
                    strokeWidth="1.4"
                  />
                  <g
                    transform={`translate(${cx} ${NODE_Y})`}
                    color="var(--cc-amber)"
                  >
                    {Icon ? <Icon /> : null}
                  </g>
                  <text
                    x={cx}
                    y={NODE_Y - NODE_R - 38}
                    textAnchor="middle"
                    fontFamily="var(--cc-font-mono), monospace"
                    fontSize="10.5"
                    letterSpacing="0.18em"
                    fill="var(--cc-amber)"
                    style={{ textTransform: "uppercase" }}
                  >
                    Stage 0{i + 1}
                  </text>
                  <text
                    x={cx}
                    y={NODE_Y - NODE_R - 16}
                    textAnchor="middle"
                    fontFamily="var(--cc-font-sans), sans-serif"
                    fontSize="18"
                    fontWeight="500"
                    fill="var(--cc-ink)"
                    letterSpacing="-0.015em"
                  >
                    {stage.label}
                  </text>
                </g>
              );
            })}
          </svg>

          {/* HTML anchor row sits absolute above the SVG. Each anchor lands
              at the centre of its corresponding SVG node, expressed as
              viewBox-relative percentages so the anchors track the nodes
              when the SVG scales horizontally. The four ConnectorLines
              thread sequentially between the five anchors with monochrome
              ink-faint Bezier curves; the per-stage colour lives on the
              SVG node icons, not on the connectors. */}
          <div className="cc-ag-loop-anchor-row" aria-hidden>
            {LOOP_STAGES.map((stage, i) => (
              <span
                key={stage.key}
                className="cc-ag-loop-anchor"
                style={{ left: `${anchorXPct[i]}%`, top: `${anchorYPct}%` }}
              >
                <Anchor id={stageAnchorId(stage.key)} />
              </span>
            ))}
          </div>

          {LOOP_STAGES.slice(0, -1).map((stage, i) => (
            <ConnectorLine
              key={`${stage.key}-${LOOP_STAGES[i + 1].key}`}
              from={stageAnchorId(stage.key)}
              to={stageAnchorId(LOOP_STAGES[i + 1].key)}
              curve="bezier"
              tone="ink-faint"
              weight="hairline"
            />
          ))}
        </div>

        <div className="cc-ag-loop-legend">
          <span className="legend-pair">
            <span className="legend-dot" /> Demo A demonstrates{" "}
            <span className="legend-stages">Observe + Reason</span>
          </span>
          <span className="legend-sep" aria-hidden>
            ·
          </span>
          <span className="legend-pair">
            <span className="legend-dot" /> Demo B demonstrates{" "}
            <span className="legend-stages">Act + Compose + Ship</span>
          </span>
        </div>
      </div>
    </Band>
  );
};
