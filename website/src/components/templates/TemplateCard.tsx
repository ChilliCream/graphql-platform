"use client";

import Link from "next/link";
import React, { CSSProperties, FC, ReactElement } from "react";

import { productLabel, topologyLabel } from "@/data/templates/filters";
import {
  THUMBNAIL_ACCENT_TOKENS,
  type Template,
  type ThumbnailKind,
} from "@/data/templates/templates";

// Inline thumbnails per template. We deliberately do not ship image assets
// for the seed gallery: hand-authored SVGs keep the brewer-icon vocabulary
// consistent with the rest of the platform pages (FederationDeepDive on
// /enterprise, StoryHeader on /customers/[slug]) and they scale crisply.
//
// Each thumbnail is a stylized schematic — three subgraph nodes for
// federation, a chat bubble for agents, a waveform for subscriptions, etc.
// The "brewer-icon vocabulary": stroke-rendered circles, cream rectangles,
// dashed edges. Same primitives the customer-story diagrams use.
//
// Color discipline: the per-template accent (THUMBNAIL_ACCENT_TOKENS) lives
// on the card root as `--cc-accent` / `--cc-accent-soft` etc., so the
// stroke and label colors below can refer to it without per-template SVG
// overrides. Federation reads enterprise-blue, polyglot reads solutions-
// plum, agents reads agents-amber, and so on.

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
      { cx: 68, cy: 50 },
      { cx: 68, cy: 90 },
      { cx: 68, cy: 130 },
    ].map((n, i) => (
      <g key={i}>
        <circle
          cx={n.cx}
          cy={n.cy}
          r={14}
          fill="none"
          stroke="var(--cc-accent)"
          strokeWidth={1.6}
        />
        <circle cx={n.cx} cy={n.cy} r={3} fill="var(--cc-accent)" />
      </g>
    ))}
    <g>
      <rect
        x={184}
        y={70}
        width={108}
        height={40}
        rx={10}
        fill="var(--cc-accent-soft)"
        stroke="var(--cc-ink)"
        strokeWidth={1.6}
      />
      <text
        x={238}
        y={94}
        fill="var(--cc-ink)"
        fontFamily="var(--cc-font-sans), sans-serif"
        fontSize="14"
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
        x={80}
        y={48}
        width={160}
        height={84}
        rx={14}
        fill="var(--cc-accent-soft)"
        stroke="var(--cc-accent)"
        strokeWidth={1.6}
      />
      <text
        x={160}
        y={86}
        fill="var(--cc-ink)"
        fontFamily="var(--cc-font-sans), sans-serif"
        fontSize="16"
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
        fontSize="11"
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
        x={20}
        y={42}
        width={84}
        height={40}
        rx={9}
        fill="var(--cc-accent-soft)"
        stroke="var(--cc-accent)"
        strokeWidth={1.6}
      />
      <text
        x={62}
        y={67}
        fill="var(--cc-ink)"
        fontFamily="var(--cc-font-mono), monospace"
        fontSize="13"
        letterSpacing="0.10em"
        textAnchor="middle"
      >
        C# / .NET
      </text>
    </g>
    <g>
      <rect
        x={20}
        y={100}
        width={84}
        height={40}
        rx={9}
        fill="var(--cc-accent-soft)"
        stroke="var(--cc-accent)"
        strokeWidth={1.6}
      />
      <text
        x={62}
        y={125}
        fill="var(--cc-ink)"
        fontFamily="var(--cc-font-mono), monospace"
        fontSize="13"
        letterSpacing="0.10em"
        textAnchor="middle"
      >
        TS / Node
      </text>
    </g>
    <g>
      <rect
        x={184}
        y={70}
        width={114}
        height={40}
        rx={10}
        fill="rgba(245,241,234,0.04)"
        stroke="var(--cc-ink)"
        strokeWidth={1.6}
      />
      <text
        x={241}
        y={94}
        fill="var(--cc-ink)"
        fontFamily="var(--cc-font-sans), sans-serif"
        fontSize="14"
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
        x={20}
        y={48}
        width={104}
        height={84}
        rx={14}
        fill="var(--cc-accent-soft)"
        stroke="var(--cc-accent)"
        strokeWidth={1.6}
      />
      <text
        x={72}
        y={88}
        fill="var(--cc-ink)"
        fontFamily="var(--cc-font-mono), monospace"
        fontSize="14"
        letterSpacing="0.14em"
        textAnchor="middle"
      >
        AGENTS
      </text>
      <circle cx={56} cy={108} r={3} fill="var(--cc-accent)" />
      <circle cx={72} cy={108} r={3} fill="var(--cc-accent)" />
      <circle cx={88} cy={108} r={3} fill="var(--cc-accent)" />
    </g>
    <g
      stroke="var(--cc-ink-dim)"
      strokeWidth={1.4}
      strokeDasharray="3 3"
      fill="none"
    >
      <line x1={124} y1={90} x2={184} y2={90} />
    </g>
    <g>
      <rect
        x={184}
        y={48}
        width={116}
        height={84}
        rx={14}
        fill="rgba(245,241,234,0.04)"
        stroke="var(--cc-ink)"
        strokeWidth={1.6}
      />
      <text
        x={242}
        y={84}
        fill="var(--cc-ink)"
        fontFamily="var(--cc-font-sans), sans-serif"
        fontSize="14"
        fontWeight={500}
        textAnchor="middle"
      >
        MCP
      </text>
      <text
        x={242}
        y={106}
        fill="var(--cc-ink-dim)"
        fontFamily="var(--cc-font-mono), monospace"
        fontSize="11"
        letterSpacing="0.14em"
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
      stroke="var(--cc-accent)"
      strokeWidth={1.8}
      fill="none"
      strokeLinecap="round"
    >
      <path d="M 30 90 Q 60 60 90 90 T 150 90 T 210 90 T 270 90 T 300 90" />
    </g>
    <g
      stroke="var(--cc-accent)"
      strokeWidth={1.4}
      fill="none"
      strokeLinecap="round"
      opacity={0.55}
    >
      <path d="M 30 110 Q 70 80 110 110 T 190 110 T 270 110 T 300 110" />
    </g>
    {[60, 110, 160, 210, 260].map((x, i) => (
      <circle key={i} cx={x} cy={140} r={3} fill="var(--cc-accent)" />
    ))}
    <text
      x={160}
      y={48}
      fill="var(--cc-ink)"
      fontFamily="var(--cc-font-mono), monospace"
      fontSize="13"
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
      { y: 56, w: 240, x: 40 },
      { y: 76, w: 180, x: 60 },
      { y: 96, w: 130, x: 90 },
      { y: 116, w: 90, x: 130 },
      { y: 136, w: 50, x: 160 },
    ].map((b, i) => (
      <g key={i}>
        <rect
          x={b.x}
          y={b.y}
          width={b.w}
          height={11}
          rx={3}
          fill="var(--cc-accent-soft)"
          stroke="var(--cc-accent)"
          strokeWidth={1.4}
        />
      </g>
    ))}
    <text
      x={40}
      y={42}
      fill="var(--cc-ink)"
      fontFamily="var(--cc-font-mono), monospace"
      fontSize="13"
      letterSpacing="0.18em"
    >
      TRACE WATERFALL
    </text>
  </svg>
);

