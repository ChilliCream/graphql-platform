"use client";

import Link from "next/link";
import React, { FC, ReactElement } from "react";

import type { Story } from "@/data/customers/stories";

// Inline architecture sketches used as the detail-page hero diagram.
// Each is a stylized schematic, not a real architecture diagram. The
// vocabulary mirrors FederationDeepDive on /enterprise: stroke-rendered
// circles for upstreams, a cream rectangle for the gateway, dashed lines
// for client edges.

const FederationDiagram: FC = () => (
  <svg viewBox="0 0 720 280" preserveAspectRatio="xMidYMid meet">
    <defs>
      <linearGradient id="cc-csd-edge" x1="0" x2="1" y1="0" y2="0">
        <stop offset="0%" stopColor="rgba(245,241,234,0.08)" />
        <stop offset="50%" stopColor="rgba(245,241,234,0.42)" />
        <stop offset="100%" stopColor="rgba(245,241,234,0.08)" />
      </linearGradient>
    </defs>

    {/* Left: BEFORE — N services with chaotic connections */}
    <text
      x={130}
      y={28}
      fill="var(--cc-ink-dim)"
      fontFamily="var(--cc-font-mono), monospace"
      fontSize="10"
      letterSpacing="0.16em"
      textAnchor="middle"
    >
      BEFORE
    </text>
    <g
      stroke="var(--cc-ink-faint)"
      strokeWidth={1.2}
      strokeDasharray="3 3"
      fill="none"
    >
      <line x1={50} y1={90} x2={210} y2={150} />
      <line x1={50} y1={150} x2={210} y2={90} />
      <line x1={50} y1={210} x2={210} y2={150} />
      <line x1={210} y1={90} x2={210} y2={210} />
      <line x1={50} y1={90} x2={50} y2={210} />
      <line x1={210} y1={150} x2={50} y2={150} />
    </g>
    {[
      { cx: 50, cy: 90, c: "var(--cc-col-cat)" },
      { cx: 50, cy: 150, c: "var(--cc-col-bil)" },
      { cx: 50, cy: 210, c: "var(--cc-col-ord)" },
      { cx: 210, cy: 90, c: "var(--cc-col-shi)" },
      { cx: 210, cy: 150, c: "var(--cc-col-usr)" },
      { cx: 210, cy: 210, c: "var(--cc-col-cat)" },
    ].map((n, i) => (
      <g key={i}>
        <circle
          cx={n.cx}
          cy={n.cy}
          r={16}
          fill="none"
          stroke={n.c}
          strokeWidth={1.4}
        />
        <circle cx={n.cx} cy={n.cy} r={3} fill={n.c} />
      </g>
    ))}

    {/* Arrow pointing right */}
    <g
      stroke="var(--cc-ink-dim)"
      strokeWidth={1.6}
      fill="none"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <line x1={278} y1={150} x2={342} y2={150} />
      <polyline points="332,142 342,150 332,158" />
    </g>

    {/* Right: AFTER — gateway with subgraphs */}
    <text
      x={580}
      y={28}
      fill="var(--cc-ink)"
      fontFamily="var(--cc-font-mono), monospace"
      fontSize="10"
      letterSpacing="0.16em"
      textAnchor="middle"
    >
      AFTER
    </text>
    <g stroke="url(#cc-csd-edge)" strokeWidth={1.4} fill="none">
      <line x1={420} y1={90} x2={580} y2={150} />
      <line x1={420} y1={150} x2={580} y2={150} />
      <line x1={420} y1={210} x2={580} y2={150} />
      <line x1={700} y1={90} x2={580} y2={150} />
      <line x1={700} y1={150} x2={580} y2={150} />
      <line x1={700} y1={210} x2={580} y2={150} />
    </g>
    {[
      { cx: 420, cy: 90, c: "var(--cc-col-cat)" },
      { cx: 420, cy: 150, c: "var(--cc-col-bil)" },
      { cx: 420, cy: 210, c: "var(--cc-col-ord)" },
      { cx: 700, cy: 90, c: "var(--cc-col-shi)" },
      { cx: 700, cy: 150, c: "var(--cc-col-usr)" },
      { cx: 700, cy: 210, c: "var(--cc-col-cat)" },
    ].map((n, i) => (
      <g key={`a-${i}`}>
        <circle
          cx={n.cx}
          cy={n.cy}
          r={14}
          fill="none"
          stroke={n.c}
          strokeWidth={1.4}
        />
        <circle cx={n.cx} cy={n.cy} r={3} fill={n.c} />
      </g>
    ))}
    <g>
      <rect
        x={520}
        y={130}
        width={120}
        height={40}
        rx={10}
        fill="rgba(245,241,234,0.04)"
        stroke="var(--cc-ink)"
        strokeWidth={1.6}
      />
      <text
        x={580}
        y={155}
        fill="var(--cc-ink)"
        fontFamily="var(--cc-font-sans), sans-serif"
        fontSize="13"
        fontWeight={500}
        textAnchor="middle"
      >
        Fusion gateway
      </text>
    </g>
  </svg>
);

