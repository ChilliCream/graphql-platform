"use client";

import React, { FC } from "react";
import styled from "styled-components";

import {
  GridButton,
  GridCard,
  GridSection,
  GridSplit,
} from "@/components/redesign-system/grid";

// Federation deep-dive (archetype E, asymmetric pair). Reuses the same
// FederationDiagram from the default variant, dropped into a no-padding
// GridCard on the right of a 60/40 split. Left cell holds the sectional
// headline, body, and bullet list; the diagram bleeds to the cell edges.

interface SubgraphNode {
  readonly key: string;
  readonly language: string;
  readonly service: string;
  readonly cx: number;
  readonly cy: number;
  readonly color: string;
}

const SUBGRAPHS: readonly SubgraphNode[] = [
  {
    key: "java",
    language: "Java",
    service: "Identity",
    cx: 110,
    cy: 90,
    color: "var(--cc-col-cat, oklch(0.74 0.18 30))",
  },
  {
    key: "go",
    language: "Go",
    service: "Inventory",
    cx: 500,
    cy: 60,
    color: "var(--cc-col-bil, oklch(0.82 0.16 90))",
  },
  {
    key: "python",
    language: "Python",
    service: "Pricing ML",
    cx: 890,
    cy: 90,
    color: "var(--cc-col-ord, oklch(0.76 0.16 150))",
  },
  {
    key: "rust",
    language: "Rust",
    service: "Risk",
    cx: 110,
    cy: 410,
    color: "var(--cc-col-shi, oklch(0.74 0.14 220))",
  },
  {
    key: "kotlin",
    language: "Kotlin",
    service: "Orders",
    cx: 500,
    cy: 440,
    color: "var(--cc-col-usr, oklch(0.72 0.18 310))",
  },
  {
    key: "dotnet",
    language: ".NET",
    service: "Billing",
    cx: 890,
    cy: 410,
    color: "var(--cc-accent, oklch(0.72 0.14 230))",
  },
];

const FederationDiagram: FC = () => {
  const cx = 500;
  const cy = 250;

  return (
    <svg
      viewBox="0 0 1000 580"
      preserveAspectRatio="xMidYMid meet"
      role="img"
      aria-label="Six polyglot subgraphs (Java, Go, Python, Rust, Kotlin, .NET) composing into one Fusion gateway."
    >
      <defs>
        <linearGradient id="cc-grid-fed-edge" x1="0" x2="1" y1="0" y2="0">
          <stop offset="0%" stopColor="rgba(245,241,234,0.10)" />
          <stop offset="50%" stopColor="rgba(245,241,234,0.40)" />
          <stop offset="100%" stopColor="rgba(245,241,234,0.10)" />
        </linearGradient>
      </defs>

      <g stroke="url(#cc-grid-fed-edge)" strokeWidth={1.4} fill="none">
        {SUBGRAPHS.map((s) => (
          <line key={`edge-${s.key}`} x1={s.cx} y1={s.cy} x2={cx} y2={cy} />
        ))}
      </g>

      {SUBGRAPHS.map((s) => (
        <g key={s.key}>
          <rect
            x={s.cx - 32}
            y={s.cy - 32}
            width={64}
            height={64}
            fill="rgba(10,13,24,0.7)"
            stroke={s.color}
            strokeWidth={1.4}
          />
          <circle cx={s.cx} cy={s.cy} r={4} fill={s.color} stroke="none" />
          <text
            x={s.cx}
            y={s.cy + 56}
            fill="var(--cc-ink, #f5f1ea)"
            fontFamily="var(--cc-font-sans), sans-serif"
            fontSize="14"
            fontWeight={500}
            textAnchor="middle"
          >
            {s.language}
          </text>
          <text
            x={s.cx}
            y={s.cy + 76}
            fill="var(--cc-ink-dim, rgba(245,241,234,0.62))"
            fontFamily="var(--cc-font-mono), monospace"
            fontSize="10"
            letterSpacing="0.16em"
            textAnchor="middle"
          >
            {s.service.toUpperCase()}
          </text>
        </g>
      ))}

      <g>
        <rect
          x={cx - 130}
          y={cy - 48}
          width={260}
          height={96}
          fill="rgba(245,241,234,0.04)"
          stroke="var(--cc-accent, var(--cc-ink, #f5f1ea))"
          strokeWidth={1.6}
        />
        <text
          x={cx}
          y={cy - 8}
          fill="var(--cc-ink, #f5f1ea)"
          fontFamily="var(--cc-font-sans), sans-serif"
          fontSize="22"
          fontWeight={500}
          textAnchor="middle"
          letterSpacing="-0.02em"
        >
          Fusion
        </text>
        <text
          x={cx}
          y={cy + 14}
          fill="var(--cc-ink-dim, rgba(245,241,234,0.62))"
          fontFamily="var(--cc-font-mono), monospace"
          fontSize="10"
          letterSpacing="0.18em"
          textAnchor="middle"
        >
          BUILD-TIME COMPOSITION
        </text>
        <text
          x={cx}
          y={cy + 32}
          fill="var(--cc-ink-dim, rgba(245,241,234,0.62))"
          fontFamily="var(--cc-font-mono), monospace"
          fontSize="10"
          letterSpacing="0.18em"
          textAnchor="middle"
        >
          GATEWAY · REGISTRY · GOVERNANCE
        </text>
      </g>

      <g
        stroke="rgba(245,241,234,0.32)"
        strokeWidth={1.4}
        strokeDasharray="4 4"
        fill="none"
      >
        <line x1={cx} y1={cy + 48} x2={cx} y2={520} />
      </g>
      <g>
        <rect
          x={cx - 130}
          y={520}
          width={260}
          height={36}
          fill="none"
          stroke="var(--cc-ink-faint, rgba(245,241,234,0.16))"
          strokeWidth={1.4}
        />
        <text
          x={cx}
          y={544}
          fill="var(--cc-ink-dim, rgba(245,241,234,0.62))"
          fontFamily="var(--cc-font-mono), monospace"
          fontSize="11"
          letterSpacing="0.18em"
          textAnchor="middle"
        >
          CLIENTS · MOBILE · AGENTS
        </text>
      </g>
    </svg>
  );
};

