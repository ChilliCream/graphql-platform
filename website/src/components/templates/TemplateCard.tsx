"use client";

import Link from "next/link";
import React, { FC, ReactElement } from "react";

import { productLabel, topologyLabel } from "@/data/templates/filters";
import type { Template, ThumbnailKind } from "@/data/templates/templates";

// Inline thumbnails per template. We deliberately do not ship image assets
// for the seed gallery: hand-authored SVGs keep the brewer-icon vocabulary
// consistent with the rest of the platform pages (FederationDeepDive on
// /enterprise, StoryHeader on /customers/[slug]) and they scale crisply.
//
// Each thumbnail is a stylized schematic — three subgraph nodes for
// federation, a chat bubble for agents, a waveform for subscriptions, etc.
// The "brewer-icon vocabulary": stroke-rendered circles, cream rectangles,
// dashed edges. Same primitives the customer-story diagrams use.

const FederationThumb: FC = () => (
  <svg viewBox="0 0 320 180" preserveAspectRatio="xMidYMid meet">
    <defs>
      <linearGradient id="cc-tp-fed-edge" x1="0" x2="1" y1="0" y2="0">
        <stop offset="0%" stopColor="rgba(245,241,234,0.08)" />
        <stop offset="50%" stopColor="rgba(245,241,234,0.42)" />
        <stop offset="100%" stopColor="rgba(245,241,234,0.08)" />
      </linearGradient>
    </defs>
    <g stroke="url(#cc-tp-fed-edge)" strokeWidth={1.4} fill="none">
      <line x1={68} y1={50} x2={184} y2={90} />
      <line x1={68} y1={90} x2={184} y2={90} />
      <line x1={68} y1={130} x2={184} y2={90} />
    </g>
    {[
      { cx: 68, cy: 50, c: "var(--cc-col-cat)" },
      { cx: 68, cy: 90, c: "var(--cc-col-bil)" },
      { cx: 68, cy: 130, c: "var(--cc-col-ord)" },
    ].map((n, i) => (
      <g key={i}>
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
        x={184}
        y={70}
        width={108}
        height={40}
        rx={10}
        fill="rgba(245,241,234,0.04)"
        stroke="var(--cc-ink)"
        strokeWidth={1.6}
      />
      <text
        x={238}
        y={94}
        fill="var(--cc-ink)"
        fontFamily="var(--cc-font-sans), sans-serif"
        fontSize="12"
        fontWeight={500}
        textAnchor="middle"
      >
        Fusion
      </text>
    </g>
  </svg>
);

const SoloThumb: FC = () => (
  <svg viewBox="0 0 320 180" preserveAspectRatio="xMidYMid meet">
    <g>
      <rect
        x={90}
        y={50}
        width={140}
        height={80}
        rx={14}
        fill="rgba(245,241,234,0.04)"
        stroke="var(--cc-ink)"
        strokeWidth={1.6}
      />
      <text
        x={160}
        y={86}
        fill="var(--cc-ink)"
        fontFamily="var(--cc-font-sans), sans-serif"
        fontSize="14"
        fontWeight={500}
        textAnchor="middle"
      >
        Hot Chocolate
      </text>
      <text
        x={160}
        y={108}
        fill="var(--cc-ink-dim)"
        fontFamily="var(--cc-font-mono), monospace"
        fontSize="9"
        letterSpacing="0.18em"
        textAnchor="middle"
      >
        ONE SERVICE
      </text>
    </g>
  </svg>
);

const PolyglotThumb: FC = () => (
  <svg viewBox="0 0 320 180" preserveAspectRatio="xMidYMid meet">
    <g stroke="rgba(245,241,234,0.32)" strokeWidth={1.4} fill="none">
      <line x1={62} y1={62} x2={184} y2={90} />
      <line x1={62} y1={120} x2={184} y2={90} />
    </g>
    <g>
      <rect
        x={26}
        y={42}
        width={72}
        height={36}
        rx={8}
        fill="rgba(245,241,234,0.04)"
        stroke="var(--cc-col-shi)"
        strokeWidth={1.4}
      />
      <text
        x={62}
        y={65}
        fill="var(--cc-ink)"
        fontFamily="var(--cc-font-mono), monospace"
        fontSize="10"
        letterSpacing="0.14em"
        textAnchor="middle"
      >
        C# / .NET
      </text>
    </g>
    <g>
      <rect
        x={26}
        y={102}
        width={72}
        height={36}
        rx={8}
        fill="rgba(245,241,234,0.04)"
        stroke="var(--cc-col-usr)"
        strokeWidth={1.4}
      />
      <text
        x={62}
        y={125}
        fill="var(--cc-ink)"
        fontFamily="var(--cc-font-mono), monospace"
        fontSize="10"
        letterSpacing="0.14em"
        textAnchor="middle"
      >
        TS / Node
      </text>
    </g>
    <g>
      <rect
        x={184}
        y={70}
        width={110}
        height={40}
        rx={10}
        fill="rgba(245,241,234,0.04)"
        stroke="var(--cc-ink)"
        strokeWidth={1.6}
      />
      <text
        x={239}
        y={94}
        fill="var(--cc-ink)"
        fontFamily="var(--cc-font-sans), sans-serif"
        fontSize="12"
        fontWeight={500}
        textAnchor="middle"
      >
        Fusion
      </text>
    </g>
  </svg>
);

const AgentsThumb: FC = () => (
  <svg viewBox="0 0 320 180" preserveAspectRatio="xMidYMid meet">
    <g>
      <rect
        x={26}
        y={50}
        width={92}
        height={80}
        rx={14}
        fill="rgba(245,241,234,0.04)"
        stroke="var(--cc-col-usr)"
        strokeWidth={1.4}
      />
      <text
        x={72}
        y={86}
        fill="var(--cc-ink)"
        fontFamily="var(--cc-font-mono), monospace"
        fontSize="11"
        letterSpacing="0.14em"
        textAnchor="middle"
      >
        AGENTS
      </text>
      <circle cx={56} cy={106} r={3} fill="var(--cc-col-usr)" />
      <circle cx={72} cy={106} r={3} fill="var(--cc-col-usr)" />
      <circle cx={88} cy={106} r={3} fill="var(--cc-col-usr)" />
    </g>
    <g
      stroke="var(--cc-ink-dim)"
      strokeWidth={1.4}
      strokeDasharray="3 3"
      fill="none"
    >
      <line x1={118} y1={90} x2={184} y2={90} />
    </g>
    <g>
      <rect
        x={184}
        y={50}
        width={108}
        height={80}
        rx={14}
        fill="rgba(245,241,234,0.04)"
        stroke="var(--cc-ink)"
        strokeWidth={1.6}
      />
      <text
        x={238}
        y={84}
        fill="var(--cc-ink)"
        fontFamily="var(--cc-font-sans), sans-serif"
        fontSize="12"
        fontWeight={500}
        textAnchor="middle"
      >
        MCP
      </text>
      <text
        x={238}
        y={104}
        fill="var(--cc-ink-dim)"
        fontFamily="var(--cc-font-mono), monospace"
        fontSize="9"
        letterSpacing="0.16em"
        textAnchor="middle"
      >
        SCHEMA → TOOLS
      </text>
    </g>
  </svg>
);

const SubscriptionsThumb: FC = () => (
  <svg viewBox="0 0 320 180" preserveAspectRatio="xMidYMid meet">
    <g
      stroke="var(--cc-col-shi)"
      strokeWidth={1.6}
      fill="none"
      strokeLinecap="round"
    >
      <path d="M 30 90 Q 60 60 90 90 T 150 90 T 210 90 T 270 90 T 300 90" />
    </g>
    <g
      stroke="var(--cc-col-usr)"
      strokeWidth={1.4}
      fill="none"
      strokeLinecap="round"
      opacity={0.7}
    >
      <path d="M 30 110 Q 70 80 110 110 T 190 110 T 270 110 T 300 110" />
    </g>
    {[60, 110, 160, 210, 260].map((x, i) => (
      <circle key={i} cx={x} cy={140} r={3} fill="var(--cc-col-shi)" />
    ))}
    <text
      x={160}
      y={48}
      fill="var(--cc-ink)"
      fontFamily="var(--cc-font-mono), monospace"
      fontSize="10"
      letterSpacing="0.18em"
      textAnchor="middle"
    >
      LIVE STREAM
    </text>
  </svg>
);

const ObservabilityThumb: FC = () => (
  <svg viewBox="0 0 320 180" preserveAspectRatio="xMidYMid meet">
    {[
      { y: 56, w: 240, c: "var(--cc-col-shi)" },
      { y: 76, w: 180, x: 60, c: "var(--cc-col-usr)" },
      { y: 96, w: 130, x: 90, c: "var(--cc-col-ord)" },
      { y: 116, w: 90, x: 130, c: "var(--cc-col-bil)" },
      { y: 136, w: 50, x: 160, c: "var(--cc-col-cat)" },
    ].map((b, i) => (
      <g key={i}>
        <rect
          x={b.x ?? 40}
          y={b.y}
          width={b.w}
          height={10}
          rx={3}
          fill="rgba(245,241,234,0.04)"
          stroke={b.c}
          strokeWidth={1.2}
        />
      </g>
    ))}
    <text
      x={40}
      y={42}
      fill="var(--cc-ink-dim)"
      fontFamily="var(--cc-font-mono), monospace"
      fontSize="10"
      letterSpacing="0.18em"
    >
      TRACE WATERFALL
    </text>
  </svg>
);

const TenancyThumb: FC = () => (
  <svg viewBox="0 0 320 180" preserveAspectRatio="xMidYMid meet">
    {[
      { x: 40, y: 50, c: "var(--cc-col-cat)" },
      { x: 130, y: 50, c: "var(--cc-col-bil)" },
      { x: 220, y: 50, c: "var(--cc-col-shi)" },
      { x: 40, y: 110, c: "var(--cc-col-ord)" },
      { x: 130, y: 110, c: "var(--cc-col-usr)" },
      { x: 220, y: 110, c: "var(--cc-col-tel)" },
    ].map((t, i) => (
      <g key={i}>
        <rect
          x={t.x}
          y={t.y}
          width={62}
          height={42}
          rx={8}
          fill="rgba(245,241,234,0.03)"
          stroke={t.c}
          strokeWidth={1.4}
        />
        <text
          x={t.x + 31}
          y={t.y + 26}
          fill="var(--cc-ink)"
          fontFamily="var(--cc-font-mono), monospace"
          fontSize="10"
          textAnchor="middle"
        >
          T{i + 1}
        </text>
      </g>
    ))}
  </svg>
);

const BlazorThumb: FC = () => (
  <svg viewBox="0 0 320 180" preserveAspectRatio="xMidYMid meet">
    <g>
      <rect
        x={26}
        y={50}
        width={108}
        height={80}
        rx={14}
        fill="rgba(245,241,234,0.04)"
        stroke="var(--cc-col-usr)"
        strokeWidth={1.4}
      />
      <text
        x={80}
        y={84}
        fill="var(--cc-ink)"
        fontFamily="var(--cc-font-sans), sans-serif"
        fontSize="13"
        fontWeight={500}
        textAnchor="middle"
      >
        Blazor
      </text>
      <text
        x={80}
        y={104}
        fill="var(--cc-ink-dim)"
        fontFamily="var(--cc-font-mono), monospace"
        fontSize="9"
        letterSpacing="0.16em"
        textAnchor="middle"
      >
        WASM SPA
      </text>
    </g>
    <g
      stroke="var(--cc-ink-dim)"
      strokeWidth={1.4}
      fill="none"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <line x1={134} y1={90} x2={184} y2={90} />
      <polyline points="174,82 184,90 174,98" />
    </g>
    <g>
      <rect
        x={184}
        y={50}
        width={108}
        height={80}
        rx={14}
        fill="rgba(245,241,234,0.04)"
        stroke="var(--cc-ink)"
        strokeWidth={1.6}
      />
      <text
        x={238}
        y={84}
        fill="var(--cc-ink)"
        fontFamily="var(--cc-font-sans), sans-serif"
        fontSize="12"
        fontWeight={500}
        textAnchor="middle"
      >
        Hot Chocolate
      </text>
      <text
        x={238}
        y={104}
        fill="var(--cc-ink-dim)"
        fontFamily="var(--cc-font-mono), monospace"
        fontSize="9"
        letterSpacing="0.16em"
        textAnchor="middle"
      >
        TYPED CLIENT
      </text>
    </g>
  </svg>
);

const THUMBNAILS: Record<ThumbnailKind, () => ReactElement> = {
  federation: () => <FederationThumb />,
  solo: () => <SoloThumb />,
  polyglot: () => <PolyglotThumb />,
  agents: () => <AgentsThumb />,
  subscriptions: () => <SubscriptionsThumb />,
  observability: () => <ObservabilityThumb />,
  tenancy: () => <TenancyThumb />,
  blazor: () => <BlazorThumb />,
};

interface TemplateCardProps {
  readonly template: Template;
}

// Card vocabulary mirrors CaseStudyCard from /customers: stroke-rendered
// thumbnail at the top, title in display style, tagline in body, product
// chips at the bottom. Hover state subtly elevates and brightens the
// border. The agent-ready badge sits in the thumbnail corner so it doesn't
// crowd the chip row at the bottom.
export const TemplateCard: FC<TemplateCardProps> = ({ template }) => {
  const Thumb = THUMBNAILS[template.thumbnail];
  return (
    <Link href={`/templates/${template.slug}`} className="cc-tp-card">
      <div className="cc-tp-card-thumb" aria-hidden>
        <span className="cc-tp-card-thumb-tag">
          {topologyLabel(template.topology)}
        </span>
        {template.agentReady && (
          <span className="cc-tp-card-thumb-agent">Agent-ready</span>
        )}
        <Thumb />
      </div>
      <div className="cc-tp-card-body">
        <h3 className="cc-tp-card-title display">{template.title}</h3>
        <p className="cc-tp-card-tagline">{template.tagline}</p>
        <div className="cc-tp-card-chips">
          {template.products.slice(0, 4).map((p) => (
            <span key={p} className="cc-tp-product-chip">
              {productLabel(p)}
            </span>
          ))}
        </div>
      </div>
    </Link>
  );
};