const MigrationDiagram: FC = () => (
  <svg viewBox="0 0 720 280" preserveAspectRatio="xMidYMid meet">
    {/* Stack of strikethrough boxes on the left */}
    <text
      x={120}
      y={28}
      fill="var(--cc-ink-dim)"
      fontFamily="var(--cc-font-mono), monospace"
      fontSize="10"
      letterSpacing="0.16em"
      textAnchor="middle"
    >
      DECOMMISSIONED
    </text>
    {[60, 110, 160, 210].map((y, i) => (
      <g key={i} opacity={0.5}>
        <rect
          x={50}
          y={y}
          width={180}
          height={28}
          rx={6}
          fill="none"
          stroke="var(--cc-ink-faint)"
          strokeWidth={1.2}
        />
        <text
          x={140}
          y={y + 18}
          fill="var(--cc-ink-dim)"
          fontFamily="var(--cc-font-mono), monospace"
          fontSize="11"
          textAnchor="middle"
        >
          {i === 0
            ? "BFF · web (DE)"
            : i === 1
            ? "BFF · web (FR)"
            : i === 2
            ? "BFF · ios"
            : "BFF · android"}
        </text>
        <line
          x1={50}
          y1={y + 14}
          x2={230}
          y2={y + 14}
          stroke="var(--cc-col-cat)"
          strokeWidth={1.2}
          opacity={0.7}
        />
      </g>
    ))}
    {/* Arrow */}
    <g
      stroke="var(--cc-ink-dim)"
      strokeWidth={1.6}
      fill="none"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <line x1={278} y1={140} x2={342} y2={140} />
      <polyline points="332,132 342,140 332,148" />
    </g>
    {/* Single supergraph card */}
    <text
      x={550}
      y={28}
      fill="var(--cc-ink)"
      fontFamily="var(--cc-font-mono), monospace"
      fontSize="10"
      letterSpacing="0.16em"
      textAnchor="middle"
    >
      SUPERGRAPH
    </text>
    <g>
      <rect
        x={420}
        y={70}
        width={260}
        height={140}
        rx={14}
        fill="rgba(245,241,234,0.04)"
        stroke="var(--cc-ink)"
        strokeWidth={1.6}
      />
      <text
        x={550}
        y={120}
        fill="var(--cc-ink)"
        fontFamily="var(--cc-font-sans), sans-serif"
        fontSize="20"
        fontWeight={500}
        textAnchor="middle"
      >
        One Fusion graph
      </text>
      <text
        x={550}
        y={148}
        fill="var(--cc-ink-dim)"
        fontFamily="var(--cc-font-mono), monospace"
        fontSize="10"
        letterSpacing="0.18em"
        textAnchor="middle"
      >
        BUILD-TIME COMPOSITION
      </text>
      <text
        x={550}
        y={180}
        fill="var(--cc-ink-dim)"
        fontFamily="var(--cc-font-mono), monospace"
        fontSize="10"
        letterSpacing="0.14em"
        textAnchor="middle"
      >
        ONE CLIENT · ONE SCHEMA
      </text>
    </g>
  </svg>
);