const Check: FC = () => (
  <svg viewBox="0 0 16 16" width="14" height="14" aria-hidden>
    <path
      d="M3 8.5 L6.5 12 L13 4.5"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
    />
  </svg>
);

const TextCell = styled.div`
  display: flex;
  flex-direction: column;
  gap: 18px;
  align-self: stretch;
`;

const Bullets = styled.ul`
  list-style: none;
  padding: 0;
  margin: 0;
  display: flex;
  flex-direction: column;
  gap: 10px;

  li {
    display: flex;
    align-items: flex-start;
    gap: 10px;
    font-size: 14px;
    color: var(--cc-ink);
    line-height: 1.5;
  }
  li svg {
    color: var(--cc-accent);
    flex-shrink: 0;
    margin-top: 4px;
  }
`;

const DiagramCell = styled.div`
  width: 100%;
  height: 100%;
  min-height: 460px;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: clamp(20px, 3vw, 36px);

  svg {
    width: 100%;
    height: 100%;
    max-height: 540px;
  }
`;

const FootRow = styled.div`
  margin-top: 32px;
  display: flex;
  justify-content: flex-end;
`;

export const EnterpriseGridFederation: FC = () => {
  return (
    <GridSection>
      <div className="cc-grid-section-head">
        <span className="cc-grid-eyebrow">Federation deep-dive</span>
        <h2 className="cc-grid-h2">
          Federate any backend in any language, on .NET or off.
        </h2>
        <p>
          Fusion composes the supergraph at build time. Subgraphs stay in the
          languages and frameworks your teams already use. There is no runtime
          gateway DSL to learn, no Rust router to keep alive, and no one team
          that owns "the federation".
        </p>
      </div>

      <GridSplit ratio="60-40">
        <GridCard>
          <TextCell>
            <span className="cc-grid-eyebrow">Architecture</span>
            <h3 className="cc-grid-h3">
              Six languages. One supergraph. Composed at build time.
            </h3>
            <p className="cc-grid-body">
              Every subgraph stays in the language and runtime its team already
              ships in. Fusion composes them at build time, ships a versioned
              supergraph, and refuses any change that breaks the contract.
            </p>
            <Bullets>
              <li>
                <Check />
                <span>
                  Build-time composition with full schema lineage and
                  breaking-change detection.
                </span>
              </li>
              <li>
                <Check />
                <span>
                  Polyglot subgraphs: Java, Go, Python, Rust, Kotlin, .NET.
                </span>
              </li>
              <li>
                <Check />
                <span>
                  Run the gateway on Nitro Cloud, on your infra, or air-gapped
                  on-prem.
                </span>
              </li>
              <li>
                <Check />
                <span>
                  No vendor lock-in: GraphQL is the only contract between
                  layers.
                </span>
              </li>
            </Bullets>
          </TextCell>
        </GridCard>
        <GridCard noPadding>
          <DiagramCell className="cc-ent-grid-federation-diagram">
            <FederationDiagram />
          </DiagramCell>
        </GridCard>
      </GridSplit>

      <FootRow>
        <GridButton variant="ghost" href="/docs/fusion">
          See the architecture
        </GridButton>
      </FootRow>
    </GridSection>
  );
};