const TenancyThumb: FC = () => (
  <svg viewBox="0 0 320 180" preserveAspectRatio="xMidYMid meet">
    {[
      { x: 40, y: 50 },
      { x: 130, y: 50 },
      { x: 220, y: 50 },
      { x: 40, y: 110 },
      { x: 130, y: 110 },
      { x: 220, y: 110 },
    ].map((t, i) => (
      <g key={i}>
        <rect
          x={t.x}
          y={t.y}
          width={62}
          height={42}
          rx={8}
          fill="var(--cc-accent-soft)"
          stroke="var(--cc-accent)"
          strokeWidth={1.4}
          opacity={0.6 + (i % 3) * 0.13}
        />
        <text
          x={t.x + 31}
          y={t.y + 27}
          fill="var(--cc-ink)"
          fontFamily="var(--cc-font-mono), monospace"
          fontSize="13"
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
        x={20}
        y={48}
        width={114}
        height={84}
        rx={14}
        fill="var(--cc-accent-soft)"
        stroke="var(--cc-accent)"
        strokeWidth={1.6}
      />
      <text
        x={77}
        y={86}
        fill="var(--cc-ink)"
        fontFamily="var(--cc-font-sans), sans-serif"
        fontSize="15"
        fontWeight={500}
        textAnchor="middle"
      >
        Blazor
      </text>
      <text
        x={77}
        y={106}
        fill="var(--cc-ink-dim)"
        fontFamily="var(--cc-font-mono), monospace"
        fontSize="11"
        letterSpacing="0.14em"
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
        y={48}
        width={116}
        height={84}
        rx={14}
        fill="rgba(245,241,234,0.04)"
        stroke="var(--cc-ink)"
        strokeWidth={1.6}
      />
      <text
        x={242}
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
        x={242}
        y={106}
        fill="var(--cc-ink-dim)"
        fontFamily="var(--cc-font-mono), monospace"
        fontSize="11"
        letterSpacing="0.14em"
        textAnchor="middle"
      >
        TYPED CLIENT
      </text>
    </g>
  </svg>
);

// Per-`ThumbnailKind` SVG renderers. Exported so the cinematic hero
// (`TemplatesCinematicHero`) can lift the featured template's thumbnail
// into the InsetWindow viz slot at exhibit scale without duplicating the
// SVG definitions.
export const TEMPLATE_THUMBNAILS: Record<ThumbnailKind, () => ReactElement> = {
  federation: () => <FederationThumb />,
  solo: () => <SoloThumb />,
  polyglot: () => <PolyglotThumb />,
  agents: () => <AgentsThumb />,
  subscriptions: () => <SubscriptionsThumb />,
  observability: () => <ObservabilityThumb />,
  tenancy: () => <TenancyThumb />,
  blazor: () => <BlazorThumb />,
};

interface AccentVarsCSS extends CSSProperties {
  "--cc-accent": string;
  "--cc-accent-soft": string;
  "--cc-accent-line": string;
  "--cc-accent-gradient": string;
  "--cc-accent-glow": string;
}

// Resolves the per-template accent CSS variables that drive the thumbnail
// stroke and label colors. Exported so cinematic surfaces (e.g.
// `TemplatesCinematicHero`) can reuse the same accent vocabulary on
// non-card chrome (InsetWindow viz slot, etc.).
export const templateAccentVars = (template: Template): AccentVarsCSS => {
  const tokens = THUMBNAIL_ACCENT_TOKENS[template.accent];
  return {
    "--cc-accent": tokens.primary,
    "--cc-accent-soft": tokens.soft,
    "--cc-accent-line": tokens.line,
    "--cc-accent-gradient": tokens.gradient,
    "--cc-accent-glow": tokens.glow,
  };
};

interface TemplateCardProps {
  readonly template: Template;
}

// Card vocabulary mirrors CaseStudyCard from /customers: stroke-rendered
// thumbnail at the top, title in display style, tagline in body, product
// chips at the bottom. Hover state subtly elevates and brightens the
// border. The agent-ready badge sits in the thumbnail corner so it doesn't
// crowd the chip row at the bottom. The thumbnail is full-bleed inside the
// card (no inner frame, no padding) and tinted by the per-template accent.
export const TemplateCard: FC<TemplateCardProps> = ({ template }) => {
  const Thumb = TEMPLATE_THUMBNAILS[template.thumbnail];
  const style = templateAccentVars(template);
  return (
    <Link
      href={`/templates/${template.slug}`}
      className="cc-tp-card"
      style={style}
    >
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

interface FeaturedTemplateCardProps {
  readonly template: Template;
}

// Hero variant of the gallery card. Same chrome + composition as the
// gallery card so the user reads it as "an example of the cards below",
// scaled up ~1.4x via dedicated styling on .cc-tp-featured-card. Used by
// TemplatesHero only.
export const FeaturedTemplateCard: FC<FeaturedTemplateCardProps> = ({
  template,
}) => {
  const Thumb = TEMPLATE_THUMBNAILS[template.thumbnail];
  const style = templateAccentVars(template);
  return (
    <Link
      href={`/templates/${template.slug}`}
      className="cc-tp-card cc-tp-featured-card"
      style={style}
    >
      <div className="cc-tp-card-thumb" aria-hidden>
        <span className="cc-tp-card-thumb-tag">
          {topologyLabel(template.topology)}
        </span>
        <span className="cc-tp-featured-pill">Featured</span>
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
        <div className="cc-tp-featured-cta-row">
          <span className="cc-btn cc-btn-primary cc-tp-featured-cta">
            View template →
          </span>
        </div>
      </div>
    </Link>
  );
};