const AgentsDiagram: FC = () => (
  <svg viewBox="0 0 720 280" preserveAspectRatio="xMidYMid meet">
    <text
      x={140}
      y={28}
      fill="var(--cc-ink-dim)"
      fontFamily="var(--cc-font-mono), monospace"
      fontSize="10"
      letterSpacing="0.16em"
      textAnchor="middle"
    >
      CONSUMERS
    </text>
    {[
      { y: 70, label: "Mobile apps · 1M+" },
      { y: 110, label: "Partner APIs" },
      { y: 150, label: "MCP agents" },
      { y: 190, label: "Web · station boards" },
    ].map((row, i) => (
      <g key={i}>
        <rect
          x={50}
          y={row.y}
          width={200}
          height={28}
          rx={8}
          fill="none"
          stroke="var(--cc-ink-faint)"
          strokeWidth={1.2}
        />
        <text
          x={150}
          y={row.y + 18}
          fill="var(--cc-ink)"
          fontFamily="var(--cc-font-mono), monospace"
          fontSize="11"
          textAnchor="middle"
        >
          {row.label}
        </text>
      </g>
    ))}
    {[70, 110, 150, 190].map((y, i) => (
      <line
        key={`l-${i}`}
        x1={250}
        y1={y + 14}
        x2={420}
        y2={140}
        stroke="rgba(245,241,234,0.32)"
        strokeWidth={1.4}
        strokeDasharray="3 3"
      />
    ))}
    <g>
      <rect
        x={420}
        y={110}
        width={180}
        height={60}
        rx={12}
        fill="rgba(245,241,234,0.04)"
        stroke="var(--cc-ink)"
        strokeWidth={1.6}
      />
      <text
        x={510}
        y={138}
        fill="var(--cc-ink)"
        fontFamily="var(--cc-font-sans), sans-serif"
        fontSize="14"
        fontWeight={500}
        textAnchor="middle"
      >
        Hot Chocolate
      </text>
      <text
        x={510}
        y={158}
        fill="var(--cc-ink-dim)"
        fontFamily="var(--cc-font-mono), monospace"
        fontSize="10"
        letterSpacing="0.16em"
        textAnchor="middle"
      >
        NITRO HOSTED
      </text>
    </g>
    {[80, 140, 200].map((y, i) => (
      <g key={`b-${i}`}>
        <line
          x1={600}
          y1={140}
          x2={660}
          y2={y}
          stroke="var(--cc-ink-faint)"
          strokeWidth={1.4}
        />
        <circle
          cx={680}
          cy={y}
          r={14}
          fill="none"
          stroke="var(--cc-col-shi)"
          strokeWidth={1.4}
        />
      </g>
    ))}
  </svg>
);

const GovernanceDiagram: FC = () => (
  <svg viewBox="0 0 720 280" preserveAspectRatio="xMidYMid meet">
    <text
      x={120}
      y={28}
      fill="var(--cc-ink-dim)"
      fontFamily="var(--cc-font-mono), monospace"
      fontSize="10"
      letterSpacing="0.16em"
      textAnchor="middle"
    >
      PR
    </text>
    <g>
      <rect
        x={50}
        y={70}
        width={160}
        height={140}
        rx={12}
        fill="rgba(8,14,26,0.85)"
        stroke="var(--cc-ink-faint)"
        strokeWidth={1.4}
      />
      <text
        x={130}
        y={100}
        fill="var(--cc-ink)"
        fontFamily="var(--cc-font-mono), monospace"
        fontSize="11"
        textAnchor="middle"
      >
        + postalCode: String
      </text>
      <text
        x={130}
        y={130}
        fill="var(--cc-col-cat)"
        fontFamily="var(--cc-font-mono), monospace"
        fontSize="11"
        textAnchor="middle"
      >
        − zip: String
      </text>
      <text
        x={130}
        y={160}
        fill="var(--cc-ink-dim)"
        fontFamily="var(--cc-font-mono), monospace"
        fontSize="10"
        letterSpacing="0.14em"
        textAnchor="middle"
      >
        BREAKING CHANGE
      </text>
    </g>
    <g
      stroke="var(--cc-ink-dim)"
      strokeWidth={1.6}
      fill="none"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <line x1={236} y1={140} x2={300} y2={140} />
      <polyline points="290,132 300,140 290,148" />
    </g>
    <g>
      <rect
        x={310}
        y={90}
        width={170}
        height={100}
        rx={12}
        fill="rgba(245,241,234,0.04)"
        stroke="var(--cc-ink)"
        strokeWidth={1.6}
      />
      <text
        x={395}
        y={130}
        fill="var(--cc-ink)"
        fontFamily="var(--cc-font-sans), sans-serif"
        fontSize="14"
        fontWeight={500}
        textAnchor="middle"
      >
        Nitro registry
      </text>
      <text
        x={395}
        y={155}
        fill="var(--cc-ink-dim)"
        fontFamily="var(--cc-font-mono), monospace"
        fontSize="10"
        letterSpacing="0.16em"
        textAnchor="middle"
      >
        BLOCK · DIFF · AUDIT
      </text>
    </g>
    <g
      stroke="var(--cc-ink-dim)"
      strokeWidth={1.6}
      fill="none"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <line x1={490} y1={140} x2={554} y2={140} />
      <polyline points="544,132 554,140 544,148" />
    </g>
    <g>
      <rect
        x={564}
        y={100}
        width={120}
        height={28}
        rx={6}
        fill="none"
        stroke="var(--cc-col-cat)"
        strokeWidth={1.4}
      />
      <text
        x={624}
        y={119}
        fill="var(--cc-col-cat)"
        fontFamily="var(--cc-font-mono), monospace"
        fontSize="11"
        textAnchor="middle"
      >
        BLOCKED
      </text>
    </g>
    <g>
      <rect
        x={564}
        y={150}
        width={120}
        height={28}
        rx={6}
        fill="none"
        stroke="var(--cc-col-ord)"
        strokeWidth={1.4}
      />
      <text
        x={624}
        y={169}
        fill="var(--cc-col-ord)"
        fontFamily="var(--cc-font-mono), monospace"
        fontSize="11"
        textAnchor="middle"
      >
        AUDITED
      </text>
    </g>
  </svg>
);

const DIAGRAMS: Record<Story["heroDiagram"], () => ReactElement> = {
  federation: () => <FederationDiagram />,
  migration: () => <MigrationDiagram />,
  agents: () => <AgentsDiagram />,
  governance: () => <GovernanceDiagram />,
};

interface StoryHeaderProps {
  readonly story: Story;
}

// Detail-page header: eyebrow, editorial H1 (verb + number), one-sentence
// sub, and a stylized inline hero diagram. Headline gets the gradient
// accent on the closing fragment.
export const StoryHeader: FC<StoryHeaderProps> = ({ story }) => {
  const Diagram = DIAGRAMS[story.heroDiagram];
  return (
    <section className="cc-csd-section cc-csd-header">
      <div className="cc-section-label">
        <span className="num">01</span> Customer Story
      </div>
      <div className="cc-csd-header-inner">
        <Link href="/customers" className="cc-csd-back">
          ← All customers
        </Link>
        <div className="eyebrow">{story.eyebrow}</div>
        <h1 className="display">{story.headline}</h1>
        <p className="cc-csd-header-sub">{story.subhead}</p>
        <div className="cc-csd-hero-diagram" aria-hidden>
          <Diagram />
        </div>
      </div>
    </section>
  );
};
